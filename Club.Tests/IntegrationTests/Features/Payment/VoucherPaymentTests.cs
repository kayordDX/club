using System.Net;
using System.Net.Http.Json;
using Club.Common.Enums;
using Club.Data;
using Club.DTO;
using Club.Entities;
using Club.Features.Payment.Voucher;
using IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Features.Payment;

[Collection("AppFixture collection")]
public class VoucherPaymentTests(AppFixture app)
{
    [Theory]
    [InlineData(VoucherRedemptionKind.Entitlement, null, null, null, 100)]
    [InlineData(VoucherRedemptionKind.Discount, VoucherDiscountMode.Percentage, 50, 30, 30)]
    [InlineData(VoucherRedemptionKind.Discount, VoucherDiscountMode.FixedAmount, 25, null, 25)]
    [InlineData(VoucherRedemptionKind.Credit, null, null, null, 2)]
    public async Task Redeem_RecordsValueAndLeavesBalanceOutstanding(VoucherRedemptionKind kind, VoucherDiscountMode? mode, int? value, int? cap, int expected)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (booking, grant) = await SetupAsync(db, kind, mode, value, cap);
        var response = await app.Client.PostAsJsonAsync("/payment/voucher", Request(booking, grant), app.Context.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = (await response.Content.ReadFromJsonAsync<PaymentVoucherResponse>())!;
        result.Amount.ShouldBe((decimal)expected);
        result.AmountOutstanding.ShouldBe(200m - expected);
        result.AmountPaid.ShouldBe((decimal)expected);
        result.IsPaid.ShouldBeFalse();
        result.PaymentStatusId.ShouldBe((int)PaymentStatusEnum.Partial);
        var ledger = await db.PaymentVoucher.AsNoTracking().Include(x => x.Payment).SingleAsync(x => x.Payment.TransactionId == result.TransactionId);
        ledger.WalletVoucherGrantId.ShouldBe(grant.Id);
        ledger.Payment.PaymentTypeId.ShouldBe((int)PaymentTypeEnum.Voucher);
        var remaining = (await db.WalletVoucherGrant.AsNoTracking().SingleAsync(x => x.Id == grant.Id)).AmountRemaining;
        remaining.ShouldBe(kind == VoucherRedemptionKind.Credit ? 0m : 1m);
    }

    [Fact]
    public async Task Entitlement_CannotPaySameRoundTwice_AndFinalRoundCompletesBooking()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (booking, grant) = await SetupAsync(db);
        var request = Request(booking, grant);
        (await app.Client.PostAsJsonAsync("/payment/voucher", request)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await app.Client.PostAsJsonAsync("/payment/voucher", request)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await db.Booking.Where(x => x.Id == booking.Id).ExecuteUpdateAsync(x => x.SetProperty(b => b.ExpiresAt, DateTime.UtcNow.AddMinutes(-1)));
        request.SlotContractBookingId = booking.SlotContractBookings.Last().Id;
        (await app.Client.PostAsJsonAsync("/payment/voucher", request)).StatusCode.ShouldBe(HttpStatusCode.OK);
        var after = await db.Booking.AsNoTracking().SingleAsync(x => x.Id == booking.Id);
        after.AmountPaid.ShouldBe(200m);
        after.AmountOutstanding.ShouldBe(0m);
        after.IsPaid.ShouldBeTrue();
        var statuses = await db.PaymentBooking.Where(x => x.BookingId == booking.Id).Select(x => x.Payment.PaymentStatusId).ToListAsync();
        statuses.Count.ShouldBe(2);
        statuses.ShouldAllBe(x => x == (int)PaymentStatusEnum.Completed);
        (await app.Client.PostAsJsonAsync("/payment/voucher", request)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConcurrentRedemptions_ConsumeRoundOnlyOnce()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (booking, grant) = await SetupAsync(db);
        var responses = await Task.WhenAll(
            app.Client.PostAsJsonAsync("/payment/voucher", Request(booking, grant)),
            app.Client.PostAsJsonAsync("/payment/voucher", Request(booking, grant))
        );
        responses.Count(x => x.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        responses.Count(x => x.StatusCode == HttpStatusCode.BadRequest).ShouldBe(1);
        (await db.Booking.AsNoTracking().SingleAsync(x => x.Id == booking.Id)).AmountPaid.ShouldBe(100m);
        (await db.WalletVoucherGrant.AsNoTracking().SingleAsync(x => x.Id == grant.Id)).AmountRemaining.ShouldBe(1m);
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("inactive")]
    [InlineData("facility")]
    [InlineData("owner")]
    [InlineData("future")]
    [InlineData("exhausted")]
    [InlineData("booked-before-validity")]
    [InlineData("booked-after-expiry")]
    [InlineData("wallet-owner")]
    [InlineData("currency")]
    [InlineData("booking-expired")]
    public async Task IneligibleVoucher_IsRejectedWithoutConsumingGrant(string invalid)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (booking, grant) = await SetupAsync(db);
        switch (invalid)
        {
            case "expired":
                grant.ExpiryDate = DateTime.UtcNow.AddDays(-1);
                break;
            case "inactive":
                grant.Wallet.IsActive = false;
                break;
            case "future":
                grant.GrantedAt = DateTime.UtcNow.AddDays(2);
                break;
            case "exhausted":
                grant.AmountRemaining = 0;
                break;
            case "booked-before-validity":
                booking.SlotContractBookings.First().SlotContract.Slot.StartDatetime = grant.GrantedAt.AddMinutes(-1);
                break;
            case "booked-after-expiry":
                booking.SlotContractBookings.First().SlotContract.Slot.StartDatetime = grant.ExpiryDate;
                break;
            case "wallet-owner":
                grant.Wallet = new Wallet
                {
                    Id = Guid.NewGuid(),
                    User = new User
                    {
                        Id = Guid.NewGuid(),
                        UserName = $"other-{Guid.NewGuid()}",
                        FirstName = "Other",
                        LastName = "Owner",
                    },
                };
                db.Wallet.Add(grant.Wallet);
                break;
            case "currency":
                grant.Wallet.Currency = "USD";
                break;
            case "owner":
                booking.UserId = null;
                break;
            case "facility":
                db.VoucherFacility.RemoveRange(await db.VoucherFacility.Where(x => x.VoucherId == grant.VoucherId).ToListAsync());
                break;
            case "booking-expired":
                booking.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
                break;
        }
        await db.SaveChangesAsync();
        var response = await app.Client.PostAsJsonAsync("/payment/voucher", Request(booking, grant));
        response.StatusCode.ShouldBe(invalid is "owner" or "wallet-owner" ? HttpStatusCode.NotFound : HttpStatusCode.BadRequest);
        (await db.WalletVoucherGrant.AsNoTracking().SingleAsync(x => x.Id == grant.Id)).AmountRemaining.ShouldBe(invalid == "exhausted" ? 0m : 2m);
        (await db.Booking.AsNoTracking().SingleAsync(x => x.Id == booking.Id)).AmountPaid.ShouldBe(0m);
    }

    [Fact]
    public async Task ListVouchers_ReturnsCardMetadataAndEligibility()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (booking, grant) = await SetupAsync(db);
        var results = (await app.Client.GetFromJsonAsync<List<BookingVoucherDTO>>($"/payment/booking/{booking.Id}/vouchers"))!;
        var card = results.Single(x => x.GrantId == grant.Id);
        card.IsEligible.ShouldBeTrue();
        card.AmountRemaining.ShouldBe(2m);
        card.RedemptionKind.ShouldBe(VoucherRedemptionKind.Entitlement);
        card.PaymentValue.ShouldBe(100m);
        card.Targets.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Discount_IsCappedByOutstanding_AndCannotBeReusedOnSameBooking()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (booking, grant) = await SetupAsync(db, VoucherRedemptionKind.Discount, VoucherDiscountMode.FixedAmount, 500);
        var response = await app.Client.PostAsJsonAsync("/payment/voucher", Request(booking, grant));
        var result = (await response.Content.ReadFromJsonAsync<PaymentVoucherResponse>())!;
        result.Amount.ShouldBe(200m);
        result.IsPaid.ShouldBeTrue();
        (await app.Client.PostAsJsonAsync("/payment/voucher", Request(booking, grant))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExtraEntitlement_UsesUnitPriceAndQuantity_WithoutCoveringRounds()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (booking, grant) = await SetupAsync(db);
        grant.Voucher.IsExtra = true;
        var facility = booking.SlotContractBookings.First().SlotContract.Slot.Facility!;
        var extra = new Extra
        {
            Name = "Cart",
            Code = "CART",
            FacilityId = facility.Id,
            OutletId = facility.OutletId,
            Price = 30,
            IsAvailable = true,
            IsOnline = true,
        };
        db.ExtraBooking.Add(
            new ExtraBooking
            {
                Booking = booking,
                Extra = extra,
                Amount = 2,
            }
        );
        booking.AmountOutstanding += 60;
        await db.SaveChangesAsync(app.Context.CancellationToken);
        var response = await app.Client.PostAsJsonAsync(
            "/payment/voucher",
            new PaymentVoucherRequest
            {
                BookingId = booking.Id,
                GrantId = grant.Id,
                ExtraId = extra.Id,
                Quantity = 2,
            },
            app.Context.CancellationToken
        );
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = (await response.Content.ReadFromJsonAsync<PaymentVoucherResponse>(app.Context.CancellationToken))!;
        result.Amount.ShouldBe(60m);
        result.AmountOutstanding.ShouldBe(200m);
        (await db.WalletVoucherGrant.AsNoTracking().SingleAsync(x => x.Id == grant.Id, app.Context.CancellationToken)).AmountRemaining.ShouldBe(0m);
    }

    [Fact]
    public async Task Discount_CannotBeReusedWhileBalanceRemains_AndHistoryShowsOutstanding()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (booking, grant) = await SetupAsync(db, VoucherRedemptionKind.Discount, VoucherDiscountMode.Percentage, 25);
        (await app.Client.PostAsJsonAsync("/payment/voucher", Request(booking, grant), app.Context.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await app.Client.PostAsJsonAsync("/payment/voucher", Request(booking, grant), app.Context.CancellationToken)).StatusCode.ShouldBe(
            HttpStatusCode.BadRequest
        );
        var history = (await app.Client.GetFromJsonAsync<BookingPaymentDTO>($"/payment/booking/{booking.Id}", app.Context.CancellationToken))!;
        history.AmountPaid.ShouldBe(50m);
        history.AmountOutstanding.ShouldBe(150m);
        history.AmountAvailable.ShouldBe(150m);
        history.IsPaid.ShouldBeFalse();
        history.Payments.Count.ShouldBe(1);
        history.Payments[0].PaymentStatus.ShouldBe("Partial");
        history.Payments[0].PaymentType.ShouldBe("Voucher");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Issue_RecordsAuthenticatedActor_AndRedeemsIndependentlyOfSource(bool contractSource)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (booking, template) = await SetupAsync(db);
        var facilityId = booking.SlotContractBookings.First().SlotContract.Slot.FacilityId!.Value;
        await AssignManagerAsync(db, facilityId);
        UserContract? membership = null;
        if (contractSource)
        {
            var contract = booking.SlotContractBookings.First().SlotContract.Contract;
            db.ContractFacility.Add(new ContractFacility { Contract = contract, Facility = booking.SlotContractBookings.First().SlotContract.Slot.Facility! });
            db.ContractVoucher.Add(
                new ContractVoucher
                {
                    Contract = contract,
                    Voucher = template.Voucher,
                    Amount = 2,
                }
            );
            membership = new UserContract
            {
                Contract = contract,
                User = template.Wallet.User,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(1),
                IsActive = true,
            };
            db.UserContract.Add(membership);
            await db.SaveChangesAsync(app.Context.CancellationToken);
        }
        var response = await app.Client.PostAsJsonAsync(
            $"/admin/facility/{facilityId}/voucher/issue",
            new
            {
                Recipient = template.Wallet.User.Email,
                template.VoucherId,
                SourceUserContractId = membership?.Id,
                Amount = 2,
                ValidFrom = DateTime.UtcNow.AddMinutes(-1),
                ExpiryDate = DateTime.UtcNow.AddDays(10),
                AssigningUserId = Guid.NewGuid(),
                SourceType = WalletVoucherGrantSource.Purchase,
                Reason = "Test issuance",
                Reference = "test-ref",
            },
            app.Context.CancellationToken
        );
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var grantId = await response.Content.ReadFromJsonAsync<Guid>(app.Context.CancellationToken);
        var grant = await db.WalletVoucherGrant.Include(x => x.Wallet).Include(x => x.Voucher).SingleAsync(x => x.Id == grantId, app.Context.CancellationToken);
        var audit = await db.WalletVoucherGrantAudit.AsNoTracking().SingleAsync(x => x.WalletVoucherGrantId == grantId, app.Context.CancellationToken);
        audit.AssigningUserId.ShouldBe(TestClaims.UserIdGuid);
        audit.SourceType.ShouldBe(contractSource ? WalletVoucherGrantSource.Contract : WalletVoucherGrantSource.Admin);
        audit.SourceUserContractId.ShouldBe(membership?.Id);
        audit.Action.ShouldBe(WalletVoucherGrantAction.Issued);
        audit.Timestamp.Kind.ShouldBe(DateTimeKind.Utc);
        audit.Reason.ShouldBe("Test issuance");
        audit.Reference.ShouldBe("test-ref");
        if (membership is not null)
        {
            membership.IsActive = false;
            membership.EndDate = DateTime.UtcNow.AddDays(-1);
            await db.SaveChangesAsync(app.Context.CancellationToken);
        }
        (await app.Client.PostAsJsonAsync("/payment/voucher", Request(booking, grant), app.Context.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
        if (membership is not null)
        {
            db.UserContract.Remove(membership);
            await db.SaveChangesAsync(app.Context.CancellationToken);
            (await db.WalletVoucherGrantAudit.AsNoTracking().SingleAsync(x => x.Id == audit.Id, app.Context.CancellationToken)).SourceUserContractId.ShouldBe(
                membership.Id
            );
            var request = Request(booking, grant);
            request.SlotContractBookingId = booking.SlotContractBookings.Last().Id;
            (await app.Client.PostAsJsonAsync("/payment/voucher", request, app.Context.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    [Theory]
    [InlineData("unauthorized")]
    [InlineData("contract")]
    [InlineData("facility")]
    public async Task Issue_RejectsUnauthorizedOrForgedSource_WithoutWrites(string invalid)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (booking, template) = await SetupAsync(db);
        var facilityId = booking.SlotContractBookings.First().SlotContract.Slot.FacilityId!.Value;
        if (invalid != "unauthorized")
            await AssignManagerAsync(db, facilityId);
        if (invalid == "facility")
            db.VoucherFacility.RemoveRange(await db.VoucherFacility.Where(x => x.VoucherId == template.VoucherId).ToListAsync(app.Context.CancellationToken));
        await db.SaveChangesAsync(app.Context.CancellationToken);
        var before = await db.WalletVoucherGrant.CountAsync(app.Context.CancellationToken);
        var auditsBefore = await db.WalletVoucherGrantAudit.CountAsync(app.Context.CancellationToken);
        var response = await app.Client.PostAsJsonAsync(
            $"/admin/facility/{facilityId}/voucher/issue",
            new
            {
                Recipient = template.Wallet.User.Email,
                template.VoucherId,
                SourceUserContractId = invalid == "contract" ? int.MaxValue : (int?)null,
                Amount = 2,
                ValidFrom = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(10),
            },
            app.Context.CancellationToken
        );
        response.StatusCode.ShouldBe(
            invalid == "unauthorized" ? HttpStatusCode.Forbidden
            : invalid == "facility" ? HttpStatusCode.NotFound
            : HttpStatusCode.BadRequest
        );
        (await db.WalletVoucherGrant.CountAsync(app.Context.CancellationToken)).ShouldBe(before);
        (await db.WalletVoucherGrantAudit.CountAsync(app.Context.CancellationToken)).ShouldBe(auditsBefore);
    }

    [Fact]
    public async Task AuditInsertFailure_RollsBackGrant_AndAuditsCannotBeChanged()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (_, template) = await SetupAsync(db);
        var grant = new WalletVoucherGrant
        {
            Id = Guid.NewGuid(),
            Wallet = template.Wallet,
            Voucher = template.Voucher,
            AmountGranted = 3,
            AmountRemaining = 3,
            GrantedAt = DateTime.UtcNow,
            ExpiryDate = template.ExpiryDate,
        };
        db.WalletVoucherGrant.Add(grant);
        db.WalletVoucherGrantAudit.Add(
            new WalletVoucherGrantAudit
            {
                Id = Guid.NewGuid(),
                WalletVoucherGrant = grant,
                Action = WalletVoucherGrantAction.Issued,
                Timestamp = DateTime.UtcNow,
                SourceType = WalletVoucherGrantSource.Contract,
            }
        );
        await Should.ThrowAsync<DbUpdateException>(() => db.SaveChangesAsync(app.Context.CancellationToken));
        db.ChangeTracker.Clear();
        (await db.WalletVoucherGrant.AnyAsync(x => x.Id == grant.Id, app.Context.CancellationToken)).ShouldBeFalse();
        (await db.WalletVoucherGrantAudit.AnyAsync(x => x.WalletVoucherGrantId == grant.Id, app.Context.CancellationToken)).ShouldBeFalse();
        var audit = await db.WalletVoucherGrantAudit.SingleAsync(x => x.WalletVoucherGrantId == template.Id, app.Context.CancellationToken);
        audit.Reason = "rewrite";
        await Should.ThrowAsync<InvalidOperationException>(() => db.SaveChangesAsync(app.Context.CancellationToken));
        db.ChangeTracker.Clear();
        await Should.ThrowAsync<Npgsql.PostgresException>(() =>
            db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM wallet_voucher_grant_audit WHERE id = {audit.Id}", app.Context.CancellationToken)
        );
    }

    private async Task AssignManagerAsync(AppDbContext db, int facilityId)
    {
        var role = await db.Roles.SingleAsync(x => x.NormalizedName == "MANAGER", app.Context.CancellationToken);
        db.UserRoles.Add(
            new UserRole
            {
                UserId = TestClaims.UserIdGuid,
                RoleId = role.Id,
                FacilityId = facilityId,
            }
        );
        await db.SaveChangesAsync(app.Context.CancellationToken);
    }

    private static PaymentVoucherRequest Request(Club.Entities.Booking booking, WalletVoucherGrant grant) =>
        new()
        {
            BookingId = booking.Id,
            GrantId = grant.Id,
            SlotContractBookingId = grant.Voucher.RedemptionKind == VoucherRedemptionKind.Entitlement ? booking.SlotContractBookings.First().Id : null,
        };

    private async Task<(Club.Entities.Booking, WalletVoucherGrant)> SetupAsync(
        AppDbContext db,
        VoucherRedemptionKind kind = VoucherRedemptionKind.Entitlement,
        VoucherDiscountMode? mode = null,
        int? value = null,
        int? cap = null
    )
    {
        var user = await db.Users.SingleAsync(x => x.Id == TestClaims.UserIdGuid);
        var facilityType = new FacilityType { Name = $"Golf-{Guid.NewGuid()}" };
        var outlet = new Outlet
        {
            Name = "Voucher test",
            Slug = $"voucher-{Guid.NewGuid()}",
            DisplayName = "Test",
            VatNumber = "0",
            Business = new Business { Name = "Test" },
            OutletType = new OutletType { Name = "Test" },
            IsActive = true,
        };
        var facility = new Facility
        {
            Name = "Golf",
            FacilityType = facilityType,
            Outlet = outlet,
            IsActive = true,
        };
        var contract = new Contract { Name = "Voucher contract" };
        var slot = new Club.Entities.Slot
        {
            Id = Guid.NewGuid(),
            Facility = facility,
            Resource = new Resource { Name = "Tee", Facility = facility },
            StartDatetime = DateTime.UtcNow.AddDays(1),
            EndDatetime = DateTime.UtcNow.AddDays(1).AddMinutes(10),
            MaxBookings = 4,
        };
        var slotContract = new SlotContract
        {
            Slot = slot,
            Contract = contract,
            Price = 100,
        };
        var booking = new Club.Entities.Booking
        {
            UserId = user.Id,
            BookingStatusId = (int)BookingStatusEnum.Pending,
            BookingStatusDate = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            AmountOutstanding = 200,
            SlotContractBookings = [new SlotContractBooking { SlotContract = slotContract }, new SlotContractBooking { SlotContract = slotContract }],
        };
        var wallet = await db.Wallet.SingleOrDefaultAsync(x => x.UserId == user.Id);
        if (wallet is null)
            wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                User = user,
                IsActive = true,
            };
        wallet.IsActive = true;
        wallet.Currency = "ZAR";
        var voucher = new Voucher
        {
            Name = "Test voucher",
            RedemptionKind = kind,
            DiscountMode = mode,
            DiscountValue = value,
            MaxDiscountAmount = cap,
        };
        var grant = new WalletVoucherGrant
        {
            Id = Guid.NewGuid(),
            Wallet = wallet,
            Voucher = voucher,
            AmountGranted = 2,
            AmountRemaining = 2,
            GrantedAt = DateTime.UtcNow.AddDays(-1),
            ExpiryDate = DateTime.UtcNow.AddMonths(1),
        };
        db.Booking.Add(booking);
        db.WalletVoucherGrant.Add(grant);
        db.WalletVoucherGrantAudit.Add(
            new WalletVoucherGrantAudit
            {
                Id = Guid.NewGuid(),
                WalletVoucherGrant = grant,
                Action = WalletVoucherGrantAction.Issued,
                Timestamp = grant.GrantedAt,
                SourceType = WalletVoucherGrantSource.System,
            }
        );
        db.VoucherFacility.Add(new VoucherFacility { Voucher = voucher, Facility = facility });
        await db.SaveChangesAsync();
        return (booking, grant);
    }
}

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
    [InlineData("contract")]
    [InlineData("reserved")]
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
            case "contract":
                grant.UserContract.IsActive = false;
                break;
            case "owner":
                booking.UserId = null;
                break;
            case "facility":
                db.VoucherFacility.RemoveRange(await db.VoucherFacility.Where(x => x.VoucherId == grant.VoucherId).ToListAsync());
                break;
            case "reserved":
                db.PaymentBooking.Add(
                    new PaymentBooking
                    {
                        Booking = booking,
                        Payment = new Club.Entities.Payment
                        {
                            PaymentStatus = await db.PaymentStatus.SingleAsync(x => x.Id == (int)PaymentStatusEnum.Pending),
                            PaymentStatusId = (int)PaymentStatusEnum.Pending,
                            PaymentStatusDate = DateTime.UtcNow,
                            PaymentTypeId = (int)PaymentTypeEnum.CreditCard,
                            TransactionId = Guid.NewGuid().ToString(),
                            ProviderName = "payfast",
                            Amount = 200m,
                        },
                    }
                );
                break;
        }
        await db.SaveChangesAsync();
        var response = await app.Client.PostAsJsonAsync("/payment/voucher", Request(booking, grant));
        response.StatusCode.ShouldBe(invalid == "owner" ? HttpStatusCode.NotFound : HttpStatusCode.BadRequest);
        (await db.WalletVoucherGrant.AsNoTracking().SingleAsync(x => x.Id == grant.Id)).AmountRemaining.ShouldBe(2m);
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
            UserContract = new UserContract
            {
                Contract = contract,
                User = user,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-1),
            },
            AmountGranted = 2,
            AmountRemaining = 2,
            GrantedAt = DateTime.UtcNow.AddDays(-1),
            ExpiryDate = DateTime.UtcNow.AddMonths(1),
        };
        db.Booking.Add(booking);
        db.WalletVoucherGrant.Add(grant);
        db.VoucherFacility.Add(new VoucherFacility { Voucher = voucher, Facility = facility });
        await db.SaveChangesAsync();
        return (booking, grant);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Club.Common.Enums;
using Club.Common.Payments.Provider.Payfast;
using Club.Data;
using Club.DTO;
using Club.Entities;
using Club.Features.Booking.Create;
using Club.Features.Payment.Initiate;
using Club.Services;
using IntegrationTests.Features.Booking;
using IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BookingEntity = Club.Entities.Booking;
using SlotEntity = Club.Entities.Slot;

namespace IntegrationTests.Features.Payment;

[Collection("AppFixture collection")]
public class AdminBookingPaymentTests(AppFixture app)
{
    private CancellationToken Ct => app.Context.CancellationToken;

    [Fact]
    public async Task HistoryAndCheckout_RequireFacilityManager_AndCustomerOwnership_RemainsSeparate()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await SetupAsync(db, scope.ServiceProvider);
        var owner = await CustomerAsync(db);
        var id = await CreateAsync(facilityId, owner.Id, slot, contract);
        var (_, _, otherFacilityId) = await SetupAsync(db, scope.ServiceProvider);
        var (_, _, unassignedFacilityId) = await AdminBookingTests.CreateBookingSetup(db);

        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Post })
        {
            var suffix = method == HttpMethod.Get ? "payments" : "payment/initiate";
            var path = $"/admin/facility/{facilityId}/booking/{id}/{suffix}";
            (await RequestAsync(method, path, owner.Id)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            (await RequestAsync(method, path, anonymous: true)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            (await RequestAsync(method, $"/admin/facility/{otherFacilityId}/booking/{id}/{suffix}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await RequestAsync(method, $"/admin/facility/{unassignedFacilityId}/booking/{id}/{suffix}")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        foreach (var path in new[] { $"/booking/{id}", $"/booking/{id}/path" })
        {
            (await RequestAsync(HttpMethod.Get, path)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await RequestAsync(HttpMethod.Get, path, owner.Id)).StatusCode.ShouldBe(HttpStatusCode.OK);
            (await RequestAsync(HttpMethod.Get, path, anonymous: true)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
        var adminPath = $"/admin/facility/{facilityId}/booking/{id}/path";
        (await RequestAsync(HttpMethod.Get, adminPath)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await RequestAsync(HttpMethod.Get, adminPath, owner.Id)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await RequestAsync(HttpMethod.Get, adminPath, anonymous: true)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await RequestAsync(HttpMethod.Get, $"/admin/facility/{otherFacilityId}/booking/{id}/path")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await RequestAsync(HttpMethod.Get, $"/admin/facility/{unassignedFacilityId}/booking/{id}/path")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        (await RequestAsync(HttpMethod.Get, $"/payment/booking/{id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await RequestAsync(HttpMethod.Get, $"/payment/booking/{id}", owner.Id)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await RequestAsync(HttpMethod.Get, $"/payment/booking/{id}", anonymous: true)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await RequestAsync(HttpMethod.Get, $"/payment/booking/{id}/vouchers")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var foreignCheckout = await RequestAsync(HttpMethod.Post, "/payment/initiate", body: new { bookingId = id, providerName = "payfast" });
        foreignCheckout.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await RequestAsync(HttpMethod.Post, "/payment/initiate", anonymous: true, body: new { bookingId = id, providerName = "payfast" })).StatusCode.ShouldBe(
            HttpStatusCode.Unauthorized
        );
        var voucher = await RequestAsync(HttpMethod.Post, "/payment/voucher", body: new { bookingId = id, grantId = Guid.NewGuid() });
        voucher.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await db.PaymentBooking.CountAsync(x => x.BookingId == id, Ct)).ShouldBe(0);

        var checkout = await RequestAsync(
            HttpMethod.Post,
            "/payment/initiate",
            owner.Id,
            body: new
            {
                bookingId = id,
                providerName = "payfast",
                amount = 40m,
            }
        );
        checkout.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await HistoryAsync(facilityId, id)).AmountAvailable.ShouldBe(100m);
        (await ReadAsync(db, id)).UserId.ShouldBe(owner.Id);
    }

    [Fact]
    public async Task AcceptPayment_RecordsInClubReceipt_AndRejectsOverpaymentAndCustomerAccess()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await SetupAsync(db, scope.ServiceProvider);
        var owner = await CustomerAsync(db);
        var id = await CreateAsync(facilityId, owner.Id, slot, contract);
        var path = $"/admin/facility/{facilityId}/booking/{id}/payment/accept";
        (await RequestAsync(HttpMethod.Post, path, owner.Id, body: new { amount = 40m })).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await RequestAsync(HttpMethod.Post, path, body: new { amount = 40m })).StatusCode.ShouldBe(HttpStatusCode.OK);
        var history = await HistoryAsync(facilityId, id);
        history.AmountPaid.ShouldBe(40m);
        history.AmountOutstanding.ShouldBe(60m);
        history.Payments.Single().ProviderName.ShouldBe("Admin received payment in the club");
        (await RequestAsync(HttpMethod.Post, path, body: new { amount = 61m })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await RequestAsync(HttpMethod.Post, path, body: new { amount = 60m })).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await HistoryAsync(facilityId, id)).IsPaid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(BookingStatusEnum.Confirmed, true, true)]
    [InlineData(BookingStatusEnum.Pending, false, true)]
    [InlineData(BookingStatusEnum.Pending, true, false)]
    [InlineData(BookingStatusEnum.Cancelled, false, false)]
    [InlineData(BookingStatusEnum.Expired, false, false)]
    public async Task Checkout_UsesActiveReservationRules_WithoutChangingStatus(BookingStatusEnum status, bool expiredDeadline, bool succeeds)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await SetupAsync(db, scope.ServiceProvider);
        var id = await CreateAsync(facilityId, TestClaims.UserIdGuid, slot, contract);
        await db
            .Booking.Where(x => x.Id == id)
            .ExecuteUpdateAsync(
                x =>
                    x.SetProperty(b => b.BookingStatusId, (int)status)
                        .SetProperty(b => b.ExpiresAt, expiredDeadline ? DateTime.UtcNow.AddMinutes(-1) : DateTime.UtcNow.AddMinutes(30)),
                Ct
            );
        var before = await ReadAsync(db, id);

        var response = await InitiateAsync(facilityId, id, 40m);
        response.StatusCode.ShouldBe(succeeds ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
        var after = await ReadAsync(db, id);
        after.BookingStatusId.ShouldBe((int)status);
        after.BookingStatusDate.ShouldBe(before.BookingStatusDate);
        after.ExpiresAt.ShouldBe(before.ExpiresAt);
        after.AmountPaid.ShouldBe(0m);
        after.AmountOutstanding.ShouldBe(100m);
        after.IsPaid.ShouldBeFalse();
        var history = await HistoryAsync(facilityId, id);
        history.AmountAvailable.ShouldBe(succeeds ? 60m : 100m);
        history.Payments.Count.ShouldBe(succeeds ? 1 : 0);

        var customer = await app.Client.PostAsJsonAsync(
            "/payment/initiate",
            new
            {
                bookingId = id,
                providerName = "payfast",
                amount = 20m,
            },
            Ct
        );
        customer.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExpiredPartiallyPaidPendingHold_RequiresRestorationBeforeFurtherCheckout()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await SetupAsync(db, scope.ServiceProvider);
        var id = await CreateAsync(facilityId, TestClaims.UserIdGuid, slot, contract);
        (await StatusAsync(facilityId, id, BookingStatusEnum.Pending)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var partial = await InitiatedAsync(facilityId, id, 40m);
        await SettleAsync(partial.TransactionId);
        await db.Booking.Where(x => x.Id == id).ExecuteUpdateAsync(x => x.SetProperty(b => b.ExpiresAt, DateTime.UtcNow.AddMinutes(-1)), Ct);
        (await InitiateAsync(facilityId, id)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await app.Client.PostAsJsonAsync("/payment/initiate", new { bookingId = id, providerName = "payfast" }, Ct)).StatusCode.ShouldBe(
            HttpStatusCode.BadRequest
        );
        (await StatusAsync(facilityId, id, BookingStatusEnum.Confirmed)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var before = await HistoryAsync(facilityId, id);
        before.AmountPaid.ShouldBe(40m);
        before.AmountOutstanding.ShouldBe(60m);
        var full = await InitiatedAsync(facilityId, id);
        await SettleAsync(full.TransactionId);
        (await HistoryAsync(facilityId, id)).AmountPaid.ShouldBe(100m);
    }

    [Fact]
    public async Task ConcurrentAdminAndCustomerCheckout_ReserveAtMostOutstanding_AndDefaultUsesAvailable()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await SetupAsync(db, scope.ServiceProvider);
        var id = await CreateAsync(facilityId, TestClaims.UserIdGuid, slot, contract);
        var responses = await Task.WhenAll(
            InitiateAsync(facilityId, id, 80m),
            app.Client.PostAsJsonAsync(
                "/payment/initiate",
                new
                {
                    bookingId = id,
                    providerName = "payfast",
                    amount = 80m,
                },
                Ct
            )
        );
        responses.Count(x => x.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        responses.Count(x => x.StatusCode == HttpStatusCode.BadRequest).ShouldBe(1);
        var history = await HistoryAsync(facilityId, id);
        history.AmountPaid.ShouldBe(0m);
        history.AmountOutstanding.ShouldBe(100m);
        history.AmountAvailable.ShouldBe(20m);
        history.Payments.ShouldHaveSingleItem().Amount.ShouldBe(80m);
        (await InitiateAsync(facilityId, id, 21m)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await InitiateAsync(facilityId, id)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await HistoryAsync(facilityId, id)).AmountAvailable.ShouldBe(0m);
        (await InitiateAsync(facilityId, id, 1m)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await HistoryAsync(facilityId, id)).Payments.Sum(x => x.Amount).ShouldBe(100m);
    }

    [Theory]
    [InlineData("missing", 10)]
    [InlineData("payfast", 101)]
    [InlineData("payfast", 0)]
    public async Task InvalidProviderAndAmount_DoNotCreateReservations(string providerName, int amount)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await SetupAsync(db, scope.ServiceProvider);
        var id = await CreateAsync(facilityId, TestClaims.UserIdGuid, slot, contract);
        var response = await app.Client.PostAsJsonAsync(InitiatePath(facilityId, id), new { providerName, amount }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await HistoryAsync(facilityId, id)).Payments.ShouldBeEmpty();
    }

    [Fact]
    public async Task VerifiedReturn_UsesSignedTransactionForSettlementAndNavigation_NotMerchantQuery()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await SetupAsync(db, scope.ServiceProvider);
        var id = await CreateAsync(facilityId, TestClaims.UserIdGuid, slot, contract);
        var extracted = await InitiatedAsync(facilityId, id, 40m);
        var signed = await InitiatedAsync(facilityId, id, 40m);
        // These URL-safe values are exactly the ordered fields PayFast signs.
        var signedFields = $"m_payment_id={signed.TransactionId}&pf_payment_id=pf-456&payment_status=COMPLETE&amount_gross=40.00";
        var signature = Convert.ToHexStringLower(
            System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(signedFields + "&passphrase=jt7NOE43FZPn"))
        );
        var response = await RedirectClient()
            .GetAsync($"/payment/result/payfast?merchantTransactionId={extracted.TransactionId}&{signedFields}&signature={signature}", Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldContain("/payment/success?");
        response.Headers.Location.ToString().ShouldContain($"transactionId={signed.TransactionId}");
        response.Headers.Location.ToString().ShouldNotContain(extracted.TransactionId);
        var history = await HistoryAsync(facilityId, id);
        history.AmountPaid.ShouldBe(40m);
        history.AmountAvailable.ShouldBe(20m);
        history.Payments.Single(x => x.TransactionId == extracted.TransactionId).PaymentStatusId.ShouldBe((int)PaymentStatusEnum.Pending);
        history.Payments.Single(x => x.TransactionId == signed.TransactionId).PaymentStatusId.ShouldBe((int)PaymentStatusEnum.Partial);
    }

    [Fact]
    public async Task UnverifiedReturn_PreservesKnownTransactionNavigation_WithoutApplyingPayment()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await SetupAsync(db, scope.ServiceProvider);
        var id = await CreateAsync(facilityId, TestClaims.UserIdGuid, slot, contract);
        var initiated = await InitiatedAsync(facilityId, id, 40m);
        var client = RedirectClient();
        var response = await client.GetAsync(
            $"/payment/result/payfast?merchantTransactionId={initiated.TransactionId}&m_payment_id={initiated.TransactionId}&payment_status=COMPLETE&signature=invalid",
            Ct
        );
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldContain("/payment/failure?");
        response.Headers.Location.ToString().ShouldContain($"transactionId={initiated.TransactionId}");
        var history = await HistoryAsync(facilityId, id);
        history.AmountPaid.ShouldBe(0m);
        history.AmountAvailable.ShouldBe(60m);
        history.Payments.ShouldHaveSingleItem().PaymentStatusId.ShouldBe((int)PaymentStatusEnum.Pending);
    }

    [Fact]
    public async Task FailedAttempt_ReleasesReservation_AndLateCaptureCannotInvalidateNewCommitment()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await SetupAsync(db, scope.ServiceProvider);
        var id = await CreateAsync(facilityId, TestClaims.UserIdGuid, slot, contract);
        var failed = await InitiatedAsync(facilityId, id, 80m);
        await db.Payment.Where(x => x.TransactionId == failed.TransactionId).ExecuteUpdateAsync(x => x.SetProperty(p => p.FormFieldsJson, (string?)null), Ct);
        var form = await app.Client.GetAsync($"/payment/form/payfast/{failed.TransactionId}", Ct);
        form.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var history = await HistoryAsync(facilityId, id);
        history.AmountAvailable.ShouldBe(100m);
        history.Payments.ShouldHaveSingleItem().PaymentStatusId.ShouldBe((int)PaymentStatusEnum.Failed);
        var replacement = await InitiatedAsync(facilityId, id);
        var lateCapture = await RedirectClient().GetAsync($"/payment/result/payfast?merchantTransactionId={failed.TransactionId}", Ct);
        lateCapture.Headers.Location!.ToString().ShouldContain("/payment/failure?");
        lateCapture.Headers.Location.ToString().ShouldContain($"transactionId={failed.TransactionId}");
        history = await HistoryAsync(facilityId, id);
        history.AmountPaid.ShouldBe(0m);
        history.AmountAvailable.ShouldBe(0m);
        history.Payments.Single(x => x.TransactionId == failed.TransactionId).PaymentStatusId.ShouldBe((int)PaymentStatusEnum.Failed);
        await SettleAsync(replacement.TransactionId);
        (await HistoryAsync(facilityId, id)).AmountPaid.ShouldBe(100m);
    }

    [Fact]
    public async Task CheckoutRacingCancellation_CannotReserveAfterCancellation_AndLateSettlementDoesNotRestore()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await SetupAsync(db, scope.ServiceProvider);
        var id = await CreateAsync(facilityId, TestClaims.UserIdGuid, slot, contract);
        var results = await Task.WhenAll(InitiateAsync(facilityId, id), StatusAsync(facilityId, id, BookingStatusEnum.Cancelled));
        results[1].StatusCode.ShouldBe(HttpStatusCode.NoContent);
        results[0].StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        (await InitiateAsync(facilityId, id, 1m)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var history = await HistoryAsync(facilityId, id);
        if (results[0].IsSuccessStatusCode)
        {
            var attempt = history.Payments.ShouldHaveSingleItem();
            await SettleAsync(attempt.TransactionId);
            history = await HistoryAsync(facilityId, id);
            history.AmountPaid.ShouldBe(100m);
            history.IsPaid.ShouldBeTrue();
        }
        else
            history.Payments.ShouldBeEmpty();
        var after = await ReadAsync(db, id);
        after.BookingStatusId.ShouldBe((int)BookingStatusEnum.Cancelled);
        after.FacilityId.ShouldBe(facilityId);
        (await db.SlotContractBooking.CountAsync(x => x.BookingId == id, Ct)).ShouldBe(1);
    }

    [Theory]
    [InlineData(BookingStatusEnum.Cancelled)]
    [InlineData(BookingStatusEnum.Expired)]
    [InlineData(BookingStatusEnum.Pending)]
    public async Task FullSettlementRacingInactiveStatus_NeverReactivatesReservation(BookingStatusEnum status)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await SetupAsync(db, scope.ServiceProvider);
        var id = await CreateAsync(facilityId, TestClaims.UserIdGuid, slot, contract);
        var initiated = await InitiatedAsync(facilityId, id, 100m);
        if (status == BookingStatusEnum.Pending)
        {
            await db
                .Booking.Where(x => x.Id == id)
                .ExecuteUpdateAsync(x => x.SetProperty(b => b.BookingStatusId, (int)status).SetProperty(b => b.ExpiresAt, DateTime.UtcNow.AddMinutes(-1)), Ct);
            await SettleAsync(initiated.TransactionId);
        }
        else
        {
            var responses = await Task.WhenAll(StatusAsync(facilityId, id, status), SettleAsync(initiated.TransactionId));
            responses[0].StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }
        var after = await ReadAsync(db, id);
        after.AmountPaid.ShouldBe(100m);
        after.IsPaid.ShouldBeTrue();
        after.BookingStatusId.ShouldBe((int)status);
    }

    [Fact]
    public async Task ExpiryAndLastPlaceCreation_FullCaptureWaitingForCapacityLock_DoesNotReactivateHold()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await SetupAsync(db, scope.ServiceProvider);
        slot.MaxBookings = 1;
        await db.SaveChangesAsync(Ct);
        var id = await CreateAsync(facilityId, TestClaims.UserIdGuid, slot, contract);
        (await StatusAsync(facilityId, id, BookingStatusEnum.Pending)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var attempt = await InitiatedAsync(facilityId, id);
        var deadline = DateTime.UtcNow.AddSeconds(5);
        await db.Booking.Where(x => x.Id == id).ExecuteUpdateAsync(x => x.SetProperty(b => b.ExpiresAt, deadline), Ct);

        // External lock control makes the deadline interleaving deterministic without invoking
        // the settlement implementation: both callers still perform their mutations via HTTP.
        await using var gate = await db.Database.BeginTransactionAsync(Ct);
        await db.Slot.FromSqlInterpolated($"SELECT * FROM slot WHERE id = {slot.Id} FOR UPDATE").SingleAsync(Ct);
        var gatePid = await db.Database.SqlQuery<int>($"SELECT pg_backend_pid() AS \"Value\"").SingleAsync(Ct);
        var capture = SettleAsync(attempt.TransactionId);
        var waitUntil = DateTime.UtcNow.AddSeconds(10);
        while (
            await db
                .Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM pg_stat_activity WHERE {gatePid} = ANY(pg_blocking_pids(pid))")
                .SingleAsync(Ct) == 0
        )
        {
            capture.IsCompleted.ShouldBeFalse("full capture must wait for the capacity lock");
            (DateTime.UtcNow < waitUntil).ShouldBeTrue("capture did not reach the capacity lock");
            await Task.Delay(20, Ct);
        }
        var delay = deadline - DateTime.UtcNow;
        if (delay > TimeSpan.Zero)
            await Task.Delay(delay + TimeSpan.FromMilliseconds(50), Ct);
        var creation = CreateAsync(facilityId, TestClaims.UserIdGuid, slot, contract);
        await gate.CommitAsync(Ct);
        await capture;
        var replacementId = await creation;

        var history = await HistoryAsync(facilityId, id);
        history.AmountPaid.ShouldBe(100m);
        history.IsPaid.ShouldBeTrue();
        var captured = (await app.Client.GetFromJsonAsync<BookingDTO>($"/admin/facility/{facilityId}/booking/{id}", Ct))!;
        captured.BookingStatusId.ShouldBe((int)BookingStatusEnum.Expired);
        var replacement = (await app.Client.GetFromJsonAsync<BookingDTO>($"/admin/facility/{facilityId}/booking/{replacementId}", Ct))!;
        replacement.BookingStatusId.ShouldBe((int)BookingStatusEnum.Confirmed);
        replacement.AmountPaid.ShouldBe(0m);
        var schedule = (
            await app.Client.GetFromJsonAsync<List<Club.Features.Admin.Slot.GetAll.AdminSlotGetAllResponse>>(
                $"/admin/facility/{facilityId}/slot?date={Uri.EscapeDataString(slot.StartDatetime.ToString("O"))}",
                Ct
            )
        )!;
        schedule.ShouldHaveSingleItem().Booked.ShouldBe(1);
        schedule.Single().Bookings.ShouldHaveSingleItem().BookingId.ShouldBe(replacementId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RepricingRacingCheckoutOrSettlement_ProtectsCommittedAmounts(bool settle)
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await SetupAsync(db, scope.ServiceProvider);
        var id = await CreateAsync(facilityId, TestClaims.UserIdGuid, slot, contract);
        var (destination, destinationContract) = await DestinationAsync(db, slot, contract, 50m);
        var initiated = settle ? await InitiatedAsync(facilityId, id, 80m) : null;
        var results = await Task.WhenAll(
            EditAsync(facilityId, id, destination, destinationContract),
            settle ? SettleAsync(initiated!.TransactionId) : InitiateAsync(facilityId, id, 80m)
        );
        if (settle)
        {
            results[0].StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await HistoryAsync(facilityId, id)).AmountPaid.ShouldBe(80m);
        }
        else
        {
            results.Count(x => x.IsSuccessStatusCode).ShouldBe(1);
            results.Count(x => x.StatusCode == HttpStatusCode.BadRequest).ShouldBe(1);
        }
        var history = await HistoryAsync(facilityId, id);
        var after = await ReadAsync(db, id);
        after.AmountOutstanding.ShouldBeGreaterThanOrEqualTo(
            history.Payments.Where(x => x.PaymentStatusId == (int)PaymentStatusEnum.Pending).Sum(x => x.Amount)
        );
        after.BookingStatusId.ShouldBe((int)BookingStatusEnum.Confirmed);
    }

    [Fact]
    public async Task ManagerJourney_CreateConfirmedUnpaid_Edit_Move_PartialAndFullCheckout_OwnerCanAccessCopiedCustomerLink()
    {
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (slot, contract, facilityId) = await SetupAsync(db, scope.ServiceProvider);
        var owner = await CustomerAsync(db);
        var schedulePath = $"/admin/facility/{facilityId}/slot?date={Uri.EscapeDataString(slot.StartDatetime.ToString("O"))}";
        var schedule = (await app.Client.GetFromJsonAsync<List<Club.Features.Admin.Slot.GetAll.AdminSlotGetAllResponse>>(schedulePath, Ct))!;
        schedule.ShouldHaveSingleItem().Booked.ShouldBe(0);
        var selected = (
            await app.Client.GetFromJsonAsync<Club.Features.Admin.Booking.AdminBookingCustomerDTO>(
                $"/admin/facility/{facilityId}/booking/customer/search?query={Uri.EscapeDataString(owner.Email!)}",
                Ct
            )
        )!;
        selected.UserId.ShouldBe(owner.Id);
        var options = await app.Client.GetAsync($"/admin/facility/{facilityId}/booking/options?slotId={slot.Id}&userId={selected.UserId}", Ct);
        options.StatusCode.ShouldBe(HttpStatusCode.OK);
        var id = await CreateAsync(facilityId, selected.UserId, slot, contract);
        var original = await ReadAsync(db, id);
        original.BookingStatusId.ShouldBe((int)BookingStatusEnum.Confirmed);
        original.UserId.ShouldBe(owner.Id);
        original.IsPaid.ShouldBeFalse();
        (await HistoryAsync(facilityId, id)).AmountPaid.ShouldBe(0m);
        (await EditAsync(facilityId, id, slot, contract, "Updated contact")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var (destination, destinationContract) = await DestinationAsync(db, slot, contract, 150m);
        (await EditAsync(facilityId, id, destination, destinationContract, "Updated contact")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var moved = await ReadAsync(db, id);
        moved.BookingStatusId.ShouldBe((int)BookingStatusEnum.Confirmed);
        moved.ExpiresAt.ShouldBe(original.ExpiresAt);
        moved.AmountOutstanding.ShouldBe(150m);
        var player = await db.SlotContractBooking.AsNoTracking().SingleAsync(x => x.BookingId == id, Ct);
        player.Name.ShouldBe("Updated contact");
        player.SlotContractId.ShouldBe(destinationContract.Id);
        schedule = (await app.Client.GetFromJsonAsync<List<Club.Features.Admin.Slot.GetAll.AdminSlotGetAllResponse>>(schedulePath, Ct))!;
        schedule.Single(x => x.Id == slot.Id).Booked.ShouldBe(0);
        schedule.Single(x => x.Id == destination.Id).Booked.ShouldBe(1);
        schedule.Single(x => x.Id == destination.Id).Bookings.ShouldHaveSingleItem().BookingId.ShouldBe(id);

        var partial = await InitiatedAsync(facilityId, id, 40m);
        (await HistoryAsync(facilityId, id)).AmountAvailable.ShouldBe(110m);
        await SettleAsync(partial.TransactionId);
        var history = await HistoryAsync(facilityId, id);
        history.AmountPaid.ShouldBe(40m);
        history.AmountOutstanding.ShouldBe(110m);
        history.Payments.ShouldHaveSingleItem().PaymentStatusId.ShouldBe((int)PaymentStatusEnum.Partial);
        (await ReadAsync(db, id)).BookingStatusId.ShouldBe((int)BookingStatusEnum.Confirmed);
        var full = await InitiatedAsync(facilityId, id);
        await SettleAsync(full.TransactionId);
        await SettleAsync(full.TransactionId);
        history = await HistoryAsync(facilityId, id);
        history.AmountPaid.ShouldBe(150m);
        history.AmountOutstanding.ShouldBe(0m);
        history.AmountAvailable.ShouldBe(0m);
        history.IsPaid.ShouldBeTrue();
        history.Payments.Count.ShouldBe(2);
        history.Payments.ShouldAllBe(x => x.PaymentStatusId == (int)PaymentStatusEnum.Completed);
        (await InitiateAsync(facilityId, id, 1m)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        foreach (var path in new[] { $"/booking/{id}", $"/booking/{id}/path", $"/payment/booking/{id}" })
        {
            (await RequestAsync(HttpMethod.Get, path, owner.Id)).StatusCode.ShouldBe(HttpStatusCode.OK);
            (await RequestAsync(HttpMethod.Get, path)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await RequestAsync(HttpMethod.Get, path, anonymous: true)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
        var customerList = await RequestAsync(HttpMethod.Get, $"/booking/user?filters={Uri.EscapeDataString($"id == {id}")}", owner.Id);
        customerList.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var listed = JsonDocument.Parse(await customerList.Content.ReadAsStringAsync(Ct));
        listed.RootElement.GetProperty("items").EnumerateArray().ShouldHaveSingleItem().GetProperty("id").GetInt32().ShouldBe(id);
        var actions = await db.BookingAction.Where(x => x.BookingId == id).OrderBy(x => x.Id).ToListAsync(Ct);
        actions.Select(x => x.Kind).ShouldBe(new[] { BookingActionKind.Created, BookingActionKind.Edited, BookingActionKind.Moved });
        actions.ShouldAllBe(x => x.ActingAdminId == TestClaims.UserIdGuid);
    }

    private async Task<(SlotEntity Slot, SlotContract Contract, int FacilityId)> SetupAsync(AppDbContext db, IServiceProvider services)
    {
        var setup = await AdminBookingTests.CreateBookingSetup(db);
        await AdminBookingTests.AssignManagerRole(db, setup.FacilityId);
        var options = new PayfastOptions
        {
            MerchantId = "10016644",
            MerchantKey = "g9xjpawq6f6pr",
            Passphrase = "jt7NOE43FZPn",
            BaseUrl = "https://sandbox.payfast.co.za/eng/process",
            ReturnUrl = "http://localhost:5173/payment/success",
            CancelUrl = "http://localhost:5173/payment/cancelled",
            NotifyUrl = "http://localhost:5000/payment/result/payfast",
        };
        var encryption = services.GetRequiredService<EncryptionService>();
        var iv = encryption.GenerateIV();
        db.PaymentProviderConfig.Add(
            new PaymentProviderConfig
            {
                FacilityId = setup.FacilityId,
                ProviderKey = PayfastOptions.Key,
                Type = PaymentProviderType.Payfast,
                Iv = iv,
                EncryptedSettings = encryption.Encrypt(JsonSerializer.Serialize(options), iv),
                Enabled = true,
            }
        );
        await db.SaveChangesAsync(Ct);
        return setup;
    }

    private async Task<User> CustomerAsync(AppDbContext db)
    {
        var owner = new User
        {
            Id = Guid.NewGuid(),
            UserName = $"payment-{Guid.NewGuid()}",
            FirstName = "Booking",
            LastName = "Owner",
            Email = $"{Guid.NewGuid()}@example.com",
        };
        db.Users.Add(owner);
        await db.SaveChangesAsync(Ct);
        return owner;
    }

    private async Task<(SlotEntity, SlotContract)> DestinationAsync(AppDbContext db, SlotEntity source, SlotContract contract, decimal price)
    {
        var destination = new SlotEntity
        {
            Id = Guid.NewGuid(),
            FacilityId = source.FacilityId,
            StartDatetime = source.StartDatetime.AddHours(2),
            EndDatetime = source.EndDatetime?.AddHours(2),
            MaxBookings = 4,
        };
        var destinationContract = new SlotContract
        {
            Slot = destination,
            ContractId = contract.ContractId,
            Price = price,
        };
        db.SlotContract.Add(destinationContract);
        await db.SaveChangesAsync(Ct);
        return (destination, destinationContract);
    }

    private static BookingRequest Player(SlotEntity slot, SlotContract contract, string name = "Customer player") =>
        new()
        {
            SlotId = slot.Id,
            SlotContractId = contract.Id,
            Name = name,
            Email = "player@example.com",
            Cellphone = "0123456789",
        };

    private async Task<int> CreateAsync(int facilityId, Guid ownerId, SlotEntity slot, SlotContract contract)
    {
        var response = await app.Client.PostAsJsonAsync(
            $"/admin/facility/{facilityId}/booking",
            new
            {
                userId = ownerId,
                bookings = new[] { Player(slot, contract) },
                extras = Array.Empty<object>(),
            },
            Ct
        );
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
        return (await response.Content.ReadFromJsonAsync<BookingCreateResponse>(Ct))!.Id;
    }

    private Task<HttpResponseMessage> EditAsync(int facilityId, int id, SlotEntity slot, SlotContract contract, string name = "Customer player") =>
        app.Client.PutAsJsonAsync(
            $"/admin/facility/{facilityId}/booking/{id}",
            new BookingCreateRequest { Bookings = [Player(slot, contract, name)], Extras = [] },
            Ct
        );

    private Task<HttpResponseMessage> StatusAsync(int facilityId, int id, BookingStatusEnum status) =>
        app.Client.PutAsJsonAsync($"/admin/facility/{facilityId}/booking/{id}/status", new { status }, Ct);

    private static string InitiatePath(int facilityId, int id) => $"/admin/facility/{facilityId}/booking/{id}/payment/initiate";

    private Task<HttpResponseMessage> InitiateAsync(int facilityId, int id, decimal? amount = null) =>
        app.Client.PostAsJsonAsync(InitiatePath(facilityId, id), new { providerName = "payfast", amount }, Ct);

    private async Task<PaymentInitiateResponse> InitiatedAsync(int facilityId, int id, decimal? amount = null)
    {
        var response = await InitiateAsync(facilityId, id, amount);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
        return (await response.Content.ReadFromJsonAsync<PaymentInitiateResponse>(Ct))!;
    }

    private async Task<BookingPaymentDTO> HistoryAsync(int facilityId, int id) =>
        (await app.Client.GetFromJsonAsync<BookingPaymentDTO>($"/admin/facility/{facilityId}/booking/{id}/payments", Ct))!;

    private Task<BookingEntity> ReadAsync(AppDbContext db, int id) => db.Booking.AsNoTracking().SingleAsync(x => x.Id == id, Ct);

    private HttpClient RedirectClient() => app.CreateClient(new FastEndpoints.Testing.ClientOptions { AllowAutoRedirect = false, HandleCookies = false });

    private async Task<HttpResponseMessage> SettleAsync(string transactionId)
    {
        var response = await RedirectClient().GetAsync($"/payment/result/payfast?merchantTransactionId={transactionId}", Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldContain("/payment/success?");
        response.Headers.Location.ToString().ShouldContain($"transactionId={transactionId}");
        return response;
    }

    private async Task<HttpResponseMessage> RequestAsync(HttpMethod method, string path, Guid? actorId = null, bool anonymous = false, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (actorId.HasValue)
            request.Headers.Add("X-Test-User-Id", actorId.ToString());
        if (anonymous)
            request.Headers.Add("X-Test-Anonymous", "true");
        if (method == HttpMethod.Post)
            request.Content = JsonContent.Create(body ?? new { providerName = "payfast", amount = 40m });
        return await app.Client.SendAsync(request, Ct);
    }
}

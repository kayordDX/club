using System.Net;
using System.Text.Json;
using Club.Common.Enums;
using Club.Common.Payments.Provider.Payfast;
using Club.Data;
using Club.Entities;
using Club.Services;
using IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BookingEntity = Club.Entities.Booking;
using PaymentEntity = Club.Entities.Payment;

namespace IntegrationTests.Features.Payment;

[Collection("AppFixture collection")]
public class PaymentResultValidationTests(AppFixture app)
{
    [Fact]
    public async Task GetResult_WithoutTransactionId_RedirectsToFrontendFailurePage()
    {
        // Act - a bare browser hit on the return URL (no signed PayFast query params).
        // Use a client that does not auto-follow redirects so the Location header is inspectable.
        var redirectClient = app.CreateClient(new FastEndpoints.Testing.ClientOptions { AllowAutoRedirect = false, HandleCookies = false });
        var response = await redirectClient.GetAsync("/payment/result/payfast", app.Context.CancellationToken);

        // Assert - the browser must never see raw JSON; bounce to the frontend failure page.
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldStartWith("http://localhost:5173/payment/failure?error=");
    }

    [Fact]
    public async Task PostResult_WithoutPayload_ReturnsInvalidWebhookJson()
    {
        // Act - the gateway's ITN/webhook contract stays JSON
        var response = await app.Client.PostAsync("/payment/result/payfast", content: null, app.Context.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync(app.Context.CancellationToken);
        body.ShouldContain("Invalid webhook request.");
    }

    [Fact]
    public async Task GetResult_WithMerchantTransactionId_SandboxReturn_CompletesPaymentAndRedirectsToSuccess()
    {
        // Arrange - a pending payment/booking as the PayFast sandbox leaves them after the form is
        // submitted, with the return URL carrying only the merchantTransactionId the provider appends
        // (the sandbox simulator redirects back without the signed fields).
        await using var scope = app.Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // The startup seeder skips payment provider config when no facility exists yet, so seed the
        // sandbox PayFast provider config here (the same defaults PaymentProviderConfigSeeder uses).
        await CreateFacilityWithProviderConfig(db, scope.ServiceProvider.GetRequiredService<EncryptionService>());

        var transactionId = Guid.NewGuid().ToString();

        var booking = new BookingEntity
        {
            BookingStatusId = (int)BookingStatusEnum.Pending,
            BookingStatusDate = DateTime.UtcNow,
            UserId = TestClaims.UserIdGuid,
            IsPaid = false,
            AmountOutstanding = 100m,
            AmountPaid = 0m,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
        };
        db.Booking.Add(booking);

        var payment = new PaymentEntity
        {
            PaymentStatusId = (int)PaymentStatusEnum.Pending,
            PaymentStatus = await db.PaymentStatus.SingleAsync(s => s.Id == (int)PaymentStatusEnum.Pending, app.Context.CancellationToken),
            PaymentStatusDate = DateTime.UtcNow,
            PaymentType = await db.PaymentType.FirstAsync(app.Context.CancellationToken),
            TransactionId = transactionId,
            ProviderName = "payfast",
            Amount = 100m,
        };
        db.Payment.Add(payment);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        db.PaymentBooking.Add(new PaymentBooking { PaymentId = payment.Id, BookingId = booking.Id });
        await db.SaveChangesAsync(app.Context.CancellationToken);

        var redirectClient = app.CreateClient(new FastEndpoints.Testing.ClientOptions { AllowAutoRedirect = false, HandleCookies = false });

        // Act - the shopper's browser return (sandbox simulator redirect, no signature)
        var response = await redirectClient.GetAsync($"/payment/result/payfast?merchantTransactionId={transactionId}", app.Context.CancellationToken);

        // Assert - the return is accepted and the booking is marked paid in the sandbox. AsNoTracking
        // reads the fresh row because the request handler wrote via its own DbContext instance.
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldStartWith("http://localhost:5173/payment/success");

        var paymentAfter = await db.Payment.AsNoTracking().SingleAsync(p => p.TransactionId == transactionId, app.Context.CancellationToken);
        paymentAfter.PaymentStatusId.ShouldBe((int)PaymentStatusEnum.Completed);

        var bookingAfter = await db.Booking.AsNoTracking().SingleAsync(b => b.Id == booking.Id, app.Context.CancellationToken);
        bookingAfter.IsPaid.ShouldBeTrue();
        bookingAfter.AmountPaid.ShouldBe(100m);
        bookingAfter.AmountOutstanding.ShouldBe(0m);
        bookingAfter.BookingStatusId.ShouldBe((int)BookingStatusEnum.Confirmed);
    }

    private async Task<Facility> CreateFacilityWithProviderConfig(AppDbContext db, EncryptionService encryption)
    {
        var business = new Business { Name = $"Business_{Guid.NewGuid()}" };
        db.Business.Add(business);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        var outletType = new OutletType { Name = $"OutletType_{Guid.NewGuid()}" };
        db.OutletType.Add(outletType);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        var outlet = new Outlet
        {
            Name = $"Outlet_{Guid.NewGuid()}",
            Slug = $"outlet-{Guid.NewGuid()}",
            Business = business,
            BusinessId = business.Id,
            VatNumber = "00000000",
            DisplayName = "Test Outlet",
            OutletType = outletType,
            OutletTypeId = outletType.Id,
            IsActive = true,
        };
        db.Outlet.Add(outlet);
        await db.SaveChangesAsync(app.Context.CancellationToken);

        await db.Database.ExecuteSqlRawAsync("INSERT INTO facility_type (name) VALUES ({0}) ON CONFLICT DO NOTHING", $"FacilityType_{Guid.NewGuid()}");

        var facilityType = await db.Database.SqlQueryRaw<FacilityType>("SELECT id, name FROM facility_type ORDER BY id DESC LIMIT 1").FirstOrDefaultAsync();

        var facility = new Facility
        {
            Name = "Payment Facility",
            Outlet = outlet,
            OutletId = outlet.Id,
            FacilityTypeId = facilityType?.Id ?? 1,
            IsActive = true,
        };
        db.Facility.Add(facility);
        await db.SaveChangesAsync(app.Context.CancellationToken);

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
        var json = JsonSerializer.Serialize(options);
        var iv = encryption.GenerateIV();

        db.PaymentProviderConfig.Add(
            new PaymentProviderConfig
            {
                FacilityId = facility.Id,
                ProviderKey = PayfastOptions.Key,
                Type = PaymentProviderType.Payfast,
                Iv = iv,
                EncryptedSettings = encryption.Encrypt(json, iv),
                Enabled = true,
            }
        );
        await db.SaveChangesAsync(app.Context.CancellationToken);

        return facility;
    }
}

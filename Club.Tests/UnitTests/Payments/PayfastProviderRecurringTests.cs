using System.Globalization;
using Club.Common.Enums;
using Club.Common.Payments;
using Microsoft.AspNetCore.Http;

namespace UnitTests.Payments;

public class PayfastProviderRecurringTests
{
    private static HttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("localhost", 5000);
        return httpContext;
    }

    private static PaymentRequest CreateRequest(PaymentRecurring? recurring)
    {
        return new PaymentRequest
        {
            Amount = 150m,
            Currency = "ZAR",
            TransactionId = "txn-recurring-1",
            Description = "Booking #1",
            Recurring = recurring,
        };
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithRecurring_AppendsSubscriptionFields()
    {
        // Arrange
        var provider = PayfastTestHelpers.CreateProvider(CreateHttpContext());
        var request = CreateRequest(
            new PaymentRecurring
            {
                Frequency = PaymentFrequencyEnum.Monthly,
                Cycles = 12,
                RecurringAmount = 99.50m,
                FirstBillingDate = new DateOnly(2026, 7, 1),
            }
        );

        // Act
        var response = await provider.ProcessPaymentAsync(request, CancellationToken.None);

        // Assert - the subscription fields are added exactly as Payfast expects for recurring billing.
        Assert.True(response.Success);
        Assert.Equal("1", response.FormFields!["subscription_type"]);
        Assert.Equal("2026-07-01", response.FormFields["billing_date"]);
        Assert.Equal("99.50", response.FormFields["recurring_amount"]);
        Assert.Equal("3", response.FormFields["frequency"]);
        Assert.Equal("12", response.FormFields["cycles"]);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithRecurring_SignatureCoversSubscriptionFields()
    {
        // Arrange
        var options = PayfastTestHelpers.SandboxOptions();
        var provider = PayfastTestHelpers.CreateProvider(CreateHttpContext(), options);
        var request = CreateRequest(
            new PaymentRecurring
            {
                Frequency = PaymentFrequencyEnum.Annually,
                Cycles = 0,
                RecurringAmount = 150m,
                FirstBillingDate = new DateOnly(2026, 1, 15),
            }
        );

        // Act
        var response = await provider.ProcessPaymentAsync(request, CancellationToken.None);

        // Assert - recomputing the signature over every submitted field except the signature itself
        // must match, proving the subscription fields are part of the signed set (Payfast rejects an
        // unsigned recurring field).
        var formFields = response.FormFields!;
        var signed = formFields.Where(kvp => kvp.Key != "signature").Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value));
        var expected = PayfastTestHelpers.ComputeSignature(signed, options.Passphrase);

        Assert.Equal(expected, formFields["signature"]);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithRecurring_DefaultsRecurringAmountAndBillingDate()
    {
        // Arrange - omit the optional recurring amount and billing date.
        var provider = PayfastTestHelpers.CreateProvider(CreateHttpContext());
        var request = CreateRequest(new PaymentRecurring { Frequency = PaymentFrequencyEnum.Quarterly, Cycles = 4 });

        // Act
        var response = await provider.ProcessPaymentAsync(request, CancellationToken.None);

        // Assert - recurring amount falls back to the request amount and billing date to today.
        Assert.Equal("150.00", response.FormFields!["recurring_amount"]);
        Assert.Equal("4", response.FormFields["frequency"]);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), response.FormFields["billing_date"]);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithoutRecurring_DoesNotAddSubscriptionFields()
    {
        // Arrange - a once-off payment (the existing behaviour) must be completely unaffected.
        var provider = PayfastTestHelpers.CreateProvider(CreateHttpContext());
        var request = CreateRequest(recurring: null);

        // Act
        var response = await provider.ProcessPaymentAsync(request, CancellationToken.None);

        // Assert - none of the recurring fields are present, guarding the working once-off flow.
        Assert.True(response.Success);
        Assert.False(response.FormFields!.ContainsKey("subscription_type"));
        Assert.False(response.FormFields.ContainsKey("billing_date"));
        Assert.False(response.FormFields.ContainsKey("recurring_amount"));
        Assert.False(response.FormFields.ContainsKey("frequency"));
        Assert.False(response.FormFields.ContainsKey("cycles"));
    }
}

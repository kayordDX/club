using System.Text.Json;
using Club.Common.Payments;
using Club.Data;
using Club.Entities;
using Club.Services;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Payment.Initiate;

public class Endpoint(AppDbContext dbContext, IPaymentFactory paymentFactory, PaymentLogger paymentLogger, ILogger<Endpoint> logger)
    : Endpoint<PaymentInitiateRequest, PaymentInitiateResponse>
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly IPaymentFactory _paymentFactory = paymentFactory;
    private readonly PaymentLogger _paymentLogger = paymentLogger;

    public override void Configure()
    {
        Post("/payment/initiate");
        Description(x => x.WithName("PaymentInitiate"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(PaymentInitiateRequest req, CancellationToken ct)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        var booking = await BookingPayments.LockAsync(_dbContext, req.BookingId, ct);

        if (booking is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (booking.BookingStatusId != (int)Common.Enums.BookingStatusEnum.Pending || (booking.AmountPaid == 0 && booking.ExpiresAt <= DateTime.UtcNow))
        {
            AddError(b => b.BookingId, "Booking is not in a pending state.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        IPaymentProvider provider;
        try
        {
            provider = _paymentFactory.GetProvider(req.ProviderName);
        }
        catch (InvalidOperationException ex)
        {
            AddError(x => x.ProviderName, ex.Message);
            await Send.ErrorsAsync(400, ct);
            return;
        }

        // Fail fast when recurring is requested from a provider that cannot set up a subscription.
        // Silently falling back to a once-off charge would debit the payer while they believe they
        // created a subscription.
        if (req.Recurring is not null && !provider.SupportsRecurring)
        {
            AddError(x => x.Recurring, $"Provider '{req.ProviderName}' does not support recurring payments.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var available = await BookingPayments.AvailableAsync(_dbContext, booking, ct);
        var amount = req.Amount ?? available;
        if (!BookingPayments.IsValidAmount(amount, available))
        {
            AddError(
                x => x.Amount,
                available <= 0
                    ? "No outstanding balance is available to pay."
                    : $"Payment amount must be greater than zero, have at most two decimal places, and cannot exceed the outstanding balance of R{available:F2}."
            );
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var pendingStatus = await _dbContext.PaymentStatus.FirstAsync(s => s.Id == (int)Common.Enums.PaymentStatusEnum.Pending, ct);
        var creditCardType = await _dbContext.PaymentType.FirstAsync(t => t.Id == (int)Common.Enums.PaymentTypeEnum.CreditCard, ct);

        var transactionId = Guid.NewGuid().ToString();

        var payment = new Entities.Payment
        {
            PaymentStatusId = pendingStatus.Id,
            PaymentStatus = pendingStatus,
            PaymentStatusDate = DateTime.UtcNow,
            Amount = amount,
            PaymentTypeId = creditCardType.Id,
            PaymentType = creditCardType,
            TransactionId = transactionId,
            ProviderName = req.ProviderName,
        };

        _dbContext.Payment.Add(payment);
        _dbContext.PaymentBooking.Add(new PaymentBooking { Payment = payment, Booking = booking });
        await _dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        await _paymentLogger.LogAsync(
            payment.Id,
            transactionId,
            req.ProviderName,
            "payment.initiated",
            "pending",
            $"Payment initiated for Booking #{booking.Id}, amount R{amount:F2}",
            new
            {
                bookingId = booking.Id,
                amount,
                recurring = req.Recurring is null
                    ? null
                    : new
                    {
                        frequency = req.Recurring.Frequency.ToString(),
                        cycles = req.Recurring.Cycles,
                        recurringAmount = req.Recurring.RecurringAmount,
                        firstBillingDate = req.Recurring.FirstBillingDate,
                    },
            },
            ct
        );

        var paymentRequest = new PaymentRequest
        {
            Amount = amount,
            Currency = "ZAR",
            TransactionId = transactionId,
            Description = $"Booking #{booking.Id}",
            Recurring = req.Recurring is null
                ? null
                : new PaymentRecurring
                {
                    Frequency = req.Recurring.Frequency,
                    Cycles = req.Recurring.Cycles,
                    RecurringAmount = req.Recurring.RecurringAmount,
                    FirstBillingDate = req.Recurring.FirstBillingDate,
                },
        };

        PaymentResponse result;
        try
        {
            result = await provider.ProcessPaymentAsync(paymentRequest, ct);
            if (
                result.Success
                && (
                    string.IsNullOrWhiteSpace(result.RedirectUrl)
                    || (
                        req.ProviderName.Equals("payfast", StringComparison.OrdinalIgnoreCase)
                        && (string.IsNullOrWhiteSpace(result.FormActionUrl) || result.FormFields is not { Count: > 0 })
                    )
                )
            )
            {
                result.Success = false;
                result.ErrorMessage = "The provider did not return valid payment checkout details. Please try again.";
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Payment initiation failed for provider {Provider}, transaction {TransactionId}.", req.ProviderName, transactionId);
            result = new PaymentResponse
            {
                TransactionId = transactionId,
                Success = false,
                ErrorMessage = "Unable to start payment with this provider. Please try again.",
            };
        }

        await using var resultTransaction = await _dbContext.Database.BeginTransactionAsync(ct);
        await BookingPayments.LockAsync(_dbContext, booking.Id, ct);
        await _dbContext.Entry(payment).ReloadAsync(ct);
        payment.RedirectUrl = result.RedirectUrl;
        payment.FormActionUrl = result.FormActionUrl;
        payment.FormFieldsJson = result.FormFields is not null ? JsonSerializer.Serialize(result.FormFields) : null;
        payment.ProviderReference = result.ProviderReference;
        if (!result.Success && !BookingPayments.IsSettled(payment.PaymentStatusId))
        {
            payment.PaymentStatusId = (int)Common.Enums.PaymentStatusEnum.Failed;
            payment.PaymentStatusDate = DateTime.UtcNow;
            payment.ErrorMessage = result.ErrorMessage;
        }
        await _dbContext.SaveChangesAsync(ct);
        await resultTransaction.CommitAsync(ct);

        await _paymentLogger.LogAsync(
            payment.Id,
            transactionId,
            req.ProviderName,
            "provider.redirect_returned",
            result.Success ? "pending" : "failed",
            result.Success ? $"Provider returned redirect: {result.RedirectUrl ?? "(form)"}" : $"Provider failed: {result.ErrorMessage}",
            new
            {
                redirectUrl = result.RedirectUrl,
                formActionUrl = result.FormActionUrl,
                errorMessage = result.ErrorMessage,
            },
            ct
        );

        if (!result.Success)
        {
            AddError(result.ErrorMessage ?? "Payment initiation failed. Please try again.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        await Send.OkAsync(
            new PaymentInitiateResponse
            {
                TransactionId = transactionId,
                RedirectUrl = result.RedirectUrl!,
                ProviderReference = result.ProviderReference,
            },
            ct
        );
    }
}

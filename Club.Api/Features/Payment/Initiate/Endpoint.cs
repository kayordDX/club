using Microsoft.EntityFrameworkCore;
using Club.Common.Payments;
using Club.Data;
using Club.Entities;
using Club.Services;

namespace Club.Features.Payment.Initiate;

public class Endpoint(
    AppDbContext dbContext,
    IPaymentFactory paymentFactory
) : Endpoint<PaymentInitiateRequest, PaymentInitiateResponse>
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly IPaymentFactory _paymentFactory = paymentFactory;

    public override void Configure()
    {
        Post("/payment/initiate");
        Description(x => x.WithName("PaymentInitiate"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(PaymentInitiateRequest req, CancellationToken ct)
    {
        var booking = await _dbContext.Booking
            .Include(b => b.BookingStatus)
            .FirstOrDefaultAsync(b => b.Id == req.BookingId, ct);

        if (booking is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (booking.BookingStatusId != (int)Common.Enums.BookingStatusEnum.Pending)
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

        var pendingStatus = await _dbContext.PaymentStatus
            .FirstAsync(s => s.Id == (int)Common.Enums.PaymentStatusEnum.Pending, ct);
        var creditCardType = await _dbContext.PaymentType
            .FirstAsync(t => t.Id == (int)Common.Enums.PaymentTypeEnum.CreditCard, ct);

        var transactionId = Guid.NewGuid().ToString();

        var payment = new Entities.Payment
        {
            PaymentStatusId = pendingStatus.Id,
            PaymentStatus = pendingStatus,
            PaymentStatusDate = DateTime.UtcNow,
            Amount = booking.AmountOutstanding,
            PaymentTypeId = creditCardType.Id,
            PaymentType = creditCardType,
            TransactionId = transactionId,
            ProviderName = req.ProviderName
        };

        _dbContext.Payment.Add(payment);
        await _dbContext.SaveChangesAsync(ct);

        _dbContext.PaymentBooking.Add(new PaymentBooking
        {
            PaymentId = payment.Id,
            Payment = payment,
            BookingId = booking.Id,
            Booking = booking
        });

        var paymentRequest = new PaymentRequest
        {
            Amount = booking.AmountOutstanding,
            Currency = "ZAR",
            TransactionId = transactionId,
            Description = $"Booking #{booking.Id}"
        };

        var result = await provider.ProcessPaymentAsync(paymentRequest, ct);

        payment.RedirectUrl = result.RedirectUrl;
        payment.ProviderReference = result.ProviderReference;
        await _dbContext.SaveChangesAsync(ct);

        if (!result.Success)
        {
            payment.PaymentStatusId = (int)Common.Enums.PaymentStatusEnum.Failed;
            payment.PaymentStatusDate = DateTime.UtcNow;
            payment.ErrorMessage = result.ErrorMessage;
            await _dbContext.SaveChangesAsync(ct);

            await Send.OkAsync(new PaymentInitiateResponse
            {
                TransactionId = transactionId,
                RedirectUrl = "",
                ProviderReference = result.ProviderReference
            }, ct);
            return;
        }

        await Send.OkAsync(new PaymentInitiateResponse
        {
            TransactionId = transactionId,
            RedirectUrl = result.RedirectUrl!,
            ProviderReference = result.ProviderReference
        }, ct);
    }
}

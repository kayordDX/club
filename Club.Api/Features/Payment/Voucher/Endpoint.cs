using Club.Common;
using Club.Common.Payments;
using Club.Data;
using Club.Services;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Payment.Voucher;

public class Endpoint(AppDbContext db, BookingVoucherService vouchers) : Endpoint<PaymentVoucherRequest, PaymentVoucherResponse>
{
    public override void Configure()
    {
        Post("/payment/voucher");
        Description(x => x.WithName("PaymentVoucher"));
    }

    public override async Task HandleAsync(PaymentVoucherRequest req, CancellationToken ct)
    {
        var userId = Helpers.GetCurrentUserId(HttpContext);
        if (!userId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var lockedBooking = await BookingPayments.LockAsync(db, req.BookingId, ct);
        if (lockedBooking is null || lockedBooking.UserId != userId)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        var booking = (await vouchers.GetBookingAsync(req.BookingId, userId.Value, ct))!;
        var grant = await db
            .WalletVoucherGrant.FromSqlInterpolated($"SELECT * FROM wallet_voucher_grant WHERE id = {req.GrantId} FOR UPDATE")
            .FirstOrDefaultAsync(ct);
        if (grant is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await db.Entry(grant).Reference(x => x.Wallet).LoadAsync(ct);
        if (grant.Wallet.UserId != userId)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await db.Entry(grant).Reference(x => x.Voucher).LoadAsync(ct);
        await db.Entry(grant).Reference(x => x.UserContract).LoadAsync(ct);
        var available = await BookingPayments.AvailableAsync(db, booking, ct);
        var description = await vouchers.DescribeAsync(booking, grant, available, ct);
        try
        {
            var payment = await vouchers.RedeemAsync(booking, grant, description, available, req.SlotContractBookingId, req.ExtraId, req.Quantity, ct);
            await transaction.CommitAsync(ct);
            await Send.OkAsync(
                new PaymentVoucherResponse
                {
                    TransactionId = payment.TransactionId,
                    Amount = payment.Amount,
                    PaymentStatusId = payment.PaymentStatusId,
                    AmountPaid = booking.AmountPaid,
                    AmountOutstanding = booking.AmountOutstanding,
                    IsPaid = booking.IsPaid,
                },
                ct
            );
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}

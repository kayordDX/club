using Club.Common.Enums;
using Club.Common.Payments;
using Club.Data;
using Club.DTO;
using Club.Entities;
using Microsoft.EntityFrameworkCore;

namespace Club.Services;

public class BookingVoucherService(AppDbContext db)
{
    public Task<List<WalletVoucherGrant>> GetGrantsAsync(Guid userId, CancellationToken ct) =>
        db.WalletVoucherGrant.Include(x => x.Wallet).Include(x => x.Voucher).Where(x => x.Wallet.UserId == userId).OrderBy(x => x.ExpiryDate).ToListAsync(ct);

    public Task<Booking?> GetBookingAsync(int bookingId, Guid userId, CancellationToken ct) =>
        db
            .Booking.Include(x => x.SlotContractBookings)
                .ThenInclude(x => x.SlotContract)
                    .ThenInclude(x => x.Slot)
            .Include(x => x.ExtraBookings)
                .ThenInclude(x => x.Extra)
            .FirstOrDefaultAsync(x => x.Id == bookingId && x.UserId == userId, ct);

    public async Task<BookingVoucherDTO> DescribeAsync(Booking booking, WalletVoucherGrant grant, decimal available, CancellationToken ct)
    {
        var voucher = grant.Voucher;
        var dto = new BookingVoucherDTO
        {
            GrantId = grant.Id,
            VoucherId = voucher.Id,
            Name = voucher.Name,
            Description = voucher.Description,
            IsExtra = voucher.IsExtra,
            RedemptionKind = voucher.RedemptionKind,
            DiscountMode = voucher.DiscountMode,
            DiscountValue = voucher.DiscountValue,
            MaxDiscountAmount = voucher.MaxDiscountAmount,
            AmountRemaining = grant.AmountRemaining,
            ExpiryDate = grant.ExpiryDate,
        };
        var now = DateTime.UtcNow;
        if (booking.BookingStatusId != (int)BookingStatusEnum.Pending || booking.ExpiresAt <= now || available <= 0)
            dto.IneligibleReason = "Booking is not payable or has no unreserved balance.";
        else if (grant.Wallet.UserId != booking.UserId || !grant.Wallet.IsActive || grant.Wallet.Currency != "ZAR")
            dto.IneligibleReason = "Wallet is inactive or uses an unsupported currency.";
        else if (grant.GrantedAt > now || grant.ExpiryDate <= now || grant.AmountRemaining <= 0)
            dto.IneligibleReason = "Voucher is expired, not yet valid or exhausted.";
        if (dto.IneligibleReason is not null)
            return dto;

        var facilities = await db.VoucherFacility.Where(x => x.VoucherId == voucher.Id).Select(x => x.FacilityId).ToListAsync(ct);
        var history = await db
            .PaymentVoucher.Include(x => x.Payment)
            .Include(x => x.WalletVoucherGrant)
                .ThenInclude(x => x.Voucher)
            .Where(x => db.PaymentBooking.Any(pb => pb.PaymentId == x.PaymentId && pb.BookingId == booking.Id))
            .ToListAsync(ct);
        history = history.Where(x => BookingPayments.IsSettled(x.Payment.PaymentStatusId)).ToList();
        bool ValidDate(DateTime date) => date >= grant.GrantedAt && date < grant.ExpiryDate;

        var entitlement = voucher.RedemptionKind == VoucherRedemptionKind.Entitlement;
        var contractIds =
            entitlement && !voucher.IsExtra ? await db.VoucherContract.Where(x => x.VoucherId == voucher.Id).Select(x => x.ContractId).ToListAsync(ct) : [];
        var extraIds = entitlement && voucher.IsExtra ? await db.VoucherExtra.Where(x => x.VoucherId == voucher.Id).Select(x => x.ExtraId).ToListAsync(ct) : [];

        if (voucher.IsExtra)
        {
            foreach (var extra in booking.ExtraBookings.Where(x => facilities.Contains(x.Extra.FacilityId) && (!entitlement || extraIds.Contains(x.ExtraId))))
            {
                var dates = booking
                    .SlotContractBookings.Where(x => x.SlotContract.Slot.FacilityId == extra.Extra.FacilityId)
                    .Select(x => x.SlotContract.Slot.StartDatetime)
                    .ToList();
                if (dates.Count == 0 || dates.Any(date => !ValidDate(date)))
                    continue;
                var used = history
                    .Where(x => x.ExtraId == extra.ExtraId && x.WalletVoucherGrant.Voucher.RedemptionKind == VoucherRedemptionKind.Entitlement)
                    .Sum(x => x.Units);
                dto.Targets.Add(
                    new VoucherTargetDTO
                    {
                        ExtraId = extra.ExtraId,
                        Name = extra.Extra.Name,
                        UnitsAvailable = Math.Max(0, extra.Amount - (int)used),
                        UnitPrice = extra.Extra.Price,
                    }
                );
            }
        }
        else
        {
            foreach (
                var slot in booking.SlotContractBookings.Where(x =>
                    x.SlotContract.Slot.FacilityId.HasValue
                    && facilities.Contains(x.SlotContract.Slot.FacilityId.Value)
                    && ValidDate(x.SlotContract.Slot.StartDatetime)
                    && (!entitlement || contractIds.Contains(x.SlotContract.ContractId))
                )
            )
            {
                var used = history.Any(x =>
                    x.SlotContractBookingId == slot.Id && x.WalletVoucherGrant.Voucher.RedemptionKind == VoucherRedemptionKind.Entitlement
                );
                dto.Targets.Add(
                    new VoucherTargetDTO
                    {
                        SlotContractBookingId = slot.Id,
                        Name = slot.SlotContract.Description ?? "Round",
                        UnitsAvailable = used ? 0 : 1,
                        UnitPrice = slot.SlotContract.Price,
                    }
                );
            }
        }

        // Previously redeemed voucher value is not discountable a second time.
        var eligible = dto.Targets.Sum(x => x.UnitPrice * (voucher.IsExtra ? booking.ExtraBookings.Single(e => e.ExtraId == x.ExtraId).Amount : 1));
        eligible = Math.Max(0, eligible - history.Where(x => x.WalletVoucherGrant.Voucher.IsExtra == voucher.IsExtra).Sum(x => x.Payment.Amount));
        dto.EligibleAmount = eligible;
        if (voucher.RedemptionKind == VoucherRedemptionKind.Discount && history.Any(x => x.WalletVoucherGrantId == grant.Id))
            dto.IneligibleReason = "This discount grant has already been used for this booking.";
        else
        {
            var entitlementValue = dto.Targets.Where(x => x.UnitsAvailable > 0).Select(x => x.UnitPrice).DefaultIfEmpty(0).Max();
            dto.PaymentValue = VoucherValue.Calculate(voucher, eligible, available, grant.AmountRemaining, entitlementValue);
            if ((voucher.RedemptionKind != VoucherRedemptionKind.Credit && grant.AmountRemaining < 1) || dto.PaymentValue <= 0)
                dto.IneligibleReason = "No eligible unpaid items or voucher value.";
        }
        dto.IsEligible = dto.IneligibleReason is null;
        return dto;
    }

    public async Task<Entities.Payment> RedeemAsync(
        Booking booking,
        WalletVoucherGrant grant,
        decimal available,
        int? slotContractBookingId,
        int? extraId,
        int quantity,
        CancellationToken ct
    )
    {
        // Recompute against current restrictions, not a caller's previously fetched preview.
        var description = await DescribeAsync(booking, grant, available, ct);
        if (!description.IsEligible)
            throw new InvalidOperationException(description.IneligibleReason);
        decimal units;
        var amount = description.PaymentValue;
        if (grant.Voucher.RedemptionKind == VoucherRedemptionKind.Entitlement)
        {
            var target = description.Targets.SingleOrDefault(x => x.SlotContractBookingId == slotContractBookingId && x.ExtraId == extraId);
            if (target is null || quantity <= 0 || quantity > target.UnitsAvailable || quantity > grant.AmountRemaining)
                throw new InvalidOperationException("Select an eligible item and an available number of units.");
            amount = VoucherValue.Calculate(grant.Voucher, description.EligibleAmount, available, grant.AmountRemaining, target.UnitPrice * quantity);
            units = quantity;
        }
        else
        {
            if (slotContractBookingId.HasValue || extraId.HasValue || quantity != 1)
                throw new InvalidOperationException("Discount and credit vouchers apply to the eligible subtotal, without an item or quantity.");
            units = grant.Voucher.RedemptionKind == VoucherRedemptionKind.Credit ? amount : 1;
        }
        amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (!BookingPayments.IsValidAmount(amount, available))
            throw new InvalidOperationException("Voucher has no payable value.");
        var payment = new Entities.Payment
        {
            PaymentStatus = await db.PaymentStatus.SingleAsync(x => x.Id == (int)PaymentStatusEnum.Partial, ct),
            PaymentStatusId = (int)PaymentStatusEnum.Partial,
            PaymentStatusDate = DateTime.UtcNow,
            PaymentTypeId = (int)PaymentTypeEnum.Voucher,
            Amount = amount,
            TransactionId = Guid.NewGuid().ToString(),
            ProviderName = "voucher",
            ProviderReference = grant.Id.ToString(),
        };
        db.Payment.Add(payment);
        db.PaymentBooking.Add(new PaymentBooking { Payment = payment, Booking = booking });
        db.PaymentVoucher.Add(
            new PaymentVoucher
            {
                Payment = payment,
                WalletVoucherGrant = grant,
                Units = units,
                SlotContractBookingId = slotContractBookingId,
                ExtraId = extraId,
            }
        );
        grant.AmountRemaining -= units;
        await BookingPayments.ApplyAsync(db, booking, payment, ct);
        await db.SaveChangesAsync(ct);
        return payment;
    }
}

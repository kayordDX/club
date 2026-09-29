using Club.Common.Enums;

namespace Club.DTO;

public class BookingVoucherDTO
{
    public Guid GrantId { get; set; }
    public int VoucherId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public bool IsExtra { get; set; }
    public VoucherRedemptionKind RedemptionKind { get; set; }
    public VoucherDiscountMode? DiscountMode { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal AmountRemaining { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsEligible { get; set; }
    public string? IneligibleReason { get; set; }
    public decimal EligibleAmount { get; set; }
    public decimal PaymentValue { get; set; }
    public List<VoucherTargetDTO> Targets { get; set; } = [];
}

public class VoucherTargetDTO
{
    public int? SlotContractBookingId { get; set; }
    public int? ExtraId { get; set; }
    public required string Name { get; set; }
    public int UnitsAvailable { get; set; }
    public decimal UnitPrice { get; set; }
}

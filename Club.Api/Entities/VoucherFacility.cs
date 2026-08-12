namespace Club.Entities;

public class VoucherFacility
{
    public int VoucherId { get; set; }
    public required Voucher Voucher { get; set; }
    public int FacilityId { get; set; }
    public required Facility Facility { get; set; }
}

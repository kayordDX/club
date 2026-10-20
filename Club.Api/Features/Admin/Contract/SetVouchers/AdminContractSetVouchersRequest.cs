namespace Club.Features.Admin.Contract.SetVouchers;

public class AdminContractSetVouchersRequest
{
    public int FacilityId { get; set; }
    public int Id { get; set; }
    public List<AdminContractVoucherDTO> Vouchers { get; set; } = [];
}

using Club.DTO;

namespace Club.Features.Admin.Contract;

public class AdminContractVoucherDTO
{
    public int VoucherId { get; set; }
    public decimal Amount { get; set; }
}

public class AdminContractVouchersDTO
{
    public List<AdminContractVoucherDTO> Vouchers { get; set; } = [];
    public List<AdminVoucherDTO> AvailableVouchers { get; set; } = [];
}

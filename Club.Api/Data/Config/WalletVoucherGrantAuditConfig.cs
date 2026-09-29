using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class WalletVoucherGrantAuditConfig : IEntityTypeConfiguration<WalletVoucherGrantAudit>
{
    public void Configure(EntityTypeBuilder<WalletVoucherGrantAudit> builder)
    {
        builder.HasOne(x => x.WalletVoucherGrant).WithMany().HasForeignKey(x => x.WalletVoucherGrantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.WalletVoucherGrantId).IsUnique().HasFilter("action = 1");
        builder.HasIndex(x => x.SourceUserContractId);
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.Property(x => x.Reference).HasMaxLength(200);
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_wallet_voucher_grant_audit_action", "action = 1");
            t.HasCheckConstraint("ck_wallet_voucher_grant_audit_source", "source_type BETWEEN 1 AND 5");
            t.HasCheckConstraint("ck_wallet_voucher_grant_audit_contract", "(source_type = 1) = (source_user_contract_id IS NOT NULL)");
        });
    }
}

using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class VoucherFacilityConfig : IEntityTypeConfiguration<VoucherFacility>
{
    public void Configure(EntityTypeBuilder<VoucherFacility> builder)
    {
        builder.HasKey(x => new { x.VoucherId, x.FacilityId });

        builder
            .HasOne(x => x.Voucher)
            .WithMany()
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_voucher_facility_voucher_voucher_id");

        builder
            .HasOne(x => x.Facility)
            .WithMany()
            .HasForeignKey(x => x.FacilityId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_voucher_facility_facility_facility_id");
    }
}

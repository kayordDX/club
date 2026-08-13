using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class ContractFacilityConfig : IEntityTypeConfiguration<ContractFacility>
{
    public void Configure(EntityTypeBuilder<ContractFacility> builder)
    {
        builder.HasKey(x => new { x.ContractId, x.FacilityId });

        builder
            .HasOne(x => x.Contract)
            .WithMany(c => c.ContractFacilities)
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_contract_facility_contract_contract_id");

        builder
            .HasOne(x => x.Facility)
            .WithMany(f => f.ContractFacilities)
            .HasForeignKey(x => x.FacilityId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_contract_facility_facility_facility_id");
    }
}

using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class ContractFacilityConfig : IEntityTypeConfiguration<ContractFacility>
{
    public void Configure(EntityTypeBuilder<ContractFacility> builder)
    {
        builder.HasKey(x => new { x.ContractId, x.FacilityId });
    }
}

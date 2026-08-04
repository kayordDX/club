using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class ContractEntityConfig : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(250).IsRequired();

        builder.Property(c => c.Created).HasDefaultValue(DateTime.MinValue);

        builder.HasOne(c => c.Facility).WithMany(b => b.Contracts).HasForeignKey(c => c.FacilityId).OnDelete(DeleteBehavior.Restrict);
    }
}

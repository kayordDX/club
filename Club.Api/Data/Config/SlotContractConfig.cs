using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class SlotContractConfig : IEntityTypeConfiguration<SlotContract>
{
    public void Configure(EntityTypeBuilder<SlotContract> builder) { }
}

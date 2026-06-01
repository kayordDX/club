using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Online.Entities;

namespace Online.Data.Config;

public class SlotContractConfig : IEntityTypeConfiguration<SlotContract>
{
    public void Configure(EntityTypeBuilder<SlotContract> builder)
    {

    }
}

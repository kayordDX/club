using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Online.Entities;

namespace Online.Data.Config;

public class SlotGroupConfig : IEntityTypeConfiguration<SlotGroup>
{
    public void Configure(EntityTypeBuilder<SlotGroup> builder)
    {

    }
}

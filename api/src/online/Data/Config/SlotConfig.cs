using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Online.Entities;

namespace Online.Data.Config;

public class SlotConfig : IEntityTypeConfiguration<Slot>
{
    public void Configure(EntityTypeBuilder<Slot> builder)
    {

    }
}

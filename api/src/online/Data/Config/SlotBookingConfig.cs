using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Online.Entities;

namespace Online.Data.Config;

public class SlotBookingConfig : IEntityTypeConfiguration<SlotBooking>
{
    public void Configure(EntityTypeBuilder<SlotBooking> builder)
    {
    }
}

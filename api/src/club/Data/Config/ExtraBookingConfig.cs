using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Online.Entities;

namespace Online.Data.Config;

public class ExtraBookingConfig : IEntityTypeConfiguration<ExtraBooking>
{
    public void Configure(EntityTypeBuilder<ExtraBooking> builder)
    {
        builder.HasKey(x => new { x.ExtraId, x.BookingId });
    }
}

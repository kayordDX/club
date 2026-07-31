using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class ExtraBookingConfig : IEntityTypeConfiguration<ExtraBooking>
{
    public void Configure(EntityTypeBuilder<ExtraBooking> builder)
    {
        builder.HasKey(x => new { x.ExtraId, x.BookingId });
    }
}

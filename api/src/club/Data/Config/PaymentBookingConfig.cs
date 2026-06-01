using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Online.Entities;

namespace Online.Data.Config;

public class PaymentBookingConfig : IEntityTypeConfiguration<PaymentBooking>
{
    public void Configure(EntityTypeBuilder<PaymentBooking> builder)
    {
        builder.HasKey(x => new { x.PaymentId, x.BookingId });
    }
}

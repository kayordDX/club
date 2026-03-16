using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Online.Entities;

namespace Online.Data.Config;

public class SlotBookingConfig : IEntityTypeConfiguration<SlotBooking>
{
    public void Configure(EntityTypeBuilder<SlotBooking> builder)
    {
        builder.Property(x => x.Email)
            .HasMaxLength(256);

        builder.HasOne(x => x.User)
            .WithMany(x => x.SlotBookings)
            .HasForeignKey(x => x.UserId)
            .IsRequired(false);

        builder.HasOne(x => x.Slot)
            .WithMany(x => x.SlotBookings)
            .HasForeignKey(x => x.SlotId)
            .IsRequired(false);
    }
}

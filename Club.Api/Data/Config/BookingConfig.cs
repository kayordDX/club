using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class BookingConfig : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.Property(b => b.GuestName).HasMaxLength(200);
        builder.Property(b => b.GuestEmail).HasMaxLength(256);
        builder.Property(b => b.GuestPhone).HasMaxLength(50);
        builder.HasOne(b => b.Facility).WithMany().HasForeignKey(b => b.FacilityId).OnDelete(DeleteBehavior.Restrict);
    }
}

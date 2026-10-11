using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class BookingActionConfig : IEntityTypeConfiguration<BookingAction>
{
    public void Configure(EntityTypeBuilder<BookingAction> builder)
    {
        builder.HasOne(x => x.Booking).WithMany().HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.BookingId, x.Timestamp });
    }
}

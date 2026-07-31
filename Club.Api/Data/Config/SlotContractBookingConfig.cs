using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class SlotContractBookingConfig : IEntityTypeConfiguration<SlotContractBooking>
{
    public void Configure(EntityTypeBuilder<SlotContractBooking> builder) { }
}

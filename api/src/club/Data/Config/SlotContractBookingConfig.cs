using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Online.Entities;

namespace Online.Data.Config;

public class SlotContractBookingConfig : IEntityTypeConfiguration<SlotContractBooking>
{
    public void Configure(EntityTypeBuilder<SlotContractBooking> builder)
    {

    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Club.Entities;

namespace Club.Data.Config;

public class PaymentStatusConfig : IEntityTypeConfiguration<PaymentStatus>
{
    public void Configure(EntityTypeBuilder<PaymentStatus> builder)
    {
        builder.Property(ps => ps.Name).HasMaxLength(250);
    }
}

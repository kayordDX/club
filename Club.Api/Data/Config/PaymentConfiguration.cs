using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Club.Entities;

namespace Club.Data.Config;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(p => p.TransactionId)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(p => p.TransactionId)
            .IsUnique();

        builder.Property(p => p.ProviderReference)
            .HasMaxLength(255);

        builder.Property(p => p.ProviderName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.RedirectUrl)
            .HasMaxLength(1000);

        builder.Property(p => p.ErrorMessage)
            .HasMaxLength(1000);
    }
}

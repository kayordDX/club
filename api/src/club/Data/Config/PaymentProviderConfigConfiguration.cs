using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Club.Entities;

namespace Club.Data.Config;

public class PaymentProviderConfigConfiguration : IEntityTypeConfiguration<PaymentProviderConfig>
{
    public void Configure(EntityTypeBuilder<PaymentProviderConfig> builder)
    {
        builder.HasIndex(c => c.ProviderKey).IsUnique();
        builder.Property(c => c.ProviderKey).HasMaxLength(100).IsRequired();
        builder.Property(c => c.EncryptedSettings).IsRequired();
        builder.Property(c => c.Iv).IsRequired();
    }
}

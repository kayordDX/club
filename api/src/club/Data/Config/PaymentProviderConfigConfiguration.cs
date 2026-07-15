using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Club.Entities;

namespace Club.Data.Config;

public class PaymentProviderConfigConfiguration : IEntityTypeConfiguration<PaymentProviderConfig>
{
    public void Configure(EntityTypeBuilder<PaymentProviderConfig> builder)
    {
        builder.HasIndex(c => new { c.FacilityId, c.ProviderKey }).IsUnique();
        builder.Property(c => c.ProviderKey).HasMaxLength(100).IsRequired();
        builder.Property(c => c.EncryptedSettings).IsRequired();
        builder.Property(c => c.Iv).IsRequired();
        builder.HasOne(c => c.Facility)
            .WithMany(f => f.PaymentProviderConfigs)
            .HasForeignKey(c => c.FacilityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

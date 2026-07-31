using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class PaymentLogConfiguration : IEntityTypeConfiguration<PaymentLog>
{
    public void Configure(EntityTypeBuilder<PaymentLog> builder)
    {
        builder.ToTable("payment_log");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.TransactionId).HasMaxLength(100).IsRequired();

        builder.Property(l => l.ProviderName).HasMaxLength(50).IsRequired();

        builder.Property(l => l.EventType).HasMaxLength(100).IsRequired();

        builder.Property(l => l.Status).HasMaxLength(50).IsRequired();

        builder.Property(l => l.Message).HasMaxLength(1000);

        builder.Property(l => l.Metadata).HasColumnType("text");

        builder.HasOne(l => l.Payment).WithMany().HasForeignKey(l => l.PaymentId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => l.TransactionId);
        builder.HasIndex(l => l.EventType);
        builder.HasIndex(l => l.CreatedAt);
    }
}

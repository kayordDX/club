using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Online.Entities;

namespace Online.Data.Config;

public class EmailLogConfig : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.Property(e => e.Payload)
            .HasColumnName("Payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.Subject)
            .HasColumnName("Subject")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Message)
            .HasColumnName("Message")
            .HasColumnType("text")
            .IsRequired();

        builder.HasIndex(e => new { e.IsSent, e.CreatedAtUtc });
    }
}

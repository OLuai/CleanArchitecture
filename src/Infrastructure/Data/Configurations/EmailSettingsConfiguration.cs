using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class EmailSettingsConfiguration : IEntityTypeConfiguration<EmailSettings>
{
    public void Configure(EntityTypeBuilder<EmailSettings> builder)
    {
        // No value generator here, unlike every other entity: the key is the constant
        // EmailSettings.SingletonId, and that is what makes the table hold one row at most —
        // a second insert collides on the primary key rather than quietly creating a rival
        // configuration.
        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        // Stored as a string like every other enum here: a column that reads "StartTls" survives
        // someone reordering the enum, which an ordinal would not.
        builder.Property(s => s.Security)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.Host)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(s => s.UserName)
            .HasMaxLength(255);

        // Ciphertext from ISecretProtector, comfortably longer than the plaintext.
        builder.Property(s => s.ProtectedPassword)
            .HasMaxLength(1024);

        builder.Property(s => s.FromAddress)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(s => s.FromName)
            .HasMaxLength(100);
    }
}

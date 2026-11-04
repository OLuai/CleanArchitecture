using CleanArchitecture.Infrastructure.Codification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class CodeSequenceConfiguration : IEntityTypeConfiguration<CodeSequence>
{
    public void Configure(EntityTypeBuilder<CodeSequence> builder)
    {
        builder.ToTable("CodeSequences");

        builder.HasKey(s => s.Radical);

        builder.Property(s => s.Radical)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(s => s.CurrentValue)
            .IsRequired();
    }
}

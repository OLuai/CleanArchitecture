using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CleanArchitecture.Infrastructure.IdGeneration;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class IdSequenceConfiguration : IEntityTypeConfiguration<IdSequence>
{
    public void Configure(EntityTypeBuilder<IdSequence> builder)
    {
        builder.ToTable("IdSequences");

        builder.HasKey(s => s.Radical);

        builder.Property(s => s.Radical)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(s => s.CurrentValue)
            .IsRequired();
    }
}

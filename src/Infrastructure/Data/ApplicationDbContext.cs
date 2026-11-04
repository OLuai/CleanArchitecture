using System.Reflection;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Search;
using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Identity;
using CleanArchitecture.Infrastructure.Codification;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<TodoList> TodoLists => Set<TodoList>();

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    public DbSet<EmailSettings> EmailSettings => Set<EmailSettings>();

    public DbSet<CodeSequence> CodeSequences => Set<CodeSequence>();

    /// <summary>
    /// Backing store for the ASP.NET Data Protection key ring — see
    /// <c>PersistKeysToDbContext</c> in Infrastructure's DependencyInjection.
    /// </summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        ApplyKeyAndCodeConventions(builder);

        // Map DbSearchExtensions.Unaccent to the f_unaccent SQL function (an IMMUTABLE wrapper
        // around the unaccent extension). See the AddAccentInsensitiveSearch migration.
        builder.HasDbFunction(typeof(DbSearchExtensions).GetMethod(nameof(DbSearchExtensions.Unaccent))!)
            .HasName("f_unaccent")
            .HasSchema("public");
    }

    /// <summary>
    /// Conventions shared by every entity, so that a new one only has to pick its base class:
    /// <list type="bullet">
    /// <item><c>BaseEntity&lt;Guid&gt;</c> keys are UUID v7, generated in the application
    /// (<see cref="SequentialGuidValueGenerator"/>).</item>
    /// <item><see cref="IHasCode"/> entities get a required, unique <c>Code</c>, filled in on insert
    /// by <see cref="CodeGenerationInterceptor"/> once the type is declared with <c>RegisterCode</c>.</item>
    /// </list>
    /// Applied after the entity configurations, so these settings win over theirs.
    /// </summary>
    private static void ApplyKeyAndCodeConventions(ModelBuilder builder)
    {
        var entityTypes = builder.Model.GetEntityTypes()
            .Where(t => t.BaseType is null && !t.IsOwned())
            .Select(t => t.ClrType)
            .ToList();

        foreach (var clrType in entityTypes)
        {
            if (typeof(BaseEntity<Guid>).IsAssignableFrom(clrType))
            {
                builder.Entity(clrType).Property(nameof(BaseEntity<Guid>.Id))
                    .ValueGeneratedOnAdd()
                    .HasValueGenerator<SequentialGuidValueGenerator>();
            }

            if (typeof(IHasCode).IsAssignableFrom(clrType))
            {
                var entity = builder.Entity(clrType);

                entity.Property(nameof(IHasCode.Code))
                    .HasMaxLength(32)
                    .IsRequired();

                entity.HasIndex(nameof(IHasCode.Code))
                    .IsUnique();
            }
        }
    }
}

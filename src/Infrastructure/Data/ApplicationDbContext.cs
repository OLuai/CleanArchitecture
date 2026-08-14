using System.Reflection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Search;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Identity;
using CleanArchitecture.Infrastructure.IdGeneration;

namespace CleanArchitecture.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<TodoList> TodoLists => Set<TodoList>();

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    public DbSet<IdSequence> IdSequences => Set<IdSequence>();

    /// <summary>
    /// Backing store for the ASP.NET Data Protection key ring — see
    /// <c>PersistKeysToDbContext</c> in Infrastructure's DependencyInjection.
    /// </summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Map DbSearchExtensions.Unaccent to the f_unaccent SQL function (an IMMUTABLE wrapper
        // around the unaccent extension). See the AddAccentInsensitiveSearch migration.
        builder.HasDbFunction(typeof(DbSearchExtensions).GetMethod(nameof(DbSearchExtensions.Unaccent))!)
            .HasName("f_unaccent")
            .HasSchema("public");
    }
}

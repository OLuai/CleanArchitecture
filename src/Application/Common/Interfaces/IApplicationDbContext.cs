using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<TodoList> TodoLists { get; }

    DbSet<TodoItem> TodoItems { get; }

    DbSet<EmailSettings> EmailSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

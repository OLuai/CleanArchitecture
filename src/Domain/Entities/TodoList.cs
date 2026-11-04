namespace CleanArchitecture.Domain.Entities;

public class TodoList : BaseAuditableEntity<Guid>, IHasCode
{
    public string Code { get; set; } = string.Empty;

    public string? Title { get; set; }

    public Colour Colour { get; set; } = Colour.Grey;

    public IList<TodoItem> Items { get; private set; } = new List<TodoItem>();
}

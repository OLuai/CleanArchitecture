using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Application.TodoLists.Queries.GetTodos;

public class TodoListDto
{
    public TodoListDto()
    {
        Items = [];
    }

    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string? Title { get; init; }

    public string? Colour { get; init; }

    public IReadOnlyCollection<TodoItemDto> Items { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<TodoList, TodoListDto>();
        }
    }
}

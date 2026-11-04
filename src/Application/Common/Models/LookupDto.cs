namespace CleanArchitecture.Application.Common.Models;

/// <summary>An id/label pair for drop-downs, e.g. the priority levels in <c>TodosVm</c>.</summary>
public class LookupDto
{
    public int Id { get; init; }

    public string? Title { get; init; }
}

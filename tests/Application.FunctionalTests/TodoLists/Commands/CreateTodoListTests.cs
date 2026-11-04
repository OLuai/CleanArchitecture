using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.TodoLists.Commands.CreateTodoList;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Application.FunctionalTests.TodoLists.Commands;

public class CreateTodoListTests : TestBase
{
    [Test]
    public async Task ShouldRequireMinimumFields()
    {
        var command = new CreateTodoListCommand();
        await Should.ThrowAsync<ValidationException>(() => TestApp.SendAsync(command));
    }

    [Test]
    public async Task ShouldRequireUniqueTitle()
    {
        await TestApp.SendAsync(new CreateTodoListCommand
        {
            Title = "Shopping"
        });

        var command = new CreateTodoListCommand
        {
            Title = "Shopping"
        };

        await Should.ThrowAsync<ValidationException>(() => TestApp.SendAsync(command));
    }

    [Test]
    public async Task ShouldCreateTodoList()
    {
        var userId = await TestApp.RunAsDefaultUserAsync();

        var command = new CreateTodoListCommand
        {
            Title = "Tasks"
        };

        var id = await TestApp.SendAsync(command);

        var list = await TestApp.FindAsync<TodoList>(id);

        list.ShouldNotBeNull();
        list!.Title.ShouldBe(command.Title);
        list.CreatedBy.ShouldBe(userId);
        list.Created.ShouldBe(DateTime.Now, TimeSpan.FromMilliseconds(10000));
    }

    [Test]
    public async Task ShouldAssignAVersion7KeyAndABusinessCode()
    {
        await TestApp.RunAsDefaultUserAsync();

        var firstId = await TestApp.SendAsync(new CreateTodoListCommand { Title = "First" });
        var secondId = await TestApp.SendAsync(new CreateTodoListCommand { Title = "Second" });

        var first = await TestApp.FindAsync<TodoList>(firstId);
        var second = await TestApp.FindAsync<TodoList>(secondId);

        // UUID v7: generated in the application, time-ordered.
        firstId.Version.ShouldBe(7);
        secondId.Version.ShouldBe(7);

        // Codes come from a HiLo block: same radical, unique, increasing. Gaps are allowed (a
        // block survives a database reset between tests), so only the order is asserted.
        first!.Code.ShouldMatch(@"^LST-\d{6}$");
        second!.Code.ShouldMatch(@"^LST-\d{6}$");
        string.CompareOrdinal(second.Code, first.Code).ShouldBePositive();
    }
}

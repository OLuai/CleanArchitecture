using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.ValueObjects;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Identity;

namespace CleanArchitecture.Migration.Seed;

public class DevelopmentSeeder : ProductionSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DevelopmentSeeder> _logger;

    public DevelopmentSeeder(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger<ProductionSeeder> baseLogger,
        ApplicationDbContext context,
        ILogger<DevelopmentSeeder> logger)
        : base(roleManager, userManager, baseLogger)
    {
        _context = context;
        _logger = logger;
    }

    public override async Task SeedAsync(CancellationToken cancellationToken)
    {
        await base.SeedAsync(cancellationToken);
        await SeedSampleTodoListAsync(cancellationToken);
    }

    private async Task SeedSampleTodoListAsync(CancellationToken cancellationToken)
    {
        if (await _context.TodoLists.AnyAsync(cancellationToken))
        {
            return;
        }

        _logger.LogInformation("Seeding sample TodoList for development");
        _context.TodoLists.Add(new TodoList
        {
            Title = "Tasks",
            Colour = Colour.Green,
            Items =
            {
                new TodoItem { Title = "Make a todo list 📃" },
                new TodoItem { Title = "Check off the first item ✅" },
                new TodoItem { Title = "Realise you've already done two things on the list! 🤯" },
                new TodoItem { Title = "Reward yourself with a nice, long nap 🏆" }
            }
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}

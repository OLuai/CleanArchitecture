using Microsoft.Extensions.DependencyInjection;
using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Infrastructure.IdGeneration;

public static class EntityCodeRegistrationExtensions
{
    public static IServiceCollection RegisterEntityCode<TEntity>(
        this IServiceCollection services,
        string radical,
        int padLength = 6,
        string separator = "-",
        int hiLoBlockSize = 50)
        where TEntity : class, IHasCode
    {
        if (string.IsNullOrWhiteSpace(radical))
            throw new ArgumentException("Radical cannot be empty.", nameof(radical));
        if (padLength < 1)
            throw new ArgumentOutOfRangeException(nameof(padLength), "Pad length must be >= 1.");
        if (hiLoBlockSize < 1)
            throw new ArgumentOutOfRangeException(nameof(hiLoBlockSize), "Block size must be >= 1.");

        services.AddSingleton(new EntityCodeRegistration(
            EntityType: typeof(TEntity),
            Radical: radical,
            PadLength: padLength,
            Separator: separator,
            HiLoBlockSize: hiLoBlockSize));

        return services;
    }
}

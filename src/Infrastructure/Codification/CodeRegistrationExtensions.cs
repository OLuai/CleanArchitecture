using CleanArchitecture.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.Infrastructure.Codification;

public static class CodeRegistrationExtensions
{
    /// <summary>
    /// Declares that <typeparamref name="TEntity"/> carries a business code built from
    /// <paramref name="radical"/>: <c>RegisterCode&lt;Project&gt;("PRJ")</c> yields
    /// <c>PRJ-000123</c>. The entity's primary key stays a <see cref="Guid"/>; only the code is
    /// sequential.
    /// </summary>
    public static IServiceCollection RegisterCode<TEntity>(
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

        services.AddSingleton(new CodeRegistration(
            EntityType: typeof(TEntity),
            Radical: radical,
            PadLength: padLength,
            Separator: separator,
            HiLoBlockSize: hiLoBlockSize));

        return services;
    }
}

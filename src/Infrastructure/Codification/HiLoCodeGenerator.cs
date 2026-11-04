using System.Collections.Concurrent;
using CleanArchitecture.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.Infrastructure.Codification;

/// <summary>
/// Hands out sequential business codes, reserving them in blocks so a code costs a round-trip only
/// once every <see cref="CodeRegistration.HiLoBlockSize"/> insertions.
/// </summary>
public sealed class HiLoCodeGenerator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CodeRegistry _registry;

    private readonly ConcurrentDictionary<string, HiLoState> _states = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public HiLoCodeGenerator(IServiceScopeFactory scopeFactory, CodeRegistry registry)
    {
        _scopeFactory = scopeFactory;
        _registry = registry;
    }

    public async Task<string> NextAsync(CodeRegistration registration, CancellationToken cancellationToken = default)
    {
        var gate = _locks.GetOrAdd(registration.Radical, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_states.TryGetValue(registration.Radical, out var state) || !state.HasNext)
            {
                var newCurrent = await ReserveBlockAsync(registration, cancellationToken).ConfigureAwait(false);
                state = new HiLoState(newCurrent - registration.HiLoBlockSize + 1, newCurrent);
                _states[registration.Radical] = state;
            }

            var value = state.Take();
            return Format(registration, value);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<long> ReserveBlockAsync(CodeRegistration registration, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // PostgreSQL upsert: insert if missing, else add block, returning the new high value.
        var radical = registration.Radical;
        var block = registration.HiLoBlockSize;

        // ToListAsync, not FirstAsync: First appends a LIMIT, which makes EF wrap the statement in
        // an outer SELECT, and an INSERT ... RETURNING is not composable, so that throws.
        // Enumerating the query as written executes exactly this statement.
        var rows = await db.Database
            .SqlQuery<long>($@"
                INSERT INTO ""CodeSequences"" (""Radical"", ""CurrentValue"")
                VALUES ({radical}, {(long)block})
                ON CONFLICT (""Radical"") DO UPDATE
                SET ""CurrentValue"" = ""CodeSequences"".""CurrentValue"" + {(long)block}
                RETURNING ""CurrentValue""")
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows[0];
    }

    private static string Format(CodeRegistration registration, long value)
    {
        var formatted = value.ToString("D" + registration.PadLength);
        return string.Concat(registration.Radical, registration.Separator, formatted);
    }

    private sealed class HiLoState
    {
        private long _next;
        private readonly long _max;

        public HiLoState(long start, long max)
        {
            _next = start;
            _max = max;
        }

        public bool HasNext => _next <= _max;

        public long Take() => _next++;
    }
}

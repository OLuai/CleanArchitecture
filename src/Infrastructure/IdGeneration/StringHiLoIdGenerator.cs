using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CleanArchitecture.Infrastructure.Data;

namespace CleanArchitecture.Infrastructure.IdGeneration;

public sealed class StringHiLoIdGenerator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly StringIdRegistry _registry;

    private readonly ConcurrentDictionary<string, HiLoState> _states = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public StringHiLoIdGenerator(IServiceScopeFactory scopeFactory, StringIdRegistry registry)
    {
        _scopeFactory = scopeFactory;
        _registry = registry;
    }

    public async Task<string> NextAsync(StringIdRegistration registration, CancellationToken cancellationToken = default)
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

    private async Task<long> ReserveBlockAsync(StringIdRegistration registration, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // PostgreSQL upsert: insert if missing, else add block, returning the new high value.
        var radical = registration.Radical;
        var block = registration.HiLoBlockSize;

        var newHigh = await db.Database
            .SqlQuery<long>($@"
                INSERT INTO ""IdSequences"" (""Radical"", ""CurrentValue"")
                VALUES ({radical}, {(long)block})
                ON CONFLICT (""Radical"") DO UPDATE
                SET ""CurrentValue"" = ""IdSequences"".""CurrentValue"" + {(long)block}
                RETURNING ""CurrentValue""")
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);

        return newHigh;
    }

    private static string Format(StringIdRegistration registration, long value)
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

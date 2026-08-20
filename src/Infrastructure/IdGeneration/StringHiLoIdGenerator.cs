using System.Collections.Concurrent;
using System.Data.Common;
using System.Globalization;
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
        // A scope of its own, deliberately: the block must be reserved outside whatever transaction
        // the caller is in. Enlisting would replay the same block on a rollback and hand the same
        // ids out twice, whereas a rollback here only ever skips a block.
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Run as a raw command rather than db.Database.SqlQuery<long>(...): any LINQ operator on
        // top of SqlQuery — FirstAsync included — makes EF wrap the statement in an outer SELECT,
        // and PostgreSQL refuses to compose over a data-modifying INSERT ... RETURNING. Executing
        // the command directly keeps the upsert atomic in a single round trip.
        await db.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var connection = db.Database.GetDbConnection();
            await using var command = connection.CreateCommand();

            command.CommandText = """
                INSERT INTO "IdSequences" ("Radical", "CurrentValue")
                VALUES (@radical, @block)
                ON CONFLICT ("Radical") DO UPDATE
                SET "CurrentValue" = "IdSequences"."CurrentValue" + @block
                RETURNING "CurrentValue"
                """;

            command.Parameters.Add(CreateParameter(command, "@radical", registration.Radical));
            command.Parameters.Add(CreateParameter(command, "@block", (long)registration.HiLoBlockSize));

            var newHigh = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            if (newHigh is null or DBNull)
            {
                throw new InvalidOperationException(
                    $"Reserving an id block for radical '{registration.Radical}' returned no value.");
            }

            return Convert.ToInt64(newHigh, CultureInfo.InvariantCulture);
        }
        finally
        {
            await db.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }

    private static DbParameter CreateParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        return parameter;
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

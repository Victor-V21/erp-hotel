using System.Data;
using hotel_erp.Api.Database;
using Microsoft.EntityFrameworkCore;

namespace hotel_erp.Api.Services
{
    internal sealed class CorrelativeStateException : InvalidOperationException
    {
        public CorrelativeStateException(string message) : base(message)
        {
        }
    }

    internal static class PostgresCorrelativeLock
    {
        public static async Task<T> ExecuteAsync<T>(ApplicationDbContext context, string lockName, Func<Task<T>> action, CancellationToken cancellationToken = default)
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            // PostgreSQL Transaction Advisory Lock ensures single-worker execution per lockName across all nodes/threads
            await context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({lockName}));", cancellationToken);

            var now = DateTime.UtcNow;
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO \"CorrelativeLocks\" (\"LockName\", \"UpdatedAt\") VALUES ({lockName}, {now}) ON CONFLICT (\"LockName\") DO UPDATE SET \"UpdatedAt\" = {now};", 
                cancellationToken);

            try
            {
                var result = await action();
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch (CorrelativeStateException)
            {
                await transaction.CommitAsync(cancellationToken);
                throw;
            }
        }
    }
}

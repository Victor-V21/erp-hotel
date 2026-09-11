using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using hotel_erp.Api.Database;
using hotel_erp.Api.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace hotel_erp.Api.Services;

public sealed class IdempotencyService(ApplicationDbContext context)
{
    private static readonly JsonSerializerOptions HashOptions = new(JsonSerializerDefaults.Web);

    public static bool TryNormalizeKey(string? value, out string key)
    {
        if (Guid.TryParse(value, out var parsed))
        {
            key = parsed.ToString("D");
            return true;
        }

        key = string.Empty;
        return false;
    }

    public static string ComputeRequestHash<T>(T request)
    {
        var json = JsonSerializer.Serialize(request, HashOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    public async Task<IdempotencyRecord?> LockAndFindAsync(
        Guid userId,
        string scope,
        string key,
        string requestHash,
        CancellationToken cancellationToken = default)
    {
        if (context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("La idempotencia financiera requiere una transacción activa.");

        var lockName = $"idempotency:{userId:N}:{scope}:{key}";
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({lockName}));",
            cancellationToken);

        var existing = await context.IdempotencyRecords
            .SingleOrDefaultAsync(record =>
                record.UserId == userId
                && record.Scope == scope
                && record.Key == key,
                cancellationToken);

        if (existing is not null && existing.RequestHash != requestHash)
            throw new IdempotencyConflictException("La clave de idempotencia ya se utilizó con datos diferentes.");

        return existing;
    }

    public async Task StoreAsync(
        Guid userId,
        string scope,
        string key,
        string requestHash,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        context.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Scope = scope,
            Key = key,
            RequestHash = requestHash,
            ResourceId = resourceId
        });
        await context.SaveChangesAsync(cancellationToken);
    }
}

public sealed class IdempotencyConflictException(string message) : InvalidOperationException(message);

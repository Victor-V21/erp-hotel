using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Database.Entities;
using Microsoft.EntityFrameworkCore;
using hotel_erp.Api.Database;

namespace hotel_erp.Api.Services
{
    public class AuditService
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ApplicationDbContext _context;

        public AuditService(IAuditLogRepository auditLogRepository, ApplicationDbContext context)
        {
            _auditLogRepository = auditLogRepository;
            _context = context;
        }

        public async Task LogAsync(Guid? userId, string action, string? entityName, Guid? entityId, object? changes = null, string? correlativeNumber = null, string? paymentMethod = null)
        {
            await PostgresCorrelativeLock.ExecuteAsync(_context, "audit-chain", async () =>
            {
                var previousHash = await _context.AuditLogs
                    .OrderByDescending(a => a.Timestamp)
                    .Select(a => a.Hash)
                    .FirstOrDefaultAsync();
                var serializedChanges = changes != null ? JsonSerializer.Serialize(changes) : null;
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    CorrelativeNumber = correlativeNumber,
                    PaymentMethod = paymentMethod,
                    Changes = serializedChanges,
                    Timestamp = DateTime.UtcNow,
                    HondurasTimestamp = DateTime.UtcNow.AddHours(-6),
                    PreviousHash = previousHash
                };
                auditLog.Hash = ComputeHash($"{auditLog.PreviousHash}|{auditLog.UserId}|{auditLog.Action}|{auditLog.EntityName}|{auditLog.EntityId}|{auditLog.CorrelativeNumber}|{auditLog.PaymentMethod}|{auditLog.Changes}|{auditLog.Timestamp:O}");

                await _auditLogRepository.AddAsync(auditLog);
                return true;
            });
        }

        private static string ComputeHash(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes);
        }
    }
}

using System.Text.Json;
using hotel_erp.Application.Interfaces;
using hotel_erp.Domain.Entities;

namespace hotel_erp.Infrastructure.Services
{
    public class AuditService
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public AuditService(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
        }

        public async Task LogAsync(Guid? userId, string action, string? entityName, Guid? entityId, object? changes = null)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Changes = changes != null ? JsonSerializer.Serialize(changes) : null,
                Timestamp = DateTime.UtcNow
            };

            await _auditLogRepository.AddAsync(auditLog);
        }
    }
}

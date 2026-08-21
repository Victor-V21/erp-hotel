using System.Security.Cryptography;
using System.Text;
using hotel_erp.Api.Database;
using hotel_erp.Api.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hotel_erp.Api.Controllers
{
    public record AuditLogDto
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserFullName { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? EntityName { get; set; }
        public Guid? EntityId { get; set; }
        public string? CorrelativeNumber { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PreviousHash { get; set; }
        public string? Hash { get; set; }
        public string? Changes { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime HondurasTimestamp { get; set; }
    }

    public record AuditIntegrityResultDto
    {
        public bool IsValid { get; set; }
        public int TotalRecordsVerified { get; set; }
        public DateTime VerifiedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public Guid? BrokenLogId { get; set; }
    }

    [ApiController]
    [Route("api/audit-logs")]
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetLogs(
            [FromQuery] string? action,
            [FromQuery] string? entityName,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? search)
        {
            var query = _context.AuditLogs
                .AsNoTracking()
                .Include(a => a.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(a => a.Action.ToLower().Contains(action.ToLower()));

            if (!string.IsNullOrWhiteSpace(entityName))
                query = query.Where(a => a.EntityName != null && a.EntityName.ToLower().Contains(entityName.ToLower()));

            if (startDate.HasValue)
                query = query.Where(a => a.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.Timestamp <= endDate.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(a =>
                    a.Action.ToLower().Contains(s) ||
                    (a.EntityName != null && a.EntityName.ToLower().Contains(s)) ||
                    (a.CorrelativeNumber != null && a.CorrelativeNumber.ToLower().Contains(s)) ||
                    (a.User != null && (a.User.Username.ToLower().Contains(s) || a.User.FirstName.ToLower().Contains(s) || a.User.LastName.ToLower().Contains(s))));
            }

            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Take(200)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserName = a.User != null ? a.User.Username : null,
                    UserFullName = a.User != null ? $"{a.User.FirstName} {a.User.LastName}" : "Sistema",
                    Action = a.Action,
                    EntityName = a.EntityName,
                    EntityId = a.EntityId,
                    CorrelativeNumber = a.CorrelativeNumber,
                    PaymentMethod = a.PaymentMethod,
                    PreviousHash = a.PreviousHash,
                    Hash = a.Hash,
                    Changes = a.Changes,
                    Timestamp = a.Timestamp,
                    HondurasTimestamp = a.HondurasTimestamp
                })
                .ToListAsync();

            return Ok(logs);
        }

        [HttpGet("verify-integrity")]
        public async Task<ActionResult<AuditIntegrityResultDto>> VerifyIntegrity()
        {
            var logs = await _context.AuditLogs
                .AsNoTracking()
                .OrderBy(a => a.Timestamp)
                .ToListAsync();

            if (logs.Count == 0)
            {
                return Ok(new AuditIntegrityResultDto
                {
                    IsValid = true,
                    TotalRecordsVerified = 0,
                    VerifiedAt = DateTime.UtcNow,
                    ErrorMessage = null
                });
            }

            string? expectedPreviousHash = null;
            for (int i = 0; i < logs.Count; i++)
            {
                var log = logs[i];

                if (i > 0 && log.PreviousHash != expectedPreviousHash)
                {
                    return Ok(new AuditIntegrityResultDto
                    {
                        IsValid = false,
                        TotalRecordsVerified = i,
                        VerifiedAt = DateTime.UtcNow,
                        ErrorMessage = $"Discrepancia en la cadena de hashes previa en el registro #{i + 1}",
                        BrokenLogId = log.Id
                    });
                }

                var calculatedHash = ComputeHash($"{log.PreviousHash}|{log.UserId}|{log.Action}|{log.EntityName}|{log.EntityId}|{log.CorrelativeNumber}|{log.PaymentMethod}|{log.Changes}|{log.Timestamp:O}");
                if (log.Hash != calculatedHash)
                {
                    return Ok(new AuditIntegrityResultDto
                    {
                        IsValid = false,
                        TotalRecordsVerified = i,
                        VerifiedAt = DateTime.UtcNow,
                        ErrorMessage = $"Firma de integridad manipulada o corrupta en el registro {log.Id}",
                        BrokenLogId = log.Id
                    });
                }

                expectedPreviousHash = log.Hash;
            }

            return Ok(new AuditIntegrityResultDto
            {
                IsValid = true,
                TotalRecordsVerified = logs.Count,
                VerifiedAt = DateTime.UtcNow,
                ErrorMessage = null
            });
        }

        private static string ComputeHash(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes);
        }
    }
}

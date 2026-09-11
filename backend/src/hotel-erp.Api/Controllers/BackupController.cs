using hotel_erp.Api.Database;
using hotel_erp.Api.Authorization;
using hotel_erp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = PermissionNames.ManageBackups)]
    public class BackupController : ControllerBase
    {
        private readonly DatabaseBackupService _backupService;
        private readonly ApplicationDbContext _context;

        public BackupController(DatabaseBackupService backupService, ApplicationDbContext context)
        {
            _backupService = backupService;
            _context = context;
        }

        [HttpPost("manual")]
        public async Task<IActionResult> CreateManualBackup()
        {
            await _backupService.RunLocalBackupAsync();

            var last = await _context.BackupLogs
                .OrderByDescending(b => b.StartedAt)
                .FirstOrDefaultAsync();

            if (last == null || last.Status == hotel_erp.Api.Database.Entities.BackupStatus.Failed)
                return BadRequest(new { message = "Error al crear el respaldo", error = last?.ErrorMessage });

            if (!System.IO.File.Exists(last.LocalPath))
                return NotFound(new { message = "Archivo de respaldo no encontrado en disco" });

            var stream = System.IO.File.OpenRead(last.LocalPath);
            return File(stream, "application/octet-stream", last.FileName);
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs([FromQuery] int limit = 20)
        {
            var logs = await _context.BackupLogs
                .OrderByDescending(b => b.StartedAt)
                .Take(limit)
                .Select(b => new
                {
                    b.Id,
                    b.StartedAt,
                    b.CompletedAt,
                    b.FileName,
                    b.SizeBytes,
                    b.Sha256Hash,
                    Status = b.Status.ToString(),
                    b.UploadedAt,
                    b.UploadAttempts,
                    b.ErrorMessage
                })
                .ToListAsync();

            return Ok(logs);
        }
    }
}


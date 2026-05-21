using System.Diagnostics;
using System.Security.Cryptography;
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace hotel_erp.Api.Services
{
    public class DatabaseBackupService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly BackupOptions _options;

        public DatabaseBackupService(ApplicationDbContext context, IConfiguration configuration, IOptions<BackupOptions> options)
        {
            _context = context;
            _configuration = configuration;
            _options = options.Value;
        }

        public async Task RunLocalBackupAsync(CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(_options.LocalPath);
            var fileName = $"hotel_erp_{DateTime.UtcNow:yyyyMMdd_HHmmss}.dump";
            var fullPath = Path.Combine(_options.LocalPath, fileName);
            var log = new BackupLog
            {
                StartedAt = DateTime.UtcNow,
                LocalPath = fullPath,
                FileName = fileName,
                Status = BackupStatus.CreatedLocal
            };

            try
            {
                await ExecutePgDumpAsync(fullPath, cancellationToken);
                var info = new FileInfo(fullPath);
                log.SizeBytes = info.Length;
                log.Sha256Hash = await ComputeSha256Async(fullPath, cancellationToken);
                log.CompletedAt = DateTime.UtcNow;
                log.Status = BackupStatus.PendingUpload;
            }
            catch (Exception ex)
            {
                log.Status = BackupStatus.Failed;
                log.ErrorMessage = ex.Message;
                log.CompletedAt = DateTime.UtcNow;
            }

            await _context.BackupLogs.AddAsync(log, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task SyncPendingUploadsAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.GoogleDriveUploadCommand)) return;

            var pending = await _context.BackupLogs
                .Where(b => b.Status == BackupStatus.PendingUpload || b.Status == BackupStatus.UploadFailed)
                .OrderBy(b => b.StartedAt)
                .Take(5)
                .ToListAsync(cancellationToken);

            foreach (var backup in pending)
            {
                if (!File.Exists(backup.LocalPath))
                {
                    backup.Status = BackupStatus.UploadFailed;
                    backup.ErrorMessage = "El archivo local del backup no existe";
                    continue;
                }

                backup.UploadAttempts++;
                try
                {
                    await ExecuteUploadCommandAsync(backup.LocalPath, cancellationToken);
                    backup.Status = BackupStatus.UploadedToDrive;
                    backup.UploadedAt = DateTime.UtcNow;
                    backup.ErrorMessage = null;
                }
                catch (Exception ex)
                {
                    backup.Status = BackupStatus.UploadFailed;
                    backup.ErrorMessage = ex.Message;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task ExecutePgDumpAsync(string outputPath, CancellationToken cancellationToken)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No hay cadena de conexión DefaultConnection");
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var args = $"--format=custom --file=\"{outputPath}\" --host={builder.Host} --port={builder.Port} --username={builder.Username} --dbname={builder.Database}";
            await ExecuteProcessAsync(_options.PgDumpPath, args, builder.Password, cancellationToken);
        }

        private async Task ExecuteUploadCommandAsync(string filePath, CancellationToken cancellationToken)
        {
            var command = _options.GoogleDriveUploadCommand!.Replace("{file}", filePath);
            var parts = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var fileName = parts[0];
            var args = parts.Length > 1 ? parts[1] : string.Empty;
            await ExecuteProcessAsync(fileName, args, null, cancellationToken);
        }

        private static async Task ExecuteProcessAsync(string fileName, string arguments, string? pgPassword, CancellationToken cancellationToken)
        {
            var startInfo = new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            if (!string.IsNullOrEmpty(pgPassword)) startInfo.Environment["PGPASSWORD"] = pgPassword;

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"No se pudo iniciar {fileName}");
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            if (process.ExitCode != 0) throw new InvalidOperationException(error);
        }

        private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
        {
            await using var stream = File.OpenRead(filePath);
            var hash = await SHA256.HashDataAsync(stream, cancellationToken);
            return Convert.ToHexString(hash);
        }
    }
}


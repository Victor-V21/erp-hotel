using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Database.Entities;

namespace hotel_erp.Api.Database.Entities
{
    public class BackupLog : BaseEntity
    {
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public string LocalPath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string? Sha256Hash { get; set; }
        public BackupStatus Status { get; set; } = BackupStatus.CreatedLocal;
        public string? GoogleDriveFileId { get; set; }
        public DateTime? UploadedAt { get; set; }
        public int UploadAttempts { get; set; }
        public string? ErrorMessage { get; set; }
    }
}



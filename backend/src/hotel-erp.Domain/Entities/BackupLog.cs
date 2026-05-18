using hotel_erp.Domain.Common;
using hotel_erp.Domain.Enums;

namespace hotel_erp.Domain.Entities
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

namespace hotel_erp.Infrastructure.Services
{
    public class BackupOptions
    {
        public bool Enabled { get; set; }
        public string LocalPath { get; set; } = "C:\\HotelERP\\backups";
        public string PgDumpPath { get; set; } = "pg_dump";
        public string RunAt { get; set; } = "02:00";
        public int PendingUploadIntervalHours { get; set; } = 2;
        public int RetentionYears { get; set; } = 5;
        public string? GoogleDriveUploadCommand { get; set; }
    }
}

namespace hotel_erp.Api.Database.Entities
{
    public enum BackupStatus
    {
        CreatedLocal,
        PendingUpload,
        UploadedToDrive,
        UploadFailed,
        Failed
    }
}


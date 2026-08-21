namespace hotel_erp.Api.Database.Entities
{
    public class CorrelativeLock
    {
        public string LockName { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

namespace hotel_erp.Api.Database.Entities;

public sealed class IdempotencyRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public Guid ResourceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

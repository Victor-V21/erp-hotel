namespace hotel_erp.Application.DTOs
{
    public record GuestStatsDto
    {
        public int TotalVisits { get; set; }
        public string Classification { get; set; } = "Normal";
        public string? LastVisit { get; set; }
        public bool IsFrequent { get; set; }
    }
}

namespace hotel_erp.Api.Dtos.Dashboard
{
    public record DashboardStatDto
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Trend { get; set; }
    }

    public record DashboardAlertDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
    }

    public record DashboardInvoiceDto
    {
        public string CorrelativeNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
    }

    public record DashboardReservationDto
    {
        public string GuestName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
    }

    public record DashboardCashDto
    {
        public string RegisterName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }

    public record DashboardSummaryDto
    {
        public List<DashboardStatDto> Stats { get; set; } = new();
        public List<DashboardAlertDto> Alerts { get; set; } = new();
        public List<DashboardInvoiceDto> RecentInvoices { get; set; } = new();
        public List<DashboardReservationDto> UpcomingReservations { get; set; } = new();
        public DashboardCashDto? Cash { get; set; }
        public int OccupiedRooms { get; set; }
        public int FreeRooms { get; set; }
        public int PendingCheckIns { get; set; }
        public int PendingCheckOuts { get; set; }
    }
}

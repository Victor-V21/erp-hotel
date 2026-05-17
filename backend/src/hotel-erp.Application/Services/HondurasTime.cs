namespace hotel_erp.Application.Services
{
    public static class HondurasTime
    {
        private static readonly TimeSpan Offset = new(-6, 0, 0);

        public static DateTime Now => DateTime.UtcNow.Add(Offset);
        public static DateOnly Today => DateOnly.FromDateTime(Now);
        public static string ToLongString(DateTime dt) => dt.ToString("dd/MM/yyyy hh:mm tt");
    }
}

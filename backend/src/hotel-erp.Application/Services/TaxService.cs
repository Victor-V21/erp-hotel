namespace hotel_erp.Application.Services
{
    public class TaxService
    {
        public decimal IsvRate { get; set; } = 0.15m;
        public decimal TouristTaxRate { get; set; } = 0.04m;
        public decimal TaxFactor => 1m + IsvRate + TouristTaxRate;

        public TaxResult CalculateFromSellingPrice(decimal sellingPricePerNight, int nights)
        {
            var total = Math.Round(sellingPricePerNight * nights, 2);
            var subtotal = Math.Round(total / TaxFactor, 2);
            var isv = Math.Round(subtotal * IsvRate, 2);
            var touristTax = Math.Round(subtotal * TouristTaxRate, 2);
            var sum = subtotal + isv + touristTax;
            var diff = Math.Round(total - sum, 2);
            isv = Math.Round(isv + diff, 2);

            return new TaxResult
            {
                SellingPricePerNight = sellingPricePerNight,
                Nights = nights,
                Subtotal = subtotal,
                ISV = isv,
                TouristTax = touristTax,
                Total = total
            };
        }

        public TaxResult ApplyDiscount(TaxResult result, decimal discountPercentage)
        {
            if (discountPercentage <= 0) return result;

            var discountAmount = Math.Round(result.Subtotal * discountPercentage / 100m, 2);
            var newSubtotal = Math.Round(result.Subtotal - discountAmount, 2);
            var newIsv = Math.Round(newSubtotal * IsvRate, 2);
            var newTourist = Math.Round(newSubtotal * TouristTaxRate, 2);
            var newTotal = Math.Round(newSubtotal + newIsv + newTourist, 2);

            return new TaxResult
            {
                SellingPricePerNight = result.SellingPricePerNight,
                Nights = result.Nights,
                Subtotal = newSubtotal,
                ISV = newIsv,
                TouristTax = newTourist,
                Total = newTotal,
                DiscountPercentage = discountPercentage,
                DiscountAmount = discountAmount
            };
        }
    }

    public class TaxResult
    {
        public decimal SellingPricePerNight { get; set; }
        public int Nights { get; set; }
        public decimal Subtotal { get; set; }
        public decimal ISV { get; set; }
        public decimal TouristTax { get; set; }
        public decimal Total { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
    }
}

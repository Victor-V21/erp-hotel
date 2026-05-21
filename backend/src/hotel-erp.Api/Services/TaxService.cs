namespace hotel_erp.Api.Services
{
    public class TaxService
    {
        public decimal IsvRate { get; set; } = 0.15m;
        public decimal TouristTaxRate { get; set; } = 0.04m;
        public decimal TaxFactor => 1m + IsvRate + TouristTaxRate;

        public static decimal RoundCurrency(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

        public TaxResult CalculateFromNetAmount(decimal netAmount, bool isIsvExempt = false, bool isTouristTaxExempt = false, decimal discountPercentage = 0)
        {
            var subtotal = RoundCurrency(netAmount);
            var discountAmount = discountPercentage > 0
                ? RoundCurrency(subtotal * discountPercentage / 100m)
                : 0m;
            var taxableBase = RoundCurrency(subtotal - discountAmount);
            var isv = isIsvExempt ? 0m : RoundCurrency(taxableBase * IsvRate);
            var touristTax = isTouristTaxExempt ? 0m : RoundCurrency(taxableBase * TouristTaxRate);

            return new TaxResult
            {
                Subtotal = taxableBase,
                ISV = isv,
                TouristTax = touristTax,
                Total = RoundCurrency(taxableBase + isv + touristTax),
                DiscountPercentage = discountPercentage,
                DiscountAmount = discountAmount,
                TaxableAmount = isIsvExempt ? 0m : taxableBase,
                ExoneratedAmount = isIsvExempt ? taxableBase : 0m
            };
        }

        public TaxResult CalculateFromFinalPrice(decimal finalPrice, bool isIsvExempt = false, bool isTouristTaxExempt = false, decimal discountPercentage = 0)
        {
            var totalBeforeDiscount = RoundCurrency(finalPrice);
            var divisor = 1m + (isIsvExempt ? 0m : IsvRate) + (isTouristTaxExempt ? 0m : TouristTaxRate);
            var subtotalBeforeDiscount = RoundCurrency(totalBeforeDiscount / divisor);
            var discountAmount = discountPercentage > 0
                ? RoundCurrency(subtotalBeforeDiscount * discountPercentage / 100m)
                : 0m;
            var subtotal = RoundCurrency(subtotalBeforeDiscount - discountAmount);
            var isv = isIsvExempt ? 0m : RoundCurrency(subtotal * IsvRate);
            var touristTax = isTouristTaxExempt ? 0m : RoundCurrency(subtotal * TouristTaxRate);
            var total = RoundCurrency(subtotal + isv + touristTax);

            return new TaxResult
            {
                SellingPricePerNight = finalPrice,
                Subtotal = subtotal,
                ISV = isv,
                TouristTax = touristTax,
                Total = total,
                DiscountPercentage = discountPercentage,
                DiscountAmount = discountAmount,
                TaxableAmount = isIsvExempt ? 0m : subtotal,
                ExoneratedAmount = isIsvExempt ? subtotal : 0m
            };
        }

        public TaxResult CalculateFromSellingPrice(decimal sellingPricePerNight, int nights)
        {
            var total = RoundCurrency(sellingPricePerNight * nights);
            var subtotal = RoundCurrency(total / TaxFactor);
            var isv = RoundCurrency(subtotal * IsvRate);
            var touristTax = RoundCurrency(subtotal * TouristTaxRate);
            var sum = subtotal + isv + touristTax;
            var diff = RoundCurrency(total - sum);
            isv = RoundCurrency(isv + diff);

            return new TaxResult
            {
                SellingPricePerNight = sellingPricePerNight,
                Nights = nights,
                Subtotal = subtotal,
                ISV = isv,
                TouristTax = touristTax,
                Total = total,
                TaxableAmount = subtotal
            };
        }

        public TaxResult ApplyDiscount(TaxResult result, decimal discountPercentage)
        {
            if (discountPercentage <= 0) return result;

            var discountAmount = RoundCurrency(result.Subtotal * discountPercentage / 100m);
            var newSubtotal = RoundCurrency(result.Subtotal - discountAmount);
            var newIsv = RoundCurrency(newSubtotal * IsvRate);
            var newTourist = RoundCurrency(newSubtotal * TouristTaxRate);
            var newTotal = RoundCurrency(newSubtotal + newIsv + newTourist);

            return new TaxResult
            {
                SellingPricePerNight = result.SellingPricePerNight,
                Nights = result.Nights,
                Subtotal = newSubtotal,
                ISV = newIsv,
                TouristTax = newTourist,
                Total = newTotal,
                DiscountPercentage = discountPercentage,
                DiscountAmount = discountAmount,
                TaxableAmount = newSubtotal
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
        public decimal TaxableAmount { get; set; }
        public decimal ExemptAmount { get; set; }
        public decimal ExoneratedAmount { get; set; }
    }
}


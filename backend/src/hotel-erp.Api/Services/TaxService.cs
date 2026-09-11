using hotel_erp.Api.Database;
using Microsoft.EntityFrameworkCore;

namespace hotel_erp.Api.Services
{
    public class TaxService
    {
        private readonly ApplicationDbContext? _context;
        private bool _loaded;

        public decimal IsvRate { get; set; } = 0.15m;
        public decimal TouristTaxRate { get; set; } = 0.04m;
        public decimal TaxFactor => 1m + IsvRate + TouristTaxRate;

        public TaxService() { }

        public TaxService(ApplicationDbContext context)
        {
            _context = context;
            LoadRates();
        }

        private void LoadRates()
        {
            if (_loaded || _context == null) return;
            var settings = _context.BusinessSettings
                .AsNoTracking()
                .OrderBy(setting => setting.CreatedAt)
                .ThenBy(setting => setting.Id)
                .FirstOrDefault();
            if (settings != null)
            {
                IsvRate = settings.IsvRate;
                TouristTaxRate = settings.TouristTaxRate;
            }
            _loaded = true;
        }

        public static decimal RoundCurrency(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

        public TaxResult CalculateFromNetAmount(decimal netAmount, bool isIsvExempt = false, bool isTouristTaxExempt = false, decimal discountPercentage = 0)
        {
            ValidateAmount(netAmount, nameof(netAmount));
            ValidateDiscount(discountPercentage);
            ValidateRates();
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
            ValidateAmount(finalPrice, nameof(finalPrice));
            ValidateDiscount(discountPercentage);
            ValidateRates();
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
            ValidateAmount(sellingPricePerNight, nameof(sellingPricePerNight));
            if (nights <= 0)
                throw new ArgumentOutOfRangeException(nameof(nights), "La cantidad de noches debe ser mayor que cero.");
            ValidateRates();
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
            ArgumentNullException.ThrowIfNull(result);
            ValidateDiscount(discountPercentage);
            ValidateRates();
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

        private void ValidateRates()
        {
            if (IsvRate is < 0m or > 1m)
                throw new InvalidOperationException("La tasa ISV debe estar entre 0 y 1.");
            if (TouristTaxRate is < 0m or > 1m)
                throw new InvalidOperationException("La tasa turística debe estar entre 0 y 1.");
        }

        private static void ValidateAmount(decimal amount, string parameterName)
        {
            if (amount < 0m)
                throw new ArgumentOutOfRangeException(parameterName, "El monto no puede ser negativo.");
        }

        private static void ValidateDiscount(decimal discountPercentage)
        {
            if (discountPercentage is < 0m or > 100m)
                throw new ArgumentOutOfRangeException(nameof(discountPercentage), "El descuento debe estar entre 0 y 100.");
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

using hotel_erp.Api.Database.Entities;

namespace hotel_erp.Api.Services;

public sealed record InvoiceLineInput(
    string Description,
    int Quantity,
    decimal UnitPrice,
    bool IsExempt,
    decimal ISVRate,
    bool IsTouristTaxable,
    decimal DiscountPercentage,
    Guid? OriginalInvoiceItemId = null);

public sealed record InvoiceCalculation(
    IReadOnlyList<InvoiceItem> Items,
    decimal SubTotal,
    decimal ISVAmount,
    decimal ISV15Amount,
    decimal ISV18Amount,
    decimal TouristTaxAmount,
    decimal DiscountsAmount,
    decimal TotalAmount,
    decimal TaxableAmount,
    decimal ExemptAmount,
    decimal ExoneratedAmount);

public static class InvoiceCalculationService
{
    private static readonly decimal[] SupportedIsvRates = [0m, 0.15m, 0.18m];

    public static InvoiceCalculation Calculate(
        IEnumerable<InvoiceLineInput> source,
        bool isIsvExempt = false,
        bool isTouristTaxExempt = false)
    {
        ArgumentNullException.ThrowIfNull(source);
        var lines = source.ToList();
        if (lines.Count == 0)
            throw new InvoiceCalculationException("El documento debe contener al menos una línea.");

        var items = new List<InvoiceItem>(lines.Count);
        decimal subTotal = 0m;
        decimal isv15 = 0m;
        decimal isv18 = 0m;
        decimal touristTax = 0m;
        decimal discounts = 0m;
        decimal taxable = 0m;
        decimal exempt = 0m;
        decimal exonerated = 0m;

        foreach (var line in lines)
        {
            Validate(line);
            var gross = TaxService.RoundCurrency(line.Quantity * line.UnitPrice);
            var discount = TaxService.RoundCurrency(gross * line.DiscountPercentage / 100m);
            var net = TaxService.RoundCurrency(gross - discount);
            var effectiveRate = line.IsExempt || isIsvExempt ? 0m : line.ISVRate;
            var lineIsv = TaxService.RoundCurrency(net * effectiveRate);
            var lineTouristTax = line.IsTouristTaxable && !isTouristTaxExempt
                ? TaxService.RoundCurrency(net * 0.04m)
                : 0m;

            subTotal += net;
            discounts += discount;
            touristTax += lineTouristTax;
            if (effectiveRate == 0.15m) isv15 += lineIsv;
            if (effectiveRate == 0.18m) isv18 += lineIsv;

            if (line.IsExempt)
                exempt += net;
            else if (isIsvExempt)
                exonerated += net;
            else
                taxable += net;

            items.Add(new InvoiceItem
            {
                Description = line.Description.Trim(),
                Quantity = line.Quantity,
                UnitPrice = TaxService.RoundCurrency(line.UnitPrice),
                LineTotal = gross,
                IsExempt = line.IsExempt,
                ISVRate = line.ISVRate,
                IsTouristTaxable = line.IsTouristTaxable,
                DiscountPercentage = line.DiscountPercentage,
                OriginalInvoiceItemId = line.OriginalInvoiceItemId
            });
        }

        subTotal = TaxService.RoundCurrency(subTotal);
        isv15 = TaxService.RoundCurrency(isv15);
        isv18 = TaxService.RoundCurrency(isv18);
        touristTax = TaxService.RoundCurrency(touristTax);
        discounts = TaxService.RoundCurrency(discounts);
        var isv = TaxService.RoundCurrency(isv15 + isv18);
        var total = TaxService.RoundCurrency(subTotal + isv + touristTax);
        if (total <= 0m)
            throw new InvoiceCalculationException("El total del documento debe ser mayor que cero.");

        return new InvoiceCalculation(
            items,
            subTotal,
            isv,
            isv15,
            isv18,
            touristTax,
            discounts,
            total,
            TaxService.RoundCurrency(taxable),
            TaxService.RoundCurrency(exempt),
            TaxService.RoundCurrency(exonerated));
    }

    private static void Validate(InvoiceLineInput line)
    {
        if (string.IsNullOrWhiteSpace(line.Description) || line.Description.Trim().Length > 300)
            throw new InvoiceCalculationException("Cada línea requiere una descripción de hasta 300 caracteres.");
        if (line.Quantity <= 0)
            throw new InvoiceCalculationException("La cantidad de cada línea debe ser mayor que cero.");
        if (line.UnitPrice < 0m || line.UnitPrice > 999_999_999m)
            throw new InvoiceCalculationException("El precio unitario debe estar entre 0 y 999,999,999.");
        if (line.DiscountPercentage is < 0m or > 100m)
            throw new InvoiceCalculationException("El descuento debe estar entre 0 y 100 por ciento.");
        if (!SupportedIsvRates.Contains(line.ISVRate))
            throw new InvoiceCalculationException("La tasa ISV de la línea debe ser 0%, 15% o 18%.");
        if (!line.IsExempt && line.ISVRate == 0m)
            throw new InvoiceCalculationException("Una línea gravada debe indicar una tasa ISV de 15% o 18%.");
    }
}

public sealed class InvoiceCalculationException(string message) : InvalidOperationException(message);

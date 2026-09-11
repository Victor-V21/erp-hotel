using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Services.Interfaces;

namespace hotel_erp.Api.Services;

public sealed class FiscalProfileService(IBusinessSettingsRepository settingsRepository)
{
    public async Task<string?> GetOperationBlockReasonAsync()
    {
        var settings = await settingsRepository.GetAsync();
        if (settings is null || settings.FiscalProfileStatus != FiscalProfileStatus.Aprobado)
            return "El perfil fiscal está en borrador o retirado y todavía no permite emitir documentos fiscales.";

        var today = HondurasTime.Today;
        if (!settings.FiscalValidFrom.HasValue || settings.FiscalValidFrom.Value > today)
            return "El perfil fiscal aprobado aún no está vigente.";

        if (settings.FiscalValidUntil.HasValue && settings.FiscalValidUntil.Value < today)
            return "La vigencia del perfil fiscal aprobado terminó.";

        return null;
    }

    public static IReadOnlyList<string> ValidateForApproval(BusinessSettings settings)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(settings.BusinessName))
            errors.Add("Debe registrar la razón social o nombre del negocio.");
        if (settings.RTN.Length != 14 || !settings.RTN.All(char.IsDigit))
            errors.Add("Debe registrar un RTN de 14 dígitos.");
        if (string.IsNullOrWhiteSpace(settings.Address))
            errors.Add("Debe registrar la dirección del establecimiento.");
        if (settings.IsvRate < 0 || settings.IsvRate > 1)
            errors.Add("La tasa ISV debe estar entre 0 y 1.");
        if (settings.TouristTaxRate < 0 || settings.TouristTaxRate > 1)
            errors.Add("La tasa turística debe estar entre 0 y 1.");
        return errors;
    }
}

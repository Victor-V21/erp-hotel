using System.ComponentModel.DataAnnotations;

namespace hotel_erp.Api.Dtos.Common
{
    public sealed class NotEmptyGuidAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            return value is Guid guid && guid != Guid.Empty;
        }
    }
}


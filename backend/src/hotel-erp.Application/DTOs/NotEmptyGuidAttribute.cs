using System.ComponentModel.DataAnnotations;

namespace hotel_erp.Application.DTOs
{
    public sealed class NotEmptyGuidAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            return value is Guid guid && guid != Guid.Empty;
        }
    }
}

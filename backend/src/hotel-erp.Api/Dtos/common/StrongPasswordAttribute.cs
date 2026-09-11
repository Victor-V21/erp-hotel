using System.ComponentModel.DataAnnotations;

namespace hotel_erp.Api.Dtos.Common
{
    public sealed class StrongPasswordAttribute : ValidationAttribute
    {
        public StrongPasswordAttribute()
        {
            ErrorMessage = "La contraseña debe incluir mayúscula, minúscula y número";
        }

        public override bool IsValid(object? value)
        {
            if (value is null)
                return true;

            return value is string password
                && password.Any(char.IsUpper)
                && password.Any(char.IsLower)
                && password.Any(char.IsDigit);
        }
    }
}

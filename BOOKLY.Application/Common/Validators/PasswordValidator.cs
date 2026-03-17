using BOOKLY.Application.Common.Models;
using BOOKLY.Domain;
using BOOKLY.Domain.Exceptions;

namespace BOOKLY.Application.Common.Validators
{
    public static class PasswordValidator
    {
        public static Result Validate(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                throw new DomainException("La contraseña es requerida");

            if (plainText.Length < 8)
                throw new DomainException("La contraseña debe tener al menos 8 caracteres.");

            if (plainText.Length > 128)
                throw new DomainException("La contraseña no puede exceder los 128 caracteres.");

            if (!plainText.Any(char.IsDigit))
                throw new DomainException("La contraseña debe contener al menos un número.");

            if (!plainText.Any(char.IsUpper))
                throw new DomainException("La contraseña debe contener al menos una mayúscula.");

            return Result.Success();
        }
    }
}

using BOOKLY.Application.Common.Models;

namespace BOOKLY.Application.Common.Validators
{
    public static class PasswordValidator
    {
        public static Result Validate(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                return Result.Failure(Error.Validation("La contraseña es requerida"));

            if (plainText.Length < 8)
                return Result.Failure(Error.Validation("La contraseña debe tener al menos 8 caracteres."));

            if (plainText.Length > 128)
                return Result.Failure(Error.Validation("La contraseña no puede exceder los 128 caracteres."));

            if (!plainText.Any(char.IsDigit))
                return Result.Failure(Error.Validation("La contraseña debe contener al menos un número."));

            if (!plainText.Any(char.IsUpper))
                return Result.Failure(Error.Validation("La contraseña debe contener al menos una mayúscula."));

            return Result.Success();
        }
    }
}

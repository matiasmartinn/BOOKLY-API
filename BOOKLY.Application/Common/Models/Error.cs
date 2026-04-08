namespace BOOKLY.Application.Common.Models
{
    public enum ErrorType
    {
        Validation,
        NotFound,
        Conflict,
        Domain,
        Unauthorized,
        Forbidden,
        Unexpected
    }
    public sealed record Error(ErrorType Type, string Message, string? Code = null)
    {

        public static readonly Error None = new(ErrorType.Validation, string.Empty);
        public bool IsNone => this == None;
        public static Error Validation(string message, string? code = null) => new(ErrorType.Validation, message, code);
        public static Error NotFound(string resource, string? code = null) => new(ErrorType.NotFound, $"{resource} no encontrado", code);
        public static Error Conflict(string message, string? code = null) => new(ErrorType.Conflict, message, code);
        public static Error Domain(string message, string? code = null) => new(ErrorType.Domain, message, code);
        public static Error Unexpected(string message = "Error inesperado", string? code = null) => new(ErrorType.Unexpected, message, code);
        public static Error Unauthorized(string message, string? code = null) => new(ErrorType.Unauthorized, message, code);
        public static Error Forbidden(string message, string? code = null) => new(ErrorType.Forbidden, message, code);
    }
}

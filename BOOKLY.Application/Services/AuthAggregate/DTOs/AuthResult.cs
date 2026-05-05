namespace BOOKLY.Application.Services.AuthAggregate.DTOs
{
    public sealed record AuthResult(
        LoginResponse Response,
        string RefreshToken);
}

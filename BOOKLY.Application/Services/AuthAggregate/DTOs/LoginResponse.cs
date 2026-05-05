namespace BOOKLY.Application.Services.AuthAggregate.DTOs
{
    public sealed record LoginResponse(
        string AccessToken,
        int UserId,
        string Email,
        string Role,
        string FullName);
}

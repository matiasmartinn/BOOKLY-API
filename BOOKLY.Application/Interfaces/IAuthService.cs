using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.AuthAggregate.DTOs;

namespace BOOKLY.Application.Interfaces
{
    public interface IAuthService
    {
        Task<Result<AuthResult>> Login(LoginRequest request, CancellationToken ct = default);
        Task<Result<AuthResult>> Refresh(string refreshToken, CancellationToken ct = default);
        Task<Result> Logout(string refreshToken, CancellationToken ct = default);
    }
}

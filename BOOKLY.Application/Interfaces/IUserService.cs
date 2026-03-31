using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.UserAggregate;
using BOOKLY.Application.Services.UserAggregate.DTOs;

namespace BOOKLY.Application.Interfaces
{
    public interface IUserService
    {
        Task<Result<UserDto>> GetUserById(int id, CancellationToken ct = default);
        Task<Result<UserDto>> Login(LoginDto dto, CancellationToken ct = default);
        Task<Result<UserDto>> RegisterOwner(CreateUserDto dto, CancellationToken ct = default);
        Task<Result> ConfirmEmail(ConfirmEmailDto dto, CancellationToken ct = default);
        Task<Result> ResendEmailConfirmation(ResendEmailConfirmationDto dto, CancellationToken ct = default);
        Task<Result> RequestPasswordReset(RequestPasswordResetDto dto, CancellationToken ct = default);
        Task<Result> ResetPassword(ResetPasswordDto dto, CancellationToken ct = default);
        Task<Result<IReadOnlyCollection<SecretaryDto>>> GetSecretariesByOwner(int ownerId, CancellationToken ct = default);
        Task<Result<UserDto>> CreateSecretary(int ownerId, CreateSecretaryDto dto, CancellationToken ct = default);
        Task<Result<UserDto>> CompleteInvitation(CompleteSecretaryInvitationDto dto, CancellationToken ct = default);
        Task<Result> ActivateSecretary(int id, CancellationToken ct = default);
        Task<Result> DeactivateSecretary(int id, CancellationToken ct = default);
        Task<Result<UserDto>> UpdateUser(int id, UpdateUserDto dto, CancellationToken ct = default);
        Task<Result> DeleteUser(int id, CancellationToken ct = default);
    }
}

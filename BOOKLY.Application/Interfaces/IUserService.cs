using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.UserAggregate;
using BOOKLY.Application.Services.UserAggregate.DTOs;

namespace BOOKLY.Application.Interfaces
{
    public interface IUserService
    {
        Task<Result<UserDto>> GetUserById(int id, CancellationToken ct = default);
        Task<Result<RegisterOwnerResultDto>> RegisterOwner(CreateUserDto dto, CancellationToken ct = default);
        Task<Result> ConfirmEmail(ConfirmEmailDto dto, CancellationToken ct = default);
        Task<Result<EmailDispatchResultDto>> ResendEmailConfirmation(ResendEmailConfirmationDto dto, CancellationToken ct = default);
        Task<Result> RequestPasswordReset(RequestPasswordResetDto dto, CancellationToken ct = default);
        Task<Result> ResetPassword(ResetPasswordDto dto, CancellationToken ct = default);
        Task<Result<UserEmailDispatchResultDto>> InviteAdmin(InviteAdminDto dto, CancellationToken ct = default);
        Task<Result<UserDto>> CompleteAdminInvitation(CompleteAdminInvitationDto dto, CancellationToken ct = default);
        Task<Result<IReadOnlyCollection<SecretaryDto>>> GetSecretariesByOwner(int ownerId, CancellationToken ct = default);
        Task<Result<UserDto>> GetOwnerSecretaryById(int ownerId, int secretaryId, CancellationToken ct = default);
        Task<Result<UserEmailDispatchResultDto>> CreateSecretary(int ownerId, CreateSecretaryDto dto, CancellationToken ct = default);
        Task<Result<UserDto>> CompleteInvitation(CompleteSecretaryInvitationDto dto, CancellationToken ct = default);
        Task<Result> ActivateSecretary(int id, int? ownerId = null, CancellationToken ct = default);
        Task<Result> DeactivateSecretary(int id, int? ownerId = null, CancellationToken ct = default);
        Task<Result<UserDto>> UpdateOwnerSecretary(int ownerId, int secretaryId, UpdateUserDto dto, CancellationToken ct = default);
        Task<Result<UserDto>> UpdateUser(int id, UpdateUserDto dto, CancellationToken ct = default);
        Task<Result> DeleteUser(int id, CancellationToken ct = default);
    }
}

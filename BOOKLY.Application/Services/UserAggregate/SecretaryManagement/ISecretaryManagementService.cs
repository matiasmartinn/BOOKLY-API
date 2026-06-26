using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.UserAggregate.DTOs;

namespace BOOKLY.Application.Services.UserAggregate.SecretaryManagement;

public interface ISecretaryManagementService
{
    Task<Result<UserEmailDispatchResultDto>> CreateSecretary(int ownerId, CreateSecretaryDto dto, CancellationToken ct = default);
    Task<Result<IReadOnlyCollection<SecretaryDto>>> GetSecretariesByOwner(int ownerId, CancellationToken ct = default);
    Task<Result<UserDto>> GetOwnerSecretaryById(int ownerId, int secretaryId, CancellationToken ct = default);
    Task<Result> ActivateSecretary(int id, int? ownerId = null, CancellationToken ct = default);
    Task<Result> DeactivateSecretary(int id, int? ownerId = null, CancellationToken ct = default);
    Task<Result<UserDto>> UpdateOwnerSecretary(int ownerId, int secretaryId, UpdateUserDto dto, CancellationToken ct = default);
}

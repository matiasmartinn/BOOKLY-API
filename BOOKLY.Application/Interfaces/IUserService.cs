using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.UserAggregate.DTOs;

namespace BOOKLY.Application.Interfaces
{
    public interface IUserService
    {
        Task<Result<UserDto>> GetUserById(int id, CancellationToken ct = default);
        Task<Result<UserDto>> Login(LoginDto dto, CancellationToken ct = default);
        Task<Result<UserDto>> RegisterOwner(CreateUserDto dto, CancellationToken ct = default);
        Task<Result<UserDto>> UpdateUser(int id, UpdateUserDto dto, CancellationToken ct = default);
        Task<Result> DeleteUser(int id, CancellationToken ct = default);

    }
}

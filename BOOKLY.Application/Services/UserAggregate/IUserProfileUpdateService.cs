using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.UserAggregate.DTOs;
using BOOKLY.Domain.Aggregates.UserAggregate;

namespace BOOKLY.Application.Services.UserAggregate
{
    public interface IUserProfileUpdateService
    {
        Task<Result<UserDto>> UpdateProfile(User user, UpdateUserDto dto, CancellationToken ct = default);
    }
}

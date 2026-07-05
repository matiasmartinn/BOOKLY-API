using BOOKLY.Application.Services.UserAggregate.DTOs;
using BOOKLY.Domain.Aggregates.UserAggregate;

namespace BOOKLY.Application.Services.UserAggregate
{
    public interface IUserDtoMapper
    {
        Task<UserDto> Map(User user, CancellationToken ct = default);
    }
}

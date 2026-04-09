using BOOKLY.Domain.Aggregates.UserAggregate;

namespace BOOKLY.Application.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(User user);
    }
}

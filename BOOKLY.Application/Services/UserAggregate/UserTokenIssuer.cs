using BOOKLY.Application.Interfaces;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;
using BOOKLY.Domain.Interfaces;

namespace BOOKLY.Application.Services.UserAggregate
{
    public sealed class UserTokenIssuer : IUserTokenIssuer
    {
        private readonly IUserTokenRepository _userTokenRepository;
        private readonly IInvitationTokenGenerator _invitationTokenGenerator;
        private readonly ITokenHashingService _tokenHashingService;
        private readonly IDateTimeProvider _dateTimeProvider;

        public UserTokenIssuer(
            IUserTokenRepository userTokenRepository,
            IInvitationTokenGenerator invitationTokenGenerator,
            ITokenHashingService tokenHashingService,
            IDateTimeProvider dateTimeProvider)
        {
            _userTokenRepository = userTokenRepository;
            _invitationTokenGenerator = invitationTokenGenerator;
            _tokenHashingService = tokenHashingService;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<IssuedUserToken> CreateToken(int userId, UserTokenPurpose purpose, CancellationToken ct = default)
        {
            var ttl = GetTtl(purpose);
            var rawToken = _invitationTokenGenerator.GenerateToken();
            var token = UserToken.Create(
                userId,
                purpose,
                _tokenHashingService.HashToken(rawToken),
                _dateTimeProvider.UtcNow(),
                ttl);

            await _userTokenRepository.AddOne(token, ct);
            return new IssuedUserToken(rawToken, ttl);
        }

        private static TimeSpan GetTtl(UserTokenPurpose purpose) => purpose switch
        {
            UserTokenPurpose.PasswordReset => TimeSpan.FromHours(2),
            UserTokenPurpose.EmailConfirmation => TimeSpan.FromHours(24),
            UserTokenPurpose.SecretaryInvitation => TimeSpan.FromHours(24),
            UserTokenPurpose.AdminInvitation => TimeSpan.FromHours(24),
            _ => TimeSpan.FromHours(24)
        };
    }
}

using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Common.Security;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.AuthAggregate.DTOs;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Exceptions;
using BOOKLY.Domain.Interfaces;

namespace BOOKLY.Application.Services.AuthAggregate
{
    public sealed class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ITokenHashingService _tokenHashingService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IDateTimeProvider dateTimeProvider,
            ITokenHashingService tokenHashingService,
            IJwtTokenService jwtTokenService,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _dateTimeProvider = dateTimeProvider;
            _tokenHashingService = tokenHashingService;
            _jwtTokenService = jwtTokenService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<LoginResponse>> Login(LoginRequest request, CancellationToken ct = default)
        {
            var user = await _userRepository.GetByEmail(request.Email, ct);
            if (user is null || !user.VerifyPassword(request.Password, _passwordHasher))
                return Result<LoginResponse>.Failure(Error.Unauthorized("Credenciales invalidas."));

            try
            {
                user.RegisterLogin(_dateTimeProvider.NowArgentina());
            }
            catch (DomainException ex)
            {
                return Result<LoginResponse>.Failure(Error.Unauthorized(ex.Message));
            }

            var now = _dateTimeProvider.UtcNow();
            var rawRefreshToken = RefreshToken.GenerateToken();
            var refreshToken = RefreshToken.Create(user.Id, _tokenHashingService.HashToken(rawRefreshToken), now);
            var accessToken = _jwtTokenService.GenerateAccessToken(user);

            _userRepository.Update(user);
            await _userRepository.AddRefreshToken(refreshToken, ct);
            await _unitOfWork.SaveChanges(ct);

            return Result<LoginResponse>.Success(
                CreateLoginResponse(user, accessToken, rawRefreshToken));
        }

        public async Task<Result<LoginResponse>> Refresh(RefreshRequest request, CancellationToken ct = default)
        {
            var now = _dateTimeProvider.UtcNow();
            var refreshTokenHash = _tokenHashingService.HashToken(request.RefreshToken);
            var storedRefreshToken = await _userRepository.GetRefreshToken(refreshTokenHash, request.RefreshToken, ct);

            if (storedRefreshToken is null || !storedRefreshToken.IsValid(now))
                return Result<LoginResponse>.Failure(Error.Unauthorized("Refresh token invÃ¡lido o vencido."));

            var user = await _userRepository.GetById(storedRefreshToken.UserId, ct);
            if (user is null)
                return Result<LoginResponse>.Failure(Error.Unauthorized("Refresh token invÃ¡lido."));

            try
            {
                user.EnsureCanLogin();
            }
            catch (DomainException ex)
            {
                storedRefreshToken.Revoke();
                await _unitOfWork.SaveChanges(ct);
                return Result<LoginResponse>.Failure(Error.Unauthorized(ex.Message));
            }

            var rawRefreshToken = RefreshToken.GenerateToken();
            var newRefreshToken = RefreshToken.Create(user.Id, _tokenHashingService.HashToken(rawRefreshToken), now);
            var accessToken = _jwtTokenService.GenerateAccessToken(user);

            storedRefreshToken.Revoke();
            await _userRepository.AddRefreshToken(newRefreshToken, ct);
            await _unitOfWork.SaveChanges(ct);

            return Result<LoginResponse>.Success(
                CreateLoginResponse(user, accessToken, rawRefreshToken));
        }

        public async Task<Result> Logout(string refreshToken, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return Result.Failure(Error.Unauthorized("El refresh token es requerido."));

            var refreshTokenHash = _tokenHashingService.HashToken(refreshToken);
            var storedRefreshToken = await _userRepository.GetRefreshToken(refreshTokenHash, refreshToken, ct);
            if (storedRefreshToken is null || !storedRefreshToken.IsValid(_dateTimeProvider.UtcNow()))
            {
                return Result.Failure(Error.Unauthorized("Refresh token invÃ¡lido o vencido."));
            }

            storedRefreshToken.Revoke();
            await _unitOfWork.SaveChanges(ct);

            return Result.Success();
        }

        private static LoginResponse CreateLoginResponse(User user, string accessToken, string refreshToken)
            => new(
                accessToken,
                refreshToken,
                user.Id,
                user.Email.Value,
                Roles.From(user.Role),
                user.PersonName.FullName);
    }
}

using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.UserAggregate.DTOs;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;
using BOOKLY.Domain.Aggregates.UserAggregate.ValueObjects;
using BOOKLY.Domain.Emailing;
using BOOKLY.Domain.Exceptions;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.SharedKernel;

namespace BOOKLY.Application.Services.UserAggregate
{
    public sealed class UserProfileUpdateService : IUserProfileUpdateService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserTokenIssuer _userTokenIssuer;
        private readonly IUserDtoMapper _userDtoMapper;
        private readonly ISafeEmailDispatcher _safeEmailDispatcher;
        private readonly IEmailService _emailService;

        public UserProfileUpdateService(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IUserTokenIssuer userTokenIssuer,
            IUserDtoMapper userDtoMapper,
            ISafeEmailDispatcher safeEmailDispatcher,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _userTokenIssuer = userTokenIssuer;
            _userDtoMapper = userDtoMapper;
            _safeEmailDispatcher = safeEmailDispatcher;
            _emailService = emailService;
        }

        public async Task<Result<UserDto>> UpdateProfile(User user, UpdateUserDto dto, CancellationToken ct = default)
        {
            Email newEmail;
            try
            {
                newEmail = Email.Create(dto.Email);
            }
            catch (DomainException ex)
            {
                return Result<UserDto>.Failure(Error.Validation(ex.Message));
            }

            var emailChanged = !string.Equals(user.Email.Value, newEmail.Value, StringComparison.OrdinalIgnoreCase);
            if (emailChanged)
            {
                var existingUser = await _userRepository.GetByEmail(newEmail.Value, ct);
                if (existingUser is not null && existingUser.Id != user.Id)
                    return Result<UserDto>.Failure(Error.Conflict("Email ya está registrado."));
            }

            try
            {
                user.ChangeUserName(PersonName.Create(dto.FirstName, dto.LastName));
                user.ChangeEmail(newEmail);
            }
            catch (DomainException ex)
            {
                return Result<UserDto>.Failure(Error.Validation(ex.Message));
            }

            _userRepository.Update(user);

            // El usuario ya tiene Id: el token entra en el mismo SaveChanges y la operación queda atómica.
            IssuedUserToken? token = null;
            if (emailChanged && user.Role != UserRole.Admin)
                token = await _userTokenIssuer.CreateToken(user.Id, UserTokenPurpose.EmailConfirmation, ct);

            await _unitOfWork.SaveChanges(ct);

            if (token is not null)
            {
                await _safeEmailDispatcher.TrySend(
                    () => _emailService.SendEmailConfirmation(
                        new EmailConfirmationEmailModel(
                            user.Email.Value,
                            user.PersonName.FirstName,
                            token.RawToken,
                            token.TtlHours),
                        ct),
                    "confirmación de cambio de email",
                    user.Email.Value);
            }

            return Result<UserDto>.Success(await _userDtoMapper.Map(user, ct));
        }
    }
}

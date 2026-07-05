using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Common.Validators;
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
    public partial class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUserTokenRepository _userTokenRepository;
        private readonly ITokenHashingService _tokenHashingService;
        private readonly IUserTokenIssuer _userTokenIssuer;
        private readonly IUserProfileUpdateService _userProfileUpdateService;
        private readonly IUserDtoMapper _userDtoMapper;
        private readonly ISafeEmailDispatcher _safeEmailDispatcher;
        private readonly IEmailService _emailService;
        private readonly IDateTimeProvider _dateTimeProvider;

        public UserService(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            ITokenHashingService tokenHashingService,
            IUserTokenRepository userTokenRepository,
            IUserTokenIssuer userTokenIssuer,
            IUserProfileUpdateService userProfileUpdateService,
            IUserDtoMapper userDtoMapper,
            ISafeEmailDispatcher safeEmailDispatcher,
            IEmailService emailService,
            IDateTimeProvider dateTimeProvider)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _tokenHashingService = tokenHashingService;
            _userTokenRepository = userTokenRepository;
            _userTokenIssuer = userTokenIssuer;
            _userProfileUpdateService = userProfileUpdateService;
            _userDtoMapper = userDtoMapper;
            _safeEmailDispatcher = safeEmailDispatcher;
            _emailService = emailService;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<UserDto>> GetUserById(int id, CancellationToken ct = default)
        {
            if (id <= 0)
                return Result<UserDto>.Failure(Error.Validation("Id inválido"));

            var user = await _userRepository.GetOne(id, ct);
            if (user is null)
                return Result<UserDto>.Failure(Error.NotFound("Usuario"));

            return Result<UserDto>.Success(await _userDtoMapper.Map(user, ct));
        }

        public async Task<Result<RegisterOwnerResultDto>> RegisterOwner(CreateUserDto dto, CancellationToken ct = default)
        {
            if (await _userRepository.ExistsByEmail(dto.Email, ct))
                return Result<RegisterOwnerResultDto>.Failure(Error.Conflict("Email ya está registrado."));

            var passwordValidation = PasswordValidator.Validate(dto.Password);
            if (!passwordValidation.IsSuccess)
                return Result<RegisterOwnerResultDto>.Failure(passwordValidation.Error!);

            User user;
            try
            {
                user = User.CreateOwner(
                    PersonName.Create(dto.FirstName, dto.LastName),
                    Email.Create(dto.Email),
                    Password.FromHash(_passwordHasher.Hash(dto.Password)),
                    _dateTimeProvider.NowArgentina());
            }
            catch (DomainException ex)
            {
                return Result<RegisterOwnerResultDto>.Failure(Error.Validation(ex.Message));
            }

            // Transacción única: si falla la emisión del token, el usuario tampoco queda persistido.
            var token = await _unitOfWork.ExecuteInTransaction(async () =>
            {
                await _userRepository.AddOne(user, ct);
                await _unitOfWork.SaveChanges(ct);

                var issuedToken = await _userTokenIssuer.CreateToken(user.Id, UserTokenPurpose.EmailConfirmation, ct);
                await _unitOfWork.SaveChanges(ct);
                return issuedToken;
            }, ct);

            var emailDispatch = await _safeEmailDispatcher.TrySendCritical(
                () => _emailService.SendEmailConfirmation(
                    new EmailConfirmationEmailModel(
                        user.Email.Value,
                        user.PersonName.FirstName,
                        token.RawToken,
                        token.TtlHours),
                    ct),
                "confirmación de registro",
                user.Email.Value,
                "Cuenta creada correctamente. Revisa tu correo para confirmar tu cuenta.",
                "Tu cuenta fue creada, pero no pudimos enviar el email de confirmacion. Podes reenviarlo desde la opcion \"Reenviar confirmacion\".");

            return Result<RegisterOwnerResultDto>.Success(
                new RegisterOwnerResultDto(
                    await _userDtoMapper.Map(user, ct),
                    emailDispatch));
        }

        public async Task<Result> ConfirmEmail(ConfirmEmailDto dto, CancellationToken ct = default)
        {
            var tokenResult = await GetValidToken(dto.Token, UserTokenPurpose.EmailConfirmation, "confirmación de email", ct);
            if (tokenResult.Error is not null)
                return tokenResult.Error;

            var token = tokenResult.Token!;
            var user = tokenResult.User!;

            if (token.IsUsed && user.EmailConfirmed)
                return Result.Success();

            try
            {
                user.ConfirmEmail();
                token.MarkAsUsed(_dateTimeProvider.UtcNow());
            }
            catch (DomainException ex)
            {
                return Result.Failure(Error.Validation(ex.Message));
            }

            _userTokenRepository.Update(token);
            _userRepository.Update(user);
            await _unitOfWork.SaveChanges(ct);

            return Result.Success();
        }

        public async Task<Result<EmailDispatchResultDto>> ResendEmailConfirmation(ResendEmailConfirmationDto dto, CancellationToken ct = default)
        {
            var user = await _userRepository.GetByEmail(dto.Email, ct);
            if (user is null || user.Status != UserStatus.PendingEmailConfirmation)
            {
                return Result<EmailDispatchResultDto>.Success(
                    new EmailDispatchResultDto(
                        false,
                        "No encontramos una cuenta pendiente de confirmacion para ese email."));
            }

            var token = await _userTokenIssuer.CreateToken(user.Id, UserTokenPurpose.EmailConfirmation, ct);
            await _unitOfWork.SaveChanges(ct);

            var emailDispatch = await _safeEmailDispatcher.TrySendCritical(
                () => _emailService.SendEmailConfirmation(
                    new EmailConfirmationEmailModel(
                        user.Email.Value,
                        user.PersonName.FirstName,
                        token.RawToken,
                        token.TtlHours),
                    ct),
                "reenvío de confirmación de email",
                user.Email.Value,
                "Te enviamos un nuevo email de confirmacion.",
                "No pudimos enviar el email de confirmacion.");

            return Result<EmailDispatchResultDto>.Success(emailDispatch);
        }

        public async Task<Result> RequestPasswordReset(RequestPasswordResetDto dto, CancellationToken ct = default)
        {
            var user = await _userRepository.GetByEmail(dto.Email, ct);
            if (user is null)
                return Result.Success();

            try
            {
                user.EnsureCanLogin();
            }
            catch (DomainException)
            {
                return Result.Success();
            }

            var token = await _userTokenIssuer.CreateToken(user.Id, UserTokenPurpose.PasswordReset, ct);
            await _unitOfWork.SaveChanges(ct);

            await _safeEmailDispatcher.TrySend(
                () => _emailService.SendPasswordReset(
                    new PasswordResetEmailModel(
                        user.Email.Value,
                        user.PersonName.FirstName,
                        token.RawToken,
                        token.TtlHours),
                    ct),
                "recuperación de contraseña",
                user.Email.Value);

            return Result.Success();
        }

        public async Task<Result> ResetPassword(ResetPasswordDto dto, CancellationToken ct = default)
        {
            var passwordValidation = PasswordValidator.Validate(dto.Password);
            if (!passwordValidation.IsSuccess)
                return Result.Failure(passwordValidation.Error!);

            var tokenResult = await GetValidToken(dto.Token, UserTokenPurpose.PasswordReset, "recuperación de contraseña", ct);
            if (tokenResult.Error is not null)
                return tokenResult.Error;

            var token = tokenResult.Token!;
            var user = tokenResult.User!;

            try
            {
                user.SetPassword(Password.FromHash(_passwordHasher.Hash(dto.Password)));
                token.MarkAsUsed(_dateTimeProvider.UtcNow());
            }
            catch (DomainException ex)
            {
                return Result.Failure(Error.Validation(ex.Message));
            }

            await _userRepository.RevokeAllUserTokens(user.Id, ct);
            _userTokenRepository.Update(token);
            _userRepository.Update(user);
            await _unitOfWork.SaveChanges(ct);

            return Result.Success();
        }

        public async Task<Result<UserEmailDispatchResultDto>> InviteAdmin(InviteAdminDto dto, CancellationToken ct = default)
        {
            if (await _userRepository.ExistsByEmail(dto.Email, ct))
                return Result<UserEmailDispatchResultDto>.Failure(Error.Conflict("Email ya esta registrado."));

            User user;
            try
            {
                user = User.CreateInvitedAdmin(
                    PersonName.Create(dto.FirstName, dto.LastName),
                    Email.Create(dto.Email),
                    _dateTimeProvider.NowArgentina());
            }
            catch (DomainException ex)
            {
                return Result<UserEmailDispatchResultDto>.Failure(Error.Validation(ex.Message));
            }

            var token = await _unitOfWork.ExecuteInTransaction(async () =>
            {
                await _userRepository.AddOne(user, ct);
                await _unitOfWork.SaveChanges(ct);

                var issuedToken = await _userTokenIssuer.CreateToken(user.Id, UserTokenPurpose.AdminInvitation, ct);
                await _unitOfWork.SaveChanges(ct);
                return issuedToken;
            }, ct);

            var emailDispatch = await _safeEmailDispatcher.TrySendCritical(
                () => _emailService.SendAdminInvitation(
                    new AdminInvitationEmailModel(
                        user.Email.Value,
                        user.PersonName.FirstName,
                        token.RawToken,
                        token.TtlHours),
                    ct),
                "invitacion de admin",
                user.Email.Value,
                "El admin se invito correctamente y enviamos el email para completar el acceso.",
                "El admin se creo, pero no pudimos enviar el email de invitacion.");

            return Result<UserEmailDispatchResultDto>.Success(
                new UserEmailDispatchResultDto(
                    await _userDtoMapper.Map(user, ct),
                    emailDispatch));
        }

        public Task<Result<UserDto>> CompleteInvitation(CompleteSecretaryInvitationDto dto, CancellationToken ct = default)
            => CompleteUserInvitation(dto.Token, dto.Password, UserTokenPurpose.SecretaryInvitation, "invitación", ct);

        public Task<Result<UserDto>> CompleteAdminInvitation(CompleteAdminInvitationDto dto, CancellationToken ct = default)
            => CompleteUserInvitation(dto.Token, dto.Password, UserTokenPurpose.AdminInvitation, "invitacion de admin", ct);

        public async Task<Result<UserDto>> UpdateUser(int id, UpdateUserDto dto, CancellationToken ct = default)
        {
            var user = await _userRepository.GetOne(id, ct);
            if (user is null)
                return Result<UserDto>.Failure(Error.NotFound("Usuario"));

            return await _userProfileUpdateService.UpdateProfile(user, dto, ct);
        }

        public async Task<Result> DeleteUser(int id, CancellationToken ct = default)
        {
            var user = await _userRepository.GetOne(id, ct);
            if (user is null)
                return Result.Failure(Error.NotFound("Usuario"));

            try
            {
                user.Deactivate();
            }
            catch (DomainException ex)
            {
                return Result.Failure(Error.Validation(ex.Message));
            }
            _userRepository.Update(user);
            await _unitOfWork.SaveChanges(ct);
            return Result.Success();
        }

        public Task<Result<UserDto>> CreateAdmin(CreateUserDto dto, CancellationToken ct = default)
            => CreateUserWithPassword(dto, User.CreateAdmin, ct);

        private async Task<Result<UserDto>> CreateUserWithPassword(
            CreateUserDto dto,
            Func<PersonName, Email, Password, DateTime, User> factory,
            CancellationToken ct)
        {
            if (await _userRepository.ExistsByEmail(dto.Email, ct))
                return Result<UserDto>.Failure(Error.Conflict("Email ya está registrado."));

            var passwordValidation = PasswordValidator.Validate(dto.Password);
            if (!passwordValidation.IsSuccess)
                return Result<UserDto>.Failure(passwordValidation.Error!);

            User user;
            try
            {
                user = factory(
                    PersonName.Create(dto.FirstName, dto.LastName),
                    Email.Create(dto.Email),
                    Password.FromHash(_passwordHasher.Hash(dto.Password)),
                    _dateTimeProvider.NowArgentina());
            }
            catch (DomainException ex)
            {
                return Result<UserDto>.Failure(Error.Validation(ex.Message));
            }

            await _userRepository.AddOne(user, ct);
            await _unitOfWork.SaveChanges(ct);

            return Result<UserDto>.Success(await _userDtoMapper.Map(user, ct));
        }

        private async Task<(Result? Error, UserToken? Token, User? User)> GetValidToken(
            string rawToken,
            UserTokenPurpose expectedPurpose,
            string tokenLabel,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return (Result.Failure(Error.Validation("Token requerido")), null, null);

            var tokenHash = _tokenHashingService.HashToken(rawToken);
            var token = await _userTokenRepository.GetByTokenHash(tokenHash, ct);
            if (token is null || token.Purpose != expectedPurpose)
                return (Result.Failure(Error.NotFound("Token")), null, null);

            var user = await _userRepository.GetOne(token.UserId, ct);
            if (user is null)
                return (Result.Failure(Error.NotFound("Usuario")), null, null);

            if (token.IsExpired(_dateTimeProvider.UtcNow()))
                return (Result.Failure(Error.Validation($"El token de {tokenLabel} está vencido")), token, user);

            if (token.IsUsed && expectedPurpose != UserTokenPurpose.EmailConfirmation)
                return (Result.Failure(Error.Validation($"El token de {tokenLabel} ya fue utilizado")), token, user);

            return (null, token, user);
        }

        private async Task<Result<UserDto>> CompleteUserInvitation(
            string rawToken,
            string password,
            UserTokenPurpose purpose,
            string tokenLabel,
            CancellationToken ct)
        {
            var passwordValidation = PasswordValidator.Validate(password);
            if (!passwordValidation.IsSuccess)
                return Result<UserDto>.Failure(passwordValidation.Error!);

            var tokenResult = await GetValidToken(rawToken, purpose, tokenLabel, ct);
            if (tokenResult.Error is not null)
                return Result<UserDto>.Failure(tokenResult.Error.Error!);

            var token = tokenResult.Token!;
            var user = tokenResult.User!;

            try
            {
                user.AcceptInvitation(Password.FromHash(_passwordHasher.Hash(password)));
                token.MarkAsUsed(_dateTimeProvider.UtcNow());
            }
            catch (DomainException ex)
            {
                return Result<UserDto>.Failure(Error.Validation(ex.Message));
            }

            _userTokenRepository.Update(token);
            _userRepository.Update(user);
            await _unitOfWork.SaveChanges(ct);

            return Result<UserDto>.Success(await _userDtoMapper.Map(user, ct));
        }
    }
}

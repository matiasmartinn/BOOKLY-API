using AutoMapper;
using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Common.Validators;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.UserAggregate.DTOs;
using BOOKLY.Domain.Aggregates.SubscriptionAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Emailing;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Repositories;
using BOOKLY.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

namespace BOOKLY.Application.Services.UserAggregate
{
    public partial class UserService : IUserService
    {
        private static readonly TimeSpan EmailConfirmationTtl = TimeSpan.FromHours(24);
        private static readonly TimeSpan PasswordResetTtl = TimeSpan.FromHours(2);
        private static readonly TimeSpan SecretaryInvitationTtl = TimeSpan.FromHours(24);

        private readonly IUserRepository _userRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUserTokenRepository _userTokenRepository;
        private readonly IInvitationTokenGenerator _invitationTokenGenerator;
        private readonly ITokenHashingService _tokenHashingService;
        private readonly IEmailService _emailService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository userRepository,
            IServiceRepository serviceRepository,
            ISubscriptionRepository subscriptionRepository,
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            ITokenHashingService tokenHashingService,
            IInvitationTokenGenerator invitationTokenGenerator,
            IUserTokenRepository userTokenRepository,
            IEmailService emailService,
            IDateTimeProvider dateTimeProvider,
            IMapper mapper,
            ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _serviceRepository = serviceRepository;
            _subscriptionRepository = subscriptionRepository;
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _tokenHashingService = tokenHashingService;
            _invitationTokenGenerator = invitationTokenGenerator;
            _userTokenRepository = userTokenRepository;
            _emailService = emailService;
            _dateTimeProvider = dateTimeProvider;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<UserDto>> GetUserById(int id, CancellationToken ct = default)
        {
            if (id <= 0)
                return Result<UserDto>.Failure(Error.Validation("Id inválido"));

            var user = await _userRepository.GetOne(id, ct);
            if (user is null)
                return Result<UserDto>.Failure(Error.NotFound("Usuario"));

            return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
        }

        public async Task<Result<UserDto>> Login(LoginDto dto, CancellationToken ct = default)
        {
            var user = await _userRepository.GetByEmail(dto.Email, ct);
            if (user is null || user.Password is null || !user.VerifyPassword(dto.Password, _passwordHasher))
                return Result<UserDto>.Failure(Error.Unauthorized("Credenciales inválidas."));

            if (!user.EmailConfirmed)
                return Result<UserDto>.Failure(Error.Unauthorized("Debes confirmar tu email antes de ingresar."));

            if (!user.IsActive)
                return Result<UserDto>.Failure(Error.Unauthorized("La cuenta no se encuentra activa."));

            user.RegisterLogin(_dateTimeProvider.NowArgentina());
            _userRepository.Update(user);
            await _unitOfWork.SaveChanges(ct);

            return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
        }

        public async Task<Result<UserDto>> RegisterOwner(CreateUserDto dto, CancellationToken ct = default)
        {
            if (await _userRepository.ExistsByEmail(dto.Email, ct))
                return Result<UserDto>.Failure(Error.Conflict("Email ya está registrado."));

            var passwordValidation = PasswordValidator.Validate(dto.Password);
            if (!passwordValidation.IsSuccess)
                return Result<UserDto>.Failure(passwordValidation.Error!);

            var user = User.CreateOwner(
                PersonName.Create(dto.FirstName, dto.LastName),
                Email.Create(dto.Email),
                Password.FromHash(_passwordHasher.Hash(dto.Password)),
                _dateTimeProvider.NowArgentina());

            await _userRepository.AddOne(user, ct);
            await _unitOfWork.SaveChanges(ct);

            var rawToken = await CreateUserToken(user.Id, UserTokenPurpose.EmailConfirmation, EmailConfirmationTtl, ct);

            await TrySendEmail(
                () => _emailService.SendEmailConfirmation(
                    new EmailConfirmationEmailModel(
                        user.Email.Value,
                        user.PersonName.FirstName,
                        rawToken,
                        (int)EmailConfirmationTtl.TotalHours),
                    ct),
                "confirmación de email",
                user.Email.Value);

            return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
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

            user.ConfirmEmail();
            user.Activate();
            token.MarkAsUsed(_dateTimeProvider.NowArgentina());

            _userTokenRepository.Update(token);
            _userRepository.Update(user);
            await _unitOfWork.SaveChanges(ct);

            return Result.Success();
        }

        public async Task<Result> ResendEmailConfirmation(ResendEmailConfirmationDto dto, CancellationToken ct = default)
        {
            var user = await _userRepository.GetByEmail(dto.Email, ct);
            if (user is null || user.EmailConfirmed || user.Role == UserKind.Admin)
                return Result.Success();

            var rawToken = await CreateUserToken(user.Id, UserTokenPurpose.EmailConfirmation, EmailConfirmationTtl, ct);

            await TrySendEmail(
                () => _emailService.SendEmailConfirmation(
                    new EmailConfirmationEmailModel(
                        user.Email.Value,
                        user.PersonName.FirstName,
                        rawToken,
                        (int)EmailConfirmationTtl.TotalHours),
                    ct),
                "reenvío de confirmación de email",
                user.Email.Value);

            return Result.Success();
        }

        public async Task<Result> RequestPasswordReset(RequestPasswordResetDto dto, CancellationToken ct = default)
        {
            var user = await _userRepository.GetByEmail(dto.Email, ct);
            if (user is null || user.Password is null)
                return Result.Success();

            var rawToken = await CreateUserToken(user.Id, UserTokenPurpose.PasswordReset, PasswordResetTtl, ct);

            await TrySendEmail(
                () => _emailService.SendPasswordReset(
                    new PasswordResetEmailModel(
                        user.Email.Value,
                        user.PersonName.FirstName,
                        rawToken,
                        (int)PasswordResetTtl.TotalHours),
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

            user.ChangePassword(Password.FromHash(_passwordHasher.Hash(dto.Password)));
            token.MarkAsUsed(_dateTimeProvider.NowArgentina());

            _userTokenRepository.Update(token);
            _userRepository.Update(user);
            await _unitOfWork.SaveChanges(ct);

            return Result.Success();
        }

        public async Task<Result<UserDto>> CreateSecretary(int ownerId, CreateSecretaryDto dto, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(dto.ServiceId, ct);
            if (service is null)
                return Result<UserDto>.Failure(Error.NotFound("Service"));

            if (service.OwnerId != ownerId)
                return Result<UserDto>.Failure(Error.Validation("El servicio no pertenece al owner"));

            if (await _userRepository.ExistsByEmail(dto.Email, ct))
                return Result<UserDto>.Failure(Error.Conflict("Ya existe un usuario con ese email"));

            var subscription = await GetEffectiveSubscription(ownerId, ct);
            var currentSecretaries = await _serviceRepository.CountAssignedSecretariesByOwnerId(ownerId, ct);
            subscription.EnsureCanAssignSecretary(currentSecretaries);

            var user = User.CreateSecretary(
                PersonName.Create(dto.FirstName, dto.LastName),
                Email.Create(dto.Email),
                _dateTimeProvider.NowArgentina());

            await _userRepository.AddOne(user, ct);
            await _unitOfWork.SaveChanges(ct);

            service.AssignSecretary(user.Id);
            _serviceRepository.Update(service);

            var rawToken = _invitationTokenGenerator.GenerateToken();
            var token = UserToken.Create(
                user.Id,
                UserTokenPurpose.SecretaryInvitation,
                _tokenHashingService.HashToken(rawToken),
                _dateTimeProvider.NowArgentina(),
                SecretaryInvitationTtl);

            await _userTokenRepository.AddOne(token, ct);
            await _unitOfWork.SaveChanges(ct);

            var owner = await _userRepository.GetOne(ownerId, ct);
            var invitedByName = owner is null
                ? "BOOKLY"
                : $"{owner.PersonName.FirstName} {owner.PersonName.LastName}";

            await TrySendEmail(
                () => _emailService.SendSecretaryInvitation(
                    new SecretaryInvitationEmailModel(
                        user.Email.Value,
                        user.PersonName.FirstName,
                        invitedByName,
                        service.Name,
                        rawToken,
                        (int)SecretaryInvitationTtl.TotalHours),
                    ct),
                "invitación de secretario",
                user.Email.Value);

            return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
        }

        public async Task<Result<IReadOnlyCollection<SecretaryDto>>> GetSecretariesByOwner(int ownerId, CancellationToken ct = default)
        {
            var owner = await _userRepository.GetOne(ownerId, ct);
            if (owner is null || owner.Role != UserKind.Owner)
                return Result<IReadOnlyCollection<SecretaryDto>>.Failure(Error.NotFound("Usuario"));

            var services = await _serviceRepository.GetServicesByOwnerWithSecretaries(ownerId, ct);
            var secretaryServices = services
                .SelectMany(service => service.SecretaryIds.Select(secretaryId => new { SecretaryId = secretaryId, ServiceId = service.Id }))
                .GroupBy(x => x.SecretaryId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyCollection<int>)group.Select(item => item.ServiceId).Distinct().ToList());

            var result = new List<SecretaryDto>();

            foreach (var secretaryId in secretaryServices.Keys.OrderBy(id => id))
            {
                var secretary = await _userRepository.GetOne(secretaryId, ct);
                if (secretary is null || secretary.Role != UserKind.Secretary)
                    continue;

                result.Add(new SecretaryDto
                {
                    Id = secretary.Id,
                    FirstName = secretary.PersonName.FirstName,
                    LastName = secretary.PersonName.LastName,
                    Email = secretary.Email.Value,
                    IsActive = secretary.IsActive,
                    EmailConfirmed = secretary.EmailConfirmed,
                    ServiceIds = secretaryServices[secretaryId]
                });
            }

            return Result<IReadOnlyCollection<SecretaryDto>>.Success(result);
        }

        public async Task<Result<UserDto>> CompleteInvitation(CompleteSecretaryInvitationDto dto, CancellationToken ct = default)
        {
            var passwordValidation = PasswordValidator.Validate(dto.Password);
            if (!passwordValidation.IsSuccess)
                return Result<UserDto>.Failure(passwordValidation.Error!);

            var tokenResult = await GetValidToken(dto.Token, UserTokenPurpose.SecretaryInvitation, "invitación", ct);
            if (tokenResult.Error is not null)
                return Result<UserDto>.Failure(tokenResult.Error.Error!);

            var token = tokenResult.Token!;
            var user = tokenResult.User!;

            user.ChangePassword(Password.FromHash(_passwordHasher.Hash(dto.Password)));
            user.ConfirmEmail();
            user.Activate();
            token.MarkAsUsed(_dateTimeProvider.NowArgentina());

            _userTokenRepository.Update(token);
            _userRepository.Update(user);
            await _unitOfWork.SaveChanges(ct);

            return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
        }

        public async Task<Result> ActivateSecretary(int id, CancellationToken ct = default)
        {
            var user = await _userRepository.GetOne(id, ct);
            if (user is null || user.Role != UserKind.Secretary)
                return Result.Failure(Error.NotFound("Usuario"));

            if (!user.EmailConfirmed)
                return Result.Failure(Error.Validation("El secretario debe completar la invitación antes de activarse."));

            user.Activate();
            _userRepository.Update(user);
            await _unitOfWork.SaveChanges(ct);

            return Result.Success();
        }

        public async Task<Result> DeactivateSecretary(int id, CancellationToken ct = default)
        {
            var user = await _userRepository.GetOne(id, ct);
            if (user is null || user.Role != UserKind.Secretary)
                return Result.Failure(Error.NotFound("Usuario"));

            user.Deactivate();
            _userRepository.Update(user);
            await _unitOfWork.SaveChanges(ct);

            return Result.Success();
        }

        public async Task<Result<UserDto>> UpdateUser(int id, UpdateUserDto dto, CancellationToken ct = default)
        {
            var user = await _userRepository.GetOne(id, ct);
            if (user is null)
                return Result<UserDto>.Failure(Error.NotFound("Usuario"));

            var newEmail = Email.Create(dto.Email);
            var emailChanged = !string.Equals(user.Email.Value, newEmail.Value, StringComparison.OrdinalIgnoreCase);
            if (emailChanged)
            {
                var existingUser = await _userRepository.GetByEmail(newEmail.Value, ct);
                if (existingUser is not null && existingUser.Id != user.Id)
                    return Result<UserDto>.Failure(Error.Conflict("Email ya está registrado."));
            }

            user.ChangeUserName(PersonName.Create(dto.FirstName, dto.LastName));
            user.ChangeEmail(newEmail);

            _userRepository.Update(user);
            await _unitOfWork.SaveChanges(ct);

            if (emailChanged && user.Role != UserKind.Admin)
            {
                var rawToken = await CreateUserToken(user.Id, UserTokenPurpose.EmailConfirmation, EmailConfirmationTtl, ct);

                await TrySendEmail(
                    () => _emailService.SendEmailConfirmation(
                        new EmailConfirmationEmailModel(
                            user.Email.Value,
                            user.PersonName.FirstName,
                            rawToken,
                            (int)EmailConfirmationTtl.TotalHours),
                        ct),
                    "confirmación de cambio de email",
                    user.Email.Value);
            }

            return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
        }

        public async Task<Result> DeleteUser(int id, CancellationToken ct = default)
        {
            var user = await _userRepository.GetOne(id, ct);
            if (user is null)
                return Result.Failure(Error.NotFound("Usuario"));

            user.Deactivate();
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

            var user = factory(
                PersonName.Create(dto.FirstName, dto.LastName),
                Email.Create(dto.Email),
                Password.FromHash(_passwordHasher.Hash(dto.Password)),
                _dateTimeProvider.NowArgentina());

            await _userRepository.AddOne(user, ct);
            await _unitOfWork.SaveChanges(ct);

            return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
        }

        private async Task<string> CreateUserToken(int userId, UserTokenPurpose purpose, TimeSpan ttl, CancellationToken ct)
        {
            var rawToken = _invitationTokenGenerator.GenerateToken();
            var token = UserToken.Create(
                userId,
                purpose,
                _tokenHashingService.HashToken(rawToken),
                _dateTimeProvider.NowArgentina(),
                ttl);

            await _userTokenRepository.AddOne(token, ct);
            await _unitOfWork.SaveChanges(ct);
            return rawToken;
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

            if (token.IsExpired(_dateTimeProvider.NowArgentina()))
                return (Result.Failure(Error.Validation($"El token de {tokenLabel} está vencido")), token, user);

            if (token.IsUsed && expectedPurpose != UserTokenPurpose.EmailConfirmation)
                return (Result.Failure(Error.Validation($"El token de {tokenLabel} ya fue utilizado")), token, user);

            return (null, token, user);
        }

        private async Task<Subscription> GetEffectiveSubscription(int ownerId, CancellationToken ct)
        {
            var subscription = await _subscriptionRepository.GetByOwnerId(ownerId, ct);
            var today = DateOnly.FromDateTime(_dateTimeProvider.NowArgentina());

            if (subscription == null || !subscription.IsActive(today))
                return Subscription.CreateFree(ownerId, _dateTimeProvider.NowArgentina());

            return subscription;
        }

        private async Task TrySendEmail(Func<Task> sendEmail, string purpose, string recipientEmail)
        {
            try
            {
                await sendEmail();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "La operación principal se completó, pero ocurrió un error inesperado enviando el email de {Purpose} a {RecipientEmail}.",
                    purpose,
                    recipientEmail);
            }
        }
    }
}

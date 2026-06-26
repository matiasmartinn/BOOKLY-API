using AutoMapper;
using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Mappings;
using BOOKLY.Application.Services.SubscriptionAggregate;
using BOOKLY.Application.Services.UserAggregate.DTOs;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;
using BOOKLY.Domain.Aggregates.UserAggregate.ValueObjects;
using BOOKLY.Domain.Emailing;
using BOOKLY.Domain.Exceptions;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Repositories;
using BOOKLY.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

namespace BOOKLY.Application.Services.UserAggregate.SecretaryManagement;

public sealed class SecretaryManagementService : ISecretaryManagementService
{
    private static readonly TimeSpan EmailConfirmationTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan SecretaryInvitationTtl = TimeSpan.FromHours(24);

    private readonly IUserRepository _userRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserTokenRepository _userTokenRepository;
    private readonly IInvitationTokenGenerator _invitationTokenGenerator;
    private readonly ITokenHashingService _tokenHashingService;
    private readonly IEffectiveSubscriptionResolver _effectiveSubscriptionResolver;
    private readonly IEmailService _emailService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMapper _mapper;
    private readonly ILogger<SecretaryManagementService> _logger;

    public SecretaryManagementService(
        IUserRepository userRepository,
        IServiceRepository serviceRepository,
        IUnitOfWork unitOfWork,
        IUserTokenRepository userTokenRepository,
        IInvitationTokenGenerator invitationTokenGenerator,
        ITokenHashingService tokenHashingService,
        IEffectiveSubscriptionResolver effectiveSubscriptionResolver,
        IEmailService emailService,
        IDateTimeProvider dateTimeProvider,
        IMapper mapper,
        ILogger<SecretaryManagementService> logger)
    {
        _userRepository = userRepository;
        _serviceRepository = serviceRepository;
        _unitOfWork = unitOfWork;
        _userTokenRepository = userTokenRepository;
        _invitationTokenGenerator = invitationTokenGenerator;
        _tokenHashingService = tokenHashingService;
        _effectiveSubscriptionResolver = effectiveSubscriptionResolver;
        _emailService = emailService;
        _dateTimeProvider = dateTimeProvider;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<UserEmailDispatchResultDto>> CreateSecretary(
        int ownerId,
        CreateSecretaryDto dto,
        CancellationToken ct = default)
    {
        var service = await _serviceRepository.GetOne(dto.ServiceId, ct);
        if (service is null)
            return Result<UserEmailDispatchResultDto>.Failure(Error.NotFound("Service"));

        if (service.OwnerId != ownerId)
            return Result<UserEmailDispatchResultDto>.Failure(Error.Validation("El servicio no pertenece al owner"));

        if (await _userRepository.ExistsByEmail(dto.Email, ct))
            return Result<UserEmailDispatchResultDto>.Failure(Error.Conflict("Ya existe un usuario con ese email"));

        var subscription = await _effectiveSubscriptionResolver.Resolve(ownerId, ct);
        var currentSecretaries = await _serviceRepository.CountAssignedSecretariesByOwnerId(ownerId, ct);

        User user;
        try
        {
            subscription.EnsureCanAssignSecretary(currentSecretaries);

            user = User.CreateSecretary(
                PersonName.Create(dto.FirstName, dto.LastName),
                Email.Create(dto.Email),
                _dateTimeProvider.NowArgentina());
        }
        catch (DomainException ex)
        {
            return Result<UserEmailDispatchResultDto>.Failure(Error.Validation(ex.Message));
        }

        await _userRepository.AddOne(user, ct);
        await _unitOfWork.SaveChanges(ct);

        service.AssignSecretary(user.Id);
        _serviceRepository.Update(service);
        var rawToken = await AddUserToken(user.Id, UserTokenPurpose.SecretaryInvitation, SecretaryInvitationTtl, ct);
        await _unitOfWork.SaveChanges(ct);

        var owner = await _userRepository.GetOne(ownerId, ct);
        var invitedByName = owner is null
            ? "BOOKLY"
            : $"{owner.PersonName.FirstName} {owner.PersonName.LastName}";

        var emailDispatch = await TrySendCriticalEmail(
            () => _emailService.SendSecretaryInvitation(
                new SecretaryInvitationEmailModel(
                    user.Email.Value,
                    user.PersonName.FirstName,
                    invitedByName,
                    service.Name,
                    rawToken,
                    (int)SecretaryInvitationTtl.TotalHours),
                ct),
            "invitaci\u00F3n de secretario",
            user.Email.Value,
            "El secretario se creo correctamente y enviamos el email para completar el acceso.",
            "El secretario se creo, pero no pudimos enviar el email de invitacion.");

        return Result<UserEmailDispatchResultDto>.Success(
            new UserEmailDispatchResultDto(
                await MapUserDtoAsync(user, ct),
                emailDispatch));
    }

    public async Task<Result<IReadOnlyCollection<SecretaryDto>>> GetSecretariesByOwner(
        int ownerId,
        CancellationToken ct = default)
    {
        var owner = await _userRepository.GetOne(ownerId, ct);
        if (owner is null || owner.Role != UserRole.Owner)
            return Result<IReadOnlyCollection<SecretaryDto>>.Failure(Error.NotFound("Usuario"));

        var services = await _serviceRepository.GetServicesByOwnerWithSecretaries(ownerId, ct);
        var secretaryServices = services
            .SelectMany(service => service.SecretaryIds.Select(secretaryId => new { SecretaryId = secretaryId, ServiceId = service.Id }))
            .GroupBy(x => x.SecretaryId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<int>)group.Select(item => item.ServiceId).Distinct().ToList());

        var secretaryIds = secretaryServices.Keys.OrderBy(id => id).ToList();
        var secretaries = await _userRepository.GetByIds(secretaryIds, ct);
        var secretariesById = secretaries
            .Where(secretary => secretary.Role == UserRole.Secretary)
            .ToDictionary(secretary => secretary.Id);

        var result = new List<SecretaryDto>();
        foreach (var secretaryId in secretaryIds)
        {
            if (secretariesById.TryGetValue(secretaryId, out var secretary))
                result.Add(MapSecretaryDto(secretary, secretaryServices[secretaryId]));
        }

        return Result<IReadOnlyCollection<SecretaryDto>>.Success(result);
    }

    public async Task<Result<UserDto>> GetOwnerSecretaryById(
        int ownerId,
        int secretaryId,
        CancellationToken ct = default)
    {
        var secretaryResult = await GetOwnedSecretary(ownerId, secretaryId, ct);
        if (secretaryResult.Error is not null)
            return Result<UserDto>.Failure(secretaryResult.Error);

        return Result<UserDto>.Success(await MapUserDtoAsync(secretaryResult.Secretary!, ct));
    }

    public async Task<Result> ActivateSecretary(int id, int? ownerId = null, CancellationToken ct = default)
    {
        var secretaryResult = await ResolveSecretaryForUpdate(ownerId, id, ct);
        if (secretaryResult.Error is not null)
            return Result.Failure(secretaryResult.Error);

        secretaryResult.Secretary!.Activate();
        _userRepository.Update(secretaryResult.Secretary);
        await _unitOfWork.SaveChanges(ct);

        return Result.Success();
    }

    public async Task<Result> DeactivateSecretary(int id, int? ownerId = null, CancellationToken ct = default)
    {
        var secretaryResult = await ResolveSecretaryForUpdate(ownerId, id, ct);
        if (secretaryResult.Error is not null)
            return Result.Failure(secretaryResult.Error);

        secretaryResult.Secretary!.Deactivate();
        _userRepository.Update(secretaryResult.Secretary);
        await _unitOfWork.SaveChanges(ct);

        return Result.Success();
    }

    public async Task<Result<UserDto>> UpdateOwnerSecretary(
        int ownerId,
        int secretaryId,
        UpdateUserDto dto,
        CancellationToken ct = default)
    {
        var secretaryResult = await GetOwnedSecretary(ownerId, secretaryId, ct);
        if (secretaryResult.Error is not null)
            return Result<UserDto>.Failure(secretaryResult.Error);

        return await UpdateExistingSecretary(secretaryResult.Secretary!, dto, ct);
    }

    private async Task<Result<UserDto>> UpdateExistingSecretary(User secretary, UpdateUserDto dto, CancellationToken ct)
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

        var emailChanged = !string.Equals(secretary.Email.Value, newEmail.Value, StringComparison.OrdinalIgnoreCase);
        if (emailChanged)
        {
            var existingUser = await _userRepository.GetByEmail(newEmail.Value, ct);
            if (existingUser is not null && existingUser.Id != secretary.Id)
                return Result<UserDto>.Failure(Error.Conflict("Email ya est\u00E1 registrado."));
        }

        try
        {
            secretary.ChangeUserName(PersonName.Create(dto.FirstName, dto.LastName));
            secretary.ChangeEmail(newEmail);
        }
        catch (DomainException ex)
        {
            return Result<UserDto>.Failure(Error.Validation(ex.Message));
        }

        _userRepository.Update(secretary);
        await _unitOfWork.SaveChanges(ct);

        if (emailChanged)
        {
            var rawToken = await AddUserToken(secretary.Id, UserTokenPurpose.EmailConfirmation, EmailConfirmationTtl, ct);
            await _unitOfWork.SaveChanges(ct);

            await TrySendEmail(
                () => _emailService.SendEmailConfirmation(
                    new EmailConfirmationEmailModel(
                        secretary.Email.Value,
                        secretary.PersonName.FirstName,
                        rawToken,
                        (int)EmailConfirmationTtl.TotalHours),
                    ct),
                "confirmaci\u00F3n de cambio de email",
                secretary.Email.Value);
        }

        return Result<UserDto>.Success(await MapUserDtoAsync(secretary, ct));
    }

    private async Task<(Error? Error, User? Secretary)> ResolveSecretaryForUpdate(
        int? ownerId,
        int secretaryId,
        CancellationToken ct)
    {
        if (!ownerId.HasValue)
        {
            var secretary = await _userRepository.GetOne(secretaryId, ct);
            if (secretary is null || secretary.Role != UserRole.Secretary)
                return (Error.NotFound("Usuario"), null);

            return (null, secretary);
        }

        return await GetOwnedSecretary(ownerId.Value, secretaryId, ct);
    }

    private async Task<(Error? Error, User? Secretary)> GetOwnedSecretary(
        int ownerId,
        int secretaryId,
        CancellationToken ct)
    {
        var secretary = await _userRepository.GetOne(secretaryId, ct);
        if (secretary is null || secretary.Role != UserRole.Secretary)
            return (Error.NotFound("Usuario"), null);

        var ownerIds = await _serviceRepository.GetOwnerIdsBySecretary(secretaryId, ct);
        if (!ownerIds.Contains(ownerId))
            return (Error.Forbidden("No tienes permisos para operar sobre este secretario."), null);

        return (null, secretary);
    }

    private SecretaryDto MapSecretaryDto(User secretary, IReadOnlyCollection<int> serviceIds)
    {
        return _mapper.Map<SecretaryDto>(secretary, options =>
        {
            options.Items[UserMappingProfile.ServiceIdsContextKey] = serviceIds;
        });
    }

    private async Task<UserDto> MapUserDtoAsync(User user, CancellationToken ct)
    {
        IReadOnlyCollection<int> serviceIds = [];

        if (user.Role == UserRole.Secretary)
            serviceIds = await _serviceRepository.GetServiceIdsBySecretary(user.Id, ct);

        return _mapper.Map<UserDto>(user, options =>
        {
            options.Items[UserMappingProfile.ServiceIdsContextKey] = serviceIds;
        });
    }

    private async Task<string> AddUserToken(
        int userId,
        UserTokenPurpose purpose,
        TimeSpan ttl,
        CancellationToken ct)
    {
        var rawToken = _invitationTokenGenerator.GenerateToken();
        var token = UserToken.Create(
            userId,
            purpose,
            _tokenHashingService.HashToken(rawToken),
            _dateTimeProvider.UtcNow(),
            ttl);

        await _userTokenRepository.AddOne(token, ct);
        return rawToken;
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
                "La operaci\u00F3n principal se complet\u00F3, pero ocurri\u00F3 un error inesperado enviando el email de {Purpose} a {RecipientEmail}.",
                purpose,
                recipientEmail);
        }
    }

    private async Task<EmailDispatchResultDto> TrySendCriticalEmail(
        Func<Task> sendEmail,
        string purpose,
        string recipientEmail,
        string successMessage,
        string failureMessage)
    {
        try
        {
            await sendEmail();
            return new EmailDispatchResultDto(true, successMessage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "La operaci\u00F3n principal se complet\u00F3, pero ocurri\u00F3 un error inesperado enviando el email de {Purpose} a {RecipientEmail}.",
                purpose,
                recipientEmail);

            return new EmailDispatchResultDto(false, failureMessage);
        }
    }
}

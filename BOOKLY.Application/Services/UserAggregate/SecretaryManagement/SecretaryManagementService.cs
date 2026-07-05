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
using BOOKLY.Domain.SharedKernel;

namespace BOOKLY.Application.Services.UserAggregate.SecretaryManagement;

public sealed class SecretaryManagementService : ISecretaryManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserTokenIssuer _userTokenIssuer;
    private readonly IUserProfileUpdateService _userProfileUpdateService;
    private readonly IUserDtoMapper _userDtoMapper;
    private readonly ISafeEmailDispatcher _safeEmailDispatcher;
    private readonly IEffectiveSubscriptionResolver _effectiveSubscriptionResolver;
    private readonly IEmailService _emailService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMapper _mapper;

    public SecretaryManagementService(
        IUserRepository userRepository,
        IServiceRepository serviceRepository,
        IUnitOfWork unitOfWork,
        IUserTokenIssuer userTokenIssuer,
        IUserProfileUpdateService userProfileUpdateService,
        IUserDtoMapper userDtoMapper,
        ISafeEmailDispatcher safeEmailDispatcher,
        IEffectiveSubscriptionResolver effectiveSubscriptionResolver,
        IEmailService emailService,
        IDateTimeProvider dateTimeProvider,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _serviceRepository = serviceRepository;
        _unitOfWork = unitOfWork;
        _userTokenIssuer = userTokenIssuer;
        _userProfileUpdateService = userProfileUpdateService;
        _userDtoMapper = userDtoMapper;
        _safeEmailDispatcher = safeEmailDispatcher;
        _effectiveSubscriptionResolver = effectiveSubscriptionResolver;
        _emailService = emailService;
        _dateTimeProvider = dateTimeProvider;
        _mapper = mapper;
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

        // Transacción única: usuario, asignación al servicio y token quedan persistidos todos o ninguno.
        var token = await _unitOfWork.ExecuteInTransaction(async () =>
        {
            await _userRepository.AddOne(user, ct);
            await _unitOfWork.SaveChanges(ct);

            service.AssignSecretary(user.Id);
            _serviceRepository.Update(service);
            var issuedToken = await _userTokenIssuer.CreateToken(user.Id, UserTokenPurpose.SecretaryInvitation, ct);
            await _unitOfWork.SaveChanges(ct);
            return issuedToken;
        }, ct);

        var owner = await _userRepository.GetOne(ownerId, ct);
        var invitedByName = owner is null
            ? "BOOKLY"
            : $"{owner.PersonName.FirstName} {owner.PersonName.LastName}";

        var emailDispatch = await _safeEmailDispatcher.TrySendCritical(
            () => _emailService.SendSecretaryInvitation(
                new SecretaryInvitationEmailModel(
                    user.Email.Value,
                    user.PersonName.FirstName,
                    invitedByName,
                    service.Name,
                    token.RawToken,
                    token.TtlHours),
                ct),
            "invitación de secretario",
            user.Email.Value,
            "El secretario se creo correctamente y enviamos el email para completar el acceso.",
            "El secretario se creo, pero no pudimos enviar el email de invitacion.");

        return Result<UserEmailDispatchResultDto>.Success(
            new UserEmailDispatchResultDto(
                await _userDtoMapper.Map(user, ct),
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

        return Result<UserDto>.Success(await _userDtoMapper.Map(secretaryResult.Secretary!, ct));
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

        return await _userProfileUpdateService.UpdateProfile(secretaryResult.Secretary!, dto, ct);
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
}

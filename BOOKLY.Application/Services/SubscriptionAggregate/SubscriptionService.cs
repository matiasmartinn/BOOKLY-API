using AutoMapper;
using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Common.Validators;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Mappings;
using BOOKLY.Application.Services.SubscriptionAggregate.Dto;
using BOOKLY.Domain.Aggregates.SubscriptionAggregate;
using BOOKLY.Domain.Exceptions;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Repositories;

namespace BOOKLY.Application.Services.SubscriptionAggregate
{
    public sealed class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SubscriptionService(
            ISubscriptionRepository subscriptionRepository,
            IServiceRepository serviceRepository,
            IDateTimeProvider dateTimeProvider,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _subscriptionRepository = subscriptionRepository;
            _serviceRepository = serviceRepository;
            _dateTimeProvider = dateTimeProvider;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<SubscriptionDto>> GetByOwnerId(int ownerId, CancellationToken ct = default)
        {
            var validation = SubscriptionRequestValidator.ValidateOwnerId(ownerId);
            if (validation.IsFailure)
                return Result<SubscriptionDto>.Failure(validation.Error);

            var now = _dateTimeProvider.NowArgentina();
            var subscription = await _subscriptionRepository.GetByOwnerId(ownerId, ct);

            return Result<SubscriptionDto>.Success(MapSubscriptionDto(subscription, ownerId, now));
        }

        public async Task<Result<IReadOnlyCollection<SubscriptionPlanOptionDto>>> GetPlanOptions(int ownerId, CancellationToken ct = default)
        {
            var validation = SubscriptionRequestValidator.ValidateOwnerId(ownerId);
            if (validation.IsFailure)
                return Result<IReadOnlyCollection<SubscriptionPlanOptionDto>>.Failure(validation.Error);

            var now = _dateTimeProvider.NowArgentina();
            var subscription = await _subscriptionRepository.GetByOwnerId(ownerId, ct);
            var current = subscription ?? Subscription.CreateFree(ownerId, now);
            var (currentServices, currentSecretaries) = await GetOwnerCounts(ownerId, ct);

            var options = SubscriptionPlan.GetCatalog()
                .Select(plan => MapPlanOptionDto(plan, current.Plan.Name, currentServices, currentSecretaries))
                .ToList()
                .AsReadOnly();

            return Result<IReadOnlyCollection<SubscriptionPlanOptionDto>>.Success(options);
        }

        public async Task<Result<SubscriptionDto>> CreateFreeIfMissing(int ownerId, CancellationToken ct = default)
        {
            var validation = SubscriptionRequestValidator.ValidateOwnerId(ownerId);
            if (validation.IsFailure)
                return Result<SubscriptionDto>.Failure(validation.Error);

            var now = _dateTimeProvider.NowArgentina();
            var existing = await _subscriptionRepository.GetByOwnerId(ownerId, ct);
            if (existing != null)
                return Result<SubscriptionDto>.Success(MapSubscriptionDto(existing, ownerId, now));

            var subscription = Subscription.CreateFree(ownerId, now);
            await _subscriptionRepository.AddOne(subscription, ct);
            await _unitOfWork.SaveChanges(ct);

            return Result<SubscriptionDto>.Success(MapSubscriptionDto(subscription, ownerId, now));
        }

        public async Task<Result<SubscriptionDto>> Cancel(int ownerId, CancellationToken ct = default)
        {
            var validation = SubscriptionRequestValidator.ValidateOwnerId(ownerId);
            if (validation.IsFailure)
                return Result<SubscriptionDto>.Failure(validation.Error);

            var subscription = await _subscriptionRepository.GetByOwnerIdForUpdate(ownerId, ct);
            if (subscription == null)
                return Result<SubscriptionDto>.Failure(Error.NotFound("Suscripción"));

            var now = _dateTimeProvider.NowArgentina();
            try
            {
                subscription.Cancel(now);
            }
            catch (DomainException ex)
            {
                return Result<SubscriptionDto>.Failure(Error.Validation(ex.Message));
            }

            _subscriptionRepository.Update(subscription);
            await _unitOfWork.SaveChanges(ct);

            return Result<SubscriptionDto>.Success(MapSubscriptionDto(subscription, ownerId, now));
        }

        public async Task<Result<SubscriptionDto>> Renew(RenewSubscriptionDto dto, CancellationToken ct = default)
        {
            var validation = SubscriptionRequestValidator.ValidateRenew(dto);
            if (validation.IsFailure)
                return Result<SubscriptionDto>.Failure(validation.Error);

            var subscription = await _subscriptionRepository.GetByOwnerIdForUpdate(dto.OwnerId, ct);
            if (subscription == null)
                return Result<SubscriptionDto>.Failure(Error.NotFound("Suscripción"));

            var now = _dateTimeProvider.NowArgentina();
            try
            {
                var newPeriod = CreatePaidMonthlyPeriod(now);
                subscription.Renew(newPeriod, now);
            }
            catch (DomainException ex)
            {
                return Result<SubscriptionDto>.Failure(Error.Validation(ex.Message));
            }

            _subscriptionRepository.Update(subscription!);
            await _unitOfWork.SaveChanges(ct);

            return Result<SubscriptionDto>.Success(MapSubscriptionDto(subscription!, dto.OwnerId, now));
        }

        public async Task<Result<SubscriptionDto>> ChangePlan(ChangePlanDto dto, CancellationToken ct = default)
        {
            var validation = SubscriptionRequestValidator.ValidateChangePlan(dto);
            if (validation.IsFailure)
                return Result<SubscriptionDto>.Failure(validation.Error);

            var planResult = ResolvePlan(dto);
            if (planResult.Error is not null)
                return Result<SubscriptionDto>.Failure(planResult.Error);

            var newPlan = planResult.Plan!;
            var now = _dateTimeProvider.NowArgentina();
            var today = DateOnly.FromDateTime(now);
            var paidPeriod = newPlan.Name == PlanName.Free
                ? null
                : CreatePaidMonthlyPeriod(now);

            var subscription = await _subscriptionRepository.GetByOwnerIdForUpdate(dto.OwnerId, ct);

            try
            {
                if (subscription is null)
                {
                    if (newPlan.Name == PlanName.Free)
                    {
                        var freeSubscription = Subscription.CreateFree(dto.OwnerId, now);
                        await _subscriptionRepository.AddOne(freeSubscription, ct);
                        await _unitOfWork.SaveChanges(ct);
                        return Result<SubscriptionDto>.Success(MapSubscriptionDto(freeSubscription, dto.OwnerId, now));
                    }

                    var paidSubscription = Subscription.CreatePaid(dto.OwnerId, newPlan, paidPeriod!, now);
                    await _subscriptionRepository.AddOne(paidSubscription, ct);
                    await _unitOfWork.SaveChanges(ct);
                    return Result<SubscriptionDto>.Success(MapSubscriptionDto(paidSubscription, dto.OwnerId, now));
                }

                if (newPlan.Name == subscription.Plan.Name)
                    return Result<SubscriptionDto>.Success(MapSubscriptionDto(subscription, dto.OwnerId, now));

                if (newPlan.Name == PlanName.Free)
                {
                    var (currentServices, currentSecretaries) = await GetOwnerCounts(dto.OwnerId, ct);
                    subscription.ChangeToFree(today, currentServices, currentSecretaries, now);
                }
                else if (subscription.Plan.Name == PlanName.Free)
                {
                    subscription.SwitchFromFreeToPaid(newPlan, paidPeriod!, now);
                }
                else
                {
                    if (newPlan.Name < subscription.Plan.Name)
                    {
                        var (currentServices, currentSecretaries) = await GetOwnerCounts(dto.OwnerId, ct);
                        subscription.DowngradeTo(newPlan, currentServices, currentSecretaries, now);
                    }
                    else if (newPlan.Name > subscription.Plan.Name)
                    {
                        subscription.UpgradeTo(newPlan, now);
                    }

                    subscription.Renew(paidPeriod!, now);
                }
            }
            catch (DomainException ex)
            {
                return Result<SubscriptionDto>.Failure(Error.Validation(ex.Message));
            }

            _subscriptionRepository.Update(subscription);
            await _unitOfWork.SaveChanges(ct);

            return Result<SubscriptionDto>.Success(MapSubscriptionDto(subscription, dto.OwnerId, now));
        }

        private async Task<(int Services, int Secretaries)> GetOwnerCounts(int ownerId, CancellationToken ct)
        {
            var currentServices = await _serviceRepository.CountActiveByOwnerId(ownerId, ct);
            var currentSecretaries = await _serviceRepository.CountAssignedSecretariesByOwnerId(ownerId, ct);
            return (currentServices, currentSecretaries);
        }

        private static (SubscriptionPlan? Plan, Error? Error) ResolvePlan(ChangePlanDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.TargetPlan) && dto.PlanName.HasValue)
            {
                if (!TryParsePlanName(dto.TargetPlan!, out var parsedPlanName))
                    return (null, Error.Validation("El plan indicado es inválido."));

                if (!TryGetDefinedPlanName(dto.PlanName.Value, out var legacyPlanName))
                    return (null, Error.Validation("El plan indicado es inválido."));

                if (parsedPlanName != legacyPlanName)
                    return (null, Error.Validation("Los datos del plan son inconsistentes."));

                return (SubscriptionPlan.FromName(parsedPlanName), null);
            }

            if (!string.IsNullOrWhiteSpace(dto.TargetPlan))
            {
                if (!TryParsePlanName(dto.TargetPlan!, out var parsedPlanName))
                    return (null, Error.Validation("El plan indicado es inválido."));

                return (SubscriptionPlan.FromName(parsedPlanName), null);
            }

            if (dto.PlanName.HasValue)
            {
                if (!TryGetDefinedPlanName(dto.PlanName.Value, out var legacyPlanName))
                    return (null, Error.Validation("El plan indicado es inválido."));

                return (SubscriptionPlan.FromName(legacyPlanName), null);
            }

            return (null, Error.Validation("Debe indicar el plan destino."));
        }

        private static SubscriptionPeriod CreatePaidMonthlyPeriod(DateTime now)
        {
            return SubscriptionPeriod.CreateMonthly(DateOnly.FromDateTime(now));
        }

        private static bool TryParsePlanName(string rawValue, out PlanName planName)
        {
            if (int.TryParse(rawValue, out var planCode) &&
                Enum.IsDefined(typeof(PlanName), planCode))
            {
                planName = (PlanName)planCode;
                return true;
            }

            return rawValue.Trim().ToLowerInvariant() switch
            {
                "free" => SetPlanName(PlanName.Free, out planName),
                "pro" => SetPlanName(PlanName.Pro, out planName),
                "max" => SetPlanName(PlanName.Max, out planName),
                _ => Enum.TryParse(rawValue, ignoreCase: true, out planName)
            };
        }

        private static bool SetPlanName(PlanName value, out PlanName planName)
        {
            planName = value;
            return true;
        }

        private static bool TryGetDefinedPlanName(PlanName value, out PlanName planName)
        {
            if (Enum.IsDefined(typeof(PlanName), value))
            {
                planName = value;
                return true;
            }

            planName = default;
            return false;
        }

        private SubscriptionDto MapSubscriptionDto(Subscription? persistedSubscription, int ownerId, DateTime now)
        {
            var subscription = persistedSubscription ?? Subscription.CreateFree(ownerId, now);

            return _mapper.Map<SubscriptionDto>(subscription, options =>
            {
                options.Items[SubscriptionMappingProfile.OwnerIdContextKey] = ownerId;
                options.Items[SubscriptionMappingProfile.TodayContextKey] = DateOnly.FromDateTime(now);
                options.Items[SubscriptionMappingProfile.IsPersistedContextKey] = persistedSubscription is not null;
            });
        }

        private SubscriptionPlanOptionDto MapPlanOptionDto(
            SubscriptionPlan plan,
            PlanName currentPlanName,
            int currentServices,
            int currentSecretaries)
        {
            return _mapper.Map<SubscriptionPlanOptionDto>(plan, options =>
            {
                options.Items[SubscriptionMappingProfile.CurrentPlanNameContextKey] = currentPlanName;
                options.Items[SubscriptionMappingProfile.CurrentServicesContextKey] = currentServices;
                options.Items[SubscriptionMappingProfile.CurrentSecretariesContextKey] = currentSecretaries;
            });
        }
    }
}

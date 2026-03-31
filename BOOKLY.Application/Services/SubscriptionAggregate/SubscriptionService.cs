using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.SubscriptionAggregate.Dto;
using BOOKLY.Domain.Aggregates.SubscriptionAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Repositories;

namespace BOOKLY.Application.Services.SubscriptionAggregate
{
    public sealed class SubscriptionService : ISubscriptionService
    {
        private const string FreeRequiresNoPeriodMessage = "El plan Free no utiliza período.";
        private const string PaidPlanRequiresPeriodMessage = "Para activar un plan pago debe indicar StartDate y EndDate.";
        private const string ExpiredPlanRequiresPeriodMessage = "La suscripción actual está vencida. Debe indicar StartDate y EndDate para activar el nuevo plan.";

        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IUnitOfWork _unitOfWork;

        public SubscriptionService(
            ISubscriptionRepository subscriptionRepository,
            IServiceRepository serviceRepository,
            IDateTimeProvider dateTimeProvider,
            IUnitOfWork unitOfWork)
        {
            _subscriptionRepository = subscriptionRepository;
            _serviceRepository = serviceRepository;
            _dateTimeProvider = dateTimeProvider;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<SubscriptionDto>> GetByOwnerId(int ownerId, CancellationToken ct = default)
        {
            if (ownerId <= 0)
                return Result<SubscriptionDto>.Failure(Error.Validation("OwnerId inválido."));

            var now = _dateTimeProvider.NowArgentina();
            var subscription = await _subscriptionRepository.GetByOwnerId(ownerId, ct);

            return Result<SubscriptionDto>.Success(MapSubscription(subscription, ownerId, now));
        }

        public async Task<Result<IReadOnlyCollection<SubscriptionPlanOptionDto>>> GetPlanOptions(int ownerId, CancellationToken ct = default)
        {
            if (ownerId <= 0)
                return Result<IReadOnlyCollection<SubscriptionPlanOptionDto>>.Failure(Error.Validation("OwnerId inválido."));

            var now = _dateTimeProvider.NowArgentina();
            var today = DateOnly.FromDateTime(now);
            var subscription = await _subscriptionRepository.GetByOwnerId(ownerId, ct);
            var current = subscription ?? Subscription.CreateFree(ownerId, now);
            var (currentServices, currentSecretaries) = await GetOwnerCounts(ownerId, ct);

            var options = SubscriptionPlan.GetCatalog()
                .Select(plan => BuildPlanOption(plan, current, today, currentServices, currentSecretaries))
                .ToList()
                .AsReadOnly();

            return Result<IReadOnlyCollection<SubscriptionPlanOptionDto>>.Success(options);
        }

        public async Task<Result<SubscriptionDto>> CreateFreeIfMissing(int ownerId, CancellationToken ct = default)
        {
            if (ownerId <= 0)
                return Result<SubscriptionDto>.Failure(Error.Validation("OwnerId inválido."));

            var now = _dateTimeProvider.NowArgentina();
            var existing = await _subscriptionRepository.GetByOwnerId(ownerId, ct);
            if (existing != null)
                return Result<SubscriptionDto>.Success(MapSubscription(existing, ownerId, now));

            var subscription = Subscription.CreateFree(ownerId, now);
            await _subscriptionRepository.AddOne(subscription, ct);
            await _unitOfWork.SaveChanges(ct);

            return Result<SubscriptionDto>.Success(MapSubscription(subscription, ownerId, now));
        }

        public async Task<Result<SubscriptionDto>> Cancel(int ownerId, CancellationToken ct = default)
        {
            if (ownerId <= 0)
                return Result<SubscriptionDto>.Failure(Error.Validation("OwnerId inválido."));

            var subscription = await _subscriptionRepository.GetByOwnerIdForUpdate(ownerId, ct);
            if (subscription == null)
                return Result<SubscriptionDto>.Failure(Error.NotFound("Suscripción"));

            var now = _dateTimeProvider.NowArgentina();
            subscription.Cancel(now);

            _subscriptionRepository.Update(subscription);
            await _unitOfWork.SaveChanges(ct);

            return Result<SubscriptionDto>.Success(MapSubscription(subscription, ownerId, now));
        }

        public async Task<Result<SubscriptionDto>> Renew(RenewSubscriptionDto dto, CancellationToken ct = default)
        {
            if (dto.OwnerId <= 0)
                return Result<SubscriptionDto>.Failure(Error.Validation("OwnerId inválido."));

            var subscription = await _subscriptionRepository.GetByOwnerIdForUpdate(dto.OwnerId, ct);
            if (subscription == null)
                return Result<SubscriptionDto>.Failure(Error.NotFound("Suscripción"));

            var now = _dateTimeProvider.NowArgentina();
            var newPeriod = SubscriptionPeriod.Create(dto.StartDate, dto.EndDate);

            subscription.Renew(newPeriod, now);
            _subscriptionRepository.Update(subscription);
            await _unitOfWork.SaveChanges(ct);

            return Result<SubscriptionDto>.Success(MapSubscription(subscription, dto.OwnerId, now));
        }

        public async Task<Result<SubscriptionDto>> ChangePlan(ChangePlanDto dto, CancellationToken ct = default)
        {
            if (dto.OwnerId <= 0)
                return Result<SubscriptionDto>.Failure(Error.Validation("OwnerId inválido."));

            var planResult = ResolvePlan(dto);
            if (planResult.Error is not null)
                return Result<SubscriptionDto>.Failure(planResult.Error);

            var periodResult = BuildRequestedPeriod(dto.StartDate, dto.EndDate);
            if (periodResult.Error is not null)
                return Result<SubscriptionDto>.Failure(periodResult.Error);

            var newPlan = planResult.Plan!;
            var requestedPeriod = periodResult.Period;
            var now = _dateTimeProvider.NowArgentina();
            var today = DateOnly.FromDateTime(now);

            if (newPlan.Name == PlanName.Free && requestedPeriod is not null)
                return Result<SubscriptionDto>.Failure(Error.Validation(FreeRequiresNoPeriodMessage));

            var subscription = await _subscriptionRepository.GetByOwnerIdForUpdate(dto.OwnerId, ct);

            if (subscription is null)
            {
                if (newPlan.Name == PlanName.Free)
                {
                    var freeSubscription = Subscription.CreateFree(dto.OwnerId, now);
                    await _subscriptionRepository.AddOne(freeSubscription, ct);
                    await _unitOfWork.SaveChanges(ct);
                    return Result<SubscriptionDto>.Success(MapSubscription(freeSubscription, dto.OwnerId, now));
                }

                if (requestedPeriod is null)
                    return Result<SubscriptionDto>.Failure(Error.Validation(PaidPlanRequiresPeriodMessage));

                var paidSubscription = Subscription.CreatePaid(dto.OwnerId, newPlan, requestedPeriod, now);
                await _subscriptionRepository.AddOne(paidSubscription, ct);
                await _unitOfWork.SaveChanges(ct);
                return Result<SubscriptionDto>.Success(MapSubscription(paidSubscription, dto.OwnerId, now));
            }

            if (newPlan.Name == subscription.Plan.Name && requestedPeriod is null)
                return Result<SubscriptionDto>.Success(MapSubscription(subscription, dto.OwnerId, now));

            if (newPlan.Name == PlanName.Free)
            {
                var (currentServices, currentSecretaries) = await GetOwnerCounts(dto.OwnerId, ct);
                subscription.ChangeToFree(today, currentServices, currentSecretaries, now);
            }
            else if (subscription.Plan.Name == PlanName.Free)
            {
                if (requestedPeriod is null)
                    return Result<SubscriptionDto>.Failure(Error.Validation(PaidPlanRequiresPeriodMessage));

                subscription.SwitchFromFreeToPaid(newPlan, requestedPeriod, now);
            }
            else
            {
                if (subscription.IsExpired(today) && requestedPeriod is null && newPlan.Name != subscription.Plan.Name)
                    return Result<SubscriptionDto>.Failure(Error.Validation(ExpiredPlanRequiresPeriodMessage));

                if (newPlan.Name < subscription.Plan.Name)
                {
                    var (currentServices, currentSecretaries) = await GetOwnerCounts(dto.OwnerId, ct);
                    subscription.DowngradeTo(newPlan, currentServices, currentSecretaries, now);
                }
                else if (newPlan.Name > subscription.Plan.Name)
                {
                    subscription.UpgradeTo(newPlan, now);
                }

                if (requestedPeriod is not null)
                    subscription.Renew(requestedPeriod, now);
            }

            _subscriptionRepository.Update(subscription);
            await _unitOfWork.SaveChanges(ct);

            return Result<SubscriptionDto>.Success(MapSubscription(subscription, dto.OwnerId, now));
        }

        private static SubscriptionDto MapSubscription(Subscription? persistedSubscription, int ownerId, DateTime now)
        {
            var subscription = persistedSubscription ?? Subscription.CreateFree(ownerId, now);
            var today = DateOnly.FromDateTime(now);
            var isPersisted = persistedSubscription is not null;
            var isExpired = subscription.IsExpired(today);
            var isCancelled = subscription.Status == SubscriptionStatus.Cancelled;

            return new SubscriptionDto
            {
                Id = subscription.Id,
                OwnerId = ownerId,
                IsPersisted = isPersisted,
                CurrentPlan = MapPlan(subscription.Plan),
                Status = GetEffectiveStatus(subscription, today),
                RawStatus = GetRawStatus(subscription.Status),
                RawStatusCode = (int)subscription.Status,
                IsActive = subscription.IsActive(today),
                IsCancelled = isCancelled,
                IsExpired = isExpired,
                PendingCancellation = isCancelled && !isExpired,
                CanCancel = subscription.Plan.Name != PlanName.Free && !isCancelled && !isExpired,
                CanRenew = subscription.Plan.Name != PlanName.Free,
                StartDate = subscription.Period.StartDate,
                EndDate = subscription.Period.EndDate,
                IsOpenEnded = subscription.Period.IsOpenEnded,
                CreatedOn = isPersisted ? subscription.CreatedOn : null,
                UpdatedOn = isPersisted ? subscription.UpdatedOn : null
            };
        }

        private static SubscriptionPlanOptionDto BuildPlanOption(
            SubscriptionPlan plan,
            Subscription current,
            DateOnly today,
            int currentServices,
            int currentSecretaries)
        {
            var isCurrent = plan.Name == current.Plan.Name;
            var canChange = !isCurrent;
            string? unavailableReason = isCurrent ? "Este es el plan actual." : null;

            if (!isCurrent && plan.Name < current.Plan.Name)
            {
                if (!plan.AllowsServices(currentServices))
                {
                    canChange = false;
                    unavailableReason = "No se puede bajar de plan: excede el límite de servicios.";
                }
                else if (!plan.AllowsSecretaries(currentSecretaries))
                {
                    canChange = false;
                    unavailableReason = "No se puede bajar de plan: excede el límite de secretarios.";
                }
            }

            return new SubscriptionPlanOptionDto
            {
                Plan = MapPlan(plan),
                IsCurrent = isCurrent,
                ChangeType = GetChangeType(plan.Name, current.Plan.Name),
                CanChange = canChange,
                RequiresPeriod = plan.Name != PlanName.Free &&
                    (current.Plan.Name == PlanName.Free || current.IsExpired(today) || current.Period.IsOpenEnded),
                UnavailableReason = unavailableReason
            };
        }

        private async Task<(int Services, int Secretaries)> GetOwnerCounts(int ownerId, CancellationToken ct)
        {
            var currentServices = await _serviceRepository.CountByOwnerId(ownerId, ct);
            var currentSecretaries = await _serviceRepository.CountAssignedSecretariesByOwnerId(ownerId, ct);
            return (currentServices, currentSecretaries);
        }

        private static SubscriptionPlanDto MapPlan(SubscriptionPlan plan)
        {
            return new SubscriptionPlanDto
            {
                Code = (int)plan.Name,
                Key = GetPlanKey(plan.Name),
                DisplayName = GetPlanDisplayName(plan.Name),
                Limits = new SubscriptionPlanLimitsDto
                {
                    MaxServices = plan.MaxServices,
                    MaxSecretaries = plan.MaxSecretaries,
                    AllowsExtraFields = plan.AllowsExtraFields()
                }
            };
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

        private static (SubscriptionPeriod? Period, Error? Error) BuildRequestedPeriod(DateOnly? startDate, DateOnly? endDate)
        {
            if (!startDate.HasValue && !endDate.HasValue)
                return (null, null);

            if (!startDate.HasValue || !endDate.HasValue)
                return (null, Error.Validation("Debe indicar StartDate y EndDate juntos."));

            return (SubscriptionPeriod.Create(startDate.Value, endDate.Value), null);
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

        private static string GetEffectiveStatus(Subscription subscription, DateOnly today)
        {
            if (subscription.IsExpired(today))
                return "expired";

            return subscription.Status == SubscriptionStatus.Cancelled
                ? "pending_cancellation"
                : "active";
        }

        private static string GetRawStatus(SubscriptionStatus status)
        {
            return status switch
            {
                SubscriptionStatus.Active => "active",
                SubscriptionStatus.Cancelled => "cancelled",
                _ => "unknown"
            };
        }

        private static string GetPlanKey(PlanName planName)
        {
            return planName switch
            {
                PlanName.Free => "free",
                PlanName.Pro => "pro",
                PlanName.Max => "max",
                _ => "unknown"
            };
        }

        private static string GetPlanDisplayName(PlanName planName)
        {
            return planName switch
            {
                PlanName.Free => "Free",
                PlanName.Pro => "Pro",
                PlanName.Max => "Max",
                _ => "Unknown"
            };
        }

        private static string GetChangeType(PlanName targetPlan, PlanName currentPlan)
        {
            if (targetPlan == currentPlan)
                return "current";

            return targetPlan > currentPlan
                ? "upgrade"
                : "downgrade";
        }
    }
}

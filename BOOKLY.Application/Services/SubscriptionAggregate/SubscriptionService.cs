using AutoMapper;
using BOOKLY.Application.Common;
using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.SubscriptionAggregate.Dto;
using BOOKLY.Domain;
using BOOKLY.Domain.Aggregates.SubscriptionAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.SharedKernel;
using BOOKLY.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace BOOKLY.Application.Services.SubscriptionAggregate
{
    public sealed class SubscriptionService : BaseService<SubscriptionService>, ISubscriptionService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public SubscriptionService(
            ISubscriptionRepository subscriptionRepository,
            IServiceRepository serviceRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ILogger<SubscriptionService> logger) : base(logger)
        {
            _subscriptionRepository = subscriptionRepository;
            _serviceRepository = serviceRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<SubscriptionDto>> GetByOwnerId(int ownerId, CancellationToken ct = default)
        {
            var subscription = await _subscriptionRepository.GetByOwnerId(ownerId, ct);
            if (subscription == null)
                return Result<SubscriptionDto>.Failure(Error.NotFound("Suscripción"));

            return Result<SubscriptionDto>.Success(_mapper.Map<SubscriptionDto>(subscription));
        }

        public async Task<Result<SubscriptionDto>> CreateFreeIfMissing(int ownerId, CancellationToken ct = default)
        {
            var existing = await _subscriptionRepository.GetByOwnerId(ownerId, ct);
            if (existing != null)
                return Result<SubscriptionDto>.Success(_mapper.Map<SubscriptionDto>(existing));

            return await Execute(async () =>
            {
                var subscription = Subscription.CreateFree(ownerId);
                await _subscriptionRepository.AddOne(subscription, ct);
                await _unitOfWork.SaveChanges(ct);
                return _mapper.Map<SubscriptionDto>(subscription);
            });
        }

        public async Task<Result> Cancel(int ownerId, CancellationToken ct = default)
        {
            var subscription = await _subscriptionRepository.GetByOwnerId(ownerId, ct);
            if (subscription == null)
                return Result.Failure(Error.NotFound("Suscripción"));

            return await Execute(async () =>
            {
                subscription.Cancel();
                _subscriptionRepository.Update(subscription);
                await _unitOfWork.SaveChanges(ct);
            });
        }

        public async Task<Result<SubscriptionDto>> Renew(RenewSubscriptionDto dto, CancellationToken ct = default)
        {
            var subscription = await _subscriptionRepository.GetByOwnerId(dto.OwnerId, ct);
            if (subscription == null)
                return Result<SubscriptionDto>.Failure(Error.NotFound("Suscripción"));

            return await Execute(async () =>
            {
                // Cambiá esto por TU factory real
                var newPeriod = SubscriptionPeriod.Create(dto.StartDate, dto.EndDate);

                subscription.Renew(newPeriod);
                _subscriptionRepository.Update(subscription);
                await _unitOfWork.SaveChanges(ct);

                return _mapper.Map<SubscriptionDto>(subscription);
            });
        }

        public async Task<Result<SubscriptionDto>> ChangePlan(ChangePlanDto dto, CancellationToken ct = default)
        {
            var subscription = await _subscriptionRepository.GetByOwnerId(dto.OwnerId, ct);
            if (subscription == null)
                return Result<SubscriptionDto>.Failure(Error.NotFound("Suscripción"));

            return await Execute(async () =>
            {
                var newPlan = ResolvePlan(dto.PlanName);

                // mismo plan => no-op
                if (newPlan.Name == subscription.Plan.Name)
                    return _mapper.Map<SubscriptionDto>(subscription);

                // Downgrade => necesita conteos (porque el agregado lo exige)
                if (newPlan.Name < subscription.Plan.Name)
                {
                    var currentServices = await _serviceRepository.CountByOwnerId(dto.OwnerId, ct);
                    var currentSecretaries = await _serviceRepository.CountAssignedSecretariesByOwnerId(dto.OwnerId, ct);

                    subscription.DowngradeTo(newPlan, currentServices, currentSecretaries);
                }
                else
                {
                    subscription.UpgradeTo(newPlan);
                }

                _subscriptionRepository.Update(subscription);
                await _unitOfWork.SaveChanges(ct);

                return _mapper.Map<SubscriptionDto>(subscription);
            });
        }

        private static SubscriptionPlan ResolvePlan(PlanName planName)
        {
            return planName switch
            {
                PlanName.Free => SubscriptionPlan.Free(),
                PlanName.Pro => SubscriptionPlan.Pro(),
                PlanName.Max => SubscriptionPlan.Max(),
                _ => throw new DomainException("Plan inválido.")
            };
        }
    }
}
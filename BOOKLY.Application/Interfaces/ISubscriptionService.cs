using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.SubscriptionAggregate.Dto;

namespace BOOKLY.Application.Interfaces
{
    public interface ISubscriptionService
    {
        Task<Result<SubscriptionDto>> GetByOwnerId(int ownerId, CancellationToken ct = default);
        Task<Result<SubscriptionDto>> CreateFreeIfMissing(int ownerId, CancellationToken ct = default);
        Task<Result> Cancel(int ownerId, CancellationToken ct = default);
        Task<Result<SubscriptionDto>> Renew(RenewSubscriptionDto dto, CancellationToken ct = default);
        Task<Result<SubscriptionDto>> ChangePlan(ChangePlanDto dto, CancellationToken ct = default);
    }
}
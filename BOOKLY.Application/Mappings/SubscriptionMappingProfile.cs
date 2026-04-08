using AutoMapper;
using BOOKLY.Application.Services.SubscriptionAggregate.Dto;
using BOOKLY.Domain.Aggregates.SubscriptionAggregate;

namespace BOOKLY.Application.Mappings
{
    public sealed class SubscriptionMappingProfile : Profile
    {
        public const string OwnerIdContextKey = "Subscription.OwnerId";
        public const string TodayContextKey = "Subscription.Today";
        public const string IsPersistedContextKey = "Subscription.IsPersisted";
        public const string CurrentPlanNameContextKey = "Subscription.CurrentPlanName";
        public const string CurrentServicesContextKey = "Subscription.CurrentServices";
        public const string CurrentSecretariesContextKey = "Subscription.CurrentSecretaries";

        public SubscriptionMappingProfile()
        {
            CreateMap<SubscriptionPlan, SubscriptionPlanLimitsDto>()
                .ForMember(d => d.AllowsExtraFields, o => o.MapFrom(s => s.AllowsExtraFields()));

            CreateMap<SubscriptionPlan, SubscriptionPlanDto>()
                .ForMember(d => d.Code, o => o.MapFrom(s => (int)s.Name))
                .ForMember(d => d.Key, o => o.MapFrom(s => GetPlanKey(s.Name)))
                .ForMember(d => d.DisplayName, o => o.MapFrom(s => GetPlanDisplayName(s.Name)))
                .ForMember(d => d.Limits, o => o.MapFrom(s => s));

            CreateMap<Subscription, SubscriptionDto>()
                .ForMember(d => d.OwnerId, o => o.MapFrom((s, _, _, context) => GetOwnerId(s, context)))
                .ForMember(d => d.IsPersisted, o => o.MapFrom((_, _, _, context) => IsPersisted(context)))
                .ForMember(d => d.CurrentPlan, o => o.MapFrom(s => s.Plan))
                .ForMember(d => d.Status, o => o.MapFrom((s, _, _, context) => GetEffectiveStatus(s, GetToday(context))))
                .ForMember(d => d.RawStatus, o => o.MapFrom(s => GetRawStatus(s.Status)))
                .ForMember(d => d.RawStatusCode, o => o.MapFrom(s => (int)s.Status))
                .ForMember(d => d.IsActive, o => o.MapFrom((s, _, _, context) => s.IsActive(GetToday(context))))
                .ForMember(d => d.IsCancelled, o => o.MapFrom(s => s.Status == SubscriptionStatus.Cancelled))
                .ForMember(d => d.IsExpired, o => o.MapFrom((s, _, _, context) => s.IsExpired(GetToday(context))))
                .ForMember(d => d.PendingCancellation, o => o.MapFrom((s, _, _, context) =>
                    s.Status == SubscriptionStatus.Cancelled && !s.IsExpired(GetToday(context))))
                .ForMember(d => d.CanCancel, o => o.MapFrom((s, _, _, context) =>
                    s.Plan.Name != PlanName.Free &&
                    s.Status != SubscriptionStatus.Cancelled &&
                    !s.IsExpired(GetToday(context))))
                .ForMember(d => d.CanRenew, o => o.MapFrom(s => s.Plan.Name != PlanName.Free))
                .ForMember(d => d.StartDate, o => o.MapFrom(s => s.Period.StartDate))
                .ForMember(d => d.EndDate, o => o.MapFrom(s => s.Period.EndDate))
                .ForMember(d => d.IsOpenEnded, o => o.MapFrom(s => s.Period.IsOpenEnded))
                .ForMember(d => d.CreatedOn, o => o.MapFrom((s, _, _, context) => IsPersisted(context) ? (DateTime?)s.CreatedOn : null))
                .ForMember(d => d.UpdatedOn, o => o.MapFrom((s, _, _, context) => IsPersisted(context) ? s.UpdatedOn : null));

            CreateMap<SubscriptionPlan, SubscriptionPlanOptionDto>()
                .ForMember(d => d.Plan, o => o.MapFrom(s => s))
                .ForMember(d => d.IsCurrent, o => o.MapFrom((s, _, _, context) => s.Name == GetCurrentPlanName(context)))
                .ForMember(d => d.ChangeType, o => o.MapFrom((s, _, _, context) => GetChangeType(s.Name, GetCurrentPlanName(context))))
                .ForMember(d => d.CanChange, o => o.MapFrom((s, _, _, context) =>
                    CanChange(s, GetCurrentPlanName(context), GetCurrentServices(context), GetCurrentSecretaries(context))))
                .ForMember(d => d.RequiresPeriod, o => o.MapFrom(_ => false))
                .ForMember(d => d.UnavailableReason, o => o.MapFrom((s, _, _, context) =>
                    GetUnavailableReason(s, GetCurrentPlanName(context), GetCurrentServices(context), GetCurrentSecretaries(context))));
        }

        private static int GetOwnerId(Subscription source, ResolutionContext context)
        {
            return context.Items.TryGetValue(OwnerIdContextKey, out var ownerId) &&
                   ownerId is int value
                ? value
                : source.OwnerId;
        }

        private static DateOnly GetToday(ResolutionContext context)
        {
            return context.Items.TryGetValue(TodayContextKey, out var today) &&
                   today is DateOnly value
                ? value
                : DateOnly.FromDateTime(DateTime.UtcNow);
        }

        private static bool IsPersisted(ResolutionContext context)
        {
            return context.Items.TryGetValue(IsPersistedContextKey, out var isPersisted) &&
                   isPersisted is bool value
                ? value
                : true;
        }

        private static PlanName GetCurrentPlanName(ResolutionContext context)
        {
            return context.Items.TryGetValue(CurrentPlanNameContextKey, out var currentPlanName) &&
                   currentPlanName is PlanName value
                ? value
                : PlanName.Free;
        }

        private static int GetCurrentServices(ResolutionContext context)
        {
            return context.Items.TryGetValue(CurrentServicesContextKey, out var currentServices) &&
                   currentServices is int value
                ? value
                : 0;
        }

        private static int GetCurrentSecretaries(ResolutionContext context)
        {
            return context.Items.TryGetValue(CurrentSecretariesContextKey, out var currentSecretaries) &&
                   currentSecretaries is int value
                ? value
                : 0;
        }

        private static bool CanChange(
            SubscriptionPlan targetPlan,
            PlanName currentPlanName,
            int currentServices,
            int currentSecretaries)
        {
            if (targetPlan.Name == currentPlanName)
                return false;

            if (targetPlan.Name < currentPlanName)
            {
                if (!targetPlan.AllowsServices(currentServices))
                    return false;

                if (!targetPlan.AllowsSecretaries(currentSecretaries))
                    return false;
            }

            return true;
        }

        private static string? GetUnavailableReason(
            SubscriptionPlan targetPlan,
            PlanName currentPlanName,
            int currentServices,
            int currentSecretaries)
        {
            if (targetPlan.Name == currentPlanName)
                return "Este es el plan actual.";

            if (targetPlan.Name < currentPlanName)
            {
                if (!targetPlan.AllowsServices(currentServices))
                    return "No se puede bajar de plan: excede el límite de servicios.";

                if (!targetPlan.AllowsSecretaries(currentSecretaries))
                    return "No se puede bajar de plan: excede el límite de secretarios.";
            }

            return null;
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

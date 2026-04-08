using AutoMapper;
using BOOKLY.Application.Services.AdminAggregate;
using BOOKLY.Application.Services.AdminAggregate.DTOs;
using BOOKLY.Domain.Aggregates.SubscriptionAggregate;
using BOOKLY.Domain.Queries;

namespace BOOKLY.Application.Mappings
{
    public sealed class AdminMappingProfile : Profile
    {
        public const string TodayContextKey = "Admin.Today";

        public AdminMappingProfile()
        {
            CreateMap<PlanName, AdminPlanDto>()
                .ConvertUsing(planName => AdminContractsSupport.ToPlanDto(planName));

            CreateMap<AdminDashboardSummaryReadModel, AdminDashboardSummaryDto>()
                .ForMember(d => d.TotalActiveSubscriptions, o => o.MapFrom(s => s.ActiveSubscriptions))
                .ForMember(d => d.TotalPaidSubscriptions, o => o.MapFrom(s => s.PaidSubscriptions));

            CreateMap<AdminPlanDistributionReadModel, AdminPlanDistributionItemDto>()
                .ForMember(d => d.Plan, o => o.MapFrom(s => s.PlanName));

            CreateMap<AdminPeriodCountReadModel, AdminTimeSeriesPointDto>()
                .ForMember(d => d.PeriodLabel, o => o.MapFrom(s => AdminContractsSupport.BuildPeriodLabel(s.PeriodStart)));

            CreateMap<AdminServiceTypeUsageReadModel, AdminServiceTypeUsageItemDto>();

            CreateMap<AdminOwnerListItemReadModel, AdminOwnerListItemDto>()
                .ForMember(d => d.FullName, o => o.MapFrom(s => BuildFullName(s.FirstName, s.LastName)))
                .ForMember(d => d.AccountStatus, o => o.MapFrom(s => AdminContractsSupport.GetOwnerAccountStatus(s.Status)))
                .ForMember(d => d.CurrentPlan, o => o.MapFrom(s => s.EffectivePlanName))
                .ForMember(d => d.SubscriptionStatus, o => o.MapFrom((s, _, _, context) =>
                    AdminContractsSupport.GetSubscriptionStatus(
                        s.HasPersistedSubscription,
                        s.SubscriptionStatus,
                        s.SubscriptionEndDate,
                        GetToday(context))));

            CreateMap<AdminServiceListItemReadModel, AdminServiceListItemDto>()
                .ForMember(d => d.Status, o => o.MapFrom(s => AdminContractsSupport.GetServiceStatus(s.IsActive)))
                .ForMember(d => d.OwnerName, o => o.MapFrom(s => BuildFullName(s.OwnerFirstName, s.OwnerLastName)))
                .ForMember(d => d.OwnerAccountStatus, o => o.MapFrom(s => AdminContractsSupport.GetOwnerAccountStatus(s.OwnerStatus)))
                .ForMember(d => d.OwnerPlan, o => o.MapFrom(s => s.OwnerEffectivePlanName));
        }

        private static DateOnly GetToday(ResolutionContext context)
        {
            return context.Items.TryGetValue(TodayContextKey, out var today) &&
                   today is DateOnly value
                ? value
                : DateOnly.FromDateTime(DateTime.UtcNow);
        }

        private static string BuildFullName(string firstName, string lastName)
            => $"{firstName} {lastName}".Trim();
    }
}

using AutoMapper;
using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.AdminAggregate.DTOs;
using BOOKLY.Application.Mappings;
using BOOKLY.Domain.Aggregates.SubscriptionAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Queries;

namespace BOOKLY.Application.Services.AdminAggregate
{
    public sealed class AdminDashboardService : IAdminDashboardService
    {
        private const int RecentActiveOwnerWindowDays = 30;

        private readonly IAdminRepository _adminRepository;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IMapper _mapper;

        public AdminDashboardService(
            IAdminRepository adminRepository,
            IDateTimeProvider dateTimeProvider,
            IMapper mapper)
        {
            _adminRepository = adminRepository;
            _dateTimeProvider = dateTimeProvider;
            _mapper = mapper;
        }

        public async Task<Result<AdminDashboardDto>> GetDashboard(AdminDashboardQueryDto dto, CancellationToken ct = default)
        {
            if (dto.Months < 1 || dto.Months > 24)
            {
                return Result<AdminDashboardDto>.Failure(
                    Error.Validation("Months debe estar entre 1 y 24."));
            }

            var now = _dateTimeProvider.NowArgentina();
            var today = DateOnly.FromDateTime(now);
            var currentMonth = new DateOnly(today.Year, today.Month, 1);
            var fromMonth = currentMonth.AddMonths(-(dto.Months - 1));
            var recentOwnerActivitySince = now.AddDays(-RecentActiveOwnerWindowDays);

            var summary = await _adminRepository.GetDashboardSummary(today, recentOwnerActivitySince, ct);
            var planDistribution = await _adminRepository.GetPlanDistribution(today, ct);
            var ownerRegistrations = await _adminRepository.GetOwnerCreationCountsByMonth(fromMonth, currentMonth, ct);
            var serviceRegistrations = await _adminRepository.GetServiceCreationCountsByMonth(fromMonth, currentMonth, ct);
            var serviceTypeUsage = await _adminRepository.GetServiceTypeUsage(ct);
            var ownerRecentActivity = await _adminRepository.GetOwnerLoginCountsByMonth(fromMonth, currentMonth, ct);
            var mappedPlanDistribution = _mapper.Map<List<AdminPlanDistributionItemDto>>(BuildPlanDistribution(planDistribution));
            var mappedOwnerRegistrations = _mapper.Map<List<AdminTimeSeriesPointDto>>(BuildSeries(fromMonth, currentMonth, ownerRegistrations));
            var mappedServiceRegistrations = _mapper.Map<List<AdminTimeSeriesPointDto>>(BuildSeries(fromMonth, currentMonth, serviceRegistrations));
            var mappedServiceTypeUsage = _mapper.Map<List<AdminServiceTypeUsageItemDto>>(serviceTypeUsage);
            var mappedOwnerRecentActivity = _mapper.Map<List<AdminTimeSeriesPointDto>>(BuildSeries(fromMonth, currentMonth, ownerRecentActivity));

            return Result<AdminDashboardDto>.Success(
                new AdminDashboardDto
                {
                    GeneratedAt = now,
                    RecentMonths = dto.Months,
                    RecentActiveOwnerWindowDays = RecentActiveOwnerWindowDays,
                    Summary = _mapper.Map<AdminDashboardSummaryDto>(summary),
                    PlanDistribution = mappedPlanDistribution,
                    OwnerRegistrations = mappedOwnerRegistrations,
                    ServiceRegistrations = mappedServiceRegistrations,
                    ServiceTypeUsage = mappedServiceTypeUsage,
                    OwnerRecentActivity = mappedOwnerRecentActivity,
                    OwnerStatusDistribution =
                    [
                        new AdminStatusCountDto
                        {
                            Status = "active",
                            Label = AdminContractsSupport.GetOwnerAccountStatusLabel("active"),
                            Total = summary.ActiveOwners
                        },
                        new AdminStatusCountDto
                        {
                            Status = "disabled",
                            Label = AdminContractsSupport.GetOwnerAccountStatusLabel("disabled"),
                            Total = summary.DisabledOwners
                        },
                        new AdminStatusCountDto
                        {
                            Status = "pending_email_confirmation",
                            Label = AdminContractsSupport.GetOwnerAccountStatusLabel("pending_email_confirmation"),
                            Total = summary.PendingConfirmationOwners
                        },
                        new AdminStatusCountDto
                        {
                            Status = "pending_invitation_acceptance",
                            Label = AdminContractsSupport.GetOwnerAccountStatusLabel("pending_invitation_acceptance"),
                            Total = summary.PendingInvitationOwners
                        }
                    ],
                    ServiceStatusDistribution =
                    [
                        new AdminStatusCountDto
                        {
                            Status = "active",
                            Label = AdminContractsSupport.GetServiceStatusLabel("active"),
                            Total = summary.ActiveServices
                        },
                        new AdminStatusCountDto
                        {
                            Status = "disabled",
                            Label = AdminContractsSupport.GetServiceStatusLabel("disabled"),
                            Total = summary.DisabledServices
                        }
                    ]
                });
        }

        private static IReadOnlyCollection<AdminPlanDistributionReadModel> BuildPlanDistribution(
            IReadOnlyCollection<AdminPlanDistributionReadModel> planDistribution)
        {
            var totalsByPlan = planDistribution.ToDictionary(item => item.PlanName, item => item.TotalOwners);

            return new[]
            {
                PlanName.Free,
                PlanName.Pro,
                PlanName.Max
            }
            .Select(planName => new AdminPlanDistributionReadModel(planName, totalsByPlan.GetValueOrDefault(planName, 0)))
            .ToList();
        }

        private static IReadOnlyCollection<AdminPeriodCountReadModel> BuildSeries(
            DateOnly fromMonth,
            DateOnly toMonth,
            IReadOnlyCollection<AdminPeriodCountReadModel> source)
        {
            var totalsByMonth = source.ToDictionary(item => item.PeriodStart, item => item.Total);
            var monthCount = ((toMonth.Year - fromMonth.Year) * 12) + toMonth.Month - fromMonth.Month + 1;

            return Enumerable.Range(0, monthCount)
                .Select(offset =>
                {
                    var periodStart = fromMonth.AddMonths(offset);
                    return new AdminPeriodCountReadModel(
                        periodStart,
                        totalsByMonth.GetValueOrDefault(periodStart, 0));
                })
                .ToList();
        }
    }
}

using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.MetricsAggregate.DTOs;
using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Queries;

namespace BOOKLY.Application.Services.MetricsAggregate
{
    public sealed class MetricsService : IMetricsService
    {
        private const int TopBucketCount = 3;
        private static readonly (DayOfWeek DayOfWeek, string Label)[] OrderedWeekdays =
        [
            (DayOfWeek.Monday, "Lunes"),
            (DayOfWeek.Tuesday, "Martes"),
            (DayOfWeek.Wednesday, "Miercoles"),
            (DayOfWeek.Thursday, "Jueves"),
            (DayOfWeek.Friday, "Viernes"),
            (DayOfWeek.Saturday, "Sabado"),
            (DayOfWeek.Sunday, "Domingo")
        ];

        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IUserRepository _userRepository;

        public MetricsService(
            IAppointmentRepository appointmentRepository,
            IServiceRepository serviceRepository,
            IUserRepository userRepository)
        {
            _appointmentRepository = appointmentRepository;
            _serviceRepository = serviceRepository;
            _userRepository = userRepository;
        }

        public async Task<Result<AppointmentMetricsDto>> GetAppointmentMetrics(
            AppointmentMetricsQueryDto dto,
            CancellationToken ct = default)
        {
            if (!dto.From.HasValue || !dto.To.HasValue)
                return Result<AppointmentMetricsDto>.Failure(
                    Error.Validation("Debe indicar el rango from/to."));

            if (dto.From.Value > dto.To.Value)
                return Result<AppointmentMetricsDto>.Failure(
                    Error.Validation("El rango de fechas es invalido."));

            if (dto.SecretaryId.HasValue && dto.SecretaryId.Value <= 0)
                return Result<AppointmentMetricsDto>.Failure(
                    Error.Validation("El secretaryId indicado no es valido."));

            var scopedServicesResult = await ResolveScopedServices(dto.OwnerId, dto.ServiceId, ct);
            if (scopedServicesResult.IsFailure)
                return Result<AppointmentMetricsDto>.Failure(scopedServicesResult.Error!);

            var services = scopedServicesResult.Data!;
            var currentFrom = dto.From.Value;
            var currentTo = dto.To.Value;
            var (previousFrom, previousTo) = BuildPreviousPeriod(currentFrom, currentTo);

            if (services.Count == 0)
            {
                var emptyByDay = BuildDayBuckets(currentFrom, currentTo, []);
                var emptyByHour = BuildHourBuckets([]);
                var emptyByWeekday = BuildWeekdayBuckets([]);

                return Result<AppointmentMetricsDto>.Success(new AppointmentMetricsDto
                {
                    From = currentFrom,
                    To = currentTo,
                    PreviousFrom = previousFrom,
                    PreviousTo = previousTo,
                    TotalAppointments = 0,
                    PreviousPeriodTotal = 0,
                    AbsoluteChange = 0,
                    PercentageChange = 0,
                    CancellationRate = 0,
                    NoShowRate = 0,
                    AttendanceRate = 0,
                    AppointmentsByDay = emptyByDay,
                    AppointmentsByHour = emptyByHour,
                    AppointmentsByWeekday = emptyByWeekday,
                    BusiestDays = [],
                    BusiestHours = []
                });
            }

            var serviceIds = services.Select(service => service.Id).ToList();

            var totalAppointments = await _appointmentRepository.CountByServices(
                serviceIds,
                currentFrom,
                currentTo,
                dto.SecretaryId,
                ct);

            var previousPeriodTotal = await _appointmentRepository.CountByServices(
                serviceIds,
                previousFrom,
                previousTo,
                dto.SecretaryId,
                ct);

            var statusCounts = await _appointmentRepository.GetStatusCountsByServices(
                serviceIds,
                currentFrom,
                currentTo,
                dto.SecretaryId,
                ct);

            var dayCounts = await _appointmentRepository.GetDayCountsByServices(
                serviceIds,
                currentFrom,
                currentTo,
                dto.SecretaryId,
                ct);

            var hourCounts = await _appointmentRepository.GetHourCountsByServices(
                serviceIds,
                currentFrom,
                currentTo,
                dto.SecretaryId,
                ct);

            var weekdayCounts = await _appointmentRepository.GetWeekdayCountsByServices(
                serviceIds,
                currentFrom,
                currentTo,
                dto.SecretaryId,
                ct);
            var appointmentsByDay = BuildDayBuckets(currentFrom, currentTo, dayCounts);
            var appointmentsByHour = BuildHourBuckets(hourCounts);
            var appointmentsByWeekday = BuildWeekdayBuckets(weekdayCounts);

            var cancelledCount = statusCounts
                .FirstOrDefault(x => x.Status == AppointmentStatus.Cancelled)?
                .TotalAppointments ?? 0;

            var noShowCount = statusCounts
                .FirstOrDefault(x => x.Status == AppointmentStatus.NoShow)?
                .TotalAppointments ?? 0;

            var attendedCount = statusCounts
                .FirstOrDefault(x => x.Status == AppointmentStatus.Attended)?
                .TotalAppointments ?? 0;

            return Result<AppointmentMetricsDto>.Success(new AppointmentMetricsDto
            {
                From = currentFrom,
                To = currentTo,
                PreviousFrom = previousFrom,
                PreviousTo = previousTo,
                TotalAppointments = totalAppointments,
                PreviousPeriodTotal = previousPeriodTotal,
                AbsoluteChange = totalAppointments - previousPeriodTotal,
                PercentageChange = CalculatePercentageChange(totalAppointments, previousPeriodTotal),
                CancellationRate = CalculateRate(cancelledCount, totalAppointments),
                NoShowRate = CalculateRate(noShowCount, totalAppointments),
                AttendanceRate = CalculateRate(attendedCount, totalAppointments),
                AppointmentsByDay = appointmentsByDay,
                AppointmentsByHour = appointmentsByHour,
                AppointmentsByWeekday = appointmentsByWeekday,
                BusiestDays = appointmentsByDay
                    .Where(x => x.TotalAppointments > 0)
                    .OrderByDescending(x => x.TotalAppointments)
                    .ThenBy(x => x.Date)
                    .Take(TopBucketCount)
                    .ToList(),
                BusiestHours = appointmentsByHour
                    .Where(x => x.TotalAppointments > 0)
                    .OrderByDescending(x => x.TotalAppointments)
                    .ThenBy(x => x.Hour)
                    .Take(TopBucketCount)
                    .ToList()
            });
        }

        private async Task<Result<List<Service>>> ResolveScopedServices(int? ownerId, int? serviceId, CancellationToken ct)
        {
            if (serviceId.HasValue)
            {
                var service = await _serviceRepository.GetOne(serviceId.Value, ct);
                if (service == null)
                    return Result<List<Service>>.Failure(Error.NotFound("Servicio"));

                if (ownerId.HasValue && service.OwnerId != ownerId.Value)
                    return Result<List<Service>>.Failure(
                        Error.Validation("El servicio no pertenece al owner indicado."));

                return Result<List<Service>>.Success([service]);
            }

            if (!ownerId.HasValue || ownerId.Value <= 0)
                return Result<List<Service>>.Failure(
                    Error.Validation("Debe indicar ownerId o serviceId."));

            var owner = await _userRepository.GetOne(ownerId.Value, ct);
            if (owner == null || owner.Role != UserKind.Owner)
                return Result<List<Service>>.Failure(Error.NotFound("Usuario"));

            var services = await _serviceRepository.GetServicesByOwner(ownerId.Value, ct);
            return Result<List<Service>>.Success(services);
        }

        private static (DateOnly PreviousFrom, DateOnly PreviousTo) BuildPreviousPeriod(DateOnly from, DateOnly to)
        {
            var totalDays = to.DayNumber - from.DayNumber + 1;
            var previousTo = from.AddDays(-1);
            var previousFrom = previousTo.AddDays(-(totalDays - 1));
            return (previousFrom, previousTo);
        }

        private static IReadOnlyCollection<AppointmentMetricsDayBucketDto> BuildDayBuckets(
            DateOnly from,
            DateOnly to,
            IReadOnlyCollection<AppointmentDayCountResult> dayCounts)
        {
            var totalsByDate = dayCounts.ToDictionary(x => x.Date, x => x.TotalAppointments);
            var totalDays = to.DayNumber - from.DayNumber + 1;

            return Enumerable
                .Range(0, totalDays)
                .Select(offset =>
                {
                    var date = from.AddDays(offset);
                    return new AppointmentMetricsDayBucketDto
                    {
                        Date = date,
                        TotalAppointments = totalsByDate.GetValueOrDefault(date, 0)
                    };
                })
                .ToList();
        }

        private static IReadOnlyCollection<AppointmentMetricsHourBucketDto> BuildHourBuckets(
            IReadOnlyCollection<AppointmentHourCountResult> hourCounts)
        {
            var totalsByHour = hourCounts.ToDictionary(x => x.Hour, x => x.TotalAppointments);

            return Enumerable
                .Range(0, 24)
                .Select(hour => new AppointmentMetricsHourBucketDto
                {
                    Hour = hour,
                    Label = $"{hour:00}:00",
                    TotalAppointments = totalsByHour.GetValueOrDefault(hour, 0)
                })
                .ToList();
        }

        private static IReadOnlyCollection<AppointmentMetricsWeekdayBucketDto> BuildWeekdayBuckets(
            IReadOnlyCollection<AppointmentWeekdayCountResult> weekdayCounts)
        {
            var totalsByWeekday = weekdayCounts.ToDictionary(x => x.DayOfWeek, x => x.TotalAppointments);

            return OrderedWeekdays
                .Select(weekday => new AppointmentMetricsWeekdayBucketDto
                {
                    DayOfWeek = (int)weekday.DayOfWeek,
                    Label = weekday.Label,
                    TotalAppointments = totalsByWeekday.GetValueOrDefault((int)weekday.DayOfWeek, 0)
                })
                .ToList();
        }

        private static decimal CalculateRate(int value, int total)
        {
            if (total <= 0)
                return 0;

            return decimal.Round((value * 100m) / total, 2, MidpointRounding.AwayFromZero);
        }

        private static decimal? CalculatePercentageChange(int currentTotal, int previousTotal)
        {
            if (previousTotal == 0)
                return currentTotal == 0 ? 0 : null;

            return decimal.Round(
                ((currentTotal - previousTotal) * 100m) / previousTotal,
                2,
                MidpointRounding.AwayFromZero);
        }
    }
}

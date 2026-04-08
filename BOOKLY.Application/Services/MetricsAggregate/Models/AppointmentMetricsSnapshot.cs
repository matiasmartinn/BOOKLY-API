using BOOKLY.Domain.Queries;

namespace BOOKLY.Application.Services.MetricsAggregate.Models
{
    internal sealed record AppointmentMetricsSnapshot
    {
        public DateOnly From { get; init; }
        public DateOnly To { get; init; }
        public DateOnly PreviousFrom { get; init; }
        public DateOnly PreviousTo { get; init; }
        public int TotalAppointments { get; init; }
        public int PreviousPeriodTotal { get; init; }
        public int AbsoluteChange { get; init; }
        public decimal? PercentageChange { get; init; }
        public decimal CancellationRate { get; init; }
        public decimal NoShowRate { get; init; }
        public decimal AttendanceRate { get; init; }
        public IReadOnlyCollection<AppointmentDayCountResult> AppointmentsByDay { get; init; } = [];
        public IReadOnlyCollection<AppointmentHourCountResult> AppointmentsByHour { get; init; } = [];
        public IReadOnlyCollection<AppointmentMetricsWeekdayBucketSource> AppointmentsByWeekday { get; init; } = [];
        public IReadOnlyCollection<AppointmentDayCountResult> BusiestDays { get; init; } = [];
        public IReadOnlyCollection<AppointmentHourCountResult> BusiestHours { get; init; } = [];
    }

    internal sealed record AppointmentMetricsWeekdayBucketSource(
        int DayOfWeek,
        string Label,
        int TotalAppointments);
}

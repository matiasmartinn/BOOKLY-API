namespace BOOKLY.Application.Services.MetricsAggregate.DTOs
{
    public sealed record AppointmentMetricsDto
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
        public IReadOnlyCollection<AppointmentMetricsDayBucketDto> AppointmentsByDay { get; init; } = [];
        public IReadOnlyCollection<AppointmentMetricsHourBucketDto> AppointmentsByHour { get; init; } = [];
        public IReadOnlyCollection<AppointmentMetricsWeekdayBucketDto> AppointmentsByWeekday { get; init; } = [];
        public IReadOnlyCollection<AppointmentMetricsDayBucketDto> BusiestDays { get; init; } = [];
        public IReadOnlyCollection<AppointmentMetricsHourBucketDto> BusiestHours { get; init; } = [];
    }

    public sealed record AppointmentMetricsDayBucketDto
    {
        public DateOnly Date { get; init; }
        public int TotalAppointments { get; init; }
    }

    public sealed record AppointmentMetricsHourBucketDto
    {
        public int Hour { get; init; }
        public string Label { get; init; } = null!;
        public int TotalAppointments { get; init; }
    }

    public sealed record AppointmentMetricsWeekdayBucketDto
    {
        public int DayOfWeek { get; init; }
        public string Label { get; init; } = null!;
        public int TotalAppointments { get; init; }
    }
}

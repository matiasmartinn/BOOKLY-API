using BOOKLY.Domain.Aggregates.AppointmentAggregate;

namespace BOOKLY.Domain.Queries
{
    public sealed record AppointmentStatusCountResult(
        AppointmentStatus Status,
        int TotalAppointments);

    public sealed record AppointmentDayCountResult(
        DateOnly Date,
        int TotalAppointments);

    public sealed record AppointmentHourCountResult(
        int Hour,
        int TotalAppointments);

    public sealed record AppointmentWeekdayCountResult(
        int DayOfWeek,
        int TotalAppointments);
}

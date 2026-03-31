using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Entities;
using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;
using BOOKLY.Domain.DomainServices;
using BOOKLY.Domain.SharedKernel;

namespace BOOKLY.Domain.Tests;

public sealed class AvailabilityServiceTests
{
    private readonly AvailabilityService _availabilityService = new();
    private static readonly DateTime ReferenceNow = DateTime.Today.AddDays(10).AddHours(10);

    [Fact]
    public void GetAvailableSlots_ShouldExcludeBlockedAndOccupiedSlots()
    {
        var date = DateOnly.FromDateTime(ReferenceNow.AddDays(1));
        var service = CreateServiceWithSchedules(date, 9, 12, 60);

        service.AddUnavailability(
            DateRange.Create(date, date),
            TimeRange.Create(new TimeOnly(10, 0), new TimeOnly(11, 0)),
            "Bloqueo parcial");

        var appointments = new List<Appointment>
        {
            Appointment.CreateHistorical(
                1,
                null,
                CreateClient(),
                date.ToDateTime(new TimeOnly(9, 0)),
                Duration.Create(60),
                AppointmentStatus.Pending,
                null,
                null,
                ReferenceNow)
        };

        var slots = _availabilityService.GetAvailableSlots(service, appointments, date, ReferenceNow);

        Assert.Single(slots);
        Assert.Equal(date.ToDateTime(new TimeOnly(11, 0)), slots.Single());
    }

    [Fact]
    public void GetAvailableDates_ShouldReturnOnlyDatesWithSlots()
    {
        var firstDate = DateOnly.FromDateTime(ReferenceNow.AddDays(1));
        var secondDate = firstDate.AddDays(1);
        var service = CreateServiceWithSchedules(firstDate, 9, 10, 60, secondDate);

        service.AddUnavailability(
            DateRange.Create(firstDate, firstDate),
            null,
            "Dia completo bloqueado");

        var appointments = Array.Empty<Appointment>();

        var dates = _availabilityService.GetAvailableDates(service, appointments, firstDate, secondDate, ReferenceNow);

        Assert.Single(dates);
        Assert.Equal(secondDate, dates.Single());
    }

    [Fact]
    public void GetAvailableSlots_ShouldRespectScheduleCapacity()
    {
        var date = DateOnly.FromDateTime(ReferenceNow.AddDays(1));
        var service = CreateServiceWithSchedules(date, 9, 11, 60, null, 2);

        var appointments = new List<Appointment>
        {
            Appointment.CreateHistorical(
                1,
                null,
                CreateClient(),
                date.ToDateTime(new TimeOnly(9, 0)),
                Duration.Create(60),
                AppointmentStatus.Pending,
                null,
                null,
                ReferenceNow)
        };

        var slotsWithOneAppointment = _availabilityService.GetAvailableSlots(service, appointments, date, ReferenceNow);
        Assert.Contains(date.ToDateTime(new TimeOnly(9, 0)), slotsWithOneAppointment);

        appointments.Add(
            Appointment.CreateHistorical(
                1,
                null,
                CreateClient(),
                date.ToDateTime(new TimeOnly(9, 0)),
                Duration.Create(60),
                AppointmentStatus.Pending,
                null,
                null,
                ReferenceNow));

        var slotsWithTwoAppointments = _availabilityService.GetAvailableSlots(service, appointments, date, ReferenceNow);
        Assert.DoesNotContain(date.ToDateTime(new TimeOnly(9, 0)), slotsWithTwoAppointments);
    }

    private static Service CreateServiceWithSchedules(
        DateOnly firstDate,
        int startHour,
        int endHour,
        int durationMinutes,
        DateOnly? secondDate = null,
        int serviceCapacity = 1,
        int? scheduleCapacity = null)
    {
        var service = Service.Create(
            "Consulta general",
            1,
            "consulta-general",
            "Servicio de prueba",
            null,
            null,
            null,
            1,
            Duration.Create(durationMinutes),
            Capacity.Create(serviceCapacity),
            Mode.Presence,
            1000m);

        var schedules = new List<ServiceSchedule>
        {
            ServiceSchedule.Create(
                TimeRange.Create(new TimeOnly(startHour, 0), new TimeOnly(endHour, 0)),
                Capacity.Create(scheduleCapacity ?? serviceCapacity),
                Day.Create((int)firstDate.DayOfWeek))
        };

        if (secondDate.HasValue && secondDate.Value.DayOfWeek != firstDate.DayOfWeek)
        {
            schedules.Add(
                ServiceSchedule.Create(
                    TimeRange.Create(new TimeOnly(startHour, 0), new TimeOnly(endHour, 0)),
                    Capacity.Create(scheduleCapacity ?? serviceCapacity),
                    Day.Create((int)secondDate.Value.DayOfWeek)));
        }

        service.SetSchedules(schedules);
        return service;
    }

    private static ClientInfo CreateClient()
    {
        return ClientInfo.Create(
            "Grace Hopper",
            "1199988877",
            Email.Create("grace@example.com"));
    }
}

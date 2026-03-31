using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.AppointmentAggregate.Events;
using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;
using BOOKLY.Domain.Exceptions;
using BOOKLY.Domain.SharedKernel;

namespace BOOKLY.Domain.Tests;

public sealed class AppointmentAggregateTests
{
    private static readonly DateTime ReferenceNow = new(2026, 3, 27, 10, 0, 0);

    [Fact]
    public void Create_ShouldInitializePendingAppointment()
    {
        var start = ReferenceNow.AddDays(1);

        var appointment = Appointment.Create(
            1,
            null,
            CreateClient(),
            start,
            Duration.Create(60),
            "Primera consulta",
            ReferenceNow);

        Assert.Equal(AppointmentStatus.Pending, appointment.Status);
        Assert.Equal(start, appointment.StartDateTime);
        Assert.Equal(start.AddHours(1), appointment.EndDateTime);
        Assert.Equal(60, appointment.Duration.Value);
    }

    [Fact]
    public void Create_ShouldStoreActorUserIdInCreatedEvent_WhenActionComesFromSystemUser()
    {
        var appointment = Appointment.Create(
            1,
            null,
            CreateClient(),
            ReferenceNow.AddDays(1),
            Duration.Create(60),
            null,
            ReferenceNow,
            actorUserId: 42);

        var createdEvent = Assert.IsType<AppointmentCreatedEvent>(Assert.Single(appointment.DomainEvents));

        Assert.Equal(42, createdEvent.UserId);
    }

    [Fact]
    public void Reschedule_ShouldUpdateDates_WhenAppointmentIsPending()
    {
        var appointment = Appointment.Create(
            1,
            null,
            CreateClient(),
            ReferenceNow.AddDays(1),
            Duration.Create(30),
            null,
            ReferenceNow);

        var newStart = ReferenceNow.AddDays(2).AddHours(1);

        appointment.Reschedule(newStart, Duration.Create(45), ReferenceNow);

        Assert.Equal(newStart, appointment.StartDateTime);
        Assert.Equal(newStart.AddMinutes(45), appointment.EndDateTime);
        Assert.Equal(45, appointment.Duration.Value);
        Assert.NotNull(appointment.UpdateOn);
    }

    [Fact]
    public void Reschedule_ShouldThrow_WhenAppointmentIsNotPending()
    {
        var appointment = Appointment.CreateHistorical(
            1,
            null,
            CreateClient(),
            ReferenceNow.AddDays(1),
            Duration.Create(30),
            AppointmentStatus.Cancelled,
            null,
            null,
            ReferenceNow);

        var action = () => appointment.Reschedule(ReferenceNow.AddDays(2), Duration.Create(30), ReferenceNow);

        var exception = Assert.Throws<DomainException>(action);
        Assert.Equal("Solo los turnos pendientes pueden reprogramarse.", exception.Message);
    }

    [Fact]
    public void MarkAsAttended_ShouldChangeStatus_WhenAppointmentAlreadyStarted()
    {
        var appointment = Appointment.CreateHistorical(
            1,
            null,
            CreateClient(),
            ReferenceNow.AddHours(-2),
            Duration.Create(30),
            AppointmentStatus.Pending,
            null,
            null,
            ReferenceNow);

        appointment.MarkAsAttended(ReferenceNow);

        Assert.Equal(AppointmentStatus.Attended, appointment.Status);
        Assert.NotNull(appointment.UpdateOn);
    }

    [Fact]
    public void MarkAsNoShow_ShouldChangeStatus_WhenAppointmentAlreadyStarted()
    {
        var appointment = Appointment.CreateHistorical(
            1,
            null,
            CreateClient(),
            ReferenceNow.AddHours(-2),
            Duration.Create(30),
            AppointmentStatus.Pending,
            null,
            null,
            ReferenceNow);

        appointment.MarkAsNoShow(ReferenceNow);

        Assert.Equal(AppointmentStatus.NoShow, appointment.Status);
        Assert.NotNull(appointment.UpdateOn);
    }

    [Fact]
    public void MarkAsCancel_ShouldStoreActorUserIdInStatusChangedEvent_WhenActionComesFromSystemUser()
    {
        var appointment = Appointment.CreateHistorical(
            1,
            null,
            CreateClient(),
            ReferenceNow.AddHours(-2),
            Duration.Create(30),
            AppointmentStatus.Pending,
            null,
            null,
            ReferenceNow);

        appointment.ClearDomainEvents();
        appointment.MarkAsCancel("Agenda interna", ReferenceNow, actorUserId: 99);

        var statusChangedEvent = Assert.IsType<AppointmentStatusChangedEvent>(Assert.Single(appointment.DomainEvents));

        Assert.Equal(99, statusChangedEvent.UserId);
    }

    [Fact]
    public void MarkAsNoShow_ShouldThrow_WhenAppointmentIsNotPending()
    {
        var appointment = Appointment.CreateHistorical(
            1,
            null,
            CreateClient(),
            ReferenceNow.AddHours(-2),
            Duration.Create(30),
            AppointmentStatus.Attended,
            null,
            null,
            ReferenceNow);

        var action = () => appointment.MarkAsNoShow(ReferenceNow);

        var exception = Assert.Throws<DomainException>(action);
        Assert.Equal("Solo los turnos pendientes pueden marcarse como ausentes.", exception.Message);
    }

    [Fact]
    public void ChangeClientNotes_ShouldUpdateClientComment_WithoutTouchingCancelReason()
    {
        var appointment = Appointment.Create(
            1,
            null,
            CreateClient(),
            ReferenceNow.AddDays(1),
            Duration.Create(30),
            "Comentario inicial",
            ReferenceNow);

        appointment.ChangeClientNotes("Comentario actualizado", ReferenceNow);

        Assert.Equal("Comentario actualizado", appointment.ClientNotes);
        Assert.Null(appointment.CancelReason);
    }

    [Fact]
    public void MarkAsCancel_ShouldStoreCancelReason_WithoutRemovingClientComment()
    {
        var appointment = Appointment.CreateHistorical(
            1,
            null,
            CreateClient(),
            ReferenceNow.AddHours(-2),
            Duration.Create(30),
            AppointmentStatus.Pending,
            "Cliente pidió silla de ruedas",
            null,
            ReferenceNow);

        appointment.MarkAsCancel("No puede asistir", ReferenceNow);

        Assert.Equal("Cliente pidió silla de ruedas", appointment.ClientNotes);
        Assert.Equal("No puede asistir", appointment.CancelReason);
    }

    private static ClientInfo CreateClient()
    {
        return ClientInfo.Create(
            "Ada Lovelace",
            "1122334455",
            Email.Create("ada@example.com"));
    }
}

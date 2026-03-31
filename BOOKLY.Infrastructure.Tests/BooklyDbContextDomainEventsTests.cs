using BOOKLY.Application.EventHandler;
using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.AppointmentAggregate.Events;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.SharedKernel;
using BOOKLY.Infrastructure;
using BOOKLY.Infrastructure.Persistence;
using BOOKLY.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BOOKLY.Infrastructure.Tests;

public sealed class BooklyDbContextDomainEventsTests
{
    private static readonly DateTime ReferenceNow = new(2026, 3, 27, 10, 0, 0);

    [Fact]
    public async Task SaveChanges_ShouldPersistHistory_WhenAppointmentIsCreated()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var scope = BuildServices(connection).CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();
        await context.Database.EnsureCreatedAsync();

        var appointment = Appointment.Create(
            1,
            null,
            CreateClient(),
            ReferenceNow.AddDays(1),
            BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects.Duration.Create(60),
            null,
            ReferenceNow);

        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        var history = await context.AppointmentStatusHistories
            .OrderBy(x => x.OccurredOn)
            .ToListAsync();

        Assert.Single(history);
        Assert.Equal(appointment.Id, history[0].AppointmentId);
        Assert.Equal(AppointmentStatus.Pending, history[0].NewStatus);
        Assert.Null(history[0].OldStatus);
        Assert.Null(history[0].UserId);
    }

    [Fact]
    public async Task SaveChanges_ShouldPersistHistory_WhenAppointmentStatusChanges()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var scope = BuildServices(connection).CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();
        await context.Database.EnsureCreatedAsync();

        var appointment = Appointment.Create(
            1,
            null,
            CreateClient(),
            ReferenceNow.AddDays(1),
            BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects.Duration.Create(60),
            null,
            ReferenceNow);

        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        appointment.MarkAsCancel("Cliente no puede asistir", ReferenceNow.AddDays(1).AddHours(1));
        context.Appointments.Update(appointment);
        await context.SaveChangesAsync();

        var history = await context.AppointmentStatusHistories
            .OrderBy(x => x.OccurredOn)
            .ToListAsync();

        Assert.Equal(2, history.Count);
        Assert.Equal(AppointmentStatus.Pending, history[1].OldStatus);
        Assert.Equal(AppointmentStatus.Cancelled, history[1].NewStatus);
        Assert.Equal("Cliente no puede asistir", history[1].Reason);
        Assert.Null(history[1].UserId);
    }

    [Fact]
    public async Task SaveChanges_ShouldPersistActorUser_WhenActionComesFromSystemUser()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var scope = BuildServices(connection).CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();
        var historyRepository = scope.ServiceProvider.GetRequiredService<IAppointmentHistoryRepository>();
        await context.Database.EnsureCreatedAsync();

        var actor = User.CreateSecretary(
            PersonName.Create("Ada", "Lovelace"),
            BOOKLY.Domain.SharedKernel.Email.Create("ada@example.com"),
            ReferenceNow);

        context.Users.Add(actor);
        await context.SaveChangesAsync();

        var appointment = Appointment.Create(
            1,
            null,
            CreateClient(),
            ReferenceNow.AddDays(1),
            BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects.Duration.Create(60),
            null,
            ReferenceNow,
            actor.Id);

        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        appointment.MarkAsCancel("Agenda interna", ReferenceNow.AddDays(1).AddHours(1), actor.Id);
        context.Appointments.Update(appointment);
        await context.SaveChangesAsync();

        var history = await historyRepository.GetByAppointment(appointment.Id);

        Assert.Equal(2, history.Count);
        Assert.All(history, item => Assert.Equal(actor.Id, item.UserId));
        Assert.All(history, item => Assert.NotNull(item.User));
        Assert.Equal("Ada", history[0].User!.PersonName.FirstName);
        Assert.Equal("Lovelace", history[0].User!.PersonName.LastName);
    }

    private static ServiceProvider BuildServices(SqliteConnection connection)
    {
        var services = new ServiceCollection();

        services.AddDbContext<BooklyDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IAppointmentHistoryRepository, AppointmentHistoryRepository>();
        services.AddScoped<IDomainEventHandler<AppointmentCreatedEvent>, RecordAppointmentCreatedHandler>();
        services.AddScoped<IDomainEventHandler<AppointmentStatusChangedEvent>, RecordStatusChangedHandler>();

        return services.BuildServiceProvider();
    }

    private static ClientInfo CreateClient()
    {
        return ClientInfo.Create(
            "Linus Torvalds",
            "1133344455",
            BOOKLY.Domain.SharedKernel.Email.Create("linus@example.com"));
    }
}

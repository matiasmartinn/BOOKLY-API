using BOOKLY.Application.EventHandler;
using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.AppointmentAggregate.Events;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Enums;
using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.ValueObjects;
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
    private static readonly DateTime ReferenceNow = new(2026, 3, 27, 10, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public async Task SaveChanges_ShouldPersistHistory_WhenAppointmentIsCreated()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var scope = BuildServices(connection).CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();
        await context.Database.EnsureCreatedAsync();
        var seed = await SeedAsync(context);

        var appointment = Appointment.Create(
            seed.Service.Id,
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
        var seed = await SeedAsync(context);

        var appointment = Appointment.Create(
            seed.Service.Id,
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
        var seed = await SeedAsync(context);

        var actor = User.CreateSecretary(
            PersonName.Create("Ada", "Lovelace"),
            BOOKLY.Domain.SharedKernel.Email.Create("ada@example.com"),
            ReferenceNow);

        context.Users.Add(actor);
        await context.SaveChangesAsync();

        var appointment = Appointment.Create(
            seed.Service.Id,
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

    [Fact]
    public async Task SaveChanges_ShouldNormalizeOnlyTimestampWithoutTimeZoneDateTimes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var scope = BuildServices(connection).CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();
        await context.Database.EnsureCreatedAsync();
        var seed = await SeedAsync(context);
        var utcNow = new DateTime(2026, 3, 27, 13, 0, 0, DateTimeKind.Utc);

        var appointment = Appointment.Create(
            seed.Service.Id,
            null,
            CreateClient(),
            utcNow.AddDays(1),
            BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects.Duration.Create(60),
            null,
            utcNow);
        var refreshToken = RefreshToken.Create(seed.Owner.Id, RefreshToken.GenerateToken(), utcNow);

        context.Appointments.Add(appointment);
        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        var history = await context.AppointmentStatusHistories.SingleAsync();

        Assert.Equal(DateTimeKind.Unspecified, appointment.StartDateTime.Kind);
        Assert.Equal(DateTimeKind.Unspecified, appointment.CreatedOn.Kind);
        Assert.Equal(DateTimeKind.Unspecified, history.OccurredOn.Kind);
        Assert.Equal(DateTimeKind.Utc, refreshToken.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, refreshToken.ExpiresAt.Kind);
    }

    private static async Task<SeedData> SeedAsync(BooklyDbContext context)
    {
        var owner = User.CreateOwner(
            PersonName.Create("Grace", "Hopper"),
            BOOKLY.Domain.SharedKernel.Email.Create("grace.owner@example.com"),
            Password.FromHash("hashed-password"),
            ReferenceNow);

        context.Users.Add(owner);

        var serviceType = ServiceType.Create(
            "Consultas",
            "consultas");

        context.ServiceTypes.Add(serviceType);

        await context.SaveChangesAsync();

        var service = Service.Create(
            "Consulta general",
            owner.Id,
            "consulta-general",
            description: null,
            phoneNumber: null,
            serviceType: serviceType.Id,
            createdAt: ReferenceNow,
            duration: Duration.Create(60),
            capacity: Capacity.Create(1),
            mode: Mode.Presence,
            price: null);

        context.Services.Add(service);
        await context.SaveChangesAsync();

        return new SeedData(service, owner);
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

    private sealed record SeedData(Service Service, User Owner);
}

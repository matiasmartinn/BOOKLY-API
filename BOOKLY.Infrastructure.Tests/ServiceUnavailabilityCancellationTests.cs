using AutoMapper;
using BOOKLY.Application.Common;
using BOOKLY.Application.EventHandler;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services;
using BOOKLY.Application.Services.AppointmentAggregate;
using BOOKLY.Application.Services.ServiceAggregate;
using BOOKLY.Application.Services.ServiceAggregate.DTOs;
using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.AppointmentAggregate.Events;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.ValueObjects;
using BOOKLY.Domain.DomainServices;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Repositories;
using BOOKLY.Domain.SharedKernel;
using BOOKLY.Infrastructure.Email;
using BOOKLY.Infrastructure.Persistence;
using BOOKLY.Infrastructure.Persistence.Repositories;
using BOOKLY.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BOOKLY.Infrastructure.Tests;

public sealed class ServiceUnavailabilityCancellationTests
{
    private static readonly DateTime ReferenceNow = new(2026, 4, 8, 10, 0, 0);

    [Fact]
    public async Task AddUnavailability_ShouldCancelAffectedPendingFutureAppointments_WhenFlagIsEnabled()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var scope = BuildServices(connection).CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();
        await context.Database.EnsureCreatedAsync();

        var seed = await SeedAsync(context);
        var notifier = new SpyAppointmentCancellationNotificationService(context);
        var sut = CreateSut(context, notifier);

        var result = await sut.AddUnavailability(
            seed.Service.Id,
            new CreateUnavailabilityDto
            {
                StartDate = seed.FutureDate,
                EndDate = seed.FutureDate,
                StartTime = new TimeOnly(10, 30),
                EndTime = new TimeOnly(11, 30),
                Reason = "Capacitacion interna",
                CancelAffectedAppointments = true,
                UserId = seed.Owner.Id
            });

        Assert.True(result.IsSuccess);

        var cancelled = await context.Appointments.AsNoTracking().SingleAsync(a => a.Id == seed.CancelledAppointmentId);
        var untouchedFuturePending = await context.Appointments.AsNoTracking().SingleAsync(a => a.Id == seed.UnaffectedFuturePendingAppointmentId);
        var pastPending = await context.Appointments.AsNoTracking().SingleAsync(a => a.Id == seed.PastPendingAppointmentId);
        var attended = await context.Appointments.AsNoTracking().SingleAsync(a => a.Id == seed.AttendedAppointmentId);

        Assert.Equal(AppointmentStatus.Cancelled, cancelled.Status);
        Assert.Equal("Cancelado por excepcion de agenda: Capacitacion interna", cancelled.CancelReason);
        Assert.Equal(AppointmentStatus.Pending, untouchedFuturePending.Status);
        Assert.Equal(AppointmentStatus.Pending, pastPending.Status);
        Assert.Equal(AppointmentStatus.Attended, attended.Status);

        var history = await context.AppointmentStatusHistories
            .AsNoTracking()
            .Where(h => h.AppointmentId == seed.CancelledAppointmentId)
            .ToListAsync();

        var cancellationHistory = Assert.Single(history);
        Assert.Equal(AppointmentStatus.Pending, cancellationHistory.OldStatus);
        Assert.Equal(AppointmentStatus.Cancelled, cancellationHistory.NewStatus);
        Assert.Equal("Cancelado por excepcion de agenda: Capacitacion interna", cancellationHistory.Reason);
        Assert.Equal(seed.Owner.Id, cancellationHistory.UserId);

        var savedService = await context.Services
            .Include(service => service.ServicesUnavailability)
            .SingleAsync(service => service.Id == seed.Service.Id);

        var savedUnavailability = Assert.Single(savedService.ServicesUnavailability);
        Assert.Equal(seed.FutureDate, savedUnavailability.DateRange.Start);
        Assert.Equal(seed.FutureDate, savedUnavailability.DateRange.End);

        var notification = Assert.Single(notifier.Calls);
        Assert.Equal(seed.CancelledAppointmentId, notification.AppointmentId);
        Assert.False(notification.NotifyOwner);
        Assert.Equal(AppointmentStatus.Cancelled, notification.PersistedStatus);
        Assert.Equal(1, notification.CancelHistoryEntriesAtNotificationTime);
    }

    [Fact]
    public async Task AddUnavailability_ShouldPreservePreviousBehavior_WhenCancellationFlagIsDisabled()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var scope = BuildServices(connection).CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();
        await context.Database.EnsureCreatedAsync();

        var seed = await SeedAsync(context);
        var notifier = new SpyAppointmentCancellationNotificationService(context);
        var sut = CreateSut(context, notifier);

        var result = await sut.AddUnavailability(
            seed.Service.Id,
            new CreateUnavailabilityDto
            {
                StartDate = seed.FutureDate,
                EndDate = seed.FutureDate,
                StartTime = new TimeOnly(10, 30),
                EndTime = new TimeOnly(11, 30),
                Reason = "Capacitacion interna",
                CancelAffectedAppointments = false
            });

        Assert.True(result.IsSuccess);

        var pending = await context.Appointments.AsNoTracking().SingleAsync(a => a.Id == seed.CancelledAppointmentId);
        Assert.Equal(AppointmentStatus.Pending, pending.Status);
        Assert.Null(pending.CancelReason);
        Assert.Empty(notifier.Calls);

        var savedService = await context.Services
            .Include(service => service.ServicesUnavailability)
            .SingleAsync(service => service.Id == seed.Service.Id);

        Assert.Single(savedService.ServicesUnavailability);
        Assert.Empty(await context.AppointmentStatusHistories.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task AddUnavailability_ShouldCancelAffectedPendingFutureAppointments_WhenFlagIsOmitted()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var scope = BuildServices(connection).CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();
        await context.Database.EnsureCreatedAsync();

        var seed = await SeedAsync(context);
        var notifier = new SpyAppointmentCancellationNotificationService(context);
        var sut = CreateSut(context, notifier);

        var result = await sut.AddUnavailability(
            seed.Service.Id,
            new CreateUnavailabilityDto
            {
                StartDate = seed.FutureDate,
                EndDate = seed.FutureDate,
                StartTime = new TimeOnly(10, 30),
                EndTime = new TimeOnly(11, 30),
                Reason = "Vacaciones",
                UserId = seed.Owner.Id
            });

        Assert.True(result.IsSuccess);

        var cancelled = await context.Appointments.AsNoTracking().SingleAsync(a => a.Id == seed.CancelledAppointmentId);
        var untouchedFuturePending = await context.Appointments.AsNoTracking().SingleAsync(a => a.Id == seed.UnaffectedFuturePendingAppointmentId);

        Assert.Equal(AppointmentStatus.Cancelled, cancelled.Status);
        Assert.Equal("Cancelado por excepcion de agenda: Vacaciones", cancelled.CancelReason);
        Assert.Equal(AppointmentStatus.Pending, untouchedFuturePending.Status);

        var cancellationHistory = await context.AppointmentStatusHistories
            .AsNoTracking()
            .SingleAsync(h => h.AppointmentId == seed.CancelledAppointmentId);

        Assert.Equal(seed.Owner.Id, cancellationHistory.UserId);
        Assert.Single(notifier.Calls);
    }

    private static ServiceApplicationService CreateSut(
        BooklyDbContext context,
        IAppointmentCancellationNotificationService notificationService)
    {
        return new ServiceApplicationService(
            new ServiceRepository(context),
            new AppointmentRepository(context),
            new AvailabilityService(),
            new UserRepository(context),
            new ServiceTypeRepository(context),
            new SubscriptionRepository(context),
            new FakeDateTimeProvider(),
            new ServiceAuthorizationService(),
            notificationService,
            new MapperConfiguration(_ => { }).CreateMapper(),
            context,
            Options.Create(new FrontendOptions()));
    }

    private static ServiceProvider BuildServices(SqliteConnection connection)
    {
        var services = new ServiceCollection();

        services.AddDbContext<BooklyDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IAppointmentHistoryRepository, AppointmentHistoryRepository>();
        services.AddScoped<IDomainEventHandler<AppointmentStatusChangedEvent>, RecordStatusChangedHandler>();

        return services.BuildServiceProvider();
    }

    private static async Task<SeedData> SeedAsync(BooklyDbContext context)
    {
        var owner = User.CreateOwner(
            PersonName.Create("Ada", "Lovelace"),
            BOOKLY.Domain.SharedKernel.Email.Create("ada@example.com"),
            Password.FromHash("hashed-password"),
            ReferenceNow);

        var serviceType = ServiceType.Create("Consulta general");

        context.Users.Add(owner);
        context.ServiceTypes.Add(serviceType);
        await context.SaveChangesAsync();

        var service = Service.Create(
            "Consulta general",
            owner.Id,
            "consulta-general",
            "Servicio de prueba",
            null,
            serviceType.Id,
            ReferenceNow,
            Duration.Create(60),
            Capacity.Create(1),
            Mode.Presence,
            1000m);

        context.Services.Add(service);
        await context.SaveChangesAsync();

        var futureDate = DateOnly.FromDateTime(ReferenceNow.AddDays(2));
        var pastDate = DateOnly.FromDateTime(ReferenceNow.AddDays(-1));

        var cancelledAppointment = Appointment.Create(
            service.Id,
            null,
            CreateClient("Grace Hopper", "grace@example.com"),
            futureDate.ToDateTime(new TimeOnly(10, 0)),
            Duration.Create(60),
            null,
            ReferenceNow);

        var unaffectedFuturePendingAppointment = Appointment.Create(
            service.Id,
            null,
            CreateClient("Katherine Johnson", "katherine@example.com"),
            futureDate.ToDateTime(new TimeOnly(12, 0)),
            Duration.Create(60),
            null,
            ReferenceNow);

        var pastPendingAppointment = Appointment.CreateHistorical(
            service.Id,
            null,
            CreateClient("Dorothy Vaughan", "dorothy@example.com"),
            pastDate.ToDateTime(new TimeOnly(10, 0)),
            Duration.Create(60),
            AppointmentStatus.Pending,
            null,
            null,
            ReferenceNow);

        var attendedAppointment = Appointment.CreateHistorical(
            service.Id,
            null,
            CreateClient("Mary Jackson", "mary@example.com"),
            futureDate.ToDateTime(new TimeOnly(10, 0)),
            Duration.Create(60),
            AppointmentStatus.Attended,
            null,
            null,
            ReferenceNow);

        context.Appointments.AddRange(
            cancelledAppointment,
            unaffectedFuturePendingAppointment,
            pastPendingAppointment,
            attendedAppointment);

        await context.SaveChangesAsync();

        return new SeedData(
            owner,
            service,
            pastDate,
            futureDate,
            cancelledAppointment.Id,
            unaffectedFuturePendingAppointment.Id,
            pastPendingAppointment.Id,
            attendedAppointment.Id);
    }

    private static ClientInfo CreateClient(string fullName, string email)
    {
        var phoneSuffix = Math.Abs(email.GetHashCode()).ToString("D10")[..6];

        return ClientInfo.Create(
            fullName,
            $"11444{phoneSuffix}",
            BOOKLY.Domain.SharedKernel.Email.Create(email));
    }

    private sealed record SeedData(
        User Owner,
        Service Service,
        DateOnly PastDate,
        DateOnly FutureDate,
        int CancelledAppointmentId,
        int UnaffectedFuturePendingAppointmentId,
        int PastPendingAppointmentId,
        int AttendedAppointmentId);

    private sealed class SpyAppointmentCancellationNotificationService(BooklyDbContext context)
        : IAppointmentCancellationNotificationService
    {
        public List<NotificationCall> Calls { get; } = [];

        public async Task NotifyAppointmentCancelled(
            Service service,
            Appointment appointment,
            bool notifyOwner,
            CancellationToken ct = default)
        {
            var persistedAppointment = await context.Appointments
                .AsNoTracking()
                .SingleAsync(a => a.Id == appointment.Id, ct);

            var cancelHistoryEntries = await context.AppointmentStatusHistories
                .AsNoTracking()
                .CountAsync(
                    h => h.AppointmentId == appointment.Id &&
                         h.NewStatus == AppointmentStatus.Cancelled,
                    ct);

            Calls.Add(new NotificationCall(
                appointment.Id,
                notifyOwner,
                persistedAppointment.Status,
                cancelHistoryEntries));
        }
    }

    private sealed record NotificationCall(
        int AppointmentId,
        bool NotifyOwner,
        AppointmentStatus PersistedStatus,
        int CancelHistoryEntriesAtNotificationTime);

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime NowArgentina() => ReferenceNow;

        public DateTime UtcNow() => ReferenceNow.ToUniversalTime();
    }
}

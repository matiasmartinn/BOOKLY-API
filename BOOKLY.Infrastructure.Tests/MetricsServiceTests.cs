using AutoMapper;
using BOOKLY.Application.Services.MetricsAggregate;
using BOOKLY.Application.Services.MetricsAggregate.DTOs;
using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Enums;
using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.ValueObjects;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.SharedKernel;
using BOOKLY.Infrastructure;
using BOOKLY.Infrastructure.Persistence;
using BOOKLY.Infrastructure.Persistence.Repositories;
using BOOKLY.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BOOKLY.Infrastructure.Tests;

public sealed class MetricsServiceTests
{
    private static readonly DateTime CreationNow = new(2026, 3, 18, 9, 0, 0);

    [Fact]
    public async Task GetAppointmentMetrics_ShouldReturnWeekdayBucketsOrderedFromMondayToSunday()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var scope = BuildServices(connection).CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();
        await context.Database.EnsureCreatedAsync();
        var seed = await SeedAsync(context);

        var appointmentRepository = scope.ServiceProvider.GetRequiredService<IAppointmentRepository>();
        var serviceRepository = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
        var metricsService = new MetricsService(
            appointmentRepository,
            serviceRepository,
            new StubUserRepository(),
            CreateMapper());

        var trackedService = seed.TrackedService;
        var ignoredService = seed.IgnoredService;

        context.Appointments.AddRange(
            CreateAppointment(trackedService.Id, seed.Secretary.Id, new DateTime(2026, 3, 23, 9, 0, 0)),
            CreateAppointment(trackedService.Id, seed.Secretary.Id, new DateTime(2026, 3, 25, 10, 0, 0)),
            CreateAppointment(trackedService.Id, seed.Secretary.Id, new DateTime(2026, 3, 25, 15, 0, 0)),
            CreateAppointment(trackedService.Id, seed.Secretary.Id, new DateTime(2026, 3, 29, 12, 0, 0)),
            CreateAppointment(ignoredService.Id, seed.Secretary.Id, new DateTime(2026, 3, 24, 11, 0, 0)),
            CreateAppointment(trackedService.Id, seed.Secretary.Id, new DateTime(2026, 3, 30, 11, 0, 0)));
        await context.SaveChangesAsync();

        var result = await metricsService.GetAppointmentMetrics(new AppointmentMetricsQueryDto
        {
            ServiceId = trackedService.Id,
            From = new DateOnly(2026, 3, 23),
            To = new DateOnly(2026, 3, 29)
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        var metrics = result.Data!;
        var weekdays = metrics.AppointmentsByWeekday.ToList();

        Assert.Equal(7, weekdays.Count);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 0 }, weekdays.Select(x => x.DayOfWeek).ToArray());
        Assert.Collection(
            weekdays,
            monday =>
            {
                Assert.Equal("Lunes", monday.Label);
                Assert.Equal(1, monday.TotalAppointments);
            },
            tuesday =>
            {
                Assert.Equal("Martes", tuesday.Label);
                Assert.Equal(0, tuesday.TotalAppointments);
            },
            wednesday =>
            {
                Assert.Equal("Miercoles", wednesday.Label);
                Assert.Equal(2, wednesday.TotalAppointments);
            },
            thursday =>
            {
                Assert.Equal("Jueves", thursday.Label);
                Assert.Equal(0, thursday.TotalAppointments);
            },
            friday =>
            {
                Assert.Equal("Viernes", friday.Label);
                Assert.Equal(0, friday.TotalAppointments);
            },
            saturday =>
            {
                Assert.Equal("Sabado", saturday.Label);
                Assert.Equal(0, saturday.TotalAppointments);
            },
            sunday =>
            {
                Assert.Equal("Domingo", sunday.Label);
                Assert.Equal(1, sunday.TotalAppointments);
            });

        Assert.Equal(4, metrics.TotalAppointments);
        Assert.Equal(7, metrics.AppointmentsByDay.Count);
        Assert.Equal(24, metrics.AppointmentsByHour.Count);
    }

    private static async Task<SeedData> SeedAsync(BooklyDbContext context)
    {
        var owner = User.CreateOwner(
            PersonName.Create("Ada", "Lovelace"),
            BOOKLY.Domain.SharedKernel.Email.Create("ada.metrics@example.com"),
            Password.FromHash("hashed-password"),
            CreationNow);

        var secretary = User.CreateSecretary(
            PersonName.Create("Grace", "Hopper"),
            BOOKLY.Domain.SharedKernel.Email.Create("grace.metrics@example.com"),
            CreationNow);

        context.Users.AddRange(owner, secretary);
        await context.SaveChangesAsync();

        var serviceTypeId = await context.ServiceTypes
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .FirstAsync();

        var trackedService = CreateService("Masajes", "masajes", owner.Id, serviceTypeId);
        var ignoredService = CreateService("Kinesiologia", "kinesiologia", owner.Id, serviceTypeId);

        context.Services.AddRange(trackedService, ignoredService);
        await context.SaveChangesAsync();

        return new SeedData(owner, secretary, trackedService, ignoredService);
    }

    private static ServiceProvider BuildServices(SqliteConnection connection)
    {
        var services = new ServiceCollection();

        services.AddDbContext<BooklyDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();

        return services.BuildServiceProvider();
    }

    private static Appointment CreateAppointment(int serviceId, int? secretaryId, DateTime startDateTime)
    {
        return Appointment.Create(
            serviceId,
            secretaryId,
            ClientInfo.Create(
                "Grace Hopper",
                "1144455566",
                BOOKLY.Domain.SharedKernel.Email.Create("grace@example.com")),
            startDateTime,
            Duration.Create(60),
            null,
            CreationNow);
    }

    private static Service CreateService(string name, string slug, int ownerId, int serviceTypeId)
    {
        return Service.Create(
            name,
            userId: ownerId,
            slug: slug,
            description: null,
            phoneNumber: null,
            location: null,
            serviceType: serviceTypeId,
            createdAt: CreationNow,
            duration: Duration.Create(60),
            capacity: Capacity.Create(1),
            mode: Mode.Presence,
            price: null);
    }

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddMaps(typeof(MetricsService).Assembly));
        return configuration.CreateMapper();
    }

    private sealed record SeedData(
        User Owner,
        User Secretary,
        Service TrackedService,
        Service IgnoredService);

    private sealed class StubUserRepository : IUserRepository
    {
        public Task<User?> GetOne(int id, CancellationToken ct = default) => Task.FromResult<User?>(null);
        public Task<User?> GetById(int id, CancellationToken ct = default) => GetOne(id, ct);
        public Task<User?> GetByEmail(string email, CancellationToken ct = default) => Task.FromResult<User?>(null);
        public Task<RefreshToken?> GetRefreshToken(string tokenHash, string? legacyRawToken = null, CancellationToken ct = default) => Task.FromResult<RefreshToken?>(null);
        public Task<bool> ExistsByEmail(string email, CancellationToken ct = default) => Task.FromResult(false);
        public Task AddOne(User user, CancellationToken ct = default) => Task.CompletedTask;
        public Task AddRefreshToken(RefreshToken refreshToken, CancellationToken ct = default) => Task.CompletedTask;
        public Task RevokeAllUserTokens(int userId, CancellationToken ct = default) => Task.CompletedTask;
        public void Update(User user) { }
        public void Remove(User user) { }
    }
}

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
using BOOKLY.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BOOKLY.Infrastructure.Tests;

public sealed class AppointmentRepositoryMetricsTests
{
    private static readonly DateTime CreationNow = new(2026, 3, 18, 9, 0, 0);

    [Fact]
    public async Task MetricsQueries_ShouldAggregateUsingDatabaseFilters()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var scope = BuildServices(connection).CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();
        await context.Database.EnsureCreatedAsync();
        var seed = await SeedAsync(context);

        var repository = scope.ServiceProvider.GetRequiredService<IAppointmentRepository>();

        var pending = CreateAppointment(seed.TrackedService.Id, seed.SecretaryA.Id, new DateTime(2026, 3, 20, 9, 0, 0));
        var cancelled = CreateAppointment(seed.TrackedService.Id, seed.SecretaryA.Id, new DateTime(2026, 3, 20, 10, 0, 0));
        var attended = CreateAppointment(seed.TrackedService.Id, seed.SecretaryB.Id, new DateTime(2026, 3, 21, 9, 0, 0));
        var noShow = CreateAppointment(seed.IgnoredService.Id, seed.SecretaryA.Id, new DateTime(2026, 3, 21, 11, 0, 0));

        context.Appointments.AddRange(pending, cancelled, attended, noShow);
        await context.SaveChangesAsync();

        cancelled.MarkAsCancel("Cliente cancelo", new DateTime(2026, 3, 19, 12, 0, 0));
        attended.MarkAsAttended(new DateTime(2026, 3, 21, 10, 30, 0));
        noShow.MarkAsNoShow(new DateTime(2026, 3, 21, 12, 30, 0));

        context.Appointments.UpdateRange(cancelled, attended, noShow);
        await context.SaveChangesAsync();

        var serviceIds = new[] { seed.TrackedService.Id };
        var from = new DateOnly(2026, 3, 20);
        var to = new DateOnly(2026, 3, 21);

        var total = await repository.CountByServices(serviceIds, from, to, seed.SecretaryA.Id);
        var statusCounts = await repository.GetStatusCountsByServices(serviceIds, from, to, seed.SecretaryA.Id);
        var dayCounts = await repository.GetDayCountsByServices(serviceIds, from, to, seed.SecretaryA.Id);
        var hourCounts = await repository.GetHourCountsByServices(serviceIds, from, to, seed.SecretaryA.Id);
        var weekdayCounts = await repository.GetWeekdayCountsByServices(serviceIds, from, to, seed.SecretaryA.Id);

        Assert.Equal(2, total);
        Assert.Equal(2, statusCounts.Count);
        Assert.Contains(statusCounts, x => x.Status == AppointmentStatus.Pending && x.TotalAppointments == 1);
        Assert.Contains(statusCounts, x => x.Status == AppointmentStatus.Cancelled && x.TotalAppointments == 1);
        var dayCount = Assert.Single(dayCounts);
        Assert.Equal(new DateOnly(2026, 3, 20), dayCount.Date);
        Assert.Equal(2, dayCount.TotalAppointments);
        Assert.Equal(2, hourCounts.Count);
        Assert.Contains(hourCounts, x => x.Hour == 9 && x.TotalAppointments == 1);
        Assert.Contains(hourCounts, x => x.Hour == 10 && x.TotalAppointments == 1);
        var weekdayCount = Assert.Single(weekdayCounts);
        Assert.Equal((int)DayOfWeek.Friday, weekdayCount.DayOfWeek);
        Assert.Equal(2, weekdayCount.TotalAppointments);
    }

    private static async Task<SeedData> SeedAsync(BooklyDbContext context)
    {
        var owner = User.CreateOwner(
            PersonName.Create("Ada", "Lovelace"),
            BOOKLY.Domain.SharedKernel.Email.Create("ada.metrics@example.com"),
            Password.FromHash("hashed-password"),
            CreationNow);

        var secretaryA = User.CreateSecretary(
            PersonName.Create("Grace", "Hopper"),
            BOOKLY.Domain.SharedKernel.Email.Create("grace.metrics@example.com"),
            CreationNow);

        var secretaryB = User.CreateSecretary(
            PersonName.Create("Katherine", "Johnson"),
            BOOKLY.Domain.SharedKernel.Email.Create("katherine.metrics@example.com"),
            CreationNow);

        context.Users.AddRange(owner, secretaryA, secretaryB);
        await context.SaveChangesAsync();

        var serviceTypeId = await context.ServiceTypes
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .FirstAsync();

        var trackedService = CreateService("Masajes", "masajes", owner.Id, serviceTypeId);
        var ignoredService = CreateService("Kinesiologia", "kinesiologia", owner.Id, serviceTypeId);

        context.Services.AddRange(trackedService, ignoredService);
        await context.SaveChangesAsync();

        return new SeedData(owner, secretaryA, secretaryB, trackedService, ignoredService);
    }

    private static ServiceProvider BuildServices(SqliteConnection connection)
    {
        var services = new ServiceCollection();

        services.AddDbContext<BooklyDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();

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
            BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects.Duration.Create(60),
            null,
            CreationNow);
    }

    private static Service CreateService(string name, string slug, int ownerId, int serviceTypeId)
    {
        return Service.Create(
            name,
            ownerId,
            slug,
            description: null,
            phoneNumber: null,
            serviceType: serviceTypeId,
            createdAt: CreationNow,
            duration: Duration.Create(60),
            capacity: Capacity.Create(1),
            mode: Mode.Presence,
            price: null);
    }

    private sealed record SeedData(
        User Owner,
        User SecretaryA,
        User SecretaryB,
        Service TrackedService,
        Service IgnoredService);
}

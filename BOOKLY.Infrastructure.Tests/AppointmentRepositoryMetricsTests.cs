using BOOKLY.Domain.Aggregates.AppointmentAggregate;
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

        var repository = scope.ServiceProvider.GetRequiredService<IAppointmentRepository>();

        var pending = CreateAppointment(1, 10, new DateTime(2026, 3, 20, 9, 0, 0));
        var cancelled = CreateAppointment(1, 10, new DateTime(2026, 3, 20, 10, 0, 0));
        var attended = CreateAppointment(1, 11, new DateTime(2026, 3, 21, 9, 0, 0));
        var noShow = CreateAppointment(2, 10, new DateTime(2026, 3, 21, 11, 0, 0));

        context.Appointments.AddRange(pending, cancelled, attended, noShow);
        await context.SaveChangesAsync();

        cancelled.MarkAsCancel("Cliente cancelo", new DateTime(2026, 3, 19, 12, 0, 0));
        attended.MarkAsAttended(new DateTime(2026, 3, 21, 10, 30, 0));
        noShow.MarkAsNoShow(new DateTime(2026, 3, 21, 12, 30, 0));

        context.Appointments.UpdateRange(cancelled, attended, noShow);
        await context.SaveChangesAsync();

        var serviceIds = new[] { 1 };
        var from = new DateOnly(2026, 3, 20);
        var to = new DateOnly(2026, 3, 21);

        var total = await repository.CountByServices(serviceIds, from, to, 10);
        var statusCounts = await repository.GetStatusCountsByServices(serviceIds, from, to, 10);
        var dayCounts = await repository.GetDayCountsByServices(serviceIds, from, to, 10);
        var hourCounts = await repository.GetHourCountsByServices(serviceIds, from, to, 10);
        var weekdayCounts = await repository.GetWeekdayCountsByServices(serviceIds, from, to, 10);

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
}

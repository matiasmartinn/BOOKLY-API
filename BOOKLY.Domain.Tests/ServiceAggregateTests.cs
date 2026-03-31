using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Entities;
using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;
using BOOKLY.Domain.SharedKernel;

namespace BOOKLY.Domain.Tests;

public sealed class ServiceAggregateTests
{
    private static readonly DateTime ReferenceNow = new(2026, 3, 27, 10, 0, 0);

    [Fact]
    public void Create_ShouldStoreDefaultCapacity()
    {
        var service = CreateService(3);

        Assert.Equal(3, service.Capacity.Value);
    }

    [Fact]
    public void GetScheduleFor_ShouldReturnMatchingSchedule()
    {
        var start = ReferenceNow.Date.AddDays(1).AddHours(9);
        var service = CreateService(2);

        service.SetSchedules(
        [
            ServiceSchedule.Create(
                TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(12, 0)),
                Capacity.Create(4),
                Day.Create((int)start.DayOfWeek))
        ]);

        var schedule = service.GetScheduleFor(start, service.DurationMinutes);

        Assert.NotNull(schedule);
        Assert.Equal(4, schedule!.Capacity.Value);
    }

    private static Service CreateService(int capacity)
    {
        return Service.Create(
            "Consulta general",
            1,
            "consulta-general",
            "Servicio de prueba",
            null,
            null,
            null,
            1,
            Duration.Create(60),
            Capacity.Create(capacity),
            Mode.Presence,
            1000m);
    }
}

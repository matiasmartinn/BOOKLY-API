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
    public void Create_ShouldInitializePublicBookingData()
    {
        var service = CreateService(3);

        Assert.True(service.IsPublicBookingEnabled);
        Assert.False(string.IsNullOrWhiteSpace(service.PublicBookingToken));
        Assert.Equal(32, service.PublicBookingToken.Length);
        Assert.Equal(ReferenceNow, service.PublicBookingTokenUpdateAt);
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

    [Fact]
    public void RegeneratePublicBookingToken_ShouldRotateTokenAndUpdateTimestamp()
    {
        var service = CreateService(3);
        var originalToken = service.PublicBookingToken;
        var regeneratedAt = ReferenceNow.AddHours(2);

        service.RegeneratePublicBookingToken(regeneratedAt);

        Assert.NotEqual(originalToken, service.PublicBookingToken);
        Assert.Equal(regeneratedAt, service.PublicBookingTokenUpdateAt);
    }

    [Fact]
    public void HasValidPublicBookingAccess_ShouldRequireMatchingSlugTokenAndEnabledActiveService()
    {
        var service = CreateService(3);

        Assert.True(service.HasValidPublicBookingAccess("consulta-general", service.PublicBookingToken));
        Assert.False(service.HasValidPublicBookingAccess("otro-servicio", service.PublicBookingToken));
        Assert.False(service.HasValidPublicBookingAccess("consulta-general", "token-invalido"));

        service.DisablePublicBooking();
        Assert.False(service.HasValidPublicBookingAccess("consulta-general", service.PublicBookingToken));

        service.EnablePublicBooking(ReferenceNow.AddMinutes(5));
        service.Deactivate();
        Assert.False(service.HasValidPublicBookingAccess("consulta-general", service.PublicBookingToken));
    }

    private static Service CreateService(int capacity)
    {
        return Service.Create(
            "Consulta general",
            1,
            "consulta-general",
            "Servicio de prueba",
            null,
            1,
            ReferenceNow,
            Duration.Create(60),
            Capacity.Create(capacity),
            Mode.Presence,
            1000m);
    }
}

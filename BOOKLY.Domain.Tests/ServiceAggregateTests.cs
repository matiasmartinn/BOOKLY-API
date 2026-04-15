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
        Assert.False(string.IsNullOrWhiteSpace(service.PublicBookingCode));
        Assert.Matches("^[A-Za-z0-9]{8}$", service.PublicBookingCode);
        Assert.Equal(ReferenceNow, service.PublicBookingCodeUpdatedAt);
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
    public void RegeneratePublicBookingCode_ShouldRotateCodeAndUpdateTimestamp()
    {
        var service = CreateService(3);
        var originalCode = service.PublicBookingCode;
        var regeneratedAt = ReferenceNow.AddHours(2);

        service.RegeneratePublicBookingCode(regeneratedAt);

        Assert.NotEqual(originalCode, service.PublicBookingCode);
        Assert.Matches("^[A-Za-z0-9]{8}$", service.PublicBookingCode);
        Assert.Equal(regeneratedAt, service.PublicBookingCodeUpdatedAt);
    }

    [Fact]
    public void HasValidPublicBookingAccess_ShouldRequireMatchingSlugCodeAndEnabledActiveService()
    {
        var service = CreateService(3);

        Assert.True(service.HasValidPublicBookingAccess("consulta-general", service.PublicBookingCode));
        Assert.False(service.HasValidPublicBookingAccess("otro-servicio", service.PublicBookingCode));
        Assert.False(service.HasValidPublicBookingAccess("consulta-general", "codigo-invalido"));

        service.DisablePublicBooking();
        Assert.False(service.HasValidPublicBookingAccess("consulta-general", service.PublicBookingCode));

        service.EnablePublicBooking(ReferenceNow.AddMinutes(5));
        service.Deactivate();
        Assert.False(service.HasValidPublicBookingAccess("consulta-general", service.PublicBookingCode));
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

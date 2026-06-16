using BOOKLY.Infrastructure.Time;

namespace BOOKLY.Infrastructure.Tests;

public sealed class DateTimeProviderTests
{
    [Fact]
    public void NowArgentina_ShouldReturnUnspecifiedKind_ForBusinessTimestamps()
    {
        var provider = new DateTimeProvider();

        var now = provider.NowArgentina();

        Assert.Equal(DateTimeKind.Unspecified, now.Kind);
    }

    [Fact]
    public void UtcNow_ShouldReturnUtcKind_ForSecurityTimestamps()
    {
        var provider = new DateTimeProvider();

        var now = provider.UtcNow();

        Assert.Equal(DateTimeKind.Utc, now.Kind);
    }
}

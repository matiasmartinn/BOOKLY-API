namespace BOOKLY.Domain.Interfaces
{
    public interface IDateTimeProvider
    {
        DateTime NowArgentina();
        DateTime UtcNow();
    }
}

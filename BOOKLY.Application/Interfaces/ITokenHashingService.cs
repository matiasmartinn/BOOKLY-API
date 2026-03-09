namespace BOOKLY.Application.Interfaces
{
    public interface ITokenHashingService
    {
        string HashToken(string token);
    }
}

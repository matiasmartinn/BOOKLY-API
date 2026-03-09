namespace BOOKLY.Domain.Interfaces
{
    public interface IPasswordHasher
    {
        string Hash(string plaintTextPassword);
        bool Verify(string plaintTextPassword, string passwordHash);
    }
}

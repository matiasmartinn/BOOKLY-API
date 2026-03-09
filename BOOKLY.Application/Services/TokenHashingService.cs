using System.Security.Cryptography;
using System.Text;
using BOOKLY.Application.Interfaces;

namespace BOOKLY.Application.Services
{
    public sealed class TokenHashingService : ITokenHashingService
    {
        public string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }
    }
}

using System.Security.Cryptography;
using BOOKLY.Domain.Interfaces;

namespace BOOKLY.Infrastructure.Security
{
    public sealed class InvitationTokenGenerator : IInvitationTokenGenerator
    {
        public string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
    }
}

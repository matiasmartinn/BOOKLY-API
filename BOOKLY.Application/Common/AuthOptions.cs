namespace BOOKLY.Application.Common
{
    public sealed class AuthOptions
    {
        public const string SectionName = "Auth";
        public const string DefaultRefreshTokenCookieName = "refreshToken";
        public const string DefaultCookieSameSite = "Lax";
        public const string DefaultRefreshTokenCookiePath = "/api/auth";
        public const int DefaultRefreshTokenDays = 7;

        public string RefreshTokenCookieName { get; init; } = DefaultRefreshTokenCookieName;
        public int RefreshTokenDays { get; init; } = DefaultRefreshTokenDays;
        public bool? UseSecureCookies { get; init; }
        public string CookieSameSite { get; init; } = DefaultCookieSameSite;
        public string RefreshTokenCookiePath { get; init; } = DefaultRefreshTokenCookiePath;
    }
}

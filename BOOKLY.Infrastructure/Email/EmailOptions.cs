namespace BOOKLY.Infrastructure.Email
{
    public sealed class EmailOptions
    {
        public const string SectionName = "Email";
        public string SenderName { get; init; } = "BOOKLY";
        public string SenderAddress { get; init; } = string.Empty;
        public SmtpOptions Smtp { get; init; } = new();
    }

    public sealed class SmtpOptions
    {
        public string Host { get; init; } = string.Empty;
        public int Port { get; init; } = 587;
        public string Username { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public bool EnableSsl { get; init; } = true;
    }
}

namespace BOOKLY.Infrastructure.Email
{
    public sealed class FrontendOptions
    {
        public const string SectionName = "Frontend";
        public string BaseUrl { get; init; } = "http://localhost:5173";
        public string ConfirmEmailPath { get; init; } = "/auth/confirm-email";
        public string ResetPasswordPath { get; init; } = "/auth/reset-password";
        public string CompleteSecretaryInvitationPath { get; init; } = "/auth/secretary-invitation";
    }
}

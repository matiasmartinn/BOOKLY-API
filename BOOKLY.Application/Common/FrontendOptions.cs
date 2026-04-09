namespace BOOKLY.Application.Common
{
    public sealed class FrontendOptions
    {
        public string BaseUrl { get; init; } = string.Empty;
        public string ConfirmEmailPath { get; init; } = "/auth/confirm-email";
        public string ResetPasswordPath { get; init; } = "/auth/reset-password";
        public string CompleteSecretaryInvitationPath { get; init; } = "/auth/secretary-invitation";
        public string CompleteAdminInvitationPath { get; init; } = "/auth/admin-invitation";
        public string PublicBookingPath { get; init; } = "/book";
    }
}

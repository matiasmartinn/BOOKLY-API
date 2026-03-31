namespace BOOKLY.Domain.Emailing
{
    public sealed record EmailConfirmationEmailModel(
        string RecipientEmail,
        string RecipientName,
        string Token,
        int ExpirationHours);

    public sealed record PasswordResetEmailModel(
        string RecipientEmail,
        string RecipientName,
        string Token,
        int ExpirationHours);

    public sealed record SecretaryInvitationEmailModel(
        string RecipientEmail,
        string RecipientName,
        string InvitedByName,
        string ServiceName,
        string Token,
        int ExpirationHours);

    public sealed record AppointmentCreatedClientEmailModel(
        string RecipientEmail,
        string RecipientName,
        string ServiceName,
        string BusinessName,
        DateTime StartDateTime,
        int DurationMinutes);

    public sealed record AppointmentCreatedOwnerEmailModel(
        string RecipientEmail,
        string RecipientName,
        string ClientName,
        string ClientEmail,
        string ClientPhone,
        string ServiceName,
        DateTime StartDateTime,
        int DurationMinutes);

    public sealed record AppointmentCancelledClientEmailModel(
        string RecipientEmail,
        string RecipientName,
        string ServiceName,
        string BusinessName,
        DateTime StartDateTime,
        string? CancelReason);

    public sealed record AppointmentCancelledOwnerEmailModel(
        string RecipientEmail,
        string RecipientName,
        string ClientName,
        string ClientEmail,
        string ClientPhone,
        string ServiceName,
        DateTime StartDateTime,
        string? CancelReason);

    public sealed record AppointmentRescheduledClientEmailModel(
        string RecipientEmail,
        string RecipientName,
        string ServiceName,
        string BusinessName,
        DateTime PreviousStartDateTime,
        DateTime NewStartDateTime,
        int DurationMinutes);

    public sealed record AppointmentRescheduledOwnerEmailModel(
        string RecipientEmail,
        string RecipientName,
        string ClientName,
        string ClientEmail,
        string ClientPhone,
        string ServiceName,
        DateTime PreviousStartDateTime,
        DateTime NewStartDateTime,
        int DurationMinutes);
}

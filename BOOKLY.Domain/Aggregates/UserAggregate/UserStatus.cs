namespace BOOKLY.Domain.Aggregates.UserAggregate
{
    public enum UserStatus
    {
        PendingEmailConfirmation = 1,
        PendingInvitationAcceptance = 2,
        Active = 3,
        Inactive = 4
    }
}

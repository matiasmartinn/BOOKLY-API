namespace BOOKLY.Application.Services.UserAggregate.DTOs
{
    public sealed record EmailDispatchResultDto(bool EmailSent, string Message);
}

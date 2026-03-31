namespace BOOKLY.Application.Services.UserAggregate.DTOs
{
    public sealed record RegisterOwnerResultDto(
        UserDto User,
        EmailDispatchResultDto EmailDispatch);
}

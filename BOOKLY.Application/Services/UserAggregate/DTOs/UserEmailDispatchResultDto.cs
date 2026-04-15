namespace BOOKLY.Application.Services.UserAggregate.DTOs
{
    public sealed record UserEmailDispatchResultDto(
        UserDto User,
        EmailDispatchResultDto EmailDispatch);
}

using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.SharedKernel;

namespace BOOKLY.Domain.Tests;

public sealed class UserAggregateTests
{
    private static readonly DateTime ReferenceNow = new(2026, 3, 27, 10, 0, 0);

    [Fact]
    public void ChangeEmail_ShouldRequireConfirmation_ForNonAdminUsers()
    {
        var user = User.CreateOwner(
            PersonName.Create("Grace", "Hopper"),
            Email.Create("grace@example.com"),
            Password.FromHash("hashed-password"),
            ReferenceNow);

        user.ConfirmEmail();
        user.Activate();

        user.ChangeEmail(Email.Create("nuevo@example.com"));

        Assert.Equal("nuevo@example.com", user.Email.Value);
        Assert.False(user.EmailConfirmed);
        Assert.False(user.IsActive);
    }

    [Fact]
    public void ChangeEmail_ShouldNotRequireConfirmation_ForAdmins()
    {
        var admin = User.CreateAdmin(
            PersonName.Create("Ada", "Lovelace"),
            Email.Create("ada@example.com"),
            Password.FromHash("hashed-password"),
            ReferenceNow);

        admin.ChangeEmail(Email.Create("admin@example.com"));

        Assert.Equal("admin@example.com", admin.Email.Value);
        Assert.True(admin.EmailConfirmed);
        Assert.True(admin.IsActive);
    }
}

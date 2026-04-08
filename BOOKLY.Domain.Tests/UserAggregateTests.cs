using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;
using BOOKLY.Domain.Aggregates.UserAggregate.ValueObjects;
using BOOKLY.Domain.Exceptions;
using BOOKLY.Domain.SharedKernel;

namespace BOOKLY.Domain.Tests;

public sealed class UserAggregateTests
{
    private static readonly DateTime ReferenceNow = new(2026, 3, 27, 10, 0, 0);

    [Fact]
    public void CreateOwner_ShouldStartPendingEmailConfirmation()
    {
        var owner = User.CreateOwner(
            PersonName.Create("Grace", "Hopper"),
            Email.Create("grace@example.com"),
            Password.FromHash("hashed-password"),
            ReferenceNow);

        Assert.Equal(UserRole.Owner, owner.Role);
        Assert.True(owner.IsActive);
        Assert.False(owner.EmailConfirmed);
        Assert.Equal(UserStatus.PendingEmailConfirmation, owner.Status);
        Assert.NotNull(owner.Password);
    }

    [Fact]
    public void ConfirmEmail_ShouldAllowOwnerLogin()
    {
        var owner = User.CreateOwner(
            PersonName.Create("Grace", "Hopper"),
            Email.Create("grace@example.com"),
            Password.FromHash("hashed-password"),
            ReferenceNow);

        owner.ConfirmEmail();
        owner.RegisterLogin(ReferenceNow.AddMinutes(15));

        Assert.True(owner.EmailConfirmed);
        Assert.Equal(UserStatus.Active, owner.Status);
        Assert.Equal(ReferenceNow.AddMinutes(15), owner.LastLoginAt);
    }

    [Fact]
    public void ChangeEmail_ShouldKeepOwnerEnabledButReturnToPendingConfirmation()
    {
        var owner = User.CreateOwner(
            PersonName.Create("Grace", "Hopper"),
            Email.Create("grace@example.com"),
            Password.FromHash("hashed-password"),
            ReferenceNow);

        owner.ConfirmEmail();
        owner.ChangeEmail(Email.Create("nuevo@example.com"));

        Assert.Equal("nuevo@example.com", owner.Email.Value);
        Assert.True(owner.IsActive);
        Assert.False(owner.EmailConfirmed);
        Assert.Equal(UserStatus.PendingEmailConfirmation, owner.Status);
        Assert.Throws<DomainException>(() => owner.EnsureCanLogin());
    }

    [Fact]
    public void CreateInvitedAdmin_ShouldStartPendingInvitationAcceptance()
    {
        var admin = User.CreateInvitedAdmin(
            PersonName.Create("Margaret", "Hamilton"),
            Email.Create("margaret@example.com"),
            ReferenceNow);

        Assert.Equal(UserRole.Admin, admin.Role);
        Assert.True(admin.IsActive);
        Assert.False(admin.EmailConfirmed);
        Assert.Null(admin.Password);
        Assert.Equal(UserStatus.PendingInvitationAcceptance, admin.Status);
    }

    [Fact]
    public void AcceptInvitation_ShouldActivateSecretaryAndAllowLogin()
    {
        var secretary = User.CreateSecretary(
            PersonName.Create("Ada", "Lovelace"),
            Email.Create("ada@example.com"),
            ReferenceNow);

        Assert.Throws<DomainException>(() => secretary.EnsureCanLogin());

        secretary.AcceptInvitation(Password.FromHash("hashed-password"));
        secretary.RegisterLogin(ReferenceNow.AddMinutes(10));

        Assert.True(secretary.IsActive);
        Assert.True(secretary.EmailConfirmed);
        Assert.Equal(UserStatus.Active, secretary.Status);
        Assert.Equal(ReferenceNow.AddMinutes(10), secretary.LastLoginAt);
    }

    [Fact]
    public void EnsureCanLogin_ShouldRejectInactiveUser()
    {
        var owner = User.CreateOwner(
            PersonName.Create("Grace", "Hopper"),
            Email.Create("grace@example.com"),
            Password.FromHash("hashed-password"),
            ReferenceNow);

        owner.ConfirmEmail();
        owner.Deactivate();

        Assert.False(owner.IsActive);
        Assert.Equal(UserStatus.Inactive, owner.Status);
        Assert.Throws<DomainException>(() => owner.EnsureCanLogin());
    }

    [Fact]
    public void Activate_ShouldRestorePendingInvitationAcceptanceForSecretaryWithoutPassword()
    {
        var secretary = User.CreateSecretary(
            PersonName.Create("Ada", "Lovelace"),
            Email.Create("ada@example.com"),
            ReferenceNow);

        secretary.Deactivate();
        secretary.Activate();

        Assert.True(secretary.IsActive);
        Assert.False(secretary.EmailConfirmed);
        Assert.Null(secretary.Password);
        Assert.Equal(UserStatus.PendingInvitationAcceptance, secretary.Status);
    }

    [Fact]
    public void Activate_ShouldRestorePendingEmailConfirmationForOwnerWithoutConfirmedEmail()
    {
        var owner = User.CreateOwner(
            PersonName.Create("Grace", "Hopper"),
            Email.Create("grace@example.com"),
            Password.FromHash("hashed-password"),
            ReferenceNow);

        owner.Deactivate();
        owner.Activate();

        Assert.True(owner.IsActive);
        Assert.False(owner.EmailConfirmed);
        Assert.Equal(UserStatus.PendingEmailConfirmation, owner.Status);
    }
}

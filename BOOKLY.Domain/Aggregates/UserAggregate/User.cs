using BOOKLY.Domain.Aggregates.UserAggregate.Enums;
using BOOKLY.Domain.Aggregates.UserAggregate.ValueObjects;
using BOOKLY.Domain.Exceptions;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.SharedKernel;

namespace BOOKLY.Domain.Aggregates.UserAggregate
{
    public class User
    {
        public int Id { get; set; }
        public PersonName PersonName { get; private set; } = null!;
        public Email Email { get; private set; } = null!;
        public Password? Password { get; private set; }
        public UserRole Role { get; private set; }
        public bool IsActive { get; private set; }
        public bool EmailConfirmed { get; private set; }
        public UserStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? LastLoginAt { get; private set; }

        private User() { }

        // ======================== FACTORIES ========================

        public static User CreateOwner(PersonName personName, Email email, Password password, DateTime now)
            => Create(
                personName,
                email,
                password,
                UserRole.Owner,
                now,
                isActive: true,
                emailConfirmed: false,
                status: UserStatus.PendingEmailConfirmation);

        public static User CreateSecretary(PersonName personName, Email email, DateTime now)
            => Create(
                personName,
                email,
                null,
                UserRole.Secretary,
                now,
                isActive: true,
                emailConfirmed: false,
                status: UserStatus.PendingInvitationAcceptance);

        public static User CreateAdmin(PersonName personName, Email email, Password password, DateTime now)
            => Create(
                personName,
                email,
                password,
                UserRole.Admin,
                now,
                isActive: true,
                emailConfirmed: true,
                status: UserStatus.Active);

        public static User CreateInvitedAdmin(PersonName personName, Email email, DateTime now)
            => Create(
                personName,
                email,
                null,
                UserRole.Admin,
                now,
                isActive: true,
                emailConfirmed: false,
                status: UserStatus.PendingInvitationAcceptance);

        private static User Create(
            PersonName personName,
            Email email,
            Password? password,
            UserRole role,
            DateTime now,
            bool isActive,
            bool emailConfirmed,
            UserStatus status)
        {
            return new User
            {
                PersonName = personName,
                Email = email,
                Password = password,
                Role = role,
                IsActive = isActive,
                EmailConfirmed = emailConfirmed,
                Status = status,
                CreatedAt = now
            };
        }

        // ======================== COMPORTAMIENTO ========================

        public void ChangeUserName(PersonName personName)
        {
            PersonName = personName;
        }

        public void ChangePassword(Password password)
            => SetPassword(password);

        public void ChangeEmail(Email email)
        {
            if (Email.Value.Equals(email.Value, StringComparison.OrdinalIgnoreCase))
                return;

            Email = email;

            if (Role == UserRole.Owner)
            {
                EmailConfirmed = false;
                Status = ResolveStatus();
            }
        }

        public void ConfirmEmail()
        {
            if (EmailConfirmed)
                return;

            EmailConfirmed = true;
            Status = ResolveStatus();
        }

        public void AcceptInvitation(Password password)
        {
            if (Status != UserStatus.PendingInvitationAcceptance)
                throw new DomainException("La invitación ya fue completada o no se encuentra pendiente.");

            EmailConfirmed = true;
            SetPassword(password);
        }

        public bool VerifyPassword(string plainPassword, IPasswordHasher hasher)
        {
            if (Password is null || string.IsNullOrWhiteSpace(plainPassword))
                return false;

            return hasher.Verify(plainPassword, Password.Hash);
        }

        public void SetPassword(Password password)
        {
            Password = password ?? throw new DomainException("La contraseña es requerida.");
            Status = ResolveStatus();
        }

        public void Deactivate()
        {
            if (Role == UserRole.Admin)
                throw new DomainException("No se puede desactivar un Admin.");

            IsActive = false;
            Status = ResolveStatus();
        }

        public void Activate()
        {
            IsActive = true;
            Status = ResolveStatus();
        }

        public void RegisterLogin(DateTime now)
        {
            EnsureCanLogin();
            LastLoginAt = now;
        }

        public void EnsureCanLogin()
        {
            if (!IsActive)
                throw new DomainException("La cuenta está desactivada.");

            if (Status != UserStatus.Active)
                throw new DomainException("La cuenta aún no está habilitada para iniciar sesión.");

            if (!EmailConfirmed)
                throw new DomainException("El email no está confirmado.");

            if (Password is null)
                throw new DomainException("La cuenta no tiene contraseña configurada.");
        }

        private UserStatus ResolveStatus()
        {
            if (!IsActive)
                return UserStatus.Inactive;

            if (Role == UserRole.Owner && !EmailConfirmed)
                return UserStatus.PendingEmailConfirmation;

            if (!EmailConfirmed || Password is null)
                return UserStatus.PendingInvitationAcceptance;

            return UserStatus.Active;
        }
    }
}

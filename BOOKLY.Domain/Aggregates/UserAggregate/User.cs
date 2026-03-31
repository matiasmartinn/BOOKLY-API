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
        public UserKind Role { get; private set; }
        public bool IsActive { get; private set; } = true;
        public bool EmailConfirmed { get; private set; } = true;
        public DateTime CreatedAt { get; private set; }
        public DateTime? LastLoginAt { get; private set; }
        // Private constructor for EF Core
        private User() { }

        // Factory Method
        public static User CreateOwner(PersonName personName, Email email, Password password, DateTime now)
            => Create(personName, email, password, UserKind.Owner, now, false, false);
        public static User CreateSecretary(PersonName personName, Email email, DateTime now)
            => Create(personName, email, null, UserKind.Secretary, now, false, false);
        public static User CreateAdmin(PersonName personName, Email email, Password password, DateTime now)
            => Create(personName, email, password, UserKind.Admin, now, true, true);

        // Only way to create new User
        private static User Create(
            PersonName personName,
            Email email,
            Password? password,
            UserKind role,
            DateTime now,
            bool isActive = true,
            bool emailConfirmed = true)
        {
            return new User
            {
                PersonName = personName,
                Email = email,
                Role = role,
                Password = password ?? null,
                IsActive = isActive,
                EmailConfirmed = emailConfirmed,
                CreatedAt = now
            };
        }

        // ======================== COMPORTAMIENTO ========================
        public void ChangeUserName(PersonName personName)
        {
            PersonName = personName;
        }

        public void ChangePassword(Password password)
        {
            Password = password;
        }

        public void ChangeEmail(Email email)
        {
            if (Email.Value.Equals(email.Value, StringComparison.OrdinalIgnoreCase))
                return;

            Email = email;
            RequireEmailConfirmation();
        }

        public void ConfirmEmail()
        {
            EmailConfirmed = true;
        }

        public void RequireEmailConfirmation()
        {
            if (Role == UserKind.Admin)
                return;

            EmailConfirmed = false;
            IsActive = false;
        }

        public bool VerifyPassword(string plainPassword, IPasswordHasher hasher)
        {
            if (Password is null) return false;
            return hasher.Verify(plainPassword, Password.Hash);
        }
        public void Deactivate()
        {
            if (Role == UserKind.Admin)
                throw new DomainException("No se puede desactivar un Admin");
            IsActive = false;
        }

        public void Activate()
        {
            if (Role != UserKind.Admin && !EmailConfirmed)
                throw new DomainException("El email debe estar confirmado para activar la cuenta.");

            IsActive = true;
        }

        public void RegisterLogin(DateTime now)
        {
            LastLoginAt = now;
        }
    }
}

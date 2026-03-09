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
        public DateTime CreatedAt { get; private set; }
        public DateTime? LastLoginAt { get; private set; }
        // Private constructor for EF Core
        private User() { }

        // Factory Method
        public static User RegisterAsOwner(PersonName personName, Email email, Password password)
            => Create(personName, email, password, UserKind.Owner);
        public static User CreateSecretary(PersonName personName, Email email)
            => Create(personName, email, null,UserKind.Secretary, false);
        public static User CreateAdmin(PersonName personName, Email email, Password password)
            => Create(personName, email, password, UserKind.Admin);

        // Only way to create new User
        private static User Create(PersonName personName, Email email, Password? password, UserKind role, bool isActive = true)
        {
            return new User
            {
                Email = email,
                Role = role,
                Password = password ?? null,
                IsActive = isActive
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
        }
        public void Deactivate()
        {
            if (Role == UserKind.Admin)
                throw new DomainException("No se puede desactivar un Admin");
            IsActive = false;
        }

        public void Activate()
        {
            IsActive = true;
        }
    }
}

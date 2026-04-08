using System.Text.Json;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Enums;
using BOOKLY.Domain.Exceptions;

namespace BOOKLY.Domain.Aggregates.ServiceAggregate.Entities
{

    public sealed class ServiceSecretary
    {
        private HashSet<SecretaryPermission> _permissions = new();

        public int ServiceId { get; private set; }
        public int SecretaryId { get; private set; }

        public IReadOnlyCollection<SecretaryPermission> Permissions => _permissions.ToList().AsReadOnly();

        private string PermissionsData
        {
            get => JsonSerializer.Serialize(
                _permissions
                    .OrderBy(permission => (int)permission)
                    .Select(permission => (int)permission)
                    .ToList());
            set => _permissions = DeserializePermissions(value);
        }

        private ServiceSecretary() { }
        private ServiceSecretary(int secretaryId)
        {
            SecretaryId = secretaryId;
        }

        public static ServiceSecretary Create(int secretaryId)
        {
            if (secretaryId <= 0)
                throw new DomainException("Id de secretario inválido");
            return new ServiceSecretary(secretaryId);
        }

        public void GrantPermission(SecretaryPermission permission)
        {
            EnsureValidPermission(permission);

            if (!_permissions.Add(permission))
                throw new DomainException("El permiso ya está asignado al secretario.");
        }

        public void RevokePermission(SecretaryPermission permission)
        {
            EnsureValidPermission(permission);

            if (!_permissions.Remove(permission))
                throw new DomainException("El permiso no está asignado al secretario.");
        }

        public bool HasPermission(SecretaryPermission permission)
        {
            EnsureValidPermission(permission);
            return _permissions.Contains(permission);
        }

        private static void EnsureValidPermission(SecretaryPermission permission)
        {
            if (!Enum.IsDefined(typeof(SecretaryPermission), permission))
                throw new DomainException("Permiso de secretario inválido.");
        }

        private static HashSet<SecretaryPermission> DeserializePermissions(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new HashSet<SecretaryPermission>();

            var permissions = JsonSerializer.Deserialize<List<int>>(value) ?? [];

            return permissions
                .Where(permission => Enum.IsDefined(typeof(SecretaryPermission), permission))
                .Select(permission => (SecretaryPermission)permission)
                .ToHashSet();
        }
    }
}

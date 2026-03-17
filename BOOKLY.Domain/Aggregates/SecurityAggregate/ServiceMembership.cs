// Security ahora modela "qué puede hacer un Secretary en un Service"
// Los permisos son fijos — el Owner solo activa/desactiva
using BOOKLY.Domain;
using BOOKLY.Domain.Exceptions;
// ── ENTITY ───────────────────────────────────────────────────────────────────

public sealed class ServiceMembership
{
    private readonly HashSet<SecretaryPermission> _permissions = new();

    public int Id { get; private set; }
    public int SecretaryId { get; private set; }
    public int ServiceId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    public IReadOnlyCollection<SecretaryPermission> ActivePermissions
        => _permissions.ToList().AsReadOnly();

    private ServiceMembership() { }

    // Por defecto el Secretary entra sin permisos — el Owner los activa
    internal static ServiceMembership CreateWithDefaultPermissions(
        int secretaryId, int serviceId)
    {
        if (secretaryId <= 0) throw new DomainException("SecretaryId inválido.");
        if (serviceId <= 0) throw new DomainException("ServiceId inválido.");

        return new ServiceMembership
        {
            SecretaryId = secretaryId,
            ServiceId = serviceId,
            AssignedAt = DateTime.UtcNow
        };
    }

    internal void Grant(SecretaryPermission permission) => _permissions.Add(permission);
    internal void Revoke(SecretaryPermission permission) => _permissions.Remove(permission);
    internal bool HasPermission(SecretaryPermission permission)
        => _permissions.Contains(permission);
}

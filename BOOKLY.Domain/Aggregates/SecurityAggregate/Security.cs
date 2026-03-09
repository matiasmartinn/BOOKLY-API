using BOOKLY.Domain;
public sealed class Security
{
    private readonly List<ServiceMembership> _memberships = new();

    public IReadOnlyCollection<ServiceMembership> Memberships
        => _memberships.AsReadOnly();

    private Security() { }
    public static Security Create() => new();

    // El Owner asigna un Secretary a un Service con permisos base
    public ServiceMembership AddSecretary(int secretaryId, int serviceId)
    {
        if (_memberships.Any(m => m.SecretaryId == secretaryId
                               && m.ServiceId == serviceId))
            throw new DomainException(
                "El secretario ya está asignado a este servicio.");

        var membership = ServiceMembership.CreateWithDefaultPermissions(
            secretaryId, serviceId);

        _memberships.Add(membership);
        return membership;
    }

    public void RemoveSecretary(int secretaryId, int serviceId)
    {
        var m = GetMembershipOrThrow(secretaryId, serviceId);
        _memberships.Remove(m);
    }

    // El Owner activa/desactiva permisos puntuales
    public void GrantPermission(int secretaryId, int serviceId,
                                 SecretaryPermission permission)
    {
        GetMembershipOrThrow(secretaryId, serviceId).Grant(permission);
    }

    public void RevokePermission(int secretaryId, int serviceId,
                                  SecretaryPermission permission)
    {
        GetMembershipOrThrow(secretaryId, serviceId).Revoke(permission);
    }

    public bool HasPermission(int secretaryId, int serviceId,
                               SecretaryPermission permission)
    {
        var m = _memberships.FirstOrDefault(
            m => m.SecretaryId == secretaryId && m.ServiceId == serviceId);
        return m?.HasPermission(permission) ?? false;
    }

    public IReadOnlyCollection<SecretaryPermission> GetPermissions(
        int secretaryId, int serviceId)
        => GetMembershipOrThrow(secretaryId, serviceId).ActivePermissions;

    private ServiceMembership GetMembershipOrThrow(int secretaryId, int serviceId)
        => _memberships.FirstOrDefault(
               m => m.SecretaryId == secretaryId && m.ServiceId == serviceId)
           ?? throw new DomainException(
               "El secretario no está asignado a ese servicio.");
}

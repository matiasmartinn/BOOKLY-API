// Security ahora modela "qué puede hacer un Secretary en un Service"
// Los permisos son fijos — el Owner solo activa/desactiva
// ── VALUE OBJECT — permisos fijos del sistema ─────────────────────────────────

public enum SecretaryPermission
{
    ViewAppointments = 1,  // Ver turnos del servicio
    ManageAppointments = 2,  // Crear / modificar / cancelar turnos
    ViewClients = 3,  // Ver listado de clientes
    ManageClients = 4,  // Agregar / editar clientes
}
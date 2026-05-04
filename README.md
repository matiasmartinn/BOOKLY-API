# BOOKLY API

Backend de BOOKLY, una API REST multi-tenant para gestionar servicios, disponibilidad y turnos. Forma parte de una tesis orientada a resolver la operación diaria de negocios que trabajan por agenda, con foco en reglas de negocio reales, separación por capas y un modelo de dominio explícito.

## Descripción breve

BOOKLY permite que cada owner administre sus servicios, horarios, inhabilitaciones, secretarios, clientes y suscripción desde una misma plataforma, además de compartir un enlace público para que clientes finales puedan reservar turnos online.

## Objetivo del sistema

El objetivo del backend es centralizar la lógica de negocio crítica del sistema: disponibilidad, reserva de turnos, control de permisos por rol, autenticación, invitaciones, administración global y soporte para escenarios multi-tenant.

## Funcionalidades principales

- Registro de owners, login, refresh de sesión, logout, confirmación de email y recuperación de contraseña.
- Invitación y alta de secretarios, además de invitación y onboarding de administradores.
- CRUD de servicios con duración, capacidad, ubicación, estado, tipo de servicio y link público de reserva.
- Gestión de horarios base, cálculo de fechas/slots disponibles e inhabilitaciones parciales o de día completo.
- Gestión de turnos: creación, edición, reprogramación, cancelación, asistencia, no-show e historial de estados.
- Reserva pública mediante `slug + code`, sin autenticación previa del cliente final.
- Gestión de suscripciones por owner y catálogo de planes.
- Catálogo de tipos de servicio y campos dinámicos para modelar datos adicionales por rubro.
- Módulos de clientes, métricas y administración global de owners y servicios.

## Endpoints principales

| Grupo | Responsabilidad |
| --- | --- |
| `api/auth` | autenticación, refresh token, registro, confirmación de email y reset de contraseña |
| `api/users` | perfil de usuario, secretarios y operaciones sobre cuentas |
| `api/services` | servicios, horarios, disponibilidad, inhabilitaciones y booking público |
| `api/appointments` | turnos, agenda diaria, búsqueda, reprogramación y estados |
| `api/public/services` | reserva pública por `slug` y `code` |
| `api/subscriptions` | suscripciones y cambios de plan |
| `api/service-types` | tipos de servicio, campos dinámicos y opciones |
| `api/clients` | historial y detalle de clientes por owner |
| `api/metrics` | métricas de turnos y reporting |
| `api/admin/dashboard`, `api/admin/owners`, `api/admin/services`, `api/admins` | administración global e invitación de admins |

## Stack tecnológico

- ASP.NET Core 8
- C# / .NET 8
- Entity Framework Core 8
- SQL Server
- JWT Bearer Authentication + refresh tokens
- AutoMapper
- Swagger / OpenAPI
- Serilog
- xUnit para pruebas de dominio e infraestructura

## Arquitectura / estructura del proyecto

La solución está separada en capas con un enfoque inspirado en DDD:

```text
BOOKLY-API.sln
|- BOOKLY.Api/                   # Controllers, middleware, bootstrap y configuración HTTP
|- BOOKLY.Application/           # Casos de uso, DTOs, servicios de aplicación, mapeos e interfaces
|- BOOKLY.Domain/                # Agregados, value objects, domain services, eventos y shared kernel
|- BOOKLY.Infrastructure/        # EF Core, repositorios, seguridad, email, tiempo y migraciones
|- BOOKLY.Domain.Tests/          # Tests del dominio
`- BOOKLY.Infrastructure.Tests/  # Tests de persistencia y servicios de aplicación
```

### Capas principales

- `BOOKLY.Api`: expone la API REST, aplica autorización, documentación Swagger y manejo centralizado de errores.
- `BOOKLY.Application`: orquesta casos de uso y modela contratos de entrada/salida entre API y dominio.
- `BOOKLY.Domain`: contiene la lógica de negocio central del sistema.
- `BOOKLY.Infrastructure`: implementa persistencia, autenticación, hashing, envío de emails y acceso a tiempo del sistema.

## Decisiones técnicas destacadas

- Arquitectura basada en DDD, con agregados principales para `Service`, `Appointment`, `ServiceType`, `Subscription` y `User`.
- Uso de Value Objects para encapsular reglas y evitar primitivas ambiguas, por ejemplo `Slug`, `Duration`, `Capacity`, `DateRange`, `TimeRange`, `Location`, `Email`, `Password` y `PersonName`.
- `BooklyDbContext` implementa `IUnitOfWork`, y el commit del agregado se resuelve vía `SaveChangesAsync` dentro de una transacción.
- Repositorios por agregado sobre EF Core, con una base común para operaciones compartidas.
- Domain Events mediante `IDomainEvent` e `IDomainEventHandler<T>`, despachados por un dispatcher propio registrado en DI; es un patrón equivalente a usar MediatR, pero resuelto de forma explícita dentro del proyecto.
- Result pattern y mapeo centralizado de respuestas desde `BaseController`, complementado con middleware de excepciones y `ProblemDetails`.
- Autorización por roles (`Owner`, `Secretary`, `Admin`) más permisos finos por secretario según el servicio.
- La lógica de disponibilidad es parte del dominio: combina horarios, capacidad, inhabilitaciones, solapamientos, fechas bloqueadas y tiempo actual para calcular slots reservables.
- Persistencia sobre SQL Server con migraciones versionadas en el repositorio.

## Valor técnico del proyecto

Desde el punto de vista de tesis y portfolio, el valor del backend está en que no se limita a exponer CRUD: modela un dominio con reglas de agenda reales, separación arquitectónica clara, autorización por contexto, eventos de dominio, migraciones evolutivas y una lógica de disponibilidad que concentra buena parte de la complejidad del negocio.

## Configuración local

### Requisitos

- .NET 8 SDK
- SQL Server
- Opcional: credenciales SMTP si se quieren probar de punta a punta los flujos que envían emails

### Archivo local de configuración

Crea `BOOKLY.Api/appsettings.Development.json` con valores de desarrollo. Ese archivo está ignorado por Git.

Ejemplo mínimo:

```json
{
  "ConnectionStrings": {
    "BooklyDb": "Server=localhost;Database=BooklyDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Issuer": "BOOKLY.API",
    "Audience": "BOOKLY.WEB",
    "SecretKey": "replace-with-a-long-local-secret-key",
    "AccessTokenExpirationMinutes": 60
  },
  "Frontend": {
    "BaseUrl": "http://localhost:5173",
    "ConfirmEmailPath": "/auth/confirm-email",
    "ResetPasswordPath": "/auth/reset-password",
    "CompleteSecretaryInvitationPath": "/auth/secretary-invitation",
    "CompleteAdminInvitationPath": "/auth/admin-invitation",
    "PublicBookingPath": "/book"
  },
  "Email": {
    "SenderName": "BOOKLY",
    "SenderAddress": "no-reply@example.com",
    "Smtp": {
      "Host": "smtp.example.com",
      "Port": 587,
      "Username": "smtp-user",
      "Password": "smtp-password",
      "EnableSsl": true
    }
  }
}
```

### Qué configuración es realmente necesaria

- `ConnectionStrings:BooklyDb`: obligatoria para levantar la API y aplicar migraciones.
- `Jwt:*`: obligatoria para autenticación.
- `Frontend:BaseUrl` y paths relacionados: necesaria para construir links de confirmación, recuperación e invitaciones.
- `Email:*`: secundaria en términos de README, pero necesaria si quieres validar end-to-end confirmación de email, reset de contraseña o invitaciones. No hace falta convertir SMTP en el foco de la puesta en marcha.

## Comandos útiles

Desde la raíz de este repositorio:

```bash
dotnet restore BOOKLY-API.sln
dotnet build BOOKLY-API.sln
dotnet test BOOKLY-API.sln
dotnet ef database update --project BOOKLY.Infrastructure --startup-project BOOKLY.Api
dotnet ef migrations add <MigrationName> --project BOOKLY.Infrastructure --startup-project BOOKLY.Api
dotnet run --project BOOKLY.Api
```

En desarrollo, Swagger queda disponible en:

- `http://localhost:5057/swagger`
- `https://localhost:7176/swagger`

## Estado del proyecto

Proyecto de tesis entregado y actualmente en etapa final de evaluación/rendida.

## Autor

Matias Martin

using System.Globalization;

namespace BOOKLY.Infrastructure.Email
{
    internal static class BooklyEmailTemplates
    {
        public static string BuildEmailConfirmationBody(string recipientName, string confirmationUrl, int expirationHours)
            => BuildTemplate(
                "Confirma tu correo",
                $"Hola {recipientName}, gracias por registrarte en BOOKLY.",
                "Para activar tu cuenta y comenzar a gestionar tus servicios, confirma tu correo desde el siguiente botón.",
                "Confirmar correo",
                confirmationUrl,
                [
                    $"Este enlace vence en {expirationHours} horas.",
                "Si no creaste esta cuenta, puedes ignorar este correo."
                ]);

        public static string BuildPasswordResetBody(string recipientName, string resetUrl, int expirationHours)
            => BuildTemplate(
                "Restablece tu contraseña",
                $"Hola {recipientName}, recibimos una solicitud para restablecer tu contraseña.",
                "Si fuiste tú, continúa desde el siguiente enlace. Si no reconoces esta acción, ignora este correo.",
                "Restablecer contraseña",
                resetUrl,
                [$"Este enlace vence en {expirationHours} horas."]);

        public static string BuildSecretaryInvitationBody(
            string recipientName,
            string invitedByName,
            string serviceName,
            string invitationUrl,
            int expirationHours)
            => BuildTemplate(
                "Has sido invitado a BOOKLY",
                $"Hola {recipientName}, {invitedByName} te invitó a colaborar en BOOKLY.",
                $"Tu acceso inicial fue asociado al servicio \"{serviceName}\". Para comenzar, define tu contraseña desde el siguiente enlace.",
                "Crear contraseña",
                invitationUrl,
                [$"Este enlace vence en {expirationHours} horas."]);

        public static string BuildAdminInvitationBody(
            string recipientName,
            string invitationUrl,
            int expirationHours)
            => BuildTemplate(
                "Has sido invitado a administrar BOOKLY",
                $"Hola {recipientName}, tu cuenta de administrador ya fue creada en BOOKLY.",
                "Para completar el registro y definir tu contraseña, continúa desde el siguiente enlace.",
                "Crear contraseña",
                invitationUrl,
                [$"Este enlace vence en {expirationHours} horas."]);

        public static string BuildAppointmentCreatedClientBody(
            string recipientName,
            string serviceName,
            string businessName,
            DateTime startDateTime,
            int durationMinutes)
            => BuildTemplate(
                "Tu turno fue confirmado",
                $"Hola {recipientName}, tu turno fue registrado correctamente.",
                $"Servicio: {serviceName}<br/>Negocio: {businessName}<br/>Fecha: {FormatDate(startDateTime)}<br/>Hora: {FormatTime(startDateTime)}<br/>Duración: {durationMinutes} minutos.",
                null,
                null,
                ["Si necesitas reprogramar o cancelar, comunícate con el negocio."]);

        public static string BuildAppointmentCreatedOwnerBody(
            string recipientName,
            string clientName,
            string clientEmail,
            string clientPhone,
            string serviceName,
            DateTime startDateTime,
            int durationMinutes)
            => BuildTemplate(
                "Nuevo turno agendado",
                $"Hola {recipientName}, se creó un nuevo turno en tu agenda.",
                $"Cliente: {clientName}<br/>Email: {clientEmail}<br/>Teléfono: {clientPhone}<br/>Servicio: {serviceName}<br/>Fecha: {FormatDate(startDateTime)}<br/>Hora: {FormatTime(startDateTime)}<br/>Duración: {durationMinutes} minutos.",
                null,
                null,
                ["Puedes revisarlo desde BOOKLY."]);

        public static string BuildAppointmentCancelledClientBody(
            string recipientName,
            string serviceName,
            string businessName,
            DateTime startDateTime,
            string? cancelReason)
            => BuildTemplate(
                "Tu turno fue cancelado",
                $"Hola {recipientName}, tu turno fue cancelado.",
                $"Servicio: {serviceName}<br/>Negocio: {businessName}<br/>Fecha original: {FormatDate(startDateTime)}<br/>Hora original: {FormatTime(startDateTime)}.",
                null,
                null,
                BuildCancellationNotes(cancelReason, "Comunícate con el negocio si necesitas coordinar un nuevo horario."));

        public static string BuildAppointmentCancelledOwnerBody(
            string recipientName,
            string clientName,
            string clientEmail,
            string clientPhone,
            string serviceName,
            DateTime startDateTime,
            string? cancelReason)
            => BuildTemplate(
                "Se canceló un turno",
                $"Hola {recipientName}, se canceló un turno de tu agenda.",
                $"Cliente: {clientName}<br/>Email: {clientEmail}<br/>Teléfono: {clientPhone}<br/>Servicio: {serviceName}<br/>Fecha original: {FormatDate(startDateTime)}<br/>Hora original: {FormatTime(startDateTime)}.",
                null,
                null,
                BuildCancellationNotes(cancelReason, "Puedes revisarlo desde BOOKLY."));

        public static string BuildAppointmentRescheduledClientBody(
            string recipientName,
            string serviceName,
            string businessName,
            DateTime previousStartDateTime,
            DateTime newStartDateTime,
            int durationMinutes)
            => BuildTemplate(
                "Tu turno fue reprogramado",
                $"Hola {recipientName}, tu turno fue reprogramado.",
                $"Servicio: {serviceName}<br/>Negocio: {businessName}<br/>Fecha anterior: {FormatDate(previousStartDateTime)} {FormatTime(previousStartDateTime)}<br/>Nueva fecha: {FormatDate(newStartDateTime)} {FormatTime(newStartDateTime)}<br/>Duración: {durationMinutes} minutos.",
                null,
                null,
                ["Si no reconoces este cambio, comunícate con el negocio."]);

        public static string BuildAppointmentRescheduledOwnerBody(
            string recipientName,
            string clientName,
            string clientEmail,
            string clientPhone,
            string serviceName,
            DateTime previousStartDateTime,
            DateTime newStartDateTime,
            int durationMinutes)
            => BuildTemplate(
                "Se reprogramó un turno",
                $"Hola {recipientName}, se reprogramó un turno de tu agenda.",
                $"Cliente: {clientName}<br/>Email: {clientEmail}<br/>Teléfono: {clientPhone}<br/>Servicio: {serviceName}<br/>Fecha anterior: {FormatDate(previousStartDateTime)} {FormatTime(previousStartDateTime)}<br/>Nueva fecha: {FormatDate(newStartDateTime)} {FormatTime(newStartDateTime)}<br/>Duración: {durationMinutes} minutos.",
                null,
                null,
                ["Puedes revisarlo desde BOOKLY."]);

        private static string BuildTemplate(
            string title,
            string heading,
            string message,
            string? actionText,
            string? actionUrl,
            IReadOnlyCollection<string> notes)
        {
            var notesHtml = string.Join(string.Empty, notes.Select(note => $"<li style=\"margin-bottom:8px;\">{note}</li>"));
            var actionHtml = string.IsNullOrWhiteSpace(actionText) || string.IsNullOrWhiteSpace(actionUrl)
                ? string.Empty
                : $"""
                   <p style="margin:32px 0;">
                       <a href="{actionUrl}" style="background:#14532d;color:#ffffff;text-decoration:none;padding:14px 22px;border-radius:8px;display:inline-block;font-weight:600;">
                           {actionText}
                       </a>
                   </p>
                   <p style="font-size:13px;color:#6b7280;word-break:break-all;">
                       Si el botón no funciona, copia y pega este enlace en tu navegador:<br/>{actionUrl}
                   </p>
                   """;

            return $"""
                    <!DOCTYPE html>
                 <html lang="es">
                 <head>
                     <meta charset="utf-8" />
                     <title>{title}</title>
                 </head>
                 <body style="margin:0;background:#f3f4f6;font-family:Segoe UI,Arial,sans-serif;color:#111827;">
                     <div style="max-width:640px;margin:32px auto;padding:0 16px 24px;">
                         <div style="background:#ffffff;border-radius:16px;padding:40px 32px 52px;box-shadow:0 8px 24px rgba(15,23,42,0.08);">
                             <div style="margin-bottom:24px;">
                                 <div style="font-size:12px;letter-spacing:0.12em;text-transform:uppercase;color:#14532d;font-weight:700;">BOOKLY</div>
                                 <h1 style="margin:12px 0 0;font-size:28px;line-height:1.2;">{heading}</h1>
                             </div>
                             <p style="font-size:15px;line-height:1.7;color:#374151;margin:0 0 16px;">{message}</p>
                             {actionHtml}
                             <ul style="margin:24px 0 8px;padding-left:20px;font-size:14px;line-height:1.6;color:#4b5563;">
                                 {notesHtml}
                             </ul>
                         </div>
                     </div>
                 </body>
                 </html>
                 """;
        }

        private static string FormatDate(DateTime dateTime)
            => dateTime.ToString("dddd d 'de' MMMM 'de' yyyy", CultureInfo.GetCultureInfo("es-AR"));

        private static string FormatTime(DateTime dateTime)
            => dateTime.ToString("HH:mm", CultureInfo.InvariantCulture);

        private static IReadOnlyCollection<string> BuildCancellationNotes(string? cancelReason, string fallbackNote)
        {
            if (string.IsNullOrWhiteSpace(cancelReason))
                return [fallbackNote];

            return [$"Motivo: {cancelReason}", fallbackNote];
        }
    }
}

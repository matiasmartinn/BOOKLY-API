# BOOKLY-API

## Configuracion de emails

Para que el flujo de emails funcione de punta a punta, la API necesita dos grupos de valores:

1. Configuracion SMTP para enviar el correo.
2. Rutas del frontend donde el usuario termina cada flujo.

### Appsettings minimo

```json
{
  "Frontend": {
    "BaseUrl": "http://localhost:5173",
    "ConfirmEmailPath": "/auth/confirm-email",
    "ResetPasswordPath": "/auth/reset-password",
    "CompleteSecretaryInvitationPath": "/auth/secretary-invitation"
  },
  "Email": {
    "SenderName": "BOOKLY",
    "SenderAddress": "no-reply@tu-dominio.com",
    "Smtp": {
      "Host": "smtp.tu-proveedor.com",
      "Port": 587,
      "Username": "usuario-smtp",
      "Password": "password-o-app-password",
      "EnableSsl": true
    }
  }
}
```

### Que hace cada valor

- `Frontend:BaseUrl`: dominio base del frontend que recibira los clicks de los emails.
- `Frontend:ConfirmEmailPath`: ruta donde el frontend toma `token` y llama a `POST /api/auth/confirm-email`.
- `Frontend:ResetPasswordPath`: ruta donde el frontend toma `token` y llama a `POST /api/auth/reset-password`.
- `Frontend:CompleteSecretaryInvitationPath`: ruta donde el frontend toma `token` y llama a `POST /api/auth/secretary-invitations/complete`.
- `Email:SenderAddress`: remitente visible del email.
- `Email:Smtp:*`: credenciales y host del servidor SMTP.

### Comportamiento esperado si falta configuracion

El negocio no se rompe: el usuario, token o invitacion se generan igual.

Pero el envio queda registrado en logs con un warning y un mensaje explicito indicando que clave falta completar, por ejemplo:

- `Email:SenderAddress`
- `Email:Smtp:Host`
- `Frontend:BaseUrl`
- `Frontend:ConfirmEmailPath`

Eso permite detectar rapido si el problema es de SMTP o de construccion del link.

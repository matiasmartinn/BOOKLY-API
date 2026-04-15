# BOOKLY-API

## Configuracion local y productiva

La API ahora queda preparada para este orden de carga:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. variables de entorno

### Que queda versionado

- `appsettings.json`: placeholders y valores no sensibles.
- `BOOKLY.Api/appsettings.Development.json` queda ignorado por git y es la fuente local simple para desarrollo.

### Configuracion local recomendada

Completa `BOOKLY.Api/appsettings.Development.json` con tus valores reales de desarrollo.

En desarrollo y en produccion la API usa el mismo servicio SMTP. La diferencia entre ambos modos queda solo en la fuente de configuracion: `appsettings.Development.json` para local, y variables de entorno o secretos del entorno para produccion.

Importante: si usas Brevo, el password debe ser la clave SMTP y no la password de la cuenta ni una API key. Si el relay responde `5.7.0 Please authenticate first`, revisa esas credenciales.

### Variables de entorno para produccion

Las claves esperadas en produccion son las estandares de .NET, por ejemplo:

- `ConnectionStrings__BooklyDb`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__SecretKey`
- `Jwt__AccessTokenExpirationMinutes`
- `Email__SenderAddress`
- `Email__Smtp__Host`
- `Email__Smtp__Port`
- `Email__Smtp__Username`
- `Email__Smtp__Password`
- `Email__Smtp__EnableSsl`
- `Frontend__BaseUrl`

## Configuracion de emails

Para que el flujo de emails funcione de punta a punta, la API necesita dos grupos de valores:

1. Configuracion SMTP para enviar el correo.
2. Rutas del frontend donde el usuario termina cada flujo.

### Estructura minima de configuracion

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

En `Development`, la API solo completa defaults seguros para `Jwt:SecretKey` y `Frontend:BaseUrl`. El envio de email sigue requiriendo configuracion SMTP real. Si falta algun valor, la API no reporta envio exitoso.

Pero el envio queda registrado en logs con un warning y un mensaje explicito indicando que clave falta completar, por ejemplo:

- `Email:SenderAddress`
- `Email:Smtp:Host`
- `Frontend:BaseUrl`
- `Frontend:ConfirmEmailPath`

Eso permite detectar rapido si el problema es de SMTP o de construccion del link.

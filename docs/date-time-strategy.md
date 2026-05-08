# Estrategia de fechas y horas de BOOKLY

BOOKLY trabaja con horario Argentina para toda fecha/hora visible de negocio.

## Contrato

- `DateOnly`: `yyyy-MM-dd`.
- `TimeOnly`: `HH:mm`.
- `LocalDateTime`: `yyyy-MM-ddTHH:mm:ss`.
- Las fechas de negocio viajan por API sin `Z` y sin offset.
- Un `LocalDateTime` de negocio representa hora local Argentina, no un instante UTC.

## Backend

- Los DTOs de comandos reciben fechas de negocio como strings locales.
- Application parsea el string local una sola vez con `BusinessDateTime`.
- Dominio, mappers y repositorios trabajan con `DateTimeKind.Unspecified`.
- `IDateTimeProvider.NowArgentina()` es la fuente de "ahora" para fechas visibles:
  `CreatedAt`, `UpdatedAt`, cancelaciones, asistencia, eventos e historial.
- `IDateTimeProvider.UtcNow()` queda reservado para expiraciones tecnicas:
  JWT, refresh tokens, cookies y security tokens.

## Persistencia y API

- Las columnas visibles de negocio se guardan como `timestamp without time zone`.
- Las columnas tecnicas de expiracion se guardan como `timestamp with time zone`.
- El `DbContext` no convierte fechas visibles de negocio a/desde UTC.
- La API devuelve fechas de negocio locales sin `Z`, por ejemplo:
  `2026-05-07T23:29:00`.

## Frontend

- El frontend envia fechas de negocio como strings locales.
- Los DatePicker usan `yyyy-MM-dd`.
- Los slots enviados a crear/reprogramar turnos usan `yyyy-MM-ddTHH:mm:ss`.
- La UI formatea strings locales sin reinterpretarlos como UTC.
- Si por excepcion llega un timestamp con `Z` u offset, el formatter central puede
  convertirlo a `America/Argentina/Buenos_Aires` solo para mostrarlo.

## Caso de referencia

Si una accion ocurre el `07/05/2026 23:29` Argentina:

- DB guarda `2026-05-07 23:29:00` en auditoria visible.
- API devuelve `2026-05-07T23:29:00`.
- Front muestra `07/05/2026 23:29`.
- Nunca debe mostrarse `08/05/2026 02:29` para esa fecha visible de negocio.

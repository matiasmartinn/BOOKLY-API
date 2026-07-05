namespace BOOKLY.Application.Common
{
    public static class ActorUserId
    {
        // Ids no positivos representan acciones sin actor identificado (p. ej. reservas públicas).
        public static int? Normalize(int? userId)
            => userId.HasValue && userId.Value > 0 ? userId.Value : null;
    }
}

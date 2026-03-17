using BOOKLY.Domain.Exceptions;

namespace BOOKLY.Domain.Aggregates.ServiceAggregate.Entities
{
    public sealed class ServiceSecretary
    {
        public int ServiceId { get; private set; }
        public int SecretaryId { get; private set; }
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
    }
}

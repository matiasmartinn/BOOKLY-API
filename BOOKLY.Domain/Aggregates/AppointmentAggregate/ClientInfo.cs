using BOOKLY.Domain.SharedKernel;

namespace BOOKLY.Domain.Aggregates.AppointmentAggregate
{
    public record ClientInfo
    {
        public string ClientName { get; init; } = null!;
        public string Phone { get; init; } = null!;
        public Email Email { get; init; } = null!;
        private ClientInfo() { }
        private ClientInfo(string clientName, string phone, Email email)
        {
            ClientName = clientName;
            Phone = phone;
            Email = email;
        }
        public static ClientInfo Create(string clientName, string phone, Email email)
        {
            if (string.IsNullOrWhiteSpace(clientName))
                throw new DomainException("El nombre del cliente es requerido");
            if (string.IsNullOrWhiteSpace(phone))
                throw new DomainException("El numero del cliente es requerido");

            return new ClientInfo(clientName.Trim(), phone.Trim(), email);
        }
    }

    
}

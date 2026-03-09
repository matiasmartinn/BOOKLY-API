using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace BOOKLY.Infrastructure
{
    /// <summary>
    /// DomainEventDispatcher resuelve e invoca, mediante DI y reflection
    /// todos los handlers registrados para cada evento de dominio,
    /// permitiendo ejecutar lógica desacoplada cuando
    /// ocurren cambios relevantes en el dominio.
    public class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IServiceProvider _provider;

        public DomainEventDispatcher(IServiceProvider provider) => _provider = provider;

        /// <summary>
        /// Despacha una colección de eventos de dominio, ejecutando todos los handlers
        /// asociados a cada tipo de evento.
        /// </summary>
        /// <param name="events">Colección de eventos de dominio a despachar.</param>
        /// <param name="ct">Token de cancelación.</param>
        /// <returns>Una tarea asíncrona que representa la operación de despacho.</returns>
        public async Task Dispatch(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
        {
            foreach (var @event in events)
            {
                // Construye el tipo cerrado:
                // IDomainEventHandler<TEventoConcreto>
                var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(@event.GetType());

                // Resuelve todos los handlers registrados para ese tipo de evento.
                var handlers = _provider.GetServices(handlerType);

                foreach (var handler in handlers)
                {
                    // Mediante reflection trae el método Handler.
                    // Invoca dinámicamente Handle(TEvent, CancellationToken)
                    // y espera su finalización.
                    await (Task)handlerType
                        .GetMethod("Handle")!
                        .Invoke(handler, [@event, ct])!;
                }
            }
        }
    }
}

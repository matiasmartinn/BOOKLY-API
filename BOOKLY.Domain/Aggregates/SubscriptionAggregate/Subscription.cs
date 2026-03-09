namespace BOOKLY.Domain.Aggregates.SubscriptionAggregate
{
    public sealed class Subscription
    {
        public int Id { get; private set; }
        public int OwnerId { get; private set; }

        public SubscriptionStatus Status { get; private set; }
        public SubscriptionPeriod Period { get; private set; } = null!;
        public SubscriptionPlan Plan { get; private set; } = null!;
        public DateTime CreatedOn { get; private set; }
        public DateTime? UpdateOn { get; private set; }

        private Subscription() { }

        public static Subscription CreateFree(int ownerId)
        {
            if (ownerId <= 0)
                throw new DomainException("El owner es requerido");

            var startDate = DateOnly.FromDateTime(DateTime.Now);

            return new Subscription
            {
                OwnerId = ownerId,
                Plan = SubscriptionPlan.Free(),
                Status = SubscriptionStatus.Active,
                Period = SubscriptionPeriod.OpenEnded(startDate),
                CreatedOn = DateTime.Now,
            };
        }

        public static Subscription CreatePaid(int ownerId, SubscriptionPlan plan, SubscriptionPeriod period)
        {
            if (ownerId <= 0)
                throw new DomainException("El owner es requerido");

            if (plan == null)
                throw new DomainException("El plan es requerido.");

            if (plan.Name == PlanName.Free)
                throw new DomainException("Use CreateFree para el plan Free.");

            if (period == null)
                throw new DomainException("El período es requerido.");

            if (period.IsOpenEnded)
                throw new DomainException("Un plan pago debe tener EndDate.");

            return new Subscription
            {
                OwnerId = ownerId,
                Plan = plan,
                Status = SubscriptionStatus.Active,
                Period = period,
                CreatedOn = DateTime.Now,
            };
        }

        public bool IsExpired(DateOnly today)
        {
            if (Period.IsOpenEnded)
                return false;

            return today > Period.EndDate!.Value;
        }

        /// <summary>
        /// Activa si:
        /// - No está cancelada
        /// - No está vencida por fecha
        /// </summary>
        public bool IsActive(DateOnly today)
        {
            if (Status != SubscriptionStatus.Active && Status != SubscriptionStatus.Cancelled)
                return false;

            return !IsExpired(today);
        }

        /// <summary>
        /// Cancelled = "No renovar". En este modelo no hay auto-renovación,
        /// pero sí existe el acto de cancelar para marcar que no se extenderá.
        /// - Sigue activa hasta EndDate.
        /// - Free no se cancela.
        /// </summary>
        public void Cancel()
        {
            if (Plan.Name == PlanName.Free)
                throw new DomainException("El plan Free no puede cancelarse.");

            if (Status == SubscriptionStatus.Cancelled)
                return;

            Status = SubscriptionStatus.Cancelled;
        }

        /// <summary>
        /// Renovación MANUAL (simulada): el Owner paga/renueva por fuera,
        /// y el sistema actualiza el periodo. También reactiva la suscripción.
        /// </summary>
        public void Renew(SubscriptionPeriod newPeriod)
        {
            if (Plan.Name == PlanName.Free)
                throw new DomainException("El plan Free no requiere renovación.");

            if (newPeriod is null)
                throw new DomainException("El período es requerido.");

            if (newPeriod.IsOpenEnded)
                throw new DomainException("Un plan pago debe tener EndDate.");

            Period = newPeriod;
            Status = SubscriptionStatus.Active;
        }

        public void UpgradeTo(SubscriptionPlan newPlan)
        {
            if (newPlan is null)
                throw new DomainException("El nuevo plan es requerido.");

            if (newPlan.Name == Plan.Name)
                return;

            if (newPlan.Name < Plan.Name)
                throw new DomainException("El upgrade debe ser hacia un plan superior.");

            Plan = newPlan;
        }

        /// <summary>
        /// Downgrade requiere validar que NO exceda límites del nuevo plan.
        /// Los conteos llegan desde la capa de aplicación (query).
        /// </summary>
        public void DowngradeTo(SubscriptionPlan newPlan, int currentServices, int currentSecretaries)
        {
            if (newPlan is null)
                throw new DomainException("El nuevo plan es requerido.");

            if (newPlan.Name == Plan.Name)
                return;

            if (newPlan.Name > Plan.Name)
                throw new DomainException("El downgrade debe ser hacia un plan inferior.");

            if (!newPlan.AllowsServices(currentServices))
                throw new DomainException("No se puede bajar de plan: excede el límite de servicios.");

            if (!newPlan.AllowsSecretaries(currentSecretaries))
                throw new DomainException("No se puede bajar de plan: excede el límite de secretarios.");

            Plan = newPlan;
        }
        public void EnsureCanCreateService(int currentServices)
        {
            if (!Plan.AllowsServices(currentServices + 1))
                throw new DomainException("El plan actual no permite crear más servicios.");
        }

        public void EnsureCanAssignSecretary(int currentSecretaries)
        {
            if (!Plan.AllowsSecretaries(currentSecretaries + 1))
                throw new DomainException("El plan actual no permite agregar más secretarios.");
        }
    }
}

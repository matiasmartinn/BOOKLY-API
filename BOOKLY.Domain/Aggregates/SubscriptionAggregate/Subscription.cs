using BOOKLY.Domain.Exceptions;

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
        public DateTime? UpdatedOn { get; private set; }

        private Subscription() { }

        public static Subscription CreateFree(int ownerId, DateTime now)
        {
            if (ownerId <= 0)
                throw new DomainException("El owner es requerido");

            var startDate = DateOnly.FromDateTime(now);

            return new Subscription
            {
                OwnerId = ownerId,
                Plan = SubscriptionPlan.Free(),
                Status = SubscriptionStatus.Active,
                Period = SubscriptionPeriod.OpenEnded(startDate),
                CreatedOn = now,
            };
        }

        public static Subscription CreatePaid(int ownerId, SubscriptionPlan plan, SubscriptionPeriod period, DateTime now)
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
                CreatedOn = now,
            };
        }

        public static Subscription Create(int ownerId, SubscriptionPlan plan, DateTime now)
        {
            if (plan is null)
                throw new DomainException("El plan es requerido.");

            var startDate = DateOnly.FromDateTime(now);

            if (plan.Name == PlanName.Free)
                return CreateFree(ownerId, now);

            var period = SubscriptionPeriod.CreateMonthly(startDate);

            return CreatePaid(ownerId, plan, period, now);
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
        /// Cancelled = "No renovar".
        /// pero sí existe el acto de cancelar para marcar que no se extenderá.
        /// - Sigue activa hasta EndDate.
        /// - Free no se cancela.
        /// </summary>
        public void Cancel(DateTime now)
        {
            if (Plan.Name == PlanName.Free)
                throw new DomainException("El plan Free no puede cancelarse.");

            if (Status == SubscriptionStatus.Cancelled)
                return;

            Status = SubscriptionStatus.Cancelled;
            UpdatedOn = now;
        }
        public void Renew(SubscriptionPeriod newPeriod, DateTime now)
        {
            if (Plan.Name == PlanName.Free)
                throw new DomainException("El plan Free no requiere renovación.");

            if (newPeriod is null)
                throw new DomainException("El período es requerido.");

            if (newPeriod.IsOpenEnded)
                throw new DomainException("Un plan pago debe tener EndDate.");

            Period = newPeriod;
            Status = SubscriptionStatus.Active;
            UpdatedOn = now;
        }

        public void UpgradeTo(SubscriptionPlan newPlan, DateTime now)
        {
            if (newPlan is null)
                throw new DomainException("El nuevo plan es requerido.");

            if (newPlan.Name == PlanName.Free)
                throw new DomainException("Use ChangeToFree para pasar al plan Free.");

            if (newPlan.Name == Plan.Name)
                return;

            if (Plan.Name == PlanName.Free)
                throw new DomainException("Para pasar de Free a un plan pago debe definir un período.");

            if (newPlan.Name < Plan.Name)
                throw new DomainException("El upgrade debe ser hacia un plan superior.");

            Plan = newPlan;
            UpdatedOn = now;
        }

        public void DowngradeTo(SubscriptionPlan newPlan, int currentServices, int currentSecretaries, DateTime now)
        {
            if (newPlan is null)
                throw new DomainException("El nuevo plan es requerido.");

            if (newPlan.Name == PlanName.Free)
                throw new DomainException("Use ChangeToFree para pasar al plan Free.");

            if (newPlan.Name == Plan.Name)
                return;

            if (newPlan.Name > Plan.Name)
                throw new DomainException("El downgrade debe ser hacia un plan inferior.");

            if (!newPlan.AllowsServices(currentServices))
                throw new DomainException("No se puede bajar de plan: excede el límite de servicios.");

            if (!newPlan.AllowsSecretaries(currentSecretaries))
                throw new DomainException("No se puede bajar de plan: excede el límite de secretarios.");

            Plan = newPlan;
            UpdatedOn = now;
        }

        public void ChangeToFree(DateOnly startDate, int currentServices, int currentSecretaries, DateTime now)
        {
            var freePlan = SubscriptionPlan.Free();

            if (!freePlan.AllowsServices(currentServices))
                throw new DomainException("No se puede bajar de plan: excede el límite de servicios.");

            if (!freePlan.AllowsSecretaries(currentSecretaries))
                throw new DomainException("No se puede bajar de plan: excede el límite de secretarios.");

            Plan = freePlan;
            Period = SubscriptionPeriod.OpenEnded(startDate);
            Status = SubscriptionStatus.Active;
            UpdatedOn = now;
        }

        public void SwitchFromFreeToPaid(SubscriptionPlan newPlan, SubscriptionPeriod newPeriod, DateTime now)
        {
            if (newPlan is null)
                throw new DomainException("El nuevo plan es requerido.");

            if (Plan.Name != PlanName.Free)
                throw new DomainException("La suscripción actual no es Free.");

            if (newPlan.Name == PlanName.Free)
                throw new DomainException("Use ChangeToFree para el plan Free.");

            if (newPeriod is null)
                throw new DomainException("El período es requerido.");

            if (newPeriod.IsOpenEnded)
                throw new DomainException("Un plan pago debe tener EndDate.");

            Plan = newPlan;
            Period = newPeriod;
            Status = SubscriptionStatus.Active;
            UpdatedOn = now;
        }

        public void ChangePlan(
            SubscriptionPlan newPlan,
            SubscriptionUsage usage,
            DateTime now)
        {
            if (newPlan is null)
                throw new DomainException("El nuevo plan es requerido.");

            usage ??= SubscriptionUsage.Empty;

            if (newPlan.Name == Plan.Name)
                return;

            var startDate = DateOnly.FromDateTime(now);

            if (newPlan.Name == PlanName.Free)
            {
                ChangeToFree(
                    startDate,
                    usage.CurrentServices,
                    usage.CurrentSecretaries,
                    now);

                return;
            }

            if (Plan.Name == PlanName.Free)
            {
                SwitchFromFreeToPaid(
                    newPlan,
                    SubscriptionPeriod.CreateMonthly(startDate),
                    now);

                return;
            }

            if (IsDowngrade(newPlan, Plan))
            {
                DowngradeTo(
                    newPlan,
                    usage.CurrentServices,
                    usage.CurrentSecretaries,
                    now);
            }
            else
            {
                UpgradeTo(newPlan, now);
            }

            Renew(SubscriptionPeriod.CreateMonthly(startDate), now);
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

        public bool CanUseExtraFields() => Plan.AllowsExtraFields();

        private static bool IsDowngrade(SubscriptionPlan newPlan, SubscriptionPlan currentPlan)
        {
            return GetPlanRank(newPlan.Name) < GetPlanRank(currentPlan.Name);
        }

        private static int GetPlanRank(PlanName planName)
        {
            return planName switch
            {
                PlanName.Free => 1,
                PlanName.Pro => 2,
                PlanName.Max => 3,
                _ => throw new DomainException("Plan inválido.")
            };
        }
    }

    public sealed record SubscriptionUsage(
        int CurrentServices,
        int CurrentSecretaries)
    {
        public static SubscriptionUsage Empty => new(0, 0);
    }
}

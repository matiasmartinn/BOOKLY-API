namespace BOOKLY.Domain.Aggregates.SubscriptionAggregate
{
    public sealed record SubscriptionPlan
    {
        public PlanName Name { get; private set; }
        public int MaxServices { get; private set; }
        public int MaxSecretaries { get; private set; }

        private SubscriptionPlan() { }

        private SubscriptionPlan(PlanName name, int maxServices, int maxSecretaries)
        {
            Name = name;
            MaxServices = maxServices;
            MaxSecretaries = maxSecretaries;
        }
        public static SubscriptionPlan Free()
        {
            return new SubscriptionPlan(PlanName.Free, 1, 0);
        }

        public static SubscriptionPlan Pro()
        {
            return new SubscriptionPlan(PlanName.Pro, 3, 1);
        }

        public static SubscriptionPlan Max()
        {
            return new SubscriptionPlan(PlanName.Max, 5, 3);
        }

        public static IReadOnlyList<SubscriptionPlan> GetCatalog()
        {
            return
            [
                Free(),
                Pro(),
                Max()
            ];
        }

        public static SubscriptionPlan FromName(PlanName planName)
        {
            return planName switch
            {
                PlanName.Free => Free(),
                PlanName.Pro => Pro(),
                PlanName.Max => Max(),
                _ => throw new Domain.Exceptions.DomainException("Plan inválido.")
            };
        }

        public bool AllowsServices(int currentServices)
        {
            if(currentServices < 0) 
                return false;
            
            return currentServices <= MaxServices;
        }

        public bool AllowsSecretaries(int currentSecretaries)
        {
            if (currentSecretaries < 0)
                return false;

            return currentSecretaries <= MaxSecretaries;
        }

        public bool AllowsExtraFields()
        {
            return Name >= PlanName.Pro;
        }
    }
}

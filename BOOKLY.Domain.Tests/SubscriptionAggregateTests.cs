using BOOKLY.Domain.Aggregates.SubscriptionAggregate;
using BOOKLY.Domain.Exceptions;

namespace BOOKLY.Domain.Tests;

public sealed class SubscriptionAggregateTests
{
    private static readonly DateTime ReferenceNow = new(2026, 3, 27, 10, 0, 0);

    [Fact]
    public void EnsureCanCreateService_ShouldThrow_WhenFreePlanReachedLimit()
    {
        var subscription = Subscription.CreateFree(1, ReferenceNow);

        var action = () => subscription.EnsureCanCreateService(1);

        var exception = Assert.Throws<DomainException>(action);
        Assert.Equal("El plan actual no permite crear m\u00E1s servicios.", exception.Message);
    }

    [Fact]
    public void EnsureCanAssignSecretary_ShouldThrow_WhenFreePlanDisallowsSecretaries()
    {
        var subscription = Subscription.CreateFree(1, ReferenceNow);

        var action = () => subscription.EnsureCanAssignSecretary(0);

        var exception = Assert.Throws<DomainException>(action);
        Assert.Equal("El plan actual no permite agregar m\u00E1s secretarios.", exception.Message);
    }

    [Fact]
    public void CanUseExtraFields_ShouldReturnFalse_WhenPlanDoesNotAllowThem()
    {
        var subscription = Subscription.CreateFree(1, ReferenceNow);

        Assert.False(subscription.CanUseExtraFields());
    }

    [Fact]
    public void CanUseExtraFields_ShouldReturnTrue_WhenPlanAllowsThem()
    {
        var subscription = Subscription.CreatePaid(
            1,
            SubscriptionPlan.Pro(),
            SubscriptionPeriod.Create(
                DateOnly.FromDateTime(ReferenceNow),
                DateOnly.FromDateTime(ReferenceNow.AddMonths(1))),
            ReferenceNow);

        Assert.True(subscription.CanUseExtraFields());
    }

    [Fact]
    public void SwitchFromFreeToPaid_ShouldSetPaidPlanAndPeriod()
    {
        var subscription = Subscription.CreateFree(1, ReferenceNow);
        var renewalNow = ReferenceNow.AddDays(1);
        var period = SubscriptionPeriod.Create(
            DateOnly.FromDateTime(renewalNow),
            DateOnly.FromDateTime(renewalNow.AddMonths(1)));

        subscription.SwitchFromFreeToPaid(SubscriptionPlan.Pro(), period, renewalNow);

        Assert.Equal(PlanName.Pro, subscription.Plan.Name);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.False(subscription.Period.IsOpenEnded);
        Assert.Equal(period.EndDate, subscription.Period.EndDate);
        Assert.Equal(renewalNow, subscription.UpdatedOn);
    }

    [Fact]
    public void ChangeToFree_ShouldResetOpenEndedPeriodAndReactivateSubscription()
    {
        var subscription = Subscription.CreatePaid(
            1,
            SubscriptionPlan.Pro(),
            SubscriptionPeriod.Create(
                DateOnly.FromDateTime(ReferenceNow),
                DateOnly.FromDateTime(ReferenceNow.AddMonths(1))),
            ReferenceNow);

        subscription.Cancel(ReferenceNow.AddDays(5));

        var downgradeNow = ReferenceNow.AddDays(10);
        var downgradeStartDate = DateOnly.FromDateTime(downgradeNow);

        subscription.ChangeToFree(downgradeStartDate, currentServices: 1, currentSecretaries: 0, downgradeNow);

        Assert.Equal(PlanName.Free, subscription.Plan.Name);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.True(subscription.Period.IsOpenEnded);
        Assert.Equal(downgradeStartDate, subscription.Period.StartDate);
        Assert.Null(subscription.Period.EndDate);
        Assert.Equal(downgradeNow, subscription.UpdatedOn);
    }

    [Fact]
    public void Create_ShouldUseAutomaticMonthlyPeriod_ForPaidPlan()
    {
        var subscription = Subscription.Create(1, SubscriptionPlan.Pro(), ReferenceNow);

        Assert.Equal(PlanName.Pro, subscription.Plan.Name);
        Assert.Equal(DateOnly.FromDateTime(ReferenceNow), subscription.Period.StartDate);
        Assert.Equal(DateOnly.FromDateTime(ReferenceNow).AddMonths(1).AddDays(-1), subscription.Period.EndDate);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
    }

    [Fact]
    public void ChangePlan_ShouldRenewAndReactivatePaidUpgrade()
    {
        var subscription = Subscription.CreatePaid(
            1,
            SubscriptionPlan.Pro(),
            SubscriptionPeriod.Create(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28)),
            ReferenceNow.AddMonths(-1));
        subscription.Cancel(ReferenceNow.AddDays(-1));

        subscription.ChangePlan(SubscriptionPlan.Max(), SubscriptionUsage.Empty, ReferenceNow);

        Assert.Equal(PlanName.Max, subscription.Plan.Name);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal(DateOnly.FromDateTime(ReferenceNow), subscription.Period.StartDate);
        Assert.Equal(DateOnly.FromDateTime(ReferenceNow).AddMonths(1).AddDays(-1), subscription.Period.EndDate);
    }

    [Fact]
    public void ChangePlan_ShouldRejectDowngrade_WhenUsageExceedsNewPlan()
    {
        var originalPeriod = SubscriptionPeriod.Create(
            DateOnly.FromDateTime(ReferenceNow),
            DateOnly.FromDateTime(ReferenceNow).AddMonths(1).AddDays(-1));
        var subscription = Subscription.CreatePaid(
            1,
            SubscriptionPlan.Max(),
            originalPeriod,
            ReferenceNow);

        var action = () => subscription.ChangePlan(
            SubscriptionPlan.Pro(),
            new SubscriptionUsage(CurrentServices: 4, CurrentSecretaries: 0),
            ReferenceNow.AddDays(1));

        var exception = Assert.Throws<DomainException>(action);
        Assert.Equal("No se puede bajar de plan: excede el límite de servicios.", exception.Message);
        Assert.Equal(PlanName.Max, subscription.Plan.Name);
        Assert.Equal(originalPeriod, subscription.Period);
    }

    [Fact]
    public void CreateMonthly_ShouldCreateInclusiveMonth_WhenStartIsFirstDay()
    {
        var period = SubscriptionPeriod.CreateMonthly(new DateOnly(2026, 3, 1));

        Assert.Equal(new DateOnly(2026, 3, 1), period.StartDate);
        Assert.Equal(new DateOnly(2026, 3, 31), period.EndDate);
        Assert.False(period.IsOpenEnded);
    }

    [Fact]
    public void CreateMonthly_ShouldCreateInclusiveMonth_WhenStartIsMidMonth()
    {
        var period = SubscriptionPeriod.CreateMonthly(new DateOnly(2026, 3, 15));

        Assert.Equal(new DateOnly(2026, 3, 15), period.StartDate);
        Assert.Equal(new DateOnly(2026, 4, 14), period.EndDate);
        Assert.False(period.IsOpenEnded);
    }

    [Fact]
    public void CreateMonthly_ShouldCreateInclusiveMonth_WhenStartIsDay31()
    {
        var period = SubscriptionPeriod.CreateMonthly(new DateOnly(2026, 3, 31));

        Assert.Equal(new DateOnly(2026, 3, 31), period.StartDate);
        Assert.Equal(new DateOnly(2026, 4, 29), period.EndDate);
        Assert.False(period.IsOpenEnded);
    }

    [Fact]
    public void CreateMonthly_ShouldCreateInclusiveMonth_WhenFebruaryIsLeapYear()
    {
        var period = SubscriptionPeriod.CreateMonthly(new DateOnly(2028, 2, 1));

        Assert.Equal(new DateOnly(2028, 2, 1), period.StartDate);
        Assert.Equal(new DateOnly(2028, 2, 29), period.EndDate);
        Assert.False(period.IsOpenEnded);
    }

    [Fact]
    public void IsActive_ShouldTreatEndDateAsInclusive()
    {
        var subscription = Subscription.CreatePaid(
            1,
            SubscriptionPlan.Pro(),
            SubscriptionPeriod.Create(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)),
            ReferenceNow);

        Assert.True(subscription.IsActive(new DateOnly(2026, 3, 31)));
        Assert.False(subscription.IsExpired(new DateOnly(2026, 3, 31)));
        Assert.False(subscription.IsActive(new DateOnly(2026, 4, 1)));
        Assert.True(subscription.IsExpired(new DateOnly(2026, 4, 1)));
    }

    [Fact]
    public void Create_ShouldThrow_WhenEndDateIsBeforeStartDate()
    {
        var action = () => SubscriptionPeriod.Create(new DateOnly(2026, 4, 1), new DateOnly(2026, 3, 31));

        Assert.Throws<DomainException>(action);
    }
}

using System.Globalization;
using BOOKLY.Application.Services.AdminAggregate.DTOs;
using BOOKLY.Domain.Aggregates.SubscriptionAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate;

namespace BOOKLY.Application.Services.AdminAggregate
{
    internal static class AdminContractsSupport
    {
        private static readonly CultureInfo EsArCulture = new("es-AR");

        public static AdminPlanDto ToPlanDto(PlanName planName)
        {
            return new AdminPlanDto
            {
                Code = (int)planName,
                Key = GetPlanKey(planName),
                DisplayName = GetPlanDisplayName(planName)
            };
        }

        public static string BuildPeriodLabel(DateOnly periodStart)
        {
            return periodStart.ToString("MMM yyyy", EsArCulture);
        }

        public static string GetOwnerAccountStatus(UserStatus status)
        {
            return status switch
            {
                UserStatus.Active => "active",
                UserStatus.Inactive => "disabled",
                UserStatus.PendingEmailConfirmation => "pending_email_confirmation",
                UserStatus.PendingInvitationAcceptance => "pending_invitation_acceptance",
                _ => "unknown"
            };
        }

        public static string GetOwnerAccountStatusLabel(string status)
        {
            return status switch
            {
                "active" => "Activos",
                "disabled" => "Deshabilitados",
                "pending_email_confirmation" => "Pendientes de confirmacion",
                "pending_invitation_acceptance" => "Pendientes de invitacion",
                _ => status
            };
        }

        public static string GetServiceStatus(bool isActive)
        {
            return isActive ? "active" : "disabled";
        }

        public static string GetServiceStatusLabel(string status)
        {
            return status switch
            {
                "active" => "Activos",
                "disabled" => "Deshabilitados",
                _ => status
            };
        }

        public static string GetSubscriptionStatus(
            bool hasPersistedSubscription,
            SubscriptionStatus? subscriptionStatus,
            DateOnly? subscriptionEndDate,
            DateOnly today)
        {
            if (!hasPersistedSubscription)
            {
                return "implicit_free";
            }

            if (subscriptionEndDate.HasValue && subscriptionEndDate.Value < today)
            {
                return "expired";
            }

            return subscriptionStatus == SubscriptionStatus.Cancelled
                ? "pending_cancellation"
                : "active";
        }

        public static bool TryParsePlan(string? rawValue, out PlanName? planName)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                planName = null;
                return true;
            }

            var normalized = rawValue.Trim();

            if (int.TryParse(normalized, out var code) &&
                Enum.IsDefined(typeof(PlanName), code))
            {
                planName = (PlanName)code;
                return true;
            }

            if (Enum.TryParse<PlanName>(normalized, ignoreCase: true, out var parsed))
            {
                planName = parsed;
                return true;
            }

            normalized = normalized.ToLowerInvariant();
            planName = normalized switch
            {
                "free" => PlanName.Free,
                "pro" => PlanName.Pro,
                "max" => PlanName.Max,
                _ => null
            };

            return planName.HasValue;
        }

        public static bool TryNormalizeOwnerStatus(string? rawValue, out string? normalizedStatus)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                normalizedStatus = null;
                return true;
            }

            normalizedStatus = rawValue.Trim().ToLowerInvariant() switch
            {
                "inactive" => "disabled",
                var value => value
            };

            return normalizedStatus is "active"
                or "disabled"
                or "pending_email_confirmation"
                or "pending_invitation_acceptance";
        }

        public static bool TryNormalizeServiceStatus(string? rawValue, out string? normalizedStatus)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                normalizedStatus = null;
                return true;
            }

            normalizedStatus = rawValue.Trim().ToLowerInvariant();
            return normalizedStatus is "active" or "disabled";
        }

        private static string GetPlanKey(PlanName planName)
        {
            return planName switch
            {
                PlanName.Free => "free",
                PlanName.Pro => "pro",
                PlanName.Max => "max",
                _ => "unknown"
            };
        }

        private static string GetPlanDisplayName(PlanName planName)
        {
            return planName switch
            {
                PlanName.Free => "Free",
                PlanName.Pro => "Pro",
                PlanName.Max => "Max",
                _ => "Unknown"
            };
        }
    }
}

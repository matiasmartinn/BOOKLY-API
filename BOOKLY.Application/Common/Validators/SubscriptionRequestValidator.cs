using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.SubscriptionAggregate.Dto;

namespace BOOKLY.Application.Common.Validators
{
    public static class SubscriptionRequestValidator
    {
        public static Result ValidateOwnerId(int ownerId)
        {
            return ownerId > 0
                ? Result.Success()
                : Result.Failure(Error.Validation("OwnerId inválido."));
        }

        public static Result ValidateRenew(RenewSubscriptionDto dto)
        {
            if (dto is null)
                return Result.Failure(Error.Validation("Los datos de la renovación son requeridos."));

            return ValidateOwnerId(dto.OwnerId);
        }

        public static Result ValidateChangePlan(ChangePlanDto dto)
        {
            if (dto is null)
                return Result.Failure(Error.Validation("Los datos del cambio de plan son requeridos."));

            return ValidateOwnerId(dto.OwnerId);
        }
    }
}

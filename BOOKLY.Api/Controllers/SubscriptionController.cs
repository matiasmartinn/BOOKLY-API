using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.SubscriptionAggregate.Dto;
using Microsoft.AspNetCore.Mvc;

namespace BOOKLY.Api.Controllers
{
    [ApiController]
    [Route("api/subscriptions")]
    public sealed class SubscriptionController : BaseController
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IUserService _userService;

        public SubscriptionController(
            ISubscriptionService subscriptionService,
            IUserService userService)
        {
            _subscriptionService = subscriptionService;
            _userService = userService;
        }

        /// <summary>
        /// Obtiene la suscripción de un Owner
        /// </summary>
        [HttpGet("owner/{ownerId:int}")]
        [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByOwnerId(int ownerId, CancellationToken ct)
        {
            var ownerValidation = await EnsureOwner(ownerId, ct);
            if (ownerValidation.IsFailure)
                return HandleResult(ownerValidation);

            return HandleResult(await _subscriptionService.GetByOwnerId(ownerId, ct));
        }

        /// <summary>
        /// Obtiene el catálogo de planes disponible para un Owner,
        /// incluyendo si es el plan actual y si puede cambiarse.
        /// </summary>
        [HttpGet("owner/{ownerId:int}/plans")]
        [ProducesResponseType(typeof(IReadOnlyCollection<SubscriptionPlanOptionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPlanOptions(int ownerId, CancellationToken ct)
        {
            var ownerValidation = await EnsureOwner(ownerId, ct);
            if (ownerValidation.IsFailure)
                return HandleResult(ownerValidation);

            return HandleResult(await _subscriptionService.GetPlanOptions(ownerId, ct));
        }

        /// <summary>
        /// Crea una suscripción Free si no existe.
        /// Si ya existe, devuelve la actual.
        /// </summary>
        [HttpPost("owner/{ownerId:int}/ensure-free")]
        [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EnsureFree(int ownerId, CancellationToken ct)
        {
            var ownerValidation = await EnsureOwner(ownerId, ct);
            if (ownerValidation.IsFailure)
                return HandleResult(ownerValidation);

            return HandleResult(await _subscriptionService.CreateFreeIfMissing(ownerId, ct));
        }

        /// <summary>
        /// Cancela la suscripción (no renovación).
        /// </summary>
        [HttpPost("owner/{ownerId:int}/cancel")]
        [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Cancel(int ownerId, CancellationToken ct)
        {
            var ownerValidation = await EnsureOwner(ownerId, ct);
            if (ownerValidation.IsFailure)
                return HandleResult(ownerValidation);

            return HandleResult(await _subscriptionService.Cancel(ownerId, ct));
        }

        /// <summary>
        /// Renueva manualmente la suscripción usando un período mensual automático.
        /// </summary>
        [HttpPost("renew")]
        [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Renew([FromBody] RenewSubscriptionDto dto, CancellationToken ct)
        {
            if (dto is null)
                return HandleResult(Result.Failure(Error.Validation("Los datos de la renovacion son requeridos.")));

            var ownerValidation = await EnsureOwner(dto.OwnerId, ct);
            if (ownerValidation.IsFailure)
                return HandleResult(ownerValidation);

            return HandleResult(await _subscriptionService.Renew(dto, ct));
        }

        /// <summary>
        /// Cambia el plan (Upgrade o Downgrade). Los planes pagos generan un nuevo período mensual automático.
        /// </summary>
        [HttpPost("change-plan")]
        [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePlan([FromBody] ChangePlanDto dto, CancellationToken ct)
        {
            if (dto is null)
                return HandleResult(Result.Failure(Error.Validation("Los datos del cambio de plan son requeridos.")));

            var ownerValidation = await EnsureOwner(dto.OwnerId, ct);
            if (ownerValidation.IsFailure)
                return HandleResult(ownerValidation);

            return HandleResult(await _subscriptionService.ChangePlan(dto, ct));
        }

        private async Task<Result> EnsureOwner(int ownerId, CancellationToken ct)
        {
            var userResult = await _userService.GetUserById(ownerId, ct);
            if (userResult.IsFailure)
                return Result.Failure(userResult.Error);

            return string.Equals(userResult.Data?.Role, "Owner", StringComparison.OrdinalIgnoreCase)
                ? Result.Success()
                : Result.Failure(Error.Validation("El id indicado no corresponde a un owner."));
        }
    }
}

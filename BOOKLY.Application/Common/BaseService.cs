using BOOKLY.Application.Common.Models;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain;
using Microsoft.Extensions.Logging;

namespace BOOKLY.Application.Common
{
    public abstract class BaseService<TService>
    {
        protected readonly ILogger<TService> Logger;

        protected BaseService(ILogger<TService> logger)
        {
            this.Logger = logger;
        }

        protected async Task<Result<T>> Execute<T>(Func<Task<T>> action)
        {
            try
            {
                return Result<T>.Success(await action());
            }
            catch (DomainException ex)
            {
                return Result<T>.Failure(Error.Domain(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error inesperado en {Service}", typeof(TService).Name);
                return Result<T>.Failure(Error.Unexpected("Ocurrió un error inesperado."));
            }
        }

        protected async Task<Result> Execute(Func<Task> action)
        {
            try
            {
                await action();
                return Result.Success();
            }
            catch (DomainException ex)
            {
                return Result.Failure(Error.Domain(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error inesperado en {Service}", typeof(TService).Name);
                return Result.Failure(Error.Unexpected("Ocurrió un error inesperado."));
            }
        }
    }
}

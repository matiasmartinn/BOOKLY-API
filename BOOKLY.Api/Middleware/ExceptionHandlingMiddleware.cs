using BOOKLY.Domain;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BOOKLY.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IProblemDetailsService _problemDetailsService;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IProblemDetailsService problemDetailsService)
        {
            _next = next;
            _logger = logger;
            _problemDetailsService = problemDetailsService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Regla de dominio violada en {Path}", context.Request.Path);
                await WriteProblemDetails(context, HttpStatusCode.BadRequest, "Regla de negocio violada", ex.Message);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogInformation("Request cancelado por el cliente en {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en {Method} {Path}", context.Request.Method, context.Request.Path);
                await WriteProblemDetails(context, HttpStatusCode.InternalServerError, "Error inesperado", "Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        private async Task WriteProblemDetails(
            HttpContext context,
            HttpStatusCode statusCode,
            string title,
            string detail)
        {
            context.Response.StatusCode = (int)statusCode;

            await _problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails =
                {
                    Status = (int)statusCode,
                    Title = title,
                    Detail = detail,
                    Instance = context.Request.Path
                }
            });
        }
    }
}
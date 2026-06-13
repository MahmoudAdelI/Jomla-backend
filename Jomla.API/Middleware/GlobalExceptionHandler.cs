using FluentValidation;
using Jomla.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Jomla.API.Middleware
{
    public class GlobalExceptionHandler(IProblemDetailsService _problemDetailsService) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            ProblemDetails problem = exception switch
            {
                ValidationException ex => new ValidationProblemDetails(
                    ex.Errors
                        .GroupBy(err => err.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(err => err.ErrorMessage).ToArray())
                )
                {
                    Title = "Validation Failed",
                    Status = StatusCodes.Status400BadRequest
                },
                NotFoundException ex => new ProblemDetails
                {
                    Title = "Resource not found!",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                },
                _ => new ProblemDetails
                {
                    Title = "Server Error!",
                    // Security Note: Avoid leaking raw exception details in production
                    Detail = exception.Message,
                    Status = StatusCodes.Status500InternalServerError
                }
            };
            httpContext.Response.StatusCode = problem.Status!.Value;

            await _problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem
            });

            return true;
        }
    }
}

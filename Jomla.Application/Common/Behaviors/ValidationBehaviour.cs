using FluentValidation;
using MediatR;

namespace Jomla.Application.Common.Behaviors
{
    public class ValidationBehaviour<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!validators.Any())
                return await next(cancellationToken);

            var context = new ValidationContext<TRequest>(request);
            var result = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            var failures = result
                .SelectMany(r => r.Errors)
                .Where(err => err is not null)
                .ToList();

            if (failures.Count != 0)
                throw new ValidationException(failures);
            return await next(cancellationToken);
        }
    }
}

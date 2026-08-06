using Axent.Abstractions.Models;
using Axent.Abstractions.Pipelines;
using FluentValidation;
using FluentValidation.Results;

namespace Axent.Extensions.FluentValidation;

internal sealed class FluentValidationPipe<TRequest, TResponse> : IAxentPipe<TRequest, TResponse>
{
    private readonly IValidator<TRequest>[] _validators;
    private readonly IFluentValidationErrorFactory _errorFactory;

    public FluentValidationPipe(IEnumerable<IValidator<TRequest>> validators, IFluentValidationErrorFactory errorFactory)
    {
        _validators = [.. validators];
        _errorFactory = errorFactory;
    }

    public async ValueTask<Response<TResponse>> ProcessAsync(
        TRequest request,
        AxentPipelineContinuation<TRequest, TResponse> next,
        CancellationToken cancellationToken = default)
    {
        if (_validators.Length == 0)
        {
            return await next(request, cancellationToken);
        }

        var validationContext = new ValidationContext<TRequest>(request);
        var validationFailures = new List<ValidationFailure>();
        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(validationContext, cancellationToken);
            validationFailures.AddRange(result.Errors);
        }

        if (validationFailures.Count == 0)
        {
            return await next(request, cancellationToken);
        }

        return _errorFactory.Create<TResponse>(validationFailures);
    }
}

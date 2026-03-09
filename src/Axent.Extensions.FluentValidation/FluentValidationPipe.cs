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
        _validators = validators.ToArray();
        _errorFactory = errorFactory;
    }

    public async ValueTask<Response<TResponse>> ProcessAsync(IPipelineChain<TRequest, TResponse> chain, RequestContext<TRequest> context, CancellationToken cancellationToken = default)
    {
        if (_validators.Length == 0)
        {
            return await chain.NextAsync(context, cancellationToken);
        }

        var validationContext = new ValidationContext<TRequest>(context.Request);
        var validationFailures = new List<ValidationFailure>();
        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(validationContext, cancellationToken);
            validationFailures.AddRange(result.Errors);
        }

        if (validationFailures.Count == 0)
        {
            return await chain.NextAsync(context, cancellationToken);
        }

        return _errorFactory.Create<TResponse>(validationFailures);
    }
}

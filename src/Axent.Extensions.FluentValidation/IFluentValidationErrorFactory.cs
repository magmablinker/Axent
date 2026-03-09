using Axent.Abstractions;
using FluentValidation.Results;

namespace Axent.Extensions.FluentValidation;

internal interface IFluentValidationErrorFactory
{
    Response<TResponse> Create<TResponse>(IReadOnlyCollection<ValidationFailure> failures);
}

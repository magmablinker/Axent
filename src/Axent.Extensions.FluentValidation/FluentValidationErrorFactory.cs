using Axent.Abstractions;
using FluentValidation.Results;

namespace Axent.Extensions.FluentValidation;

internal sealed class FluentValidationErrorFactory : IFluentValidationErrorFactory
{
    public Response<TResponse> Create<TResponse>(IReadOnlyCollection<ValidationFailure> failures)
    {
        var error = ErrorDefaults.Generic.ValidationFailure();
        foreach (var group in failures.GroupBy(f => f.PropertyName))
        {
            error.AddValidationErrors(new KeyValuePair<string, IEnumerable<string>>(
                group.Key,
                group.Select(x => x.ErrorMessage).Distinct()));
        }

        error.AddMessages(failures.Select(f => f.ErrorMessage));
        return Response.Failure(error);
    }
}

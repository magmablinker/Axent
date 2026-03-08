using Axent.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Axent.Extensions.AspNetCore;

internal static class ProblemDetailsExtensions
{
    public static void AddError(this ProblemDetails details, Error error)
    {
        details.Extensions.Add("error", error.Messages);
    }
}

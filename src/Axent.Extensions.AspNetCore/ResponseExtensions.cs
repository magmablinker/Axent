using Axent.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Axent.Extensions.AspNetCore;

public static class ResponseExtensions
{
    public static IResult ToResult<T>(this Response<T> response)
    {
        if (response.IsSuccess)
        {
            if (typeof(T) == typeof(Unit) || response.Value is null)
            {
                return Results.NoContent();
            }

            return Results.Ok(response.Value);
        }

        var problemDetails = new ProblemDetails { Status = (int)response.Error.StatusCode };
        problemDetails.AddError(response.Error);

        return Results.Problem(problemDetails);
    }
}

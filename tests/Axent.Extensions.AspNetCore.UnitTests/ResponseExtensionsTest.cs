using System.Net;
using Axent.Abstractions.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Xunit;

namespace Axent.Extensions.AspNetCore.UnitTests;

public sealed class ResponseExtensionsTest
{
    [Fact]
    public void ToResult_should_map_validation_problem_details()
    {
        // Arrange
        var response = Response.Failure<string>(ErrorDefaults.Generic.ValidationFailure());

        // Act
        var result = response.ToResult();

        // Assert
        var problem = Assert.IsType<ProblemHttpResult>(result);
        var status = (HttpStatusCode)problem.StatusCode;

        Assert.Equal(
            HttpStatusCode.BadRequest,
            status
        );
    }
}

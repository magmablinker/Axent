using System.Net;
using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.AspNetCore.Http.HttpResults;
using Axent.Abstractions;
using Axent.Core;
using Xunit;

namespace Axent.Extensions.AspNetCore.UnitTests;

public sealed class ResponseExtensionsTest
{
    [Fact]
    public void ToResult_Should_Map_All_ErrorDefaults()
    {
        // Arrange
        var errorFactoryMethods = typeof(ErrorDefaults.Generic)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.ReturnType == typeof(JSType.Error));

        foreach (var method in errorFactoryMethods)
        {
            var error = (Error)method.Invoke(null, null)!;
            var response = Response.Failure<Unit>(error);

            // Act
            var result = response.ToResult();

            // Assert
            var problem = Assert.IsType<ProblemHttpResult>(result);

            var status = (HttpStatusCode)problem.StatusCode;

            Assert.NotEqual(
                HttpStatusCode.InternalServerError,
                status
            );
        }
    }
}
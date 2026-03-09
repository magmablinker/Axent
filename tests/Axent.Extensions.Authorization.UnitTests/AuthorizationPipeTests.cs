using System.Security.Claims;
using Axent.Abstractions.Builders;
using Axent.Abstractions.Models;
using Axent.Abstractions.Services;
using Axent.Tests.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Axent.Extensions.Authorization.UnitTests;

public sealed class AuthorizationPipeTests : TestBase
{
    private readonly IAuthorizationService _authorizationService = Substitute.For<IAuthorizationService>();
    private readonly IAuthorizationPolicyProvider _policyProvider = Substitute.For<IAuthorizationPolicyProvider>();
    private readonly IPrincipalAccessor _principalAccessor = Substitute.For<IPrincipalAccessor>();

    protected override void ConfigureAxent(IAxentBuilder builder)
    {
        builder.AddAuthorization();

        builder.Services.AddScoped<IAuthorizationService>(_ => _authorizationService);
        builder.Services.AddScoped<IAuthorizationPolicyProvider>(_ => _policyProvider);
        builder.Services.AddScoped<IPrincipalAccessor>(_ => _principalAccessor);
    }

    [Fact]
    public async Task ProcessAsync_returns_unauthorized_when_user_is_not_authenticated()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        _principalAccessor.Principal.Returns(principal);

        _policyProvider.GetPolicyAsync(Arg.Any<string>())
            .Returns((AuthorizationPolicy?)null);
        _policyProvider.GetDefaultPolicyAsync()
            .Returns(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        _authorizationService.AuthorizeAsync(
                principal,
                Arg.Any<object>(),
                Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(AuthorizationResult.Failed());

        var expected = Response.Failure<Unit>(ErrorDefaults.Generic.Unauthorized());

        var query = new ProtectedQuery();

        await using var scope = ServiceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var result = await sender.SendAsync(query);

        // Assert
        Assert.Equivalent(expected, result);
    }

    [Fact]
    public async Task ProcessAsync_returns_success_when_user_is_authorized()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        _principalAccessor.Principal.Returns(principal);

        _policyProvider.GetPolicyAsync(Arg.Any<string>())
            .Returns((AuthorizationPolicy?)null);
        _policyProvider.GetDefaultPolicyAsync()
            .Returns(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        _authorizationService.AuthorizeAsync(
                principal,
                Arg.Any<object>(),
                Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(AuthorizationResult.Success());

        var expected = Response.Success(Unit.Value);

        var query = new ProtectedQuery();

        await using var scope = ServiceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var result = await sender.SendAsync(query);

        // Assert
        Assert.Equivalent(expected, result);
    }
}

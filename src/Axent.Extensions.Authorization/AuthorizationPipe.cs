using Axent.Abstractions.Models;
using Axent.Abstractions.Pipelines;
using Microsoft.AspNetCore.Authorization;

namespace Axent.Extensions.Authorization;

internal sealed class AuthorizationPipe<TRequest, TResponse>
    : IAxentPipe<TRequest, TResponse>
{
    private static readonly RequestAuthorizationMetadata _metadata = AuthorizationMetadataCache<TRequest>.Metadata;

    private readonly IAuthorizationService _authorizationService;
    private readonly IAuthorizationPolicyProvider _policyProvider;
    private readonly IPrincipalAccessor _principalAccessor;

    public AuthorizationPipe(
        IAuthorizationService authorizationService,
        IAuthorizationPolicyProvider policyProvider,
        IPrincipalAccessor principalAccessor)
    {
        _authorizationService = authorizationService;
        _policyProvider = policyProvider;
        _principalAccessor = principalAccessor;
    }

    public async ValueTask<Response<TResponse>> ProcessAsync(
        IPipelineChain<TRequest, TResponse> chain,
        RequestContext<TRequest> context,
        CancellationToken cancellationToken = default)
    {
        if (_metadata.AllowAnonymous || _metadata.AuthorizeData.Length == 0)
        {
            return await chain.NextAsync(context, cancellationToken);
        }

        var policy = await AuthorizationPolicy.CombineAsync(_policyProvider, _metadata.AuthorizeData);

        if (policy is null)
        {
            return await chain.NextAsync(context, cancellationToken);
        }

        var user = _principalAccessor.Principal;
        var authorizationResult = await _authorizationService.AuthorizeAsync(
            user,
            context.Request,
            policy);

        if (authorizationResult.Succeeded)
        {
            return await chain.NextAsync(context, cancellationToken);
        }

        return Response.Failure(user.Identity?.IsAuthenticated == true
            ? ErrorDefaults.Generic.Forbidden()
            : ErrorDefaults.Generic.Unauthorized());
    }
}

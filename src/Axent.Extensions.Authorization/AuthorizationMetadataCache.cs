using Microsoft.AspNetCore.Authorization;

namespace Axent.Extensions.Authorization;

internal sealed record RequestAuthorizationMetadata(
    bool AllowAnonymous,
    IAuthorizeData[] AuthorizeData);

internal static class AuthorizationMetadataCache<TRequest>
{
    public static readonly RequestAuthorizationMetadata Metadata = Create();

    private static RequestAuthorizationMetadata Create()
    {
        var attributes = typeof(TRequest).GetCustomAttributes(inherit: true);

        return new RequestAuthorizationMetadata(
            AllowAnonymous: attributes.OfType<IAllowAnonymous>().Any(),
            AuthorizeData: attributes.OfType<IAuthorizeData>().ToArray());
    }
}

using Axent.Abstractions.Builders;
using Axent.Abstractions.Pipelines;
using Microsoft.Extensions.DependencyInjection;

namespace Axent.Extensions.Authorization;

public static class AxentBuilderExtensions
{
    public static IAxentBuilder AddAuthorization(this IAxentBuilder builder)
    {
        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddScoped<IPrincipalAccessor, HttpContextPrincipalAccessor>();

        // Registered as a dedicated pipeline stage rather than through AddPipe, so that the
        // generated sender always runs authorization ahead of caching and transactions.
        // A cache hit must never be able to skip the authorization gate.
        builder.Services.AddScoped(typeof(IAuthorizationPipe<,>), typeof(AuthorizationPipe<,>));

        return builder;
    }
}

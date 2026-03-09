using Axent.Abstractions.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Axent.Extensions.Authorization;

public static class AxentBuilderExtensions
{
    public static IAxentBuilder AddAuthorization(this IAxentBuilder builder)
    {
        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddScoped<IPrincipalAccessor, HttpContextPrincipalAccessor>();

        builder.AddPipe(typeof(AuthorizationPipe<,>));

        return builder;
    }
}

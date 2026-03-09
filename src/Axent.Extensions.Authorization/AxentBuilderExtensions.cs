using Axent.Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Axent.Extensions.Authorization;

public static class AxentBuilderExtensions
{
    public static AxentBuilder AddAuthorization(this AxentBuilder builder)
    {
        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddScoped<IPrincipalAccessor, HttpContextPrincipalAccessor>();

        builder.AddPipe(typeof(AuthorizationPipe<,>));

        return builder;
    }
}

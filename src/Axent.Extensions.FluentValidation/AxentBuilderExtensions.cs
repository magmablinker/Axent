using Axent.Abstractions.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Axent.Extensions.FluentValidation;

public static class AxentBuilderExtensions
{
    public static IAxentBuilder AddAutoFluentValidation(this IAxentBuilder builder)
    {
        builder.AddPipe(typeof(FluentValidationPipe<,>));
        builder.Services.AddSingleton<IFluentValidationErrorFactory, FluentValidationErrorFactory>();
        return builder;
    }
}

using Axent.Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Axent.Extensions.FluentValidation;

public static class AxentBuilderExtensions
{
    public static AxentBuilder AddAutoFluentValidation(this AxentBuilder builder)
    {
        builder.AddPipe(typeof(FluentValidationPipe<,>));
        builder.Services.AddSingleton<IFluentValidationErrorFactory, FluentValidationErrorFactory>();
        return builder;
    }
}

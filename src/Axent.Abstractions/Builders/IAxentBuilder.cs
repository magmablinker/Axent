using Axent.Abstractions.Pipelines;
using Microsoft.Extensions.DependencyInjection;

namespace Axent.Abstractions.Builders;

public interface IAxentBuilder
{
    IServiceCollection Services { get; }

    IAxentBuilder AddPipe<TPipe>() where TPipe : IAxentPipe => AddPipe(typeof(TPipe));
    IAxentBuilder AddPipe(Type pipeType);
}

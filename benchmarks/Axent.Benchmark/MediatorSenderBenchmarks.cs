using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Axent.Benchmark;

[MemoryDiagnoser]
[SimpleJob]
public class MediatorSenderBenchmarks
{
    private IMediator _mediator = null!;
    private MediatorPingRequest _request = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<PingHandler>());

        var provider = services.BuildServiceProvider();
        _mediator = provider.GetRequiredService<IMediator>();
        _request = new MediatorPingRequest("hello");
    }

    [Benchmark(Description = "Send (cold)")]
    public async Task<MediatorPingResponse> Send_Cold()
    {
        return await _mediator.Send(new MediatorPingRequest("hello"));
    }

    [Benchmark(Description = "Send (warm, same instance)")]
    public async Task<MediatorPingResponse> Send_Warm()
    {
        return await _mediator.Send(_request);
    }
}

public sealed record MediatorPingRequest(string Message) : IRequest<MediatorPingResponse>;

public sealed record MediatorPingResponse(string Message);

public sealed class MediatorPingHandler : IRequestHandler<MediatorPingRequest, MediatorPingResponse>
{
    public Task<MediatorPingResponse> Handle(MediatorPingRequest request, CancellationToken cancellationToken) => Task.FromResult(new MediatorPingResponse(request.Message));
}

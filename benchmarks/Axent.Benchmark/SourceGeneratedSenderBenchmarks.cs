using Axent.Abstractions.Models;
using Axent.Abstractions.Services;
using Axent.Core.DependencyInjection;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Axent.Benchmark;

[MemoryDiagnoser]
[SimpleJob]
public class SourceGeneratedSenderBenchmarks
{
    private IRequestHandler<PingRequest, PingResponse> _handler = null!;
    private PingRequest _request = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAxent()
            .AddHandler<PingHandler>();

        var provider = services.BuildServiceProvider();
        _handler = provider.GetRequiredService<IRequestHandler<PingRequest, PingResponse>>();
        _request = new("hello");
    }

    [Benchmark(Baseline = true, Description = "SendAsync (cold)")]
    public async Task<Response<PingResponse>> SendAsync_Cold()
    {
        return await _handler.HandleAsync(new PingRequest("hello"));
    }

    [Benchmark(Description = "SendAsync (warm, same instance)")]
    public async Task<Response<PingResponse>> SendAsync_Warm()
    {
        return await _handler.HandleAsync(_request);
    }
}

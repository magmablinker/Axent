using Axent.Abstractions;
using Axent.Core;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Axent.Benchmark;

[MemoryDiagnoser]
[SimpleJob]
public class ReflectionSenderBenchmarks
{
    private ISender _sender = null!;
    private PingRequest _request = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAxent(o => o.UseSourceGeneratedSender = false)
            .AddHandler<PingHandler>();

        var provider = services.BuildServiceProvider();
        _sender = provider.GetRequiredService<ISender>();
        _request = new ("hello");
    }

    [Benchmark(Baseline = true, Description = "SendAsync (cold)")]
    public async Task<Response<PingResponse>> SendAsync_Cold()
    {
        return await _sender.SendAsync(new PingRequest("hello"));
    }

    [Benchmark(Description = "SendAsync (warm, same instance)")]
    public async Task<Response<PingResponse>> SendAsync_Warm()
    {
        return await _sender.SendAsync(_request);
    }
}

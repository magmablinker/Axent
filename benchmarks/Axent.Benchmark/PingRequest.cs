using Axent.Abstractions;
using Axent.Core;

namespace Axent.Benchmark;

public sealed record PingRequest(string Message) : IRequest<PingResponse>;
public sealed record PingResponse(string Reply);

internal sealed class PingHandler : IRequestHandler<PingRequest, PingResponse>
{
    public ValueTask<Response<PingResponse>> HandleAsync(RequestContext<PingRequest> context, CancellationToken cancellationToken = default)
    {
        var reply = new PingResponse($"Pong: {context.Request.Message}");
        return ValueTask.FromResult(Response.Success(reply));
    }
}

using Axent.Abstractions;
using Axent.Core;

namespace Axent.Benchmark;

public record PingRequest(string Message) : IRequest<PingResponse>;
public record PingResponse(string Reply);

public class PingHandler : RequestHandler<PingRequest, PingResponse>
{
    public override Task<Response<PingResponse>> HandleAsync(RequestContext<PingRequest> context, CancellationToken cancellationToken = default)
    {
        var reply = new PingResponse($"Pong: {context.Request.Message}");
        return Task.FromResult(Response.Success(reply));
    }
}

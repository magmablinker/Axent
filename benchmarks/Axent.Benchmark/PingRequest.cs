using Axent.Abstractions.Attributes;
using Axent.Abstractions.Models;
using Axent.Abstractions.Requests;
using Axent.Abstractions.Services;

namespace Axent.Benchmark;

[Axent]
public sealed record PingRequest(string Message) : IRequest<PingResponse>;
public sealed record PingResponse(string Reply);

internal sealed class PingHandler : IRequestHandler<PingRequest, PingResponse>
{
    public ValueTask<Response<PingResponse>> HandleAsync(PingRequest request, CancellationToken cancellationToken = default)
    {
        var reply = new PingResponse($"Pong: {request.Message}");
        return ValueTask.FromResult(Response.Success(reply));
    }
}

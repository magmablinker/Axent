using Axent.Abstractions.Models;
using Axent.Abstractions.Requests;
using Axent.Abstractions.Services;

namespace Axent.ExampleApi;

internal sealed record WelcomeRequest : IRequest<string>;

internal sealed class WelcomeRequestHandler : IRequestHandler<WelcomeRequest, string>
{
    public ValueTask<Response<string>> HandleAsync(RequestContext<WelcomeRequest> context,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Response.Success("Axent Example Api is up and running!"));
}

using Axent.Abstractions;

namespace Axent.Tests.Shared;

public sealed record TestCommand(string Message) : ICommand<Unit>;

internal sealed class TestCommandHandler : IRequestHandler<TestCommand, Unit>
{
    public ValueTask<Response<Unit>> HandleAsync(RequestContext<TestCommand> context, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(Response.Success(Unit.Value));
    }
}

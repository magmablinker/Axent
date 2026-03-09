using System.Diagnostics;

namespace Axent.Core.Pipes.Observability;

internal interface IActivityFactory
{
    Activity? Create<TRequest>();
}

namespace Axent.Abstractions;

#pragma warning disable S2326
// ReSharper disable once UnusedTypeParameter
public interface IHandlerPipe<TRequest, TResponse> : IAxentPipe<TRequest, TResponse> where TRequest : notnull
#pragma warning restore S2326
{
}
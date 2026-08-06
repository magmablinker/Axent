namespace Axent.Abstractions.Pipelines;

/// <summary>
/// Pipe that authorizes a request before any other stage of the pipeline runs.
/// </summary>
/// <typeparam name="TRequest">Type of the request</typeparam>
/// <typeparam name="TResponse">Type of the response</typeparam>
public interface IAuthorizationPipe<TRequest, TResponse> : IAxentPipe<TRequest, TResponse>;

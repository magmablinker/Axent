namespace Axent.Abstractions.Requests;

/// <summary>
/// Marker interface for queries
/// </summary>
/// <typeparam name="TResponse">Type of the response</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>;

namespace Axent.Abstractions;

/// <summary>
/// Marker interface for commands
/// </summary>
/// <typeparam name="TResponse">Type of the response</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>;

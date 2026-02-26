using System.Diagnostics.CodeAnalysis;

namespace Axent.Abstractions;

public interface IResponse
{
    Error? Error { get; }
    bool IsSuccess { get; }
    bool IsFailure { get; }
}

public class ResponseBase : IResponse
{
    public Error? Error { get; init; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => !IsSuccess;
}

public sealed class Response<TResponse> : IResponse
{
    public Error? Error { get; init; }

    [MemberNotNullWhen(false, nameof(Error))]
    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSuccess => Error is null;

    [MemberNotNullWhen(true, nameof(Error))]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsFailure => !IsSuccess;

    public TResponse? Value { get; init; }

    public static implicit operator Response<TResponse>(ResponseBase response) =>
        new() { Error = response.Error };
}

public static class Response
{
    public static ResponseBase Success() => new();
    public static ResponseBase Failure(Error error) => new() { Error = error };

    public static Response<TResponse> Success<TResponse>(TResponse value) => new() { Value = value };
    public static Response<TResponse> Failure<TResponse>(Error error) => new() { Error = error };
}
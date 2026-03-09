using System.Net;

namespace Axent.Abstractions.Models;

public sealed class Error : IEquatable<Error>
{
    public string Identifier { get; }
    public HttpStatusCode StatusCode { get; }
    public List<string> Messages { get; }

    public Dictionary<string, List<string>> ValidationErrors { get; }

    private readonly int _hashCode;

    public Error(string identifier, HttpStatusCode statusCode, params string[] messages)
    {
        Identifier = identifier;
        StatusCode = statusCode;
        Messages = messages.ToList();
        ValidationErrors = new Dictionary<string, List<string>>();
        _hashCode = HashCode.Combine(identifier);
    }

    public override bool Equals(object? obj) => Equals(obj as Error);

    public bool Equals(Error? other) =>
        ReferenceEquals(this, other) || (other != null && Identifier == other.Identifier);

    public override int GetHashCode() => _hashCode;

    public Error AddMessage(string message)
    {
        Messages.Add(message);
        return this;
    }

    public Error AddMessages(IEnumerable<string> messages)
    {
        foreach (var message in messages)
        {
            AddMessage(message);
        }

        return this;
    }

    public Error AddValidationErrors(KeyValuePair<string, IEnumerable<string>> kvp)
    {
        if (!ValidationErrors.TryGetValue(kvp.Key, out var errors))
        {
            errors = [];
            ValidationErrors[kvp.Key] = errors;
        }
        errors.AddRange(kvp.Value);

        return this;
    }

    public Dictionary<string, string[]> ToDictionary() =>
        new() { { Identifier, Messages.ToArray() } };

    public override string ToString() => $"{Identifier} : {string.Join(",", Messages)}";
}

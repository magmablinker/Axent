using System.Collections.Immutable;

namespace Axent.Generators;

internal sealed class UniqueRegistrations : IEquatable<UniqueRegistrations>
{
    public ImmutableArray<RequestRegistrationInfo> Items { get; }

    public UniqueRegistrations(ImmutableArray<RequestRegistrationInfo> items)
    {
        Items = [
            ..items
                .GroupBy(static r => r.RequestFullName, StringComparer.Ordinal)
                .Select(static g => g.First())
                .OrderBy(static r => r.RequestFullName, StringComparer.Ordinal)
        ];
    }

    public bool Equals(UniqueRegistrations? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Items.SequenceEqual(other.Items);
    }

    public override bool Equals(object? obj) => obj is UniqueRegistrations other && Equals(other);
    public override int GetHashCode() => Items.Length; 
}

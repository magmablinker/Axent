using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Axent.Generators;

/// <summary>
/// A value-equatable stand-in for <see cref="Location"/>.
/// </summary>
/// <remarks>
/// A raw <see cref="Location"/> must never be stored in an incremental generator model, because it
/// is not value-equatable and would defeat step caching. This record is.
/// </remarks>
internal sealed record LocationInfo(
    string FilePath,
    TextSpan TextSpan,
    LinePositionSpan LineSpan)
{
    public Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);

    public static LocationInfo? From(ISymbol symbol)
    {
        foreach (var location in symbol.Locations)
        {
            if (location.SourceTree is null)
            {
                continue;
            }

            return new LocationInfo(
                location.SourceTree.FilePath,
                location.SourceSpan,
                location.GetLineSpan().Span);
        }

        return null;
    }
}

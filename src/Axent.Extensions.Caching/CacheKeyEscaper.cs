using System.Text;

namespace Axent.Extensions.Caching;

/// <summary>
/// Escapes the delimiters used by <see cref="CacheKeyBuilder"/> so that a discriminator can
/// never forge another partition's key.
/// </summary>
/// <remarks>
/// Discriminators can be attacker influenced, for example a tenant claim on an inbound token.
/// Escaping keeps every discriminator delimiter free, and because the segment count is fixed by
/// the requested <see cref="Axent.Abstractions.Caching.CacheScope"/>, distinct discriminator
/// tuples always produce distinct keys.
/// </remarks>
internal static class CacheKeyEscaper
{
    private const string ReservedCharacters = "%:|";

    public static string Escape(string value)
    {
        var index = value.AsSpan().IndexOfAny(ReservedCharacters);

        if (index < 0)
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 8);
        builder.Append(value, 0, index);

        for (var position = index; position < value.Length; position++)
        {
            var character = value[position];

            switch (character)
            {
                case '%':
                    builder.Append("%25");
                    break;
                case ':':
                    builder.Append("%3A");
                    break;
                case '|':
                    builder.Append("%7C");
                    break;
                default:
                    builder.Append(character);
                    break;
            }
        }

        return builder.ToString();
    }
}

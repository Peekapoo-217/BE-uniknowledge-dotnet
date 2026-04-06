using System.Text;

namespace UniKnowledge.Helpers;

/// <summary>
/// Utility for encoding/decoding cursor-based pagination cursors.
/// Cursor format: Base64("ISO8601_DateTime|IntegerId")
/// Uses composite key (DateTime + Id) to handle items with identical timestamps.
/// </summary>
public static class CursorHelper
{
    private const char Separator = '|';

    /// <summary>
    /// Encode a (DateTime, int) pair into an opaque Base64 cursor string.
    /// </summary>
    public static string Encode(DateTime dateTime, int id)
    {
        var raw = $"{dateTime:O}{Separator}{id}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    /// <summary>
    /// Decode an opaque cursor string back into (DateTime, int).
    /// Returns null if the cursor is invalid.
    /// </summary>
    public static (DateTime DateTime, int Id)? Decode(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor))
            return null;

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split(Separator);

            if (parts.Length != 2)
                return null;

            if (!DateTime.TryParse(parts[0], null, System.Globalization.DateTimeStyles.RoundtripKind, out var dateTime))
                return null;

            if (!int.TryParse(parts[1], out var id))
                return null;

            return (dateTime, id);
        }
        catch
        {
            return null;
        }
    }
}

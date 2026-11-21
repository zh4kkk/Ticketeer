namespace Ticketeer.Services;

internal static class CatalogSearch
{
    public static string? Pattern(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var value = search.Trim();
        if (value.Length > 200)
        {
            throw new ValidationException("Search text cannot exceed 200 characters.");
        }

        var escaped = value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
        return $"%{escaped}%";
    }
}

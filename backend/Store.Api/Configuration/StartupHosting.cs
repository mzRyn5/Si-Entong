namespace Store.Api.Configuration;

public static class StartupHosting
{
    public static string? NormalizeContainerUrls(string? urls, string? port)
    {
        if (TryParsePort(port, out var platformPort))
        {
            return BuildAnyIpUrl(platformPort);
        }

        if (string.IsNullOrWhiteSpace(urls))
        {
            return null;
        }

        var normalizedUrls = new List<string>();
        var changed = false;

        foreach (var rawUrl in urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var normalizedUrl = NormalizeNumericHostUrl(rawUrl);
            normalizedUrls.Add(normalizedUrl ?? rawUrl);
            changed |= normalizedUrl is not null;
        }

        return changed ? string.Join(';', normalizedUrls) : null;
    }

    private static string? NormalizeNumericHostUrl(string rawUrl)
    {
        var schemeSeparatorIndex = rawUrl.IndexOf("://", StringComparison.Ordinal);
        if (schemeSeparatorIndex > 0)
        {
            var scheme = rawUrl[..schemeSeparatorIndex];
            var address = rawUrl[(schemeSeparatorIndex + 3)..];
            var addressEndIndex = address.IndexOfAny(['/', '?', '#']);
            var host = addressEndIndex >= 0 ? address[..addressEndIndex] : address;

            if (TryParsePort(host, out var numericHostPort))
            {
                return BuildAnyIpUrl(numericHostPort, scheme);
            }
        }

        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        return TryParsePort(uri.Host, out var port)
            ? BuildAnyIpUrl(port, uri.Scheme)
            : null;
    }

    private static bool TryParsePort(string? value, out int port)
    {
        return int.TryParse(value, out port) && port is > 0 and <= 65535;
    }

    private static string BuildAnyIpUrl(int port, string scheme = "http")
    {
        return $"{scheme}://0.0.0.0:{port}";
    }
}

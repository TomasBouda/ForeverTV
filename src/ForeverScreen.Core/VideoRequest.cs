using System.Globalization;
using System.Text.RegularExpressions;

namespace ForeverScreen.Core;

/// <summary>A YouTube video to play: its id, the second to start at, and the playlist it came from, if any.</summary>
public sealed record VideoRequest(string VideoId, int StartSeconds = 0, string? PlaylistId = null)
{
    private static readonly Regex IdPattern = new("^[A-Za-z0-9_-]{11}$", RegexOptions.CultureInvariant);
    private static readonly Regex ListPattern = new("^[A-Za-z0-9_-]{2,64}$", RegexOptions.CultureInvariant);
    private static readonly Regex DurationPattern = new(
        @"^(?:(?<h>\d+)h)?(?:(?<m>\d+)m)?(?:(?<s>\d+)s?)?$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static bool IsVideoId(string? value) => value is not null && IdPattern.IsMatch(value);

    public static bool IsPlaylistId(string? value) => value is not null && ListPattern.IsMatch(value);

    /// <summary>
    /// Reads a bare video id or a YouTube link: watch?v=, youtu.be/, shorts/, embed/, live/, with t= or start= in
    /// seconds or as 1h2m3s, and list= for a playlist. Anything else is refused.
    /// </summary>
    public static bool TryParse(string? text, out VideoRequest request)
    {
        request = null!;
        text = text?.Trim();
        if (string.IsNullOrEmpty(text)) return false;
        if (IsVideoId(text))
        {
            request = new VideoRequest(text);
            return true;
        }
        if (!text.Contains("://", StringComparison.Ordinal)) text = "https://" + text;
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) return false;

        var host = uri.Host.ToLowerInvariant();
        var query = ParseQuery(uri.Query);
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string? id = null;
        if (host == "youtu.be")
        {
            id = segments.FirstOrDefault();
        }
        else if (host == "youtube.com" || host.EndsWith(".youtube.com", StringComparison.Ordinal)
            || host == "youtube-nocookie.com" || host.EndsWith(".youtube-nocookie.com", StringComparison.Ordinal))
        {
            if (segments.Length == 1 && segments[0] == "watch") query.TryGetValue("v", out id);
            else if (segments.Length >= 2 && segments[0] is "shorts" or "embed" or "live" or "v") id = segments[1];
        }
        if (!IsVideoId(id)) return false;

        var start = 0;
        if (query.TryGetValue("t", out var t) || query.TryGetValue("start", out t)) start = ParseSeconds(t);
        query.TryGetValue("list", out var list);
        request = new VideoRequest(id!, start, IsPlaylistId(list) ? list : null);
        return true;
    }

    /// <summary>Seconds from "90", "90s", "1m30s" or "1h2m3s"; anything else is 0.</summary>
    public static int ParseSeconds(string? value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        var match = DurationPattern.Match(value);
        if (!match.Success) return 0;
        long Part(string name) => match.Groups[name].Success ? long.Parse(match.Groups[name].Value, CultureInfo.InvariantCulture) : 0;
        try
        {
            var total = checked(Part("h") * 3600 + Part("m") * 60 + Part("s"));
            return total > int.MaxValue ? 0 : (int)total;
        }
        catch (OverflowException)
        {
            return 0;
        }
    }

    /// <summary>The embed address the player page loads.</summary>
    public string EmbedUrl(string origin, bool muted)
    {
        var url = $"https://www.youtube.com/embed/{VideoId}?autoplay=1&enablejsapi=1&origin={Uri.EscapeDataString(origin)}";
        if (StartSeconds > 0) url += $"&start={StartSeconds.ToString(CultureInfo.InvariantCulture)}";
        if (PlaylistId is not null) url += $"&list={PlaylistId}";
        if (muted) url += "&mute=1";
        return url;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var equals = pair.IndexOf('=');
            var key = Uri.UnescapeDataString(equals < 0 ? pair : pair[..equals]);
            var value = equals < 0 ? "" : Uri.UnescapeDataString(pair[(equals + 1)..].Replace('+', ' '));
            result.TryAdd(key, value);
        }
        return result;
    }
}

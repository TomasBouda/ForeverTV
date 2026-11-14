using System.Security.Cryptography;
using System.Text;

namespace ForeverTV.Core;

/// <summary>
/// Decides whether a call to the local API may act. The API listens on 127.0.0.1 only, but any web page in the
/// browser can still send requests there, so every call needs the pairing token, and a call from a web page origin
/// is refused even with it. The browser extension calls from a chrome-extension:// origin; curl sends none.
/// </summary>
public static class ApiGuard
{
    public const string TokenHeader = "X-ForeverTV-Token";

    public static string NewToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
        .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    public static bool IsAllowed(string? origin, string? token, string expectedToken)
    {
        if (string.IsNullOrEmpty(expectedToken) || string.IsNullOrEmpty(token)) return false;
        if (!IsAllowedOrigin(origin)) return false;
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(token), Encoding.UTF8.GetBytes(expectedToken));
    }

    public static bool IsAllowedOrigin(string? origin) =>
        string.IsNullOrEmpty(origin)
        || origin.StartsWith("chrome-extension://", StringComparison.Ordinal)
        || origin.StartsWith("extension://", StringComparison.Ordinal);
}

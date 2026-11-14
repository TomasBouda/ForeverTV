using ForeverTV.Core;

namespace ForeverTV.Core.Tests;

public class VideoRequestTests
{
    [Theory]
    [InlineData("dQw4w9WgXcQ", "dQw4w9WgXcQ", 0, null)]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ", 0, null)]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=90s", "dQw4w9WgXcQ", 90, null)]
    [InlineData("https://youtube.com/watch?feature=share&v=dQw4w9WgXcQ&list=PLabc123_-x", "dQw4w9WgXcQ", 0, "PLabc123_-x")]
    [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ&t=1h2m3s", "dQw4w9WgXcQ", 3723, null)]
    [InlineData("https://youtu.be/dQw4w9WgXcQ?t=42", "dQw4w9WgXcQ", 42, null)]
    [InlineData("youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ", 0, null)]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ", "dQw4w9WgXcQ", 0, null)]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ?start=15", "dQw4w9WgXcQ", 15, null)]
    [InlineData("https://www.youtube.com/live/dQw4w9WgXcQ", "dQw4w9WgXcQ", 0, null)]
    [InlineData("  https://music.youtube.com/watch?v=dQw4w9WgXcQ&t=2m  ", "dQw4w9WgXcQ", 120, null)]
    public void Reads_links(string text, string id, int start, string? list)
    {
        Assert.True(VideoRequest.TryParse(text, out var request));
        Assert.Equal(new VideoRequest(id, start, list), request);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("https://example.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com.evil.example/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=short")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ\"><script>")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://www.youtube.com/channel/UCabcdefghijk")]
    public void Refuses_everything_else(string? text)
    {
        Assert.False(VideoRequest.TryParse(text, out _));
    }

    [Fact]
    public void Drops_a_bad_playlist_and_a_bad_time()
    {
        Assert.True(VideoRequest.TryParse("https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=a\"b&t=soon", out var request));
        Assert.Equal(new VideoRequest("dQw4w9WgXcQ"), request);
    }

    [Fact]
    public void Builds_the_embed_address()
    {
        var url = new VideoRequest("dQw4w9WgXcQ", 90, "PLabc").EmbedUrl("https://forevertv.local", muted: true);
        Assert.Equal("https://www.youtube.com/embed/dQw4w9WgXcQ?autoplay=1&enablejsapi=1&origin=https%3A%2F%2Fforevertv.local&start=90&list=PLabc&mute=1", url);
    }
}

public class ApiGuardTests
{
    private const string Token = "secret-token";

    [Theory]
    [InlineData(null)]
    [InlineData("chrome-extension://abcdefghijklmnop")]
    [InlineData("extension://abcdefghijklmnop")]
    public void Lets_the_extension_and_local_tools_in(string? origin)
    {
        Assert.True(ApiGuard.IsAllowed(origin, Token, Token));
    }

    [Theory]
    [InlineData("https://evil.example")]
    [InlineData("http://127.0.0.1:8080")]
    [InlineData("null")]
    public void Refuses_web_pages_even_with_the_token(string origin)
    {
        Assert.False(ApiGuard.IsAllowed(origin, Token, Token));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("secret-tokem")]
    public void Refuses_a_missing_or_wrong_token(string? token)
    {
        Assert.False(ApiGuard.IsAllowed(null, token, Token));
    }

    [Fact]
    public void New_tokens_differ_and_are_header_safe()
    {
        var a = ApiGuard.NewToken();
        Assert.NotEqual(a, ApiGuard.NewToken());
        Assert.Matches("^[A-Za-z0-9_-]{32}$", a);
    }
}

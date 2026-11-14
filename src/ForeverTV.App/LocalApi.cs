using System.Text.Json.Serialization;
using ForeverTV.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ForeverTV.App;

/// <summary>What the local API can do with the screen. Called from web server threads.</summary>
internal interface IScreenControl
{
    void Play(VideoRequest request, string source);
    void SetPaused(bool paused, string source);
    ScreenStatus GetStatus();
}

internal sealed record ScreenStatus(string? VideoId, string? Title, bool Playing, bool TvVisible, bool Muted);

internal sealed record PlayBody(string? Url, string? VideoId, int? Start, string? PlaylistId);

/// <summary>
/// HTTP on 127.0.0.1 for the browser extension and for curl. /health is open; everything else needs the pairing
/// token and must not come from a web page origin (see <see cref="ApiGuard"/>). No CORS headers on purpose: a web
/// page cannot send the token header without them.
/// </summary>
internal sealed class LocalApi : IAsyncDisposable
{
    private readonly WebApplication _app;

    private LocalApi(WebApplication app) => _app = app;

    public static async Task<LocalApi> StartAsync(AppSettings settings, IScreenControl screen, string version, Action<string> report)
    {
        // Always Production: no developer error pages with stack traces, whatever ASPNETCORE_ENVIRONMENT says.
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(System.Net.IPAddress.Loopback, settings.Port));
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
        var app = builder.Build();

        app.MapGet("/health", () => Results.Json(new { status = "Healthy", version, name = "ForeverTV" }));

        var api = app.MapGroup("/").AddEndpointFilter(async (context, next) =>
        {
            var request = context.HttpContext.Request;
            if (!ApiGuard.IsAllowed(request.Headers.Origin, request.Headers[ApiGuard.TokenHeader], settings.Token))
            {
                report($"API refused {request.Method} {request.Path} from origin {(string.IsNullOrEmpty(request.Headers.Origin) ? "none" : request.Headers.Origin.ToString())}");
                return Results.Json(new { error = "Pair the browser first: tray menu, Pair browser." }, statusCode: StatusCodes.Status403Forbidden);
            }
            return await next(context);
        });

        api.MapPost("/play", (PlayBody body, HttpContext context) =>
        {
            VideoRequest? request = null;
            if (VideoRequest.TryParse(body.Url ?? body.VideoId, out var parsed)) request = parsed;
            if (request is null) return Results.Json(new { error = "Not a YouTube video link." }, statusCode: StatusCodes.Status400BadRequest);
            if (body.Start is { } start and >= 0) request = request with { StartSeconds = start };
            if (VideoRequest.IsPlaylistId(body.PlaylistId)) request = request with { PlaylistId = body.PlaylistId };
            screen.Play(request, context.Request.Headers.Origin.Count > 0 ? "browser" : "API");
            return Results.Json(screen.GetStatus());
        });
        api.MapPost("/pause", () => { screen.SetPaused(true, "API"); return Results.Json(screen.GetStatus()); });
        api.MapPost("/resume", () => { screen.SetPaused(false, "API"); return Results.Json(screen.GetStatus()); });
        api.MapGet("/status", () => Results.Json(screen.GetStatus()));

        await app.StartAsync();
        report($"Local API on http://127.0.0.1:{settings.Port}");
        return new LocalApi(app);
    }

    public async ValueTask DisposeAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await _app.StopAsync(timeout.Token);
        await _app.DisposeAsync();
    }
}

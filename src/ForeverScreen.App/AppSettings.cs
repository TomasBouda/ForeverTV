using System.Text.Json;
using ForeverScreen.Core;

namespace ForeverScreen.App;

/// <summary>What the tray app remembers, in settings.json next to the logs.</summary>
internal sealed class AppSettings
{
    public const int DefaultPort = 5241;

    public int Port { get; set; } = DefaultPort;
    public string Token { get; set; } = "";
    public bool Muted { get; set; }
    public List<RecentVideo> Recent { get; set; } = [];

    /// <summary>%LOCALAPPDATA%\ForeverScreen, or FOREVERSCREEN_HOME for a test run that must not touch the real one.</summary>
    public static string Folder { get; } = Environment.GetEnvironmentVariable("FOREVERSCREEN_HOME") is { Length: > 0 } home
        ? home
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ForeverScreen");

    private static string FilePath => Path.Combine(Folder, "settings.json");
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private readonly Lock _gate = new();

    public static AppSettings Load()
    {
        AppSettings settings;
        try
        {
            settings = File.Exists(FilePath)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings()
                : new AppSettings();
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            settings = new AppSettings();
        }
        var changed = false;
        if (string.IsNullOrEmpty(settings.Token))
        {
            settings.Token = ApiGuard.NewToken();
            changed = true;
        }
        if (settings.Port is < 1024 or > 65535)
        {
            settings.Port = DefaultPort;
            changed = true;
        }
        settings.Recent = settings.Recent.Where(video => VideoRequest.IsVideoId(video.VideoId)).Take(10).ToList();
        if (changed) settings.Save();
        return settings;
    }

    public void Save()
    {
        lock (_gate)
        {
            Directory.CreateDirectory(Folder);
            var temporary = FilePath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(this, Json));
            File.Move(temporary, FilePath, overwrite: true);
        }
    }

    /// <summary>Puts a video first in the recent list (newest first, ten at most).</summary>
    public void Remember(VideoRequest request, string? title)
    {
        lock (_gate)
        {
            var old = Recent.FirstOrDefault(video => video.VideoId == request.VideoId);
            Recent.RemoveAll(video => video.VideoId == request.VideoId);
            Recent.Insert(0, new RecentVideo(request.VideoId, title ?? old?.Title, request.PlaylistId));
            if (Recent.Count > 10) Recent.RemoveRange(10, Recent.Count - 10);
        }
        Save();
    }

    public void SetTitle(string videoId, string title)
    {
        lock (_gate)
        {
            var index = Recent.FindIndex(video => video.VideoId == videoId);
            if (index < 0 || Recent[index].Title == title) return;
            Recent[index] = Recent[index] with { Title = title };
        }
        Save();
    }
}

internal sealed record RecentVideo(string VideoId, string? Title, string? PlaylistId);

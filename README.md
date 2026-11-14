# ForeverScreen

Prototype. Shows a video in a Windows window that follows a frame drawn by an addon in WoW Forever, so it looks like
part of the game. See [ADR 0001](docs/decisions/0001-video-window-follows-addon-markers.md).

| Part | Where |
|---|---|
| Addon, `/fscreen` | `addon/ForeverScreen` |
| Marker detection and tracking | `src/ForeverScreen.Core` |
| Tray app, video window and local API | `src/ForeverScreen.App` |
| Browser extension, Play in WoW | `extension` |

The game must run windowed or maximized (not exclusive fullscreen).

```powershell
dotnet test ForeverScreen.slnx
```

```powershell
node --test extension/test/extension.test.mjs
```

```powershell
dotnet run --project src/ForeverScreen.App
```

```powershell
dotnet run --project src/ForeverScreen.App -- --video https://youtu.be/dQw4w9WgXcQ
```

The app sits in the tray (see [ADR 0006](docs/decisions/0006-tray-app-with-local-api.md)). Tray menu: Play/Pause,
Mute, Paste YouTube link (takes the clipboard or asks), Recent videos, Pair browser, Start with Windows (off by
default), Logs, Exit. A left click on the icon toggles pause. Starting the app again with a link hands the link to
the running app. A video sent while the TV frame is hidden waits paused until you open it with `/fscreen`.

In game: TUNE in the TV casing, the Pause/Play button in the plain frame, or `/fscreen pause` and `/fscreen play`.

### Browser extension

Install once (Edge or Chrome): open `edge://extensions` (or `chrome://extensions`), turn on Developer mode,
Load unpacked, choose the `extension` folder. The options page opens: in the tray menu choose Pair browser, paste
the token, Save and test.

On a YouTube video: the gold TV button in the player bar, the toolbar icon, or right-click the page or any YouTube
link, Play in WoW. The video continues in WoW from the same second and the browser tab pauses. After changing the
extension's files press Reload on its card. See [ADR 0007](docs/decisions/0007-browser-extension.md).

### Local API

Listens on `http://127.0.0.1:5241` only. `GET /health` is open. `POST /play`, `POST /pause`, `POST /resume` and
`GET /status` need the header `X-ForeverScreen-Token` (tray menu, Pair browser copies it) and refuse requests with a
web page `Origin`. `/play` takes `{ "url": "<YouTube link>" }` or `{ "videoId": "...", "start": 30, "playlistId": "..." }`.

```powershell
curl.exe -s http://127.0.0.1:5241/health
```

```powershell
curl.exe -s -X POST http://127.0.0.1:5241/play -H "X-ForeverScreen-Token: $(Get-Clipboard)" -H "Content-Type: application/json" --data-binary '@play.json'
```

Settings (`port`, `token`, `muted`, recent videos) and logs live in `%LOCALAPPDATA%\ForeverScreen`. Set
`FOREVERSCREEN_HOME` to use another folder, for example for a test instance on another port.

`/fscreen tv` toggles the optional painted goblin TV casing: golden brass and copper, rivets, gears, orange-bulb
rabbit ears, a TUNE dial, power switch, brass resize crank, speaker grille, green glass jewel and a gold BILGEBOX brand plate. It is off
by default and remembered between sessions. Toggle it at any time, including in combat. Drag the casing to move the
frame. Click TUNE to play/pause, click the power switch to hide the frame, and drag the bottom-right brass crank
to resize. Hover each control for its GameTooltip. `/fscreen` turns the frame back on. The dial's painted pointer
changes position with playback, including changes from `/fscreen pause` and `/fscreen play`.

TV mode hides the title counters, plain Pause/Play and close buttons, footer help, grey resize grip and waiting
message. Brass bezel strips fill only the title area above the top marker row and the footer below the bottom
marker row. Switching TV mode off restores the original UI. Fixed corners and control details surround uniform
stretchable edges. Video and marker geometry follow the existing protocol. See [ADR 0005](docs/decisions/0005-controls-in-the-tv-casing.md).
Painted masters, prompts, slice contracts, a reproducible Python/Pillow build and the offline
[contact sheet](tools/TVArt/preview.png) are in [tools/TVArt](tools/TVArt/README.md). To rebuild the shipped uncompressed
32-bit TGA textures:

```powershell
python tools/TVArt/make-art.py
```

The contact sheet and source tests were checked; actual texture loading and combat interaction in WoW Forever
still require in-game verification.

Measurements go to `%LOCALAPPDATA%\ForeverScreen\logs` and to the tray menu's Logs window.

Icons (app `icon.png`/`icon.ico` and `extension/icons`): `py tools/Icon/make-icon.py`. It uses `tools/Icon/icon-painted.png` when that file exists, otherwise the painted TV master.

## License

[MIT](LICENSE). Not affiliated with or endorsed by Blizzard Entertainment. The addon reads nothing from the game and the app sends nothing into it; the video window only follows a frame the addon draws.

# 0006. A tray app with a local API on 127.0.0.1 replaces the console

- **Status:** Accepted (prototype)
- **Date:** 2026-11-14
- **Supersedes:** the console window and the start-only `--video` argument as the way to choose a video
  (the tracking in [0001](0001-video-window-follows-addon-markers.md) and [0002](0002-play-state-in-a-marker-cell.md) is unchanged).

## Context

Choosing a video meant starting the app from a terminal with `--video <id>`. Tomáš wants to start a YouTube video
from the browser straight into WoW. That needs a long-running app that something outside can talk to: a browser
extension (phase 2) or a link pasted by hand. The proposal is in the WoW agent's
`plans/foreverscreen-browser-tray.md`.

## Decision

The app runs as a Windows tray app (WinExe, no console). One instance per user: a named mutex, and a second start
forwards its arguments (`--video <link>` or a bare link) over a current-user named pipe and exits.

A minimal Kestrel API listens only on `127.0.0.1` (default port 5241, in `settings.json`):

- `GET /health` is open and returns `{ status, version, name }`.
- `POST /play` (`{ url }` or `{ videoId, start, playlistId }`), `POST /pause`, `POST /resume`, `GET /status` need the
  pairing token in `X-ForeverScreen-Token` and an `Origin` that is absent or a browser extension
  (`chrome-extension://`, `extension://`). Requests from web page origins are refused with 403.
- No CORS headers, so a web page cannot send the token header at all. The environment is forced to Production, so
  errors never show stack traces.

The token is random (24 bytes, base64url), generated on first start and stored in
`%LOCALAPPDATA%\ForeverScreen\settings.json` (or `FOREVERSCREEN_HOME`). The tray menu's Pair browser copies it.

The tray menu: now playing, Play/Pause, Mute, Paste YouTube link, Recent videos (10), Pair browser, Start with
Windows (off by default, HKCU Run), Logs, Exit. A left click toggles pause. WebView2 starts on the first video. A video
sent while the TV frame is not visible loads paused and a toast says to open `/fscreen`; the tracker then applies
the addon's play cell.

## Alternatives considered

- Native Messaging: the browser starts a host process per message and needs a registry manifest per browser. A
  long-running tray app needs its own channel anyway, and HTTP is easy to test with curl.
- Keep the console: no way to send a second video without restarting, and it is easy to close by mistake.
- No token, loopback only: any web page could POST to localhost (simple requests skip CORS preflight).

## Consequences

- Other local processes of the same user can read the token from settings.json; that is accepted for a personal
  prototype.
- The app now references the ASP.NET Core shared framework.
- The API, the single-instance pipe, the play/pause path and title capture were tested with curl; the tray menu and
  playback in WoW still need checking in game. The browser extension is phase 2.

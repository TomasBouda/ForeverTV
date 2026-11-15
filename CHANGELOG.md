# Changelog

## 0.1.0 — 2026-11-15

### Features
- Releases: the app updates itself from GitHub Releases (tray menu, Update and restart); the addon goes to CurseForge and the extension to the Chrome Web Store from the same version tag.
- Browser extension for Edge and Chrome: Play in WoW from a button in the YouTube player bar, the toolbar icon or the right-click menu; continues from the same second and pauses the browser tab.
- Extension options page to pair with the tray app and test the connection.
- Tray app instead of the console: Paste YouTube link, Recent videos, Play/Pause, Mute, Logs and an optional Start with Windows.
- Local API on 127.0.0.1 for the browser extension: play, pause, resume and status, protected by a pairing token (tray menu, Pair browser); open `/health`.
- A second start or a link argument hands the video to the running app.
- Shows the video title and explains YouTube errors (embedding not allowed, video not found) in a notification.
- Painted app and extension icon: the golden TV in a gold frame on a blue background.
- Integrated TV controls with hover tooltips: TUNE plays/pauses with two painted pointer states, the power switch
  hides the frame, and a bottom-right brass crank resizes it.
- Optional goblin TV casing for the addon, toggled with `/ftv tv` and remembered between sessions.
- Prototype: a video window that follows a frame in WoW Forever and lets clicks through to it.
- Play/pause from the addon: a title bar button and `/ftv pause | play`, also in combat.
- `--mute` starts the video muted.

### Changes
- The plain frame shows "ForeverTV" in its title bar instead of the prototype click counters.
- Renamed from ForeverScreen to ForeverTV: addon ForeverTV with `/ftv`, app ForeverTV.exe, extension ForeverTV - Play in WoW. Settings and the pairing token move over on the first start.
- The video stays visible when WoW is not the active window (for example a browser on another monitor); windows over WoW cover it, and it hides with a minimized WoW.
- TV mode hides the plain controls, counters, help and waiting text, filling title/footer interiors with brass
  bezel art. Turning TV mode off restores the original UI.
- Repainted the optional BILGEBOX TV casing in warm golden brass and copper, with sculpted bevels, gears, dials,
  speaker grille, green jewel and orange-bulb antenna. Fixed details and uniform edge slices preserve resizing.

### Bug fixes
- Repainted complete circular TV knobs with clear margins and removed overlapping TUNE/GAIN labels; the upper
  control column now joins its stretchable middle continuously.
- Hiding the frame or switching skins during resizing releases and saves the size.
- `--video` crashed on start (wrong thread mode for WebView2).
- (Console era) Closing the console no longer leaves the browser holding the profile, which stopped the next start from showing video.

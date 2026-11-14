# 0001. A Windows video window follows corner markers drawn by the addon

- **Status:** Accepted (prototype); play/pause over PixelBridge superseded by [0002](0002-play-state-in-a-marker-cell.md); "always on top" superseded by [0008](0008-video-stays-above-wow-out-of-focus.md)
- **Date:** 2026-11-06

## Context

Tomáš wants to watch YouTube in WoW Forever. An addon has no network access, and `MovieFrame:StartMovie` takes only
built-in movie IDs. Video converted to textures would be low quality and is not YouTube. His idea: the addon opens a
window with tracking markers and a Windows window snaps to them. Moving and play/pause must work.

## Decision

- The addon draws a frame with four markers in a band just outside the video area. A marker is three 6 x 6 px cells in
  magenta, green and cyan; their order names the corner. The frame's scale makes one UI unit one physical pixel.
- The app finds the markers with GDI captures of the screen: small areas around the last positions, the whole client
  area when one is missing. It runs at about 30 ticks/s when still and as fast as it can for 0.5 s after a move.
- The video window is always on top, takes no focus, lets clicks through and is left out of screen captures
  (`WDA_EXCLUDEFROMCAPTURE`), so it cannot hide a marker while it lags behind a drag. Clicks land on the addon's frame.
- A missing marker for more than 200 ms, or WoW not being the active window, parks the video window off the screen.
- Commands and the link will go over PixelBridge (about 4 frames/s, about 0.35 s), which is too slow for dragging.
  They are not in the prototype.
- Own repository, since the app and the addon ship together and depend on PixelBridge from NuGet and CurseForge.

## Alternatives considered

- **Frame position over PixelBridge, no markers.** Simpler, but four updates a second cannot follow a drag, and it
  cannot see that a game window covers the frame.
- **Windows.Graphics.Capture.** Sees the game's window alone, but needs a border or a consent prompt. GDI already works
  for PixelBridge. To be revisited if GDI proves too slow.

## Consequences

- Screenshots and streams do not show the video, because the window is left out of captures.
- Game windows such as tooltips, bags and the map stay under the video. One that covers a corner hides the video.
- Reading the screen is the technique pixel bots use (WoWAddons ADR 0076). Only the frame's position leaves the game,
  no game data, and nothing is sent into the game. Blizzard's position on Forever is unknown.
- The prototype must prove in game how quickly the window follows, and that clicks pass through, also in combat.

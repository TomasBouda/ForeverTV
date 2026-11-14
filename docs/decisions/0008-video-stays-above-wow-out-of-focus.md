# 0008. The video stays directly above WoW when WoW is not the active window

- **Status:** Accepted (prototype)
- **Date:** 2026-11-14
- **Supersedes:** "always on top" and the hide-when-not-active behaviour of the video window in
  [0001](0001-video-window-follows-addon-markers.md).

## Context

The video window was topmost, so it was hidden whenever WoW lost focus; otherwise it would float over the browser or
any other window. Tomáš watches with a browser on another monitor and wants the TV picture to stay while he clicks
elsewhere.

## Decision

The window is not topmost by style. While WoW is the active window it is made topmost (as before). Out of focus it
drops out of the topmost band and is inserted directly above the WoW window in the z-order, so other windows above
WoW also cover the video. The tracker checks every 250 ms at most that nothing has come between them and puts the
window back. A minimized WoW hides the video.

## Alternatives considered

- Make the WoW window the owner of the video window. Windows keeps owned windows above the owner by itself, but a
  cross-process owner attaches the input queues of both processes and destroys the owned window when WoW closes,
  which would end the app (the video window is the main form).
- Keep hiding out of focus. Simple, but loses the picture on a multi-monitor setup.

## Consequences

- Markers come from a screen capture, so a window covering the markers hides the video; sound continues, as with
  alt-tab before.
- Clicking into WoW lifts WoW above the video for up to one tracker tick before the video is topmost again.
- Verified by reading the z-order with WoW out of focus: the video window was on screen, not topmost, directly above
  WoW. Not yet watched by Tomáš across alt-tab and other windows.

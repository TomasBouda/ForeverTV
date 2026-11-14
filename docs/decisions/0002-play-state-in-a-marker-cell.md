# 0002. The play state rides in a colour cell next to a marker

- **Status:** Accepted (prototype)
- **Date:** 2026-11-14
- **Supersedes:** the part of [0001](0001-video-window-follows-addon-markers.md) that sent play/pause over PixelBridge

## Context

Play/pause is the first control Tomáš needs. ADR 0001 planned to send commands over PixelBridge, which manages about four
frames a second with about 0.35 s of delay. The tracker already reads the markers about ten times a second.

## Decision

- The addon draws one more 6 x 6 px cell right of the top-left marker: green plays, cyan pauses. Neither colour after the
  marker's own green and cyan forms another corner's pattern, so it cannot make a false marker.
- The app reads that cell from the capture it already takes of the top-left marker, so it costs no extra capture.
- The addon keeps the state in `ForeverScreenDB.paused`. A title bar button and `/fscreen pause | play` change it, also
  in combat, since only an addon frame is clicked.
- The app drives the YouTube embed through its iframe API (`enablejsapi=1`, `postMessage`) and logs the player's state.
- A frame missing for 1.5 s while WoW is the active window pauses the video, so a closed frame does not keep playing.
  Losing focus does not pause it.
- PixelBridge stays planned for the link, which is text and needs no speed.

## Consequences

- One more state needs one more cell or colour; a few booleans fit before PixelBridge is worth it.
- A game window covering the top-left marker hides the state as well as the video.

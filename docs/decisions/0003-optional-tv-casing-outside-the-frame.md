# 0003. An optional TV casing surrounds the existing addon frame

- **Status:** Accepted (prototype)
- **Status note:** [0004](0004-painted-tv-casing.md) supersedes only the colour-block construction and no-media-files portions; the outside-frame geometry and toggle decision remain in force.
- **Status note:** [0005](0005-controls-in-the-tv-casing.md) supersedes keeping plain controls accessible in TV mode and permits casing strips inside the title/footer areas above/below the marker rows.
- **Date:** 2026-11-14

## Context

The addon can optionally resemble a goblin-built TV. The desktop tracker derives the video rectangle from the four
markers and reads play state from the adjacent cell, so decorative art must preserve their geometry and colours.

## Decision

- Create the casing once as a hidden child frame. `/fscreen tv` toggles its visibility and stores `skin = "tv"` in
  `ForeverScreenDB`. An absent setting selects the original appearance.
- Anchor four metal rails outside the existing frame bounds. Opposing anchors stretch them during resizing and
  inherited scale keeps the art in physical pixels. The antenna and feet extend further outward.
- Build original rivets, dials, speaker slots and a brand plate from colour textures and gradients with muted colours.
- Let the rails share the frame's drag handlers. Keep the existing title controls and resize grip accessible.

## Consequences

- Switching uses ordinary addon frame visibility and works during combat without reloading.
- The casing needs no media files or changes to the desktop tracker.
- Screen clamping applies to the original frame, so the outer casing can be clipped at the screen edges.
- In-game appearance and combat interaction still need verification in WoW Forever.

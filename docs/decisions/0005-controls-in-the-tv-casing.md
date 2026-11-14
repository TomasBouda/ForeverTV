# 0005. Playback, power and resize controls are built into the TV casing

- **Status:** Accepted (prototype)
- **Date:** 2026-11-14
- **Supersedes:** decorative dials and visible plain controls in TV mode in [0003](0003-optional-tv-casing-outside-the-frame.md) and [0004](0004-painted-tv-casing.md), plus their outside-frame restriction for title/footer bezel strips.

## Context

The client render shows TUNE/GAIN labels overlapping cropped, flattened knobs and an abrupt seam under the upper
control column. Tomáš wants the painted casing itself to provide playback, power and resizing. The tracker still
depends on the exact four marker rows, play-state cell and video rectangle established in 0001/0002.

## Decision

Use ordinary buttons over fixed painted casing sprites for TUNE playback, power off and the bottom-right resize
crank, with GameTooltip explanations. Hide the original title, buttons, footer, grip and waiting text in TV mode,
and cover the title/footer interiors with horizontally stretchable brass art confined above/below the marker rows.

TUNE calls the existing `SetPaused` function. Two separately painted pointer states follow every playback change,
including slash commands and saved state. This uses `SetTexCoord` and requires no texture rotation API. Power hides
the existing frame, so `/fscreen` restores it with its saved skin and paused state. Both resize grips share sizing
handlers and save the resulting geometry. Skin changes and frame hiding release active moves and resizing.

The new RGBA master and exact built-in image tool prompt are saved beside the original unchanged master. A fifth
power-of-two, uncompressed 32-bit TGA atlas holds the fixed control sprites. Both knob states retain circular aspect
ratios and transparent margins. The upper column uses the same uniform painted profile as its middle, and labels
are removed. The speaker jewel retains its painted appearance.

## Alternatives considered

- Keep the original buttons visible in TV mode. They occupy the casing's intended title/footer space and do not
  satisfy the requested integrated controls.
- Rotate a pointer texture. Two painted states provide the required feedback through the existing texture API and
  preserve painterly lighting in each state.
- Change frame or marker geometry to remove the strips. This would require a tracker protocol change. Existing
  title/footer interiors already provide safe space for brass art.

## Consequences

- `/fscreen tv` remains an immediate, remembered toggle using ordinary own-frame visibility, including in combat.
  The original UI is restored when the setting is absent. All markers, the state cell, video anchors and height
  constants retain their existing geometry and colours.
- The art build verifies atlas formats, uniform stretch profiles and marker-safe edge colours. Source tests exercise
  visibility, saved settings, simulated combat, pointer/state-cell updates, tooltips, casing dragging, power and
  resize interruption, plus safe bounds at minimum/default/large sizes.
- The contact sheet provides offline visual verification. Actual client filtering, mouse interaction and combat
  behaviour require verification in WoW Forever.

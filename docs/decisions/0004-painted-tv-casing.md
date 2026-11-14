# 0004. Painted, sliced brass art replaces the TV colour blocks

- **Status:** Accepted (prototype); supersedes the colour-block construction and no-media-files portions of [0003](0003-optional-tv-casing-outside-the-frame.md).
- **Status note:** [0005](0005-controls-in-the-tv-casing.md) supersedes decorative dials, visible plain controls in TV mode, and the entirely-outside-frame art restriction for title/footer bezel strips. It adds a fifth atlas with complete painted control states.
- **Date:** 2026-11-14

## Context

The optional BILGEBOX TV casing looks dull when assembled from flat colour blocks. It should share Forever-Fone's
hand-painted aged golden brass, copper, bevels and patina while preserving the original frame and marker contract.
The frame resizes in physical pixels, so scaling a single painted shell would distort details and risk covering
the video, marker band or controls. The minimum original frame height is 195 pixels.

## Decision

Replace the decorative blocks in `CreateTVSkin()` with four original painted texture atlases and explicit
`SetTexture` / `SetTexCoord` slices. Preserve fixed corners and control details, stretch only uniform profiles,
and keep every texture outside the original frame.

The rails retain their original thicknesses: top 28, bottom 22, left 22 and right 60 pixels. The right column uses
a fixed 124-pixel upper part with two dials, a uniform middle and a fixed 64-pixel speaker part, leaving seven
middle pixels at minimum height. A separate transparent rabbit-ear antenna sits above the top rail; an engraved
BILGEBOX FontString sits on a fixed gold plate. The green jewel stays inside the speaker, away from frame markers.

Keep the unchanged painted PNG master, exact generation prompt, deterministic Pillow slicing/TGA build and contact
sheet in `tools/TVArt/`. Ship only power-of-two uncompressed 32-bit TGA atlases in `addon/ForeverScreen/art/`.
The [art README](../../tools/TVArt/README.md) defines the pixel rectangles, master crops and build command.

## Alternatives considered

- Retain colour blocks: compact but cannot provide the painted metallic relief and warmth requested.
- Stretch one complete casing image: distorts corners, circular dials, rivets and speaker ribs at different sizes.
- Tile detailed edges: repeats screws and seams; uniform painted profiles keep the resize contract simple.
- Change the desktop tracker or frame geometry: unnecessary and would risk the existing marker and play-state protocol.

## Consequences

- Media files now ship with the addon; this supersedes the no-media-files consequence in 0003. The saved master
  permits deterministic rebuilds without rerunning nondeterministic image generation.
- The toggle remains off by default, persists in `ForeverScreenDB.skin` and uses ordinary visibility changes.
  Panel drag handlers, title play/pause and the original resize grip retain their behaviour, including in combat.
- The C# application, video rectangle, marker band, title/footer heights and marker/state colours remain unchanged.
- Builds verify stretch uniformity, format and edge palette. Source tests verify disk paths, TGA headers, geometry
  and interactions in a recording host. The contact sheet is an offline art composition, not client verification.
- Real Interface 16001 loading/filtering, in-game appearance and combat interactions still require client verification.
  Screen clamping still applies only to the original frame, so outer art may be clipped near screen edges.

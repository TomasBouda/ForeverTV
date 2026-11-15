# CurseForge upload kit: ForeverTV

The CurseForge project exists (ID **1805915**, in `addon/ForeverTV/ForeverTV.toc`). Paste these into the project's
settings in the CurseForge author console. Files go up through the release workflow on a `v*` tag
([ADR 0010](../docs/decisions/0010-release-pipeline.md)); its changelog comes from `CHANGELOG.md`, and
`changelog-0.1.0.md` is a shorter player-facing version for a manual upload or for editing the file afterwards.

| Field | Value |
| --- | --- |
| Project name | ForeverTV |
| Summary | `summary.txt` |
| Description | `description.md` (switch the editor to Markdown, then paste) |
| Main category | Miscellaneous |
| Other categories | Audio & Video |
| License | MIT |
| Logo | `logo.png` (400 x 400, from `tools/Icon/icon-painted.png`) |
| Source / issues | https://github.com/TomasBouda/ForeverTV, https://github.com/TomasBouda/ForeverTV/issues |
| Game version | WoW Forever (1.60.x) |
| Release type | Release (beta only on request) |

## Images

Upload them to the project's Images tab in this order. `logo.png` is the project avatar, not a gallery image.

| File | Title | Description |
| --- | --- | --- |
| `screenshots/01-tv-in-game.png` | YouTube in WoW | *Take in game:* the TV casing with a video playing, in a recognisable place (a city or an inn). |
| `screenshots/02-tv-casing.png` | The goblin TV | `/ftv tv`: TUNE plays and pauses, the switch turns it off, the brass crank resizes it. |
| `screenshots/03-play-in-wow.png` | One click from YouTube | *Take in the browser:* the gold TV button in the YouTube player bar. |
| `screenshots/04-tray-menu.png` | The tray app | *Take on the desktop:* the ForeverTV tray menu (Paste YouTube link, Recent videos, Pair browser). |

`02-tv-casing.png` was rendered headless in AddonStudio from the addon's own code and textures, so its screen is empty:
in the game the app's video fills it. The other three need the real game, browser and tray and are not made yet.
Use a video you may show (your own, or one licensed for it), 1920 x 1080 or larger, and crop away personal names.

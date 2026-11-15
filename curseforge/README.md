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
| `summary.png` | ForeverTV | YouTube on a goblin TV inside WoW Forever. |
| `screenshots/01-tv-playing.png` | Never gonna give you up | Your video on the goblin TV, right in the game. |
| `screenshots/02-tv-casing.png` | The goblin TV | `/ftv tv`: TUNE plays and pauses, the switch turns it off, the brass crank resizes it. |
| `screenshots/03-play-in-wow.png` | One click from YouTube | *Take in the browser:* the gold TV button in the YouTube player bar. |
| `screenshots/04-tray-menu.png` | The tray app | *Take on the desktop:* the ForeverTV tray menu (Paste YouTube link, Recent videos, Pair browser). |

`02-tv-casing.png` is `source/tv-render.png` (AddonStudio, headless, the addon's own code and textures) cropped; its
screen is empty because in the game the app's video fills it. `01-tv-playing.png` and `summary.png` put an original
80s-stage picture with a goblin crooner in silhouette on that screen, made in `source/make-images.ps1`. It is neither a
frame of the real video nor the real singer, which would not be ours to publish. Rebuild:

```powershell
pwsh curseforge/source/make-images.ps1 -Shot curseforge/source/tv-render.png -OutDir curseforge -Icon tools/Icon/icon-painted.png
```

`-Frame <png>` puts a frame of a real video on the screen instead (`-OutDir curseforge/local`, git-ignored). That
output contains someone else's video and likeness; it never goes into the repository, and publishing it is the
uploader's call.

03 and 04 need the real browser and tray and are not made yet. A real in-game shot of the TV can replace 01 later; use
a video you may show (your own, or one licensed for it), 1920 x 1080 or larger, and crop away personal names.

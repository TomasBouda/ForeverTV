# 0010. One tag releases the app, the addon and the extension

- **Status:** Accepted (prototype)
- **Date:** 2026-11-14

## Context

ForeverTV is three parts that only work together: the Windows tray app, the WoW addon and the browser extension.
The repository is public on GitHub. Tomáš wants each part where its users look for it: the app on the TomLabs app
store (apps.tomlabs.cz), the addon on CurseForge, the extension on the Chrome Web Store.

## Decision

- **One version** for all three: `VersionPrefix` in `Directory.Build.props`. The tools write it into the addon TOC
  and the extension manifest when they package them. A working build shows `0.1.0-dev+<short commit>`, a nightly
  `0.1.0-nightly.<short commit>`, a tagged build `0.1.0`.
- **GitHub Actions.** `build.yml` tests every push and pull request and refreshes the rolling `nightly`
  pre-release from master. `release.yml` runs on a tag `v<version>`: checks that the tag, `VersionPrefix` and a
  `## <version>` section in CHANGELOG.md agree, tests, then publishes.
- **App:** TomLabs.AutoUpdate's reusable `publish-app.yml` builds a self-contained single-file `ForeverTV.exe`
  per RID (win-x64, win-arm64), zips it and publishes a GitHub release with `update.json`, signed with the app's
  ECDSA key (private half in the TomLabs vault and in the GitHub secret `UPDATE_SIGNING_KEY`; public half in
  `UpdateKey.cs`). The app updates itself from GitHub Releases: it downloads quietly and the tray menu offers
  "Update to x and restart"; the restarted build waits for the old one's single-instance lock (`--after-update`).
  The app store lists ForeverTV as a pointer to the GitHub releases (registered once with
  `Register-GitHubApp.ps1`); this repository never contains the store's address or token.
- **Addon:** `tools/Publish-CurseForge.ps1` uploads `ForeverTV-<version>.zip` with the changelog section to the
  project in the TOC's `X-Curse-Project-ID`, for the newest 1.60.x game version.
- **Extension:** `tools/Publish-ChromeWebStore.ps1` uploads `ForeverTV-extension-<version>.zip` through the Chrome
  Web Store API v2 (OAuth client and refresh token) and submits it for review. The API cannot create an item, so
  the first version, the listing and the privacy answers ([docs/store](../store/chrome-web-store.md),
  [PRIVACY.md](../../PRIVACY.md)) are done by hand once.
- Both zips are also attached to the GitHub release. A store whose ID or secret is missing is skipped with a
  warning, so the first releases work before the store accounts exist.

## Alternatives considered

- Azure DevOps and the store's own template, like the private apps. The code is public on GitHub; the shared
  `publish-app.yml` is the documented path for public apps and keeps the store out of the public repository.
- Separate tags per part. The parts talk over a versioned local API and marker protocol; one version avoids a
  mismatch matrix.
- Edge Add-ons as well. Edge installs from the Chrome Web Store; a second store can come later.

## Consequences

- A release is: move the CHANGELOG heading to `## x.y.z — date`, set `VersionPrefix`, commit, tag `vx.y.z`, push
  the tag. The tag push needs Tomáš's yes like any push.
- CurseForge and Google review uploads before they are public; the workflow succeeds when they are submitted.
- Losing the update signing key stops every installed copy from updating; KeePass keeps its backup.
- The Chrome Web Store needs a developer account (one-time fee, Tomáš pays) and a Google Cloud OAuth client.

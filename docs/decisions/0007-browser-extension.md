# 0007. A Manifest V3 browser extension sends YouTube videos to the tray app

- **Status:** Accepted (prototype)
- **Date:** 2026-11-14

## Context

Phase 2 of the "Play in WoW" plan: on a YouTube page, one click should hand the video to the BILGEBOX TV and continue
from the same second. The tray app's local API ([0006](0006-tray-app-with-local-api.md)) takes `/play` with a pairing
token and accepts only requests without an `Origin` or from a browser extension.

## Decision

`extension/` in this repo is an unpacked Manifest V3 extension for Edge and Chrome, versioned with the app.

- Three ways to send: a gold TV button at the start of the player bar's right controls on `/watch`, the toolbar
  icon, and the context menu "Play in WoW" (on a YouTube page or video, and on any YouTube link: watch, shorts,
  youtu.be).
- The content script reads the video id from the URL, the second from the playing `<video>` (falling back to the
  link's `t=`/`start=` when the video has not started), and `list=`. A send from the tab's own video pauses it
  through YouTube's Play button; a send from a link does not touch the tab.
- Only the service worker calls `http://127.0.0.1:<port>`, so the `Origin` is `chrome-extension://<id>` and passes
  the app's guard. The result is a badge (OK, KEY, OFF, ?) and a short toast on the page. Not paired opens the
  options page.
- The options page stores the port and the token in `chrome.storage.local` and tests them (`/health`, then
  `/status` with the token). It opens on install.
- Permissions: `contextMenus`, `storage`, hosts `http://127.0.0.1/*` and `https://*.youtube.com/*`. Nothing else.
- Link parsing and the API client are plain ES modules without `chrome.*`, tested with `node --test`.

## Alternatives considered

- Pin the extension id in the app's Origin check. An unpacked extension's id depends on its folder, so any
  `chrome-extension://` origin is accepted; the token is what pairs it.
- Store publishing now. Not needed while Tomáš is the only user; the stores remain a later decision.
- Inject the button into Shorts too. The Shorts player has no stable control bar; the toolbar icon and context menu
  cover it.

## Consequences

- YouTube page changes can break the player-bar button; the toolbar icon and context menu do not depend on it.
- The extension must be reloaded in the browser after changes (`edge://extensions`, Reload).
- Verified in headless Edge 157 with a clean profile against a test app instance: pairing (wrong port, wrong token,
  right token), send from the button at the current second, the tab paused, toast shown, app log confirmed. Not yet
  seen in Tomáš's own browser or with the TV visible in WoW.

# Chrome Web Store listing

Texts for the first, manual submission in the Developer Dashboard. Later versions are uploaded by the release
workflow ([ADR 0010](../decisions/0010-release-pipeline.md)).

## Store listing

- **Name:** ForeverTV - Play in WoW (from `manifest.json`)
- **Summary:** Sends the YouTube video you are watching to ForeverTV in WoW Forever. (from `manifest.json`)
- **Category:** Entertainment
- **Language:** English
- **Icon:** `extension/icons/icon-128.png`
- **Screenshots (1280x800):** the gold TV button in the YouTube player bar; the options page after Save and test;
  the TV playing in WoW.
- **Homepage / support:** https://github.com/TomasBouda/ForeverTV

**Description:**

> Play YouTube in World of Warcraft Forever. Click the gold TV button in the YouTube player bar, the toolbar icon,
> or right-click a YouTube link and choose Play in WoW: the video continues on the in-game TV from the same second,
> and the browser tab pauses.
>
> Needs the free ForeverTV app for Windows and the ForeverTV addon (both at https://github.com/TomasBouda/ForeverTV).
> Pair once: in the app's tray menu choose Pair browser, paste the token in the extension's options, Save and test.
>
> The extension talks only to the ForeverTV app on your own computer (127.0.0.1). No accounts, no analytics.

## Privacy practices

- **Single purpose:** Send the YouTube video the user chooses to the ForeverTV app on the same computer, so it
  plays inside the game.
- **contextMenus:** Adds "Play in WoW" to the right-click menu on YouTube pages and YouTube links.
- **storage:** Keeps the local app's port and pairing token.
- **Host permission `http://127.0.0.1/*`:** The ForeverTV app's local API on the user's own computer.
- **Host permission `https://*.youtube.com/*`:** Shows the Play in WoW button in the YouTube player and reads the
  current video and second.
- **Remote code:** No.
- **Data usage:** none of the listed data types is collected. Certify the three disclosures.
- **Privacy policy URL:** https://github.com/TomasBouda/ForeverTV/blob/master/PRIVACY.md

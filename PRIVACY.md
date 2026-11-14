# Privacy

ForeverTV (the Windows app, the WoW addon and the browser extension "ForeverTV - Play in WoW") collects no personal
data and sends nothing to its author or to any server of its own.

- **Browser extension.** On YouTube pages it reads the address of the video you choose to send, the second it is at
  and the playlist, and sends only these to the ForeverTV app on your own computer (`http://127.0.0.1`). It stores
  the app's port and pairing token in the browser's extension storage on your computer. It does not read or change
  anything else, has no analytics and does not track browsing.
- **Windows app.** It plays the video in an embedded YouTube player, so YouTube receives what any YouTube embed
  receives. Settings, recent videos and logs stay in `%LOCALAPPDATA%\ForeverTV` on your computer. It checks GitHub
  Releases of this repository for updates.
- **WoW addon.** It draws a frame in the game and stores its position and size in the game's saved variables.

Questions: open an issue at https://github.com/TomasBouda/ForeverTV/issues.

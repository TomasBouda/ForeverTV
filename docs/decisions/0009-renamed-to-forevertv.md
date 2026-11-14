# 0009. The project is renamed from ForeverScreen to ForeverTV

- **Status:** Accepted (prototype)
- **Date:** 2026-11-14

## Context

Before the first publish on GitHub, Tomáš chose a shorter name that matches the painted TV casing: ForeverTV.
Nobody else uses the prototype yet, so the rename costs one reinstall now and nothing later.

## Decision

Everything is renamed: the repository and its folder, the addon (`addon/ForeverTV`, saved variable `ForeverTVDB`,
frames `ForeverTV*`), the slash command `/ftv` (was `/fscreen`, no alias), the .NET projects and namespaces
(`ForeverTV.App`, `ForeverTV.Core`), the executable, the API header `X-ForeverTV-Token`, `FOREVERTV_HOME`, the
single-instance pipe, the Start with Windows value and the browser extension's name.

The app moves `%LOCALAPPDATA%\ForeverScreen` to `%LOCALAPPDATA%\ForeverTV` once on start, so the pairing token,
settings and recent videos stay. The BILGEBOX brand plate on the TV art stays: it is the in-world maker's name.

ADRs 0001 to 0008 keep the old name as written.

## Consequences

- The addon is installed again under the new folder name; the old `ForeverScreen` folders were removed. Saved
  settings were copied into `ForeverTV.lua` by hand for the stable client.
- An unpacked browser extension is tied to its folder, so it has to be removed and loaded again from
  `F:\Root\GIT\ForeverTV\extension`, then paired again with the token from the tray menu.
- The old `ForeverScreen.lua` saved variables file stays in WTF until removed.

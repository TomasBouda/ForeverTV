// Talks to the ForeverTV tray app on 127.0.0.1 (ADR 0006). No chrome.* here, so node tests can run it.

export const DEFAULT_PORT = 5241;
export const TOKEN_HEADER = 'X-ForeverTV-Token';

export function normalizeSettings(stored) {
  const port = Number(stored?.port);
  return {
    port: Number.isInteger(port) && port > 0 && port < 65536 ? port : DEFAULT_PORT,
    token: typeof stored?.token === 'string' ? stored.token.trim() : '',
  };
}

// Returns { kind: 'ok' | 'unpaired' | 'bad' | 'offline' | 'error', status, data }.
export async function callApp(settings, method, path, body, fetchImpl = fetch) {
  const { port, token } = normalizeSettings(settings);
  if (path !== '/health' && !token) return { kind: 'unpaired', status: 0, data: null };

  const headers = {};
  if (token) headers[TOKEN_HEADER] = token;
  if (body !== undefined) headers['Content-Type'] = 'application/json';

  const abort = new AbortController();
  const timer = setTimeout(() => abort.abort(), 3000);
  let response;
  try {
    response = await fetchImpl(`http://127.0.0.1:${port}${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      signal: abort.signal,
      cache: 'no-store',
    });
  } catch {
    return { kind: 'offline', status: 0, data: null };
  } finally {
    clearTimeout(timer);
  }

  let data = null;
  try { data = await response.json(); } catch { /* not JSON */ }
  if (response.ok) return { kind: 'ok', status: response.status, data };
  if (response.status === 403) return { kind: 'unpaired', status: 403, data };
  if (response.status === 400) return { kind: 'bad', status: 400, data };
  return { kind: 'error', status: response.status, data };
}

export function playVideo(settings, request, fetchImpl = fetch) {
  const body = { videoId: request.videoId, start: request.start || 0 };
  if (request.playlistId) body.playlistId = request.playlistId;
  return callApp(settings, 'POST', '/play', body, fetchImpl);
}

// One line for the toast on the YouTube page.
export function describe(result) {
  switch (result.kind) {
    case 'ok':
      return result.data && result.data.tvVisible === false
        ? 'Sent to ForeverTV. Open the TV in WoW (/ftv) to start it.'
        : 'Playing in WoW.';
    case 'unpaired':
      return 'Not paired: ForeverTV tray menu, Pair browser, then paste the token in the extension options.';
    case 'offline':
      return 'ForeverTV is not running. Start it, then try again.';
    case 'bad':
      return (result.data && result.data.error) || 'Not a YouTube video.';
    default:
      return `ForeverTV answered ${result.status}. See its log (tray menu, Logs).`;
  }
}

export const BADGES = {
  ok: { text: 'OK', color: '#2e7d32' },
  unpaired: { text: 'KEY', color: '#c77700' },
  offline: { text: 'OFF', color: '#616161' },
  bad: { text: '?', color: '#b3261e' },
  error: { text: '!', color: '#b3261e' },
};

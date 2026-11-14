// Reads a YouTube video id, start second and playlist from a link. Mirrors VideoRequest.TryParse in
// ForeverTV.Core; the app parses again, so this only decides whether a link is worth sending.

const VIDEO_ID = /^[A-Za-z0-9_-]{11}$/;
const PLAYLIST_ID = /^[A-Za-z0-9_-]{2,64}$/;

export function isVideoId(value) {
  return typeof value === 'string' && VIDEO_ID.test(value);
}

export function isPlaylistId(value) {
  return typeof value === 'string' && PLAYLIST_ID.test(value);
}

// "90", "90s", "1m30s", "1h2m3s" -> seconds; anything else -> 0.
export function parseSeconds(value) {
  if (!value) return 0;
  if (/^\d+$/.test(value)) return Number(value);
  const match = /^(?:(\d+)h)?(?:(\d+)m)?(?:(\d+)s?)?$/i.exec(value);
  if (!match || match[0] === '') return 0;
  return Number(match[1] || 0) * 3600 + Number(match[2] || 0) * 60 + Number(match[3] || 0);
}

function isYouTubeHost(host) {
  return host === 'youtube.com' || host.endsWith('.youtube.com') ||
    host === 'youtube-nocookie.com' || host.endsWith('.youtube-nocookie.com');
}

// Returns { videoId, start, playlistId? } or null.
export function parseLink(text) {
  if (typeof text !== 'string') return null;
  const value = text.trim();
  if (isVideoId(value)) return { videoId: value, start: 0 };

  let url;
  try { url = new URL(value); } catch { return null; }
  if (url.protocol !== 'https:' && url.protocol !== 'http:') return null;

  const host = url.hostname.toLowerCase();
  let videoId = null;
  if (host === 'youtu.be' || host === 'www.youtu.be') {
    videoId = url.pathname.split('/')[1] || null;
  } else if (isYouTubeHost(host)) {
    const parts = url.pathname.split('/').filter(Boolean);
    if (parts[0] === 'watch') videoId = url.searchParams.get('v');
    else if (['shorts', 'embed', 'live', 'v'].includes(parts[0])) videoId = parts[1] || null;
  }
  if (!isVideoId(videoId)) return null;

  const result = { videoId, start: parseSeconds(url.searchParams.get('t') || url.searchParams.get('start')) };
  const list = url.searchParams.get('list');
  if (isPlaylistId(list)) result.playlistId = list;
  return result;
}

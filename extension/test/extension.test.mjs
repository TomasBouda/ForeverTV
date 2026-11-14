// Run: node --test extension/test
import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { parseLink, parseSeconds } from '../youtube.js';
import { callApp, describe, normalizeSettings, playVideo, TOKEN_HEADER } from '../client.js';

test('parses the links YouTube shows', () => {
  assert.deepEqual(parseLink('https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=42s'), { videoId: 'dQw4w9WgXcQ', start: 42 });
  assert.deepEqual(parseLink('https://youtu.be/dQw4w9WgXcQ?t=1m30s'), { videoId: 'dQw4w9WgXcQ', start: 90 });
  assert.deepEqual(parseLink('https://www.youtube.com/shorts/dQw4w9WgXcQ'), { videoId: 'dQw4w9WgXcQ', start: 0 });
  assert.deepEqual(parseLink('https://m.youtube.com/watch?v=dQw4w9WgXcQ&list=PLabc123'),
    { videoId: 'dQw4w9WgXcQ', start: 0, playlistId: 'PLabc123' });
  assert.deepEqual(parseLink(' dQw4w9WgXcQ '), { videoId: 'dQw4w9WgXcQ', start: 0 });
});

test('refuses what is not a YouTube video', () => {
  assert.equal(parseLink('https://evil.example/watch?v=dQw4w9WgXcQ'), null);
  assert.equal(parseLink('https://www.youtube.com/watch?v=short'), null);
  assert.equal(parseLink('https://www.youtube.com/'), null);
  assert.equal(parseLink('javascript:alert(1)'), null);
  assert.equal(parseLink(undefined), null);
});

test('reads times like the app does', () => {
  assert.equal(parseSeconds('90'), 90);
  assert.equal(parseSeconds('1h2m3s'), 3723);
  assert.equal(parseSeconds('2m'), 120);
  assert.equal(parseSeconds('junk'), 0);
  assert.equal(parseSeconds(''), 0);
});

test('settings fall back to the default port', () => {
  assert.deepEqual(normalizeSettings({}), { port: 5241, token: '' });
  assert.deepEqual(normalizeSettings({ port: '5249', token: ' abc ' }), { port: 5249, token: 'abc' });
  assert.equal(normalizeSettings({ port: 99999 }).port, 5241);
});

function fakeFetch(status, data, seen) {
  return async (url, options) => {
    seen.push({ url, options });
    return { ok: status >= 200 && status < 300, status, json: async () => data };
  };
}

test('play sends the token and the video', async () => {
  const seen = [];
  const result = await playVideo({ port: 5249, token: 'tok' }, { videoId: 'dQw4w9WgXcQ', start: 12, playlistId: 'PLx1' },
    fakeFetch(200, { videoId: 'dQw4w9WgXcQ', tvVisible: true }, seen));
  assert.equal(result.kind, 'ok');
  assert.equal(seen[0].url, 'http://127.0.0.1:5249/play');
  assert.equal(seen[0].options.headers[TOKEN_HEADER], 'tok');
  assert.deepEqual(JSON.parse(seen[0].options.body), { videoId: 'dQw4w9WgXcQ', start: 12, playlistId: 'PLx1' });
  assert.equal(describe(result), 'Playing in WoW.');
});

test('results map to clear messages', async () => {
  assert.equal((await callApp({ token: '' }, 'POST', '/play', {}, fakeFetch(200, {}, []))).kind, 'unpaired');
  assert.equal((await callApp({ token: 't' }, 'GET', '/status', undefined, fakeFetch(403, {}, []))).kind, 'unpaired');
  assert.equal((await callApp({ token: 't' }, 'GET', '/status', undefined, async () => { throw new TypeError('refused'); })).kind, 'offline');
  const bad = await callApp({ token: 't' }, 'POST', '/play', {}, fakeFetch(400, { error: 'Not a YouTube video link.' }, []));
  assert.equal(describe(bad), 'Not a YouTube video link.');
  assert.match(describe({ kind: 'ok', data: { tvVisible: false } }), /\/ftv/);
});

test('manifest asks only for the planned permissions', () => {
  const manifest = JSON.parse(readFileSync(new URL('../manifest.json', import.meta.url), 'utf8'));
  assert.equal(manifest.manifest_version, 3);
  assert.deepEqual(manifest.permissions.sort(), ['contextMenus', 'storage']);
  assert.deepEqual(manifest.host_permissions.sort(), ['http://127.0.0.1/*', 'https://*.youtube.com/*']);
});

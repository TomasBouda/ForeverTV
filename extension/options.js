import { DEFAULT_PORT, callApp, normalizeSettings } from './client.js';

const form = document.getElementById('form');
const token = document.getElementById('token');
const port = document.getElementById('port');
const show = document.getElementById('show');
const result = document.getElementById('result');

document.getElementById('version').textContent = chrome.runtime.getManifest().version;

function say(text, kind) {
  result.textContent = text;
  result.className = kind;
}

async function test(settings) {
  const health = await callApp(settings, 'GET', '/health');
  if (health.kind !== 'ok') {
    say(`ForeverTV is not running on port ${settings.port}. Start it and test again.`, 'error');
    return;
  }
  const status = await callApp(settings, 'GET', '/status');
  if (status.kind === 'unpaired') {
    say(settings.token ? 'ForeverTV refused the token. Copy it again: tray menu, Pair browser.' : 'Paste the token first.', 'error');
    return;
  }
  if (status.kind !== 'ok') {
    say(`ForeverTV answered ${status.status}.`, 'error');
    return;
  }
  const now = status.data?.title ? ` Now: ${status.data.title}.` : '';
  say(`Paired with ForeverTV ${health.data?.version ?? ''}.${now}`, 'ok');
}

show.addEventListener('change', () => { token.type = show.checked ? 'text' : 'password'; });

form.addEventListener('submit', async event => {
  event.preventDefault();
  const settings = normalizeSettings({ port: port.value, token: token.value });
  await chrome.storage.local.set(settings);
  port.value = settings.port;
  say('Testing...', '');
  await test(settings);
});

const stored = normalizeSettings(await chrome.storage.local.get(['port', 'token']));
token.value = stored.token;
port.value = stored.port || DEFAULT_PORT;
if (stored.token) await test(stored);

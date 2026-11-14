import { parseLink } from './youtube.js';
import { BADGES, describe, normalizeSettings, playVideo } from './client.js';

const LINK_PATTERNS = ['*://*.youtube.com/watch*', '*://*.youtube.com/shorts/*', '*://youtu.be/*'];
const PAGE_PATTERNS = ['https://*.youtube.com/watch*', 'https://*.youtube.com/shorts/*'];

chrome.runtime.onInstalled.addListener(details => {
  chrome.contextMenus.removeAll(() => {
    chrome.contextMenus.create({ id: 'link', title: 'Play in WoW', contexts: ['link'], targetUrlPatterns: LINK_PATTERNS });
    chrome.contextMenus.create({ id: 'page', title: 'Play in WoW', contexts: ['page', 'video'], documentUrlPatterns: PAGE_PATTERNS });
  });
  if (details.reason === 'install') chrome.runtime.openOptionsPage();
});

async function settings() {
  return normalizeSettings(await chrome.storage.local.get(['port', 'token']));
}

function tell(tabId, type, extra) {
  if (tabId === undefined || tabId < 0) return Promise.resolve();
  return chrome.tabs.sendMessage(tabId, { type, ...extra }).catch(() => { /* no content script on this page */ });
}

async function showResult(tabId, result) {
  const badge = BADGES[result.kind] || BADGES.error;
  if (tabId !== undefined && tabId >= 0) {
    await chrome.action.setBadgeBackgroundColor({ tabId, color: badge.color });
    await chrome.action.setBadgeText({ tabId, text: badge.text });
    setTimeout(() => chrome.action.setBadgeText({ tabId, text: '' }).catch(() => {}), 4000);
  }
  await tell(tabId, 'toast', { text: describe(result), ok: result.kind === 'ok' });
  if (result.kind === 'unpaired') chrome.runtime.openOptionsPage();
}

// Sends a video; pauses the tab's own player afterwards when the video came from it.
async function send(request, tabId, pauseTab) {
  if (!request) {
    await showResult(tabId, { kind: 'bad', status: 0, data: { error: 'Open a YouTube video first.' } });
    return;
  }
  const result = await playVideo(await settings(), request);
  if (result.kind === 'ok' && pauseTab) await tell(tabId, 'pause');
  await showResult(tabId, result);
}

// The tab's player knows the current second; the URL is the fallback.
async function fromTab(tab) {
  try {
    const state = await chrome.tabs.sendMessage(tab.id, { type: 'state' });
    if (state && state.videoId) return state;
  } catch { /* content script not loaded yet */ }
  return parseLink(tab.url || '');
}

chrome.action.onClicked.addListener(async tab => send(await fromTab(tab), tab.id, true));

chrome.contextMenus.onClicked.addListener(async (info, tab) => {
  if (info.menuItemId === 'link') await send(parseLink(info.linkUrl || ''), tab?.id, false);
  else if (tab) await send(await fromTab(tab), tab.id, true);
});

chrome.runtime.onMessage.addListener((message, sender) => {
  if (message && message.type === 'play' && sender.tab) send(message.request, sender.tab.id, true);
  return false;
});

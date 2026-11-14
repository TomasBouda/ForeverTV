// YouTube page: a "Play in WoW" button in the player bar, the current video and second for the toolbar button and
// context menu, pausing after a send, and a short toast with the result.
(() => {
  const BUTTON_CLASS = 'foreverscreen-button';
  const VIDEO_ID = /^[A-Za-z0-9_-]{11}$/;
  const PLAYLIST_ID = /^[A-Za-z0-9_-]{2,64}$/;

  function player() {
    const videos = [...document.querySelectorAll('video')];
    return videos.find(video => !video.paused) || document.querySelector('#movie_player video') || videos[0] || null;
  }

  // Same rules as parseSeconds in youtube.js (content scripts cannot import modules).
  function seconds(value) {
    if (!value) return 0;
    if (/^\d+$/.test(value)) return Number(value);
    const match = /^(?:(\d+)h)?(?:(\d+)m)?(?:(\d+)s?)?$/i.exec(value);
    if (!match || match[0] === '') return 0;
    return Number(match[1] || 0) * 3600 + Number(match[2] || 0) * 60 + Number(match[3] || 0);
  }

  function currentVideo() {
    const url = new URL(location.href);
    let videoId = null;
    if (url.pathname === '/watch') videoId = url.searchParams.get('v');
    else if (url.pathname.startsWith('/shorts/')) videoId = url.pathname.split('/')[2];
    if (!videoId || !VIDEO_ID.test(videoId)) return null;

    const video = player();
    // Not started yet (cookie dialog, autoplay off): fall back to the link's t= / start=.
    const played = video && Number.isFinite(video.currentTime) ? Math.floor(video.currentTime) : 0;
    const start = played > 0 ? played : seconds(url.searchParams.get('t') || url.searchParams.get('start'));
    const result = { videoId, start };
    const list = url.searchParams.get('list');
    if (list && PLAYLIST_ID.test(list)) result.playlistId = list;
    return result;
  }

  // YouTube's own button keeps its player state right; a bare video.pause() can be undone by the player.
  function pause() {
    const video = player();
    if (!video || video.paused) return;
    const button = document.querySelector('#movie_player .ytp-play-button');
    if (button && location.pathname === '/watch') button.click();
    setTimeout(() => { if (!video.paused) video.pause(); }, 300);
  }

  let toastTimer;
  function toast(text, ok) {
    let box = document.getElementById('foreverscreen-toast');
    if (!box) {
      box = document.createElement('div');
      box.id = 'foreverscreen-toast';
      box.setAttribute('role', 'status');
      document.body.appendChild(box);
    }
    box.textContent = text;
    box.classList.toggle('foreverscreen-toast-error', !ok);
    box.classList.add('foreverscreen-toast-show');
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => box.classList.remove('foreverscreen-toast-show'), ok ? 3500 : 7000);
  }

  function sendCurrent() {
    const request = currentVideo();
    if (!request) {
      toast('Open a YouTube video first.', false);
      return;
    }
    try {
      chrome.runtime.sendMessage({ type: 'play', request });
    } catch {
      // The extension was reloaded or updated; this page still runs the old script.
      toast('ForeverScreen extension was updated. Reload this page.', false);
    }
  }

  function ensureButton() {
    if (location.pathname !== '/watch') return;
    const controls = document.querySelector('#movie_player .ytp-right-controls');
    if (!controls || controls.querySelector('.' + BUTTON_CLASS)) return;

    const button = document.createElement('button');
    button.className = 'ytp-button ' + BUTTON_CLASS;
    button.type = 'button';
    button.title = 'Play in WoW';
    button.setAttribute('aria-label', 'Play in WoW');
    const icon = document.createElement('img');
    icon.src = chrome.runtime.getURL('icons/icon-32.png');
    icon.alt = '';
    button.appendChild(icon);
    button.addEventListener('click', event => {
      event.stopPropagation();
      sendCurrent();
    });
    controls.prepend(button);
  }

  chrome.runtime.onMessage.addListener((message, _sender, reply) => {
    if (!message) return false;
    if (message.type === 'state') reply(currentVideo());
    else if (message.type === 'pause') pause();
    else if (message.type === 'toast') toast(message.text, message.ok);
    return false;
  });

  // YouTube is a single-page app and rebuilds the player bar; check again after navigation and now and then.
  document.addEventListener('yt-navigate-finish', ensureButton);
  setInterval(ensureButton, 1500);
  ensureButton();
})();

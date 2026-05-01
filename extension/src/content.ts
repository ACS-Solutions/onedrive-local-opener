import { getConfig, shouldIntercept } from './config.js';

let cachedConfig = getConfig();

chrome.storage.onChanged.addListener(() => {
  cachedConfig = getConfig();
});

document.addEventListener(
  'click',
  async (event: MouseEvent) => {
    const anchor = (event.target as Element).closest('a') as HTMLAnchorElement | null;
    if (!anchor) return;

    const url = anchor.href;
    if (!url.match(/^https:\/\/[^/]*\.sharepoint\.com\//i)) return;

    const config = await cachedConfig;
    if (!shouldIntercept(url, config)) return;

    event.preventDefault();
    event.stopImmediatePropagation();

    const target = anchor.target;

    chrome.runtime.sendMessage({ action: 'openLocal', url }, (response: { opened: boolean } | undefined) => {
      if (!response?.opened) {
        window.open(url, target || '_self');
      }
    });
  },
  true,
);

import { getConfig } from './config.js';

const toggle = document.getElementById('enabled') as HTMLInputElement;
const optionsLink = document.getElementById('options') as HTMLAnchorElement;

getConfig().then((cfg) => { toggle.checked = cfg.enabled; });

toggle.addEventListener('change', () => {
  chrome.storage.sync.set({ enabled: toggle.checked });
});

optionsLink.addEventListener('click', (e) => {
  e.preventDefault();
  chrome.runtime.openOptionsPage();
});

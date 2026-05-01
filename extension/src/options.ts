import { Config, getConfig } from './config.js';

const toggle = document.getElementById('enabled') as HTMLInputElement;
const includeArea = document.getElementById('includeGlobs') as HTMLTextAreaElement;
const excludeArea = document.getElementById('excludeGlobs') as HTMLTextAreaElement;
const saveBtn = document.getElementById('save') as HTMLButtonElement;
const status = document.getElementById('status') as HTMLElement;

function showStatus(msg: string, isError = false) {
  status.textContent = msg;
  status.className = isError ? 'error' : 'saved';
  setTimeout(() => { status.textContent = ''; status.className = ''; }, 2500);
}

async function load() {
  const [managed, synced] = await Promise.all([
    chrome.storage.managed.get(null).catch(() => ({})) as Promise<Partial<Config>>,
    getConfig(),
  ]);

  toggle.checked = synced.enabled;
  includeArea.value = synced.includeGlobs.join('\n');
  excludeArea.value = synced.excludeGlobs.join('\n');

  const lock = (el: HTMLInputElement | HTMLTextAreaElement, labelEl: Element | null) => {
    el.disabled = true;
    labelEl?.insertAdjacentHTML('beforeend', ' <span class="managed">Managed by your organisation</span>');
  };

  if ('enabled' in managed) lock(toggle, document.querySelector('label[for=enabled]'));
  if ('includeGlobs' in managed) lock(includeArea, document.querySelector('label[for=includeGlobs]'));
  if ('excludeGlobs' in managed) lock(excludeArea, document.querySelector('label[for=excludeGlobs]'));
}

saveBtn.addEventListener('click', async () => {
  const config: Config = {
    enabled: toggle.checked,
    includeGlobs: includeArea.value.split('\n').map((s) => s.trim()).filter(Boolean),
    excludeGlobs: excludeArea.value.split('\n').map((s) => s.trim()).filter(Boolean),
  };
  if (config.includeGlobs.length === 0) config.includeGlobs = ['*'];

  try {
    await chrome.storage.sync.set(config);
    showStatus('Settings saved.');
  } catch {
    showStatus('Failed to save settings.', true);
  }
});

load();

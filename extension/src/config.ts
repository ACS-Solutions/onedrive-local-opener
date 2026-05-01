export interface Config {
  enabled: boolean;
  includeGlobs: string[];
  excludeGlobs: string[];
}

const DEFAULTS: Config = { enabled: true, includeGlobs: ['*'], excludeGlobs: [] };

export async function getConfig(): Promise<Config> {
  const [managed, synced] = await Promise.all([
    chrome.storage.managed.get(null).catch(() => ({})),
    chrome.storage.sync.get(DEFAULTS),
  ]);
  return { ...DEFAULTS, ...synced, ...managed } as Config;
}

export function matchesGlob(filename: string, pattern: string): boolean {
  const re = new RegExp(
    '^' +
      pattern
        .replace(/[.+^${}()|[\]\\]/g, '\\$&')
        .replace(/\*/g, '.*')
        .replace(/\?/g, '.') +
      '$',
    'i',
  );
  return re.test(filename);
}

export function shouldIntercept(url: string, config: Config): boolean {
  if (!config.enabled) return false;
  const filename = url.split('/').pop()?.split('?')[0] ?? '';
  const included = config.includeGlobs.some((g) => matchesGlob(filename, g));
  const excluded = config.excludeGlobs.some((g) => matchesGlob(filename, g));
  return included && !excluded;
}

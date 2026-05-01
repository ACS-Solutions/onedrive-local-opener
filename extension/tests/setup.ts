// Minimal Chrome extension API mock for Jest tests

const makeStorageArea = () => {
  const store: Record<string, unknown> = {};
  return {
    get: jest.fn(async (defaults?: Record<string, unknown>) => ({ ...defaults, ...store })),
    set: jest.fn(async (vals: Record<string, unknown>) => { Object.assign(store, vals); }),
    remove: jest.fn(async () => {}),
    clear: jest.fn(async () => {}),
  };
};

const listeners: Array<(...args: unknown[]) => unknown> = [];

global.chrome = {
  storage: {
    sync: makeStorageArea(),
    managed: makeStorageArea(),
    onChanged: {
      addListener: jest.fn(),
      removeListener: jest.fn(),
    },
  },
  runtime: {
    sendMessage: jest.fn(),
    sendNativeMessage: jest.fn(),
    openOptionsPage: jest.fn(),
    lastError: undefined,
    onMessage: {
      addListener: jest.fn((fn) => listeners.push(fn)),
      removeListener: jest.fn(),
    },
  },
} as unknown as typeof chrome;

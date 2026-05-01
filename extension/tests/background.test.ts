// background.ts registers its listener on import; we test by triggering it directly.

const EXPECTED_HOST = 'uk.co.acs_solutions.onedrive_local_opener';

describe('background service worker', () => {
  beforeEach(() => {
    jest.resetAllMocks();
    jest.isolateModules(() => {
      require('../src/background');
    });
  });

  function getLastListener() {
    const calls = (chrome.runtime.onMessage.addListener as jest.Mock).mock.calls;
    expect(calls.length).toBeGreaterThan(0);
    return calls[calls.length - 1][0] as (
      msg: { action: string; url: string },
      sender: object,
      sendResponse: (r: { opened: boolean }) => void,
    ) => boolean | void;
  }

  it('calls sendNativeMessage with the url from the message', () => {
    (chrome.runtime.sendNativeMessage as jest.Mock).mockImplementation((_host, _msg, cb) =>
      cb({ opened: true }),
    );

    const sendResponse = jest.fn();
    getLastListener()({ action: 'openLocal', url: 'https://example.sharepoint.com/file.msg' }, {}, sendResponse);

    expect(chrome.runtime.sendNativeMessage).toHaveBeenCalledWith(
      EXPECTED_HOST,
      { url: 'https://example.sharepoint.com/file.msg' },
      expect.any(Function),
    );
    expect(sendResponse).toHaveBeenCalledWith({ opened: true });
  });

  it('responds opened:false when native host returns opened:false', () => {
    (chrome.runtime.sendNativeMessage as jest.Mock).mockImplementation((_host, _msg, cb) =>
      cb({ opened: false }),
    );

    const sendResponse = jest.fn();
    getLastListener()({ action: 'openLocal', url: 'https://example.sharepoint.com/unsynced.msg' }, {}, sendResponse);

    expect(sendResponse).toHaveBeenCalledWith({ opened: false });
  });

  it('responds opened:false when native host errors', () => {
    (chrome.runtime.sendNativeMessage as jest.Mock).mockImplementation((_host, _msg, cb) => {
      Object.defineProperty(chrome.runtime, 'lastError', {
        value: { message: 'Host not found' },
        configurable: true,
      });
      cb(undefined);
      Object.defineProperty(chrome.runtime, 'lastError', { value: undefined, configurable: true });
    });

    const sendResponse = jest.fn();
    getLastListener()({ action: 'openLocal', url: 'https://example.sharepoint.com/file.msg' }, {}, sendResponse);

    expect(sendResponse).toHaveBeenCalledWith({ opened: false });
  });

  it('ignores messages with unknown action', () => {
    const sendResponse = jest.fn();
    getLastListener()({ action: 'somethingElse', url: 'https://example.sharepoint.com/file.msg' }, {}, sendResponse);

    expect(chrome.runtime.sendNativeMessage).not.toHaveBeenCalled();
    expect(sendResponse).not.toHaveBeenCalled();
  });
});

const HOST_NAME = 'uk.co.acs_solutions.onedrive_local_opener';

chrome.runtime.onMessage.addListener(
  (
    message: { action: string; url: string },
    _sender: chrome.runtime.MessageSender,
    sendResponse: (response: { opened: boolean }) => void,
  ) => {
    if (message.action !== 'openLocal') return false;

    chrome.runtime.sendNativeMessage(HOST_NAME, { url: message.url }, (response: { opened: boolean } | undefined) => {
      if (chrome.runtime.lastError) {
        sendResponse({ opened: false });
        return;
      }
      sendResponse(response ?? { opened: false });
    });

    return true; // keep channel open for async response
  },
);

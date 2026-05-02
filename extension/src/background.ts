const HOST_NAME = 'uk.co.acs_solutions.onedrive_local_opener';

function tryOpenNative(url: string): Promise<boolean> {
  return new Promise(resolve => {
    chrome.runtime.sendNativeMessage(HOST_NAME, { url }, (response: { opened: boolean } | undefined) => {
      resolve(!chrome.runtime.lastError && (response?.opened ?? false));
    });
  });
}

// Fallback for SharePoint downloads not covered by the declarativeNetRequest regex
// (e.g. URLs without a recognised file extension). The declarativeNetRequest rules + handler.ts
// handle the common case; this catches anything that slips through to a download.
chrome.downloads.onCreated.addListener(async (item) => {
  // Skip downloads that handler.ts intentionally let through via the bypass marker.
  if (item.url.includes('_odlo_bypass')) return;
  try { if (!new URL(item.url).hostname.endsWith('.sharepoint.com')) return; }
  catch { return; }

  const opened = await tryOpenNative(item.url);
  if (opened) {
    chrome.downloads.cancel(item.id, () => chrome.downloads.erase({ id: item.id }));
  }
});

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

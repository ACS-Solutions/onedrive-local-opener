// The declarativeNetRequest rule passes the original URL in the fragment.
const originalUrl = location.hash.slice(1);

if (!originalUrl) {
  window.close();
} else {
  chrome.runtime.sendMessage(
    { action: 'openLocal', url: originalUrl },
    (response: { opened: boolean } | undefined) => {
      const opened = !chrome.runtime.lastError && (response?.opened ?? false);

      if (!opened) {
        // Can't open locally — redirect back, bypassing the intercept rule so we don't loop.
        const sep = originalUrl.includes('?') ? '&' : '?';
        location.href = `${originalUrl}${sep}_odlo_bypass=1`;
        return;
      }

      chrome.tabs.getCurrent(tab => {
        if (tab?.id !== undefined) chrome.tabs.remove(tab.id);
        else window.close();
      });
    },
  );
}

# OneDrive Local Opener

A Chrome and Edge browser extension that intercepts SharePoint document links and opens the locally-synced copy in the file's registered application — instead of downloading it from the browser.

## The problem

When OneDrive syncs a SharePoint document library to your desktop, you get a local copy of every file. But clicking a SharePoint link in your browser, or following a hyperlink from Excel, Outlook, or another Office application, ignores the local copy entirely. The browser navigates to the SharePoint URL, SharePoint serves the file as a download, and you end up with a second copy sitting in your Downloads folder — opened in a second application window that has no connection to the synced file.

For organisations that rely heavily on SharePoint document libraries, this friction compounds quickly: `.msg` email attachments, `.docx` reports, `.xlsx` spreadsheets — every click produces a stale downloaded copy rather than opening the live, synced file.

## The solution

OneDrive Local Opener resolves SharePoint URLs to their local OneDrive path and opens the file directly, using the same registered application as if you had double-clicked it in Windows Explorer.

- A `.msg` file opens in Outlook — the real, synced copy.
- A `.docx` file opens in Word with full co-authoring context.
- A `.pdf` file opens in your configured PDF viewer.
- If a file is cloud-only (not yet downloaded), OneDrive hydrates it on demand — the same behaviour as opening it from Explorer.
- If no local copy exists (the file is not in a synced library), the browser falls back to the normal SharePoint download.

## Architecture

The solution has three components.

### Browser extension (Chrome / Edge MV3)

The extension is built to the Manifest V3 standard and works in both Google Chrome and Microsoft Edge.

**`declarativeNetRequest` rules** intercept top-level navigations to SharePoint document URLs before any network request is made. The browser never contacts SharePoint for matched URLs; it redirects immediately to the extension's handler page. This covers links opened from external applications such as Excel, Outlook, and other Office apps.

**A handler page** (`handler.html`) receives the original URL, asks the native messaging host whether a local copy exists, and either closes itself (file opened locally) or redirects the browser to the original SharePoint URL (file not synced locally, normal download proceeds).

**A content script** intercepts anchor-tag clicks on SharePoint pages themselves, preventing in-page navigation for document links and opening the local copy instead.

**A background service worker** handles native messaging calls from both the content script and the handler page, and acts as a fallback download canceller for any SharePoint download that reaches the browser (covering URL patterns not matched by the static rules).

### Native messaging host (.NET 10)

A small, self-contained Windows executable (`Host.exe`) that implements the [Chrome Native Messaging protocol](https://developer.chrome.com/docs/extensions/develop/concepts/native-messaging) over stdio.

When it receives a SharePoint URL it:

1. Reads the Windows registry (`HKCU\Software\SyncEngines\Providers\OneDrive`) to discover which SharePoint namespaces are synced locally and where.
2. Resolves the URL path to a local filesystem path.
3. Calls `ShellExecute` on the local path, which opens it in whatever application Windows has registered for that file type.

The host also handles its own registration (`Host.exe register`) and deregistration (`Host.exe unregister`), writing the native messaging manifest JSON and the required browser registry keys.

### MSI installer (WiX v5)

A single `.msi` installer built with WiX Toolset v5.

- Interactive UI (via `WixUI_Advanced`) lets the user choose between a **per-user install** (no elevation required, installs to `%LOCALAPPDATA%\Programs\OneDriveLocalOpener`) and a **per-machine install** (administrator rights required, installs to `%ProgramFiles%\ACS Solutions\OneDriveLocalOpener`).
- Silent deployment for corporate rollout:
  ```
  Per-user    — msiexec /i OneDriveLocalOpener.msi MSIINSTALLPERUSER=1 /quiet
  Per-machine — msiexec /i OneDriveLocalOpener.msi ALLUSERS=1 /quiet
  ```
- Post-install, the MSI runs `Host.exe register` to write the native messaging manifest and browser registry keys.
- Pre-uninstall, the MSI runs `Host.exe unregister` to clean up.
- For per-machine installs, the extension is force-installed into Chrome and Edge via the `ExtensionInstallForcelist` policy registry key, so end users receive it automatically without visiting the extension store.

## Repository layout

```
extension/          Chrome / Edge MV3 extension (TypeScript, esbuild)
host/               .NET 10 native messaging host
host.tests/         xUnit unit and integration tests for the host
installer/          WiX v5 MSI installer project
.github/workflows/  CI/CD pipeline (GitHub Actions)
```

## Building

**Extension**
```powershell
cd extension
npm install
npm run build
```

**Host** (produces a self-contained win-x64 executable)
```powershell
dotnet publish host/Host.csproj -c Release -r win-x64 --self-contained -o host/publish
```

**MSI** (requires the host to be published first)
```powershell
dotnet build installer/OneDriveLocalOpener.wixproj -c Release
```

**Tests**
```powershell
dotnet test host.tests/Host.Tests.csproj
cd extension && npm test
```

## Manual installation (development)

1. Build the extension (`npm run build`) and load the `extension/` folder as an unpacked extension in `edge://extensions/` or `chrome://extensions/`. Enable Developer mode first.
2. Publish and register the host:
   ```powershell
   cd host
   dotnet publish Host.csproj -c Release -r win-x64 --self-contained -o publish
   .\publish\Host.exe register --scope user
   ```
3. Reload the extension. The extension ID shown in the extensions page must match the `key` in `manifest.json` — it should be `pholklaheclnaflaniopejhmkikfmlel`.

## Licence and support

OneDrive Local Opener is released under the **MIT Licence** and is free to use, deploy, and modify — including within corporate environments.

If your organisation would like assistance with deployment, integration with your device management tooling, or ongoing support, **ACS Solutions Ltd** offers a straightforward installation and support agreement at a fixed, flat annual fee. Please get in touch at [alasdaircs@gmail.com](mailto:alasdaircs@gmail.com).

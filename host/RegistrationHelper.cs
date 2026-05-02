using System.Text.Json;
using Microsoft.Win32;

namespace OneDriveLocalOpener;

static class RegistrationHelper
{
    private const string HostName = "uk.co.acs_solutions.onedrive_local_opener";
    private const string ExtensionId = "pholklaheclnaflaniopejhmkikfmlel";

    private const string ChromeNmhKey = @"Software\Google\Chrome\NativeMessagingHosts\" + HostName;
    private const string EdgeNmhKey = @"Software\Microsoft\Edge\NativeMessagingHosts\" + HostName;
    private const string ChromeForcelist = @"Software\Policies\Google\Chrome\ExtensionInstallForcelist";
    private const string EdgeForcelist = @"Software\Policies\Microsoft\Edge\ExtensionInstallForcelist";

    private const string ChromeUpdateUrl = "https://clients2.google.com/service/update2/crx";
    private const string EdgeUpdateUrl = "https://edge.microsoft.com/extensionwebstorebase/v1/crx";

    public static void Register(string scope, string installDir)
    {
        var manifestPath = Path.Combine(installDir, "nmh-manifest.json");
        var hostPath = Path.Combine(installDir, "Host.exe");

        WriteManifest(manifestPath, hostPath);

        var hive = scope == "machine" ? Registry.LocalMachine : Registry.CurrentUser;
        WriteNmhRegistry(hive, manifestPath);
        // Force-install policy only applies to machine-scope; HKCU\Software\Policies is
        // managed by GPO infrastructure and is not writable by regular user processes.
        if (scope == "machine")
            WriteForcelistRegistry(hive);

        Console.Error.WriteLine($"Registered ({scope}) — manifest: {manifestPath}");
        if (scope == "user")
            Console.Error.WriteLine("Install the browser extension from the Chrome Web Store or Edge Add-ons to complete setup.");
    }

    public static void Unregister(string scope)
    {
        var hive = scope == "machine" ? Registry.LocalMachine : Registry.CurrentUser;

        DeleteKey(hive, ChromeNmhKey);
        DeleteKey(hive, EdgeNmhKey);
        if (scope == "machine")
        {
            RemoveForcelistEntry(hive, ChromeForcelist, ExtensionId);
            RemoveForcelistEntry(hive, EdgeForcelist, ExtensionId);
        }

        Console.Error.WriteLine($"Unregistered ({scope})");
    }

    private static void WriteManifest(string manifestPath, string hostPath)
    {
        var manifest = new
        {
            name = HostName,
            description = "Opens OneDrive-synced files locally",
            path = hostPath,
            type = "stdio",
            allowed_origins = new[] { $"chrome-extension://{ExtensionId}/" }
        };
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void WriteNmhRegistry(RegistryKey hive, string manifestPath)
    {
        using var chromeKey = hive.CreateSubKey(ChromeNmhKey, writable: true);
        chromeKey.SetValue(null, manifestPath);

        using var edgeKey = hive.CreateSubKey(EdgeNmhKey, writable: true);
        edgeKey.SetValue(null, manifestPath);
    }

    private static void WriteForcelistRegistry(RegistryKey hive)
    {
        WriteForcelistEntry(hive, ChromeForcelist, $"{ExtensionId};{ChromeUpdateUrl}");
        WriteForcelistEntry(hive, EdgeForcelist, $"{ExtensionId};{EdgeUpdateUrl}");
    }

    private static void WriteForcelistEntry(RegistryKey hive, string keyPath, string value)
    {
        using var key = hive.CreateSubKey(keyPath, writable: true);
        // Find first unused numeric value name
        var existing = key.GetValueNames();
        var n = 1;
        while (existing.Contains(n.ToString())) n++;
        key.SetValue(n.ToString(), value);
    }

    private static void DeleteKey(RegistryKey hive, string keyPath)
    {
        try { hive.DeleteSubKey(keyPath, throwOnMissingSubKey: false); }
        catch { /* best effort */ }
    }

    private static void RemoveForcelistEntry(RegistryKey hive, string keyPath, string extensionId)
    {
        try
        {
            using var key = hive.OpenSubKey(keyPath, writable: true);
            if (key is null) return;
            foreach (var name in key.GetValueNames())
            {
                var val = key.GetValue(name) as string;
                if (val?.StartsWith(extensionId, StringComparison.OrdinalIgnoreCase) == true)
                    key.DeleteValue(name);
            }
        }
        catch { /* best effort */ }
    }
}

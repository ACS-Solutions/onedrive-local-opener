using Microsoft.Win32;

namespace OneDriveLocalOpener;

public interface IRegistryProvider
{
    IEnumerable<(string UrlNamespace, string MountPoint)> GetProviderMappings();
}

sealed class WindowsRegistryProvider : IRegistryProvider
{
    public IEnumerable<(string, string)> GetProviderMappings()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\SyncEngines\Providers\OneDrive");
        if (key is null) yield break;

        foreach (var guidName in key.GetSubKeyNames())
        {
            using var sub = key.OpenSubKey(guidName);
            var ns = sub?.GetValue("UrlNamespace") as string;
            var mp = sub?.GetValue("MountPoint") as string;
            if (ns is not null && mp is not null)
                yield return (ns, mp);
        }
    }
}

public sealed class OneDriveMapper
{
    private readonly IRegistryProvider _registry;

    public OneDriveMapper(IRegistryProvider? registry = null)
    {
        _registry = registry ?? new WindowsRegistryProvider();
    }

    public string? TryResolveToLocalPath(string url)
    {
        Uri uri;
        try { uri = new Uri(url); }
        catch (UriFormatException) { return null; }

        // Decode once so we compare apples-to-apples regardless of whether the
        // registry UrlNamespace or the incoming URL uses %20 vs literal spaces.
        var decodedPath = Uri.UnescapeDataString(uri.GetLeftPart(UriPartial.Path));

        string? bestDecodedNs = null;
        string? bestMount = null;

        foreach (var (ns, mp) in _registry.GetProviderMappings())
        {
            var decodedNs = Uri.UnescapeDataString(ns).TrimEnd('/');
            if (decodedPath.StartsWith(decodedNs, StringComparison.OrdinalIgnoreCase)
                && (bestDecodedNs is null || decodedNs.Length > bestDecodedNs.Length))
            {
                bestDecodedNs = decodedNs;
                bestMount = mp;
            }
        }

        if (bestDecodedNs is null || bestMount is null) return null;

        var relativePart = decodedPath[bestDecodedNs.Length..]
                              .TrimStart('/')
                              .Replace('/', '\\');

        return Path.Combine(bestMount, relativePart);
    }
}

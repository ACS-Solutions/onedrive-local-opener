using System.Text.Json;
using OneDriveLocalOpener;

// Registration / unregistration mode (called by MSI installer)
if (args.Length > 0 && args[0] is "--register" or "--unregister")
{
    var scope = args.SkipWhile(a => !a.StartsWith("--scope="))
                    .Select(a => a["--scope=".Length..])
                    .FirstOrDefault() ?? "user";

    if (args[0] == "--register")
        RegistrationHelper.Register(scope, AppContext.BaseDirectory);
    else
        RegistrationHelper.Unregister(scope);

    return 0;
}

// Native messaging host loop
var mapper = new OneDriveMapper();
var stdin = Console.OpenStandardInput();
var stdout = Console.OpenStandardOutput();

while (true)
{
    string json;
    try { json = NativeMessaging.ReadMessage(stdin); }
    catch (EndOfStreamException) { break; } // browser closed the pipe

    JsonElement request;
    try { request = JsonSerializer.Deserialize<JsonElement>(json); }
    catch { NativeMessaging.WriteMessage(stdout, new { opened = false }); continue; }

    var url = request.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
    var localPath = url is not null ? mapper.TryResolveToLocalPath(url) : null;

    if (localPath is not null && File.Exists(localPath))
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = localPath,
            UseShellExecute = true
        });
        NativeMessaging.WriteMessage(stdout, new { opened = true });
    }
    else
    {
        NativeMessaging.WriteMessage(stdout, new { opened = false });
    }
}

return 0;

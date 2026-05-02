using System.CommandLine;
using System.Text.Json;
using OneDriveLocalOpener;

var logPath = Path.Combine( AppContext.BaseDirectory, "host-debug.log" );
void Log( string msg ) => File.AppendAllText( logPath, $"{DateTime.Now:o} {msg}{Environment.NewLine}" );

Log( $"Host starting, Args = {string.Join(" ", args)}" );

// Registration mode — called by the MSI installer or manually.
// Usage: Host.exe register   [--scope user|machine]
//        Host.exe unregister [--scope user|machine]
if (args.Length > 0 && (args[0] == "register" || args[0] == "unregister"))
{
    // Each command gets its own Option instance — options cannot be shared across commands.
    static Option<string> ScopeOption() =>
        new Option<string>("--scope")
        {
            Description = "Install scope: 'user' (HKCU, no elevation) or 'machine' (HKLM, admin required)",
            DefaultValueFactory = _ => "user"
        }.AcceptOnlyFromAmong("user", "machine");

    var registerScope = ScopeOption();
    var registerCmd = new Command("register", "Register the native messaging host and browser extensions");
    registerCmd.Add(registerScope);
    registerCmd.SetAction(r => RegistrationHelper.Register(r.GetValue(registerScope)!, AppContext.BaseDirectory));

    var unregisterScope = ScopeOption();
    var unregisterCmd = new Command("unregister", "Remove the native messaging host registration");
    unregisterCmd.Add(unregisterScope);
    unregisterCmd.SetAction(r => RegistrationHelper.Unregister(r.GetValue(unregisterScope)!));

    var rootCommand = new RootCommand("OneDrive Local Opener native messaging host");
    rootCommand.Add(registerCmd);
    rootCommand.Add(unregisterCmd);

    return rootCommand.Parse(args).Invoke(new());
}

// Native messaging host loop — no args means Chrome/Edge launched us via stdio.

try
{
    Log("Host started");
    var mapper = new OneDriveMapper();
    var stdin = Console.OpenStandardInput();
    var stdout = Console.OpenStandardOutput();
    Log("Entering message loop");

    while (true)
    {
        string json;
        try { json = NativeMessaging.ReadMessage(stdin); }
        catch (EndOfStreamException) { Log("stdin closed — exiting"); break; }

        Log($"Received: {json}");

        JsonElement request;
        try { request = JsonSerializer.Deserialize<JsonElement>(json); }
        catch (Exception ex)
        {
            Log($"JSON parse error: {ex.Message}");
            NativeMessaging.WriteMessage(stdout, new { opened = false });
            continue;
        }

        var url = request.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
        Log($"URL: {url}");
        var localPath = url is not null ? mapper.TryResolveToLocalPath(url) : null;
        Log($"Resolved: {localPath ?? "(null)"}");

        if (localPath is not null && File.Exists(localPath))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = localPath,
                UseShellExecute = true
            });
            Log("Opened");
            NativeMessaging.WriteMessage(stdout, new { opened = true });
        }
        else
        {
            Log("Not found or no mapping");
            NativeMessaging.WriteMessage(stdout, new { opened = false });
        }
    }
}
catch (Exception ex)
{
    Log($"FATAL: {ex}");
}

return 0;

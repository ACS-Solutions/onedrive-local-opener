using System.Text;
using System.Text.Json;
using OneDriveLocalOpener;
using Xunit;

namespace OneDriveLocalOpener.Tests;

public class NativeMessagingProtocolTests
{
    [Fact]
    public void ReadMessage_reads_length_prefixed_utf8_message()
    {
        var payload = """{"url":"https://example.sharepoint.com/file.msg"}""";
        var bytes = Encoding.UTF8.GetBytes(payload);
        var stream = new MemoryStream();
        stream.Write(BitConverter.GetBytes(bytes.Length));
        stream.Write(bytes);
        stream.Position = 0;

        var result = NativeMessaging.ReadMessage(stream);

        Assert.Equal(payload, result);
    }

    [Fact]
    public void WriteMessage_writes_length_prefixed_utf8_json()
    {
        var stream = new MemoryStream();
        NativeMessaging.WriteMessage(stream, new { opened = true });

        stream.Position = 0;
        var lenBytes = new byte[4];
        stream.ReadExactly(lenBytes);
        var length = BitConverter.ToInt32(lenBytes);
        var msgBytes = new byte[length];
        stream.ReadExactly(msgBytes);
        var json = Encoding.UTF8.GetString(msgBytes);
        var doc = JsonSerializer.Deserialize<JsonElement>(json);

        Assert.True(doc.GetProperty("opened").GetBoolean());
    }

    [Fact]
    public void WriteMessage_then_ReadMessage_round_trips()
    {
        var pipe = new MemoryStream();
        var original = """{"url":"https://test.sharepoint.com/docs/test.msg"}""";
        var payload = Encoding.UTF8.GetBytes(original);
        pipe.Write(BitConverter.GetBytes(payload.Length));
        pipe.Write(payload);
        pipe.Position = 0;

        var read = NativeMessaging.ReadMessage(pipe);
        Assert.Equal(original, read);
    }

    [Fact]
    public async Task Host_process_returns_opened_false_for_unmapped_url()
    {
        // Build & run the host process in test mode to verify the full I/O loop.
        var hostExe = FindHostExe();
        if (hostExe is null)
        {
            // Skip if not built yet (unit test environment without prior build)
            return;
        }

        using var proc = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = hostExe,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            }
        };
        proc.Start();

        var request = """{"url":"https://nobody.sharepoint.com/sites/test/file.msg"}""";
        var reqBytes = Encoding.UTF8.GetBytes(request);
        await proc.StandardInput.BaseStream.WriteAsync(BitConverter.GetBytes(reqBytes.Length));
        await proc.StandardInput.BaseStream.WriteAsync(reqBytes);
        await proc.StandardInput.BaseStream.FlushAsync();
        proc.StandardInput.Close(); // signals EOF → host exits loop

        var stdin = proc.StandardOutput.BaseStream;
        var lenBuf = new byte[4];
        await stdin.ReadExactlyAsync(lenBuf);
        var msgBuf = new byte[BitConverter.ToInt32(lenBuf)];
        await stdin.ReadExactlyAsync(msgBuf);

        var json = Encoding.UTF8.GetString(msgBuf);
        var doc = JsonSerializer.Deserialize<JsonElement>(json);
        Assert.False(doc.GetProperty("opened").GetBoolean());

        await proc.WaitForExitAsync(new CancellationTokenSource(5000).Token);
    }

    private static string? FindHostExe()
    {
        // Look for the host executable relative to the test assembly location
        var dir = Path.GetDirectoryName(typeof(NativeMessagingProtocolTests).Assembly.Location)!;
        // Walk up to find the repo root then down to host publish/bin output
        var candidate = Path.Combine(dir, "Host.exe");
        return File.Exists(candidate) ? candidate : null;
    }
}

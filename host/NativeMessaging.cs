using System.Text;
using System.Text.Json;

namespace OneDriveLocalOpener;

public static class NativeMessaging
{
    public static string ReadMessage(Stream stdin)
    {
        var lenBytes = new byte[4];
        stdin.ReadExactly(lenBytes);
        var msgBytes = new byte[BitConverter.ToInt32(lenBytes)];
        stdin.ReadExactly(msgBytes);
        return Encoding.UTF8.GetString(msgBytes);
    }

    public static void WriteMessage(Stream stdout, object response)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(response);
        stdout.Write(BitConverter.GetBytes(json.Length));
        stdout.Write(json);
        stdout.Flush();
    }
}

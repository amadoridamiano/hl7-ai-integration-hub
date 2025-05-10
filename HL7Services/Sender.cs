using System.Net.Sockets;
using System.Text;

namespace HL7Services;

public static class Sender
{
    /// <summary>
    /// Sends an HL7 message to a specified IP and port and read ACK response.
    /// </summary>
    /// <param name="ip"></param>
    /// <param name="port"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public static async Task<string> SendHl7MessageAsync(string ip, int port, string message)
    {
        // Start of Block
        const byte sb = 0x0B;
        // End of Block
        const byte eb1 = 0x1C;
        // Carriage Return
        const byte cr = 0x0D;

        var streamMessage = new MemoryStream();
        streamMessage.WriteByte(sb);
        streamMessage.Write(Encoding.ASCII.GetBytes(message));
        streamMessage.WriteByte(eb1);
        streamMessage.WriteByte(cr);

        using var client = new TcpClient(ip, port);
        var stream = client.GetStream();
        await stream.WriteAsync(streamMessage.ToArray().AsMemory(0, (int)streamMessage.Length));

        var buffer = new byte[1024];
        var bytesRead = await stream.ReadAsync(buffer);
        return Encoding.ASCII.GetString(buffer, 0, bytesRead);
    }
}
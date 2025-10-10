using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TictactoeWorker;

/// <summary>
/// Handles client connections and message processing
/// </summary>
public class ClientHandler
{
    private readonly RequestProcessor _requestProcessor;

    public ClientHandler(RequestProcessor requestProcessor)
    {
        _requestProcessor = requestProcessor;
    }

    /// <summary>
    /// Handle an incoming client connection
    /// </summary>
    /// <param name="client">The TCP client connection</param>
    public async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                var stream = client.GetStream();
                var buffer = new byte[4096];
                var messageBuilder = new StringBuilder();

                // Read data
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    messageBuilder.Append(message);

                    // Process complete messages
                    string fullMessage = messageBuilder.ToString();
                    if (fullMessage.EndsWith("\n"))
                    {
                        string[] messages = fullMessage.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var msg in messages)
                        {
                            if (!string.IsNullOrEmpty(msg))
                            {
                                Console.WriteLine($"Received from main server: {msg}");
                                var response = await _requestProcessor.ProcessMessageAsync(msg);
                                
                                // Send response back
                                var responseBytes = Encoding.UTF8.GetBytes(response + "\n");
                                await stream.WriteAsync(responseBytes);
                                Console.WriteLine($"Sent to main server: {response}");
                            }
                        }
                        messageBuilder.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
        }
    }
}
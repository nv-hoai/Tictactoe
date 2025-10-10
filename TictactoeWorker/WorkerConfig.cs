using System;
using System.Net;
using System.Threading.Tasks;

namespace TictactoeWorker;

/// <summary>
/// Configuration settings for the worker
/// </summary>
public class WorkerConfig
{
    // Default values
    public const int DEFAULT_PORT = 6000;
    public const string DEFAULT_SERVER_IP = "localhost";
    public const int DEFAULT_SERVER_PORT = 5000;

    public string Role { get; set; } = "Logic";
    public int Port { get; set; } = DEFAULT_PORT;
    public string LocalIp { get; set; } = "localhost";
    public string MainServerIp { get; set; } = DEFAULT_SERVER_IP;
    public int MainServerPort { get; set; } = DEFAULT_SERVER_PORT;
    public bool AutoRegister { get; set; } = false;

    /// <summary>
    /// Parse command line arguments to set configuration
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Populated WorkerConfig instance</returns>
    public static WorkerConfig ParseCommandLineArgs(string[] args)
    {
        var config = new WorkerConfig();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--port" && i + 1 < args.Length)
            {
                if (int.TryParse(args[i + 1], out int parsedPort))
                {
                    config.Port = parsedPort;
                }
            }
            else if (args[i] == "--role" && i + 1 < args.Length)
            {
                config.Role = args[i + 1];
            }
            else if (args[i] == "--server" && i + 1 < args.Length)
            {
                config.MainServerIp = args[i + 1];
            }
            else if (args[i] == "--serverport" && i + 1 < args.Length)
            {
                if (int.TryParse(args[i + 1], out int parsedPort))
                {
                    config.MainServerPort = parsedPort;
                }
            }
            else if (args[i] == "--autoregister")
            {
                config.AutoRegister = true;
            }
            else if (args[i] == "--ip" && i + 1 < args.Length)
            {
                config.LocalIp = args[i + 1];
            }
        }

        return config;
    }

    /// <summary>
    /// Try to detect the local IP address
    /// </summary>
    public async Task DetectLocalIpAsync()
    {
        if (LocalIp == "localhost")
        {
            try
            {
                var hostName = Dns.GetHostName();
                var addresses = await Dns.GetHostAddressesAsync(hostName);
                var ipv4 = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                if (ipv4 != null)
                {
                    LocalIp = ipv4.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to detect local IP: {ex.Message}. Using localhost.");
            }
        }
    }
}
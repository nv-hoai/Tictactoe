using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TictactoeWorker;

/// <summary>
/// Handles registration with the main server and heartbeat
/// </summary>
public class ServerRegistrationService
{
    private readonly WorkerConfig _config;
    private readonly StatisticsService _statisticsService;

    public ServerRegistrationService(WorkerConfig config, StatisticsService statisticsService)
    {
        _config = config;
        _statisticsService = statisticsService;
    }

    /// <summary>
    /// Start the registration process with the main server
    /// </summary>
    public void StartRegistrationProcess()
    {
        if (_config.AutoRegister && !string.IsNullOrEmpty(_config.MainServerIp))
        {
            Console.WriteLine($"Auto-registering with main server at {_config.MainServerIp}:{_config.MainServerPort}...");
            _ = Task.Run(() => RegisterWithMainServerAsync());
        }
    }

    /// <summary>
    /// Register with the main server and periodically send heartbeats
    /// </summary>
    private async Task RegisterWithMainServerAsync()
    {
        while (true)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(_config.MainServerIp, _config.MainServerPort);
                using var stream = client.GetStream();

                var request = new WorkerRequest
                {
                    RequestType = "REGISTER_WORKER",
                    RequestId = Guid.NewGuid().ToString(),
                    WorkerInfo = new WorkerInfo
                    {
                        Ip = _config.LocalIp,
                        Port = _config.Port,
                        Role = _config.Role,
                        CurrentLoad = _statisticsService.ConcurrentTasks
                    }
                };

                string requestJson = JsonSerializer.Serialize(request);
                byte[] requestData = Encoding.UTF8.GetBytes(requestJson + "\n");
                await stream.WriteAsync(requestData);
                
                Console.WriteLine($"Registered with main server at {_config.MainServerIp}:{_config.MainServerPort}");
                
                // Wait and re-register periodically to update stats
                await Task.Delay(TimeSpan.FromMinutes(5));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to register with main server: {ex.Message}");
                // Retry after delay
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }
    }
}
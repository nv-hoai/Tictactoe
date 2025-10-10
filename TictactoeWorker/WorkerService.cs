using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TictactoeWorker;

/// <summary>
/// Main worker service that manages the TCP server
/// </summary>
public class WorkerService
{
    private readonly WorkerConfig _config;
    private readonly StatisticsService _statisticsService;
    private readonly ServerRegistrationService _registrationService;
    private readonly RequestProcessor _requestProcessor;
    private readonly ClientHandler _clientHandler;

    public WorkerService(WorkerConfig config)
    {
        _config = config;
        _statisticsService = new StatisticsService();
        _requestProcessor = new RequestProcessor(_statisticsService, config.Role);
        _clientHandler = new ClientHandler(_requestProcessor);
        _registrationService = new ServerRegistrationService(config, _statisticsService);
    }

    /// <summary>
    /// Start the worker service
    /// </summary>
    public async Task StartAsync()
    {
        Console.WriteLine($"Starting Tic-tac-toe Worker Server with role: {_config.Role} on port: {_config.Port}, IP: {_config.LocalIp}");

        // Auto-register with main server if enabled
        _registrationService.StartRegistrationProcess();

        // Start periodic statistics reporting
        _ = Task.Run(ReportStatisticsPeriodicallyAsync);

        // Start TCP listener
        var tcpListener = new TcpListener(IPAddress.Any, _config.Port);
        tcpListener.Start();
        Console.WriteLine($"Worker listening on port {_config.Port} with role {_config.Role}");

        // Accept connections
        while (true)
        {
            try
            {
                var client = await tcpListener.AcceptTcpClientAsync();
                Console.WriteLine("Received connection from main server");

                // Handle client in background
                _ = Task.Run(() => _clientHandler.HandleClientAsync(client));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting connection: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Periodically report worker statistics
    /// </summary>
    private async Task ReportStatisticsPeriodicallyAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            Console.WriteLine(_statisticsService.GetStatisticsReport());
        }
    }
}
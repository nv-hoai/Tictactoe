using System;

namespace TictactoeWorker;

public class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            // Parse command line arguments
            var config = WorkerConfig.ParseCommandLineArgs(args);
            
            // Detect local IP if needed
            await config.DetectLocalIpAsync();

            // Create and start worker service
            var workerService = new WorkerService(config);
            await workerService.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}

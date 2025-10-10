
var server = new TicTacToeServer.TicTacToeServer(5000);

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    server.Stop();
};

try
{
    await server.StartAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Server error: {ex.Message}");
}

Console.WriteLine("Press any key to exit...");
Console.ReadKey();

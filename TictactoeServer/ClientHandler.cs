using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TicTacToeServer;

public class ClientHandler
{
    public string ClientId { get; set; }
    public TcpClient TcpClient { get; set; }
    public NetworkStream Stream { get; set; }
    public PlayerInfo PlayerInfo { get; set; }
    public GameRoom CurrentRoom { get; set; }
    public string PlayerSymbol { get; set; }
    public bool IsConnected { get; set; } = true;
    private readonly TicTacToeServer server;

    public ClientHandler(TcpClient tcpClient, TicTacToeServer server)
    {
        TcpClient = tcpClient;
        Stream = tcpClient.GetStream();
        ClientId = Guid.NewGuid().ToString();
        this.server = server;
    }

    public async Task HandleClientAsync()
    {
        byte[] buffer = new byte[4096];
        StringBuilder messageBuilder = new StringBuilder();

        try
        {
            while (IsConnected && TcpClient.Connected)
            {
                int bytesRead = await Stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                    break;

                string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                messageBuilder.Append(data);

                string messages = messageBuilder.ToString();
                string[] lines = messages.Split('\n');

                for (int i = 0; i < lines.Length - 1; i++)
                {
                    string message = lines[i].Trim();
                    if (!string.IsNullOrEmpty(message))
                    {
                        await ProcessMessage(message);
                    }
                }

                messageBuilder.Clear();
                if (lines.Length > 0)
                {
                    messageBuilder.Append(lines[lines.Length - 1]);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client {ClientId} error: {ex.Message}");
        }
        finally
        {
            await Disconnect();
        }
    }

    private async Task ProcessMessage(string message)
    {
        Console.WriteLine($"Client {ClientId}: {message}");

        try
        {
            // Allow workers to register on the same port using a JSON line
            if (message.Length > 0 && message[0] == '{')
            {
                try
                {
                    var req = JsonSerializer.Deserialize<WorkerRequest>(message, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (req != null && string.Equals(req.RequestType, "REGISTER_WORKER", StringComparison.OrdinalIgnoreCase))
                    {
                        if (req.WorkerInfo != null)
                        {
                            server.RegisterWorker(req.WorkerInfo.Ip, req.WorkerInfo.Port, req.WorkerInfo.Role);
                            Console.WriteLine($"Worker registered via client channel: {req.WorkerInfo.Ip}:{req.WorkerInfo.Port} role={req.WorkerInfo.Role}");
                        }
                        await SendMessage("WORKER_REGISTERED");
                        // Close connection after acknowledging registration
                        await Disconnect();
                        return;
                    }
                }
                catch
                {
                    // Not a worker registration payload, continue with normal client messaging
                }
            }

            if (message.StartsWith("PLAYER_INFO:"))
            {
                await HandlePlayerInfo(message.Substring("PLAYER_INFO:".Length));
            }
            else if (message.StartsWith("GAME_MOVE:"))
            {
                await HandleGameMove(message.Substring("GAME_MOVE:".Length));
            }
            else if (message == "FIND_MATCH")
            {
                await HandleFindMatch();
            }
            else if (message == "START_GAME")
            {
                await HandleStartGame();
            }
            else if (message == "LEAVE_MATCH")
            {
                await HandleLeaveMatch();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message from {ClientId}: {ex.Message}");
            await SendMessage($"ERROR:Failed to process message - {ex.Message}");
        }
    }

    private async Task HandlePlayerInfo(string json)
    {
        try
        {
            PlayerInfo = JsonSerializer.Deserialize<PlayerInfo>(json);
            Console.WriteLine($"Player {PlayerInfo.playerName} connected with ID {ClientId}");
            await SendMessage("PLAYER_INFO_ACK:Player info received");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse player info: {ex.Message}");
            await SendMessage("ERROR:Invalid player info format");
        }
    }

    private async Task HandleGameMove(string json)
    {
        if (CurrentRoom == null || !CurrentRoom.IsGameActive)
        {
            await SendMessage("ERROR:Not in an active game");
            return;
        }

        if (CurrentRoom.CurrentPlayer != PlayerSymbol)
        {
            await SendMessage("ERROR:Not your turn");
            return;
        }

        try
        {
            var moveData = JsonSerializer.Deserialize<MoveData>(json);

            if (await server.ProcessGameMove(CurrentRoom, this, moveData))
            {
                // Move was valid and processed
                var opponent = CurrentRoom.GetOpponent(this);
                if (opponent != null)
                {
                    await SendMessage($"GAME_MOVE:{json}");
                    await opponent.SendMessage($"GAME_MOVE:{json}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse game move: {ex.Message}");
            await SendMessage("ERROR:Invalid move format");
        }
    }

    private async Task HandleFindMatch()
    {
        if (PlayerInfo == null)
        {
            await SendMessage("ERROR:Send player info first");
            return;
        }

        var room = await server.FindOrCreateRoom(this);
        if (room != null)
        {
            CurrentRoom = room;
            await SendMessage($"JOIN_ROOM:{room.RoomId}");

            if (room.IsFull)
            {
                // Send the info so both players know about each other
                var opponent = room.GetOpponent(this);
                if (opponent != null)
                {
                    if (opponent.PlayerInfo != null)
                    {
                        string opponentJson = JsonSerializer.Serialize(opponent.PlayerInfo);
                        await SendMessage($"OPPONENT_INFO:{opponentJson}");
                    }

                    if (PlayerInfo != null)
                    {
                        string playerJson = JsonSerializer.Serialize(PlayerInfo);
                        await opponent.SendMessage($"OPPONENT_INFO:{playerJson}");
                    }

                    await SendMessage("MATCH_FOUND:Match found, ready to start");
                    await opponent.SendMessage("MATCH_FOUND:Match found, ready to start");
                }

            }
            else
            {
                await SendMessage("WAITING_FOR_OPPONENT:Waiting for another player");
            }
        }
        else
        {
            await SendMessage("ERROR:Failed to find or create match");
        }
    }

    private async Task HandleStartGame()
    {
        if (CurrentRoom == null)
        {
            await SendMessage("ERROR:Not in a room");
            return;
        }

        if (!CurrentRoom.IsFull)
        {
            await SendMessage("ERROR:Waiting for opponent");
            return;
        }

        // Mark this player as ready
        if (CurrentRoom.Player1 == this)
        {
            CurrentRoom.Player1Ready = true;
        }
        else if (CurrentRoom.Player2 == this)
        {
            CurrentRoom.Player2Ready = true;
        }

        await SendMessage("READY_ACK:You are ready to start");

        // Check if both players are ready
        if (CurrentRoom.BothPlayersReady)
        {
            await SendMessage("BOTH_READY:Both players are ready, starting game");
            var opponent = CurrentRoom.GetOpponent(this);
            if (opponent != null)
            {
                await opponent.SendMessage("BOTH_READY:Both players are ready, starting game");
            }
            await Task.Delay(1000);

            await server.StartGame(CurrentRoom);
        }
        else
        {
            await SendMessage("WAITING_FOR_OPPONENT_READY:Waiting for opponent to be ready");

            // Notify opponent that this player is ready
            var opponent = CurrentRoom.GetOpponent(this);
            if (opponent != null)
            {
                await opponent.SendMessage("OPPONENT_READY:Your opponent is ready to start");
            }
        }
    }

    private async Task HandleLeaveMatch()
    {
        if (CurrentRoom != null)
        {
            await server.LeaveRoom(this, CurrentRoom);
            CurrentRoom = null;
            await SendMessage("MATCH_LEFT:You left the match");
        }
    }

    public async Task SendMessage(string message)
    {
        if (!IsConnected || !TcpClient.Connected)
            return;

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            await Stream.WriteAsync(data, 0, data.Length);
            await Stream.FlushAsync();
            Console.WriteLine($"Sent to {ClientId}: {message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send message to {ClientId}: {ex.Message}");
            await Disconnect();
        }
    }

    public async Task Disconnect()
    {
        if (!IsConnected)
            return;

        IsConnected = false;

        if (CurrentRoom != null)
        {
            await server.LeaveRoom(this, CurrentRoom);
        }

        try
        {
            Stream?.Close();
            TcpClient?.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during disconnect for {ClientId}: {ex.Message}");
        }

        server.RemoveClient(this);
        Console.WriteLine($"Client {ClientId} disconnected");
    }
}

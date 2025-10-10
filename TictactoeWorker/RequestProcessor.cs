using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace TictactoeWorker;

/// <summary>
/// Processes incoming requests from the main server
/// </summary>
public class RequestProcessor
{
    private readonly StatisticsService _statisticsService;
    private readonly string _role;
    private readonly SemaphoreSlim _parallelGate; // Limit concurrency

    public RequestProcessor(StatisticsService statisticsService, string role)
    {
        _statisticsService = statisticsService;
        _role = role;
        _parallelGate = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
    }

    /// <summary>
    /// Process an incoming message from the main server
    /// </summary>
    /// <param name="message">The JSON message to process</param>
    /// <returns>A JSON response string</returns>
    public async Task<string> ProcessMessageAsync(string message)
    {
        var startTime = DateTime.UtcNow;
        var requestId = Guid.NewGuid().ToString();
        
        await _parallelGate.WaitAsync(); // Limit concurrency
        
        try
        {
            // Track concurrent tasks
            _statisticsService.IncrementConcurrentTasks();
            Console.WriteLine($"Current load: {_statisticsService.ConcurrentTasks} concurrent tasks");

            WorkerRequest? request = null;
            try
            {
                request = JsonSerializer.Deserialize<WorkerRequest>(message);
                if (request != null)
                {
                    requestId = request.RequestId;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to deserialize request: {ex.Message}");
                return JsonSerializer.Serialize(new WorkerResponse
                {
                    RequestId = requestId,
                    ResponseType = "ERROR",
                    IsSuccess = false,
                    ErrorMessage = "Failed to parse request"
                });
            }

            if (request == null)
            {
                return JsonSerializer.Serialize(new WorkerResponse
                {
                    RequestId = requestId,
                    ResponseType = "ERROR",
                    IsSuccess = false,
                    ErrorMessage = "Null request"
                });
            }

            Console.WriteLine($"[Worker:{_role}] Processing {request.RequestType} request (ID: {request.RequestId})");

            // Process based on request type
            string response;
            switch (request.RequestType)
            {
                case "AI_MOVE":
                    response = await ProcessAIMoveAsync(request);
                    break;
                case "NORMAL_MOVE":
                    response = await ProcessNormalMoveAsync(request);
                    break;
                case "REGISTER_WORKER":
                    response = ProcessRegisterWorker(request);
                    break;
                default:
                    response = JsonSerializer.Serialize(new WorkerResponse
                    {
                        RequestId = request.RequestId,
                        ResponseType = "ERROR",
                        IsSuccess = false,
                        ErrorMessage = $"Unknown request type: {request.RequestType}"
                    });
                    break;
            }

            // Track timing statistics
            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _statisticsService.RecordProcessingTime(elapsedMs);
            
            return response;
        }
        finally
        {
            _statisticsService.DecrementConcurrentTasks();
            _parallelGate.Release(); // Release concurrency limit
        }
    }

    /// <summary>
    /// Process an AI move request
    /// </summary>
    /// <param name="request">The AI move request</param>
    /// <returns>A JSON response string</returns>
    private async Task<string> ProcessAIMoveAsync(WorkerRequest request)
    {
        if (_role != "AI")
        {
            Console.WriteLine($"Warning: Non-AI worker ({_role}) processing AI_MOVE request");
        }

        // Simulate AI processing time based on board complexity
        int filledCells = 0;
        if (request.Board != null)
        {
            for (int i = 0; i < 15; i++)
            {
                for (int j = 0; j < 15; j++)
                {
                    if (!string.IsNullOrEmpty(request.Board[i, j]))
                    {
                        filledCells++;
                    }
                }
            }
        }

        // Simulate more complex AI thinking as the board gets fuller
        int processingTime = 200 + (filledCells * 5);
        await Task.Delay(processingTime);

        Console.WriteLine($"[Worker:{_role}] Processing AI move for game {request.GameId} " +
                        $"(board has {filledCells} filled cells, took {processingTime}ms)");

        // In a real implementation, we would call the AI logic here
        // For now, just generate a random valid move
        var move = GameUtils.GenerateRandomMove(request.Board ?? new string[15, 15]);

        return JsonSerializer.Serialize(new WorkerResponse
        {
            RequestId = request.RequestId,
            ResponseType = "AI_MOVE_RESULT",
            IsSuccess = true,
            Move = move
        });
    }

    /// <summary>
    /// Process a normal move validation request
    /// </summary>
    /// <param name="request">The move validation request</param>
    /// <returns>A JSON response string</returns>
    private async Task<string> ProcessNormalMoveAsync(WorkerRequest request)
    {
        // Simulate processing time
        await Task.Delay(100);

        Console.WriteLine($"[Worker:{_role}] Validating move for game {request.GameId}");

        if (request.LastMove == null)
        {
            return JsonSerializer.Serialize(new WorkerResponse
            {
                RequestId = request.RequestId,
                ResponseType = "MOVE_VALIDATION_RESULT",
                IsSuccess = false,
                ErrorMessage = "No move data provided"
            });
        }

        if (request.Board == null)
        {
            return JsonSerializer.Serialize(new WorkerResponse
            {
                RequestId = request.RequestId,
                ResponseType = "MOVE_VALIDATION_RESULT",
                IsSuccess = false,
                ErrorMessage = "No board data provided"
            });
        }

        bool isValid = GameLogic.IsValidMove(request.Board, request.LastMove.row, request.LastMove.col);
        
        if (!isValid)
        {
            return JsonSerializer.Serialize(new WorkerResponse
            {
                RequestId = request.RequestId,
                ResponseType = "MOVE_VALIDATION_RESULT",
                IsSuccess = false,
                ErrorMessage = "Invalid move"
            });
        }

        // Apply the move to check for win condition
        var boardCopy = (string[,])request.Board.Clone();
        boardCopy[request.LastMove.row, request.LastMove.col] = request.PlayerSymbol ?? "X";

        bool isWinningMove = GameLogic.CheckWin(boardCopy, request.LastMove.row, request.LastMove.col, request.PlayerSymbol ?? "X");

        return JsonSerializer.Serialize(new WorkerResponse
        {
            RequestId = request.RequestId,
            ResponseType = "MOVE_VALIDATION_RESULT",
            IsSuccess = true,
            Move = request.LastMove,
            IsWinningMove = isWinningMove
        });
    }

    /// <summary>
    /// Process a worker registration request
    /// </summary>
    /// <param name="request">The registration request</param>
    /// <returns>A JSON response string</returns>
    private string ProcessRegisterWorker(WorkerRequest request)
    {
        // This is just for echo confirmation if needed
        return JsonSerializer.Serialize(new WorkerResponse
        {
            RequestId = request.RequestId,
            ResponseType = "WORKER_REGISTERED",
            IsSuccess = true
        });
    }
}
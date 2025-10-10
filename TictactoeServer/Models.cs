namespace TicTacToeServer;

public class WorkerInfo
{
    public string Ip { get; set; } = "localhost";
    public int Port { get; set; }
    public string Role { get; set; } = "Logic"; // "AI" or "Logic"
    public int CurrentLoad { get; set; } = 0;
}

public class WorkerRequest
{
    public string RequestType { get; set; } = string.Empty; // AI_MOVE, NORMAL_MOVE, REGISTER_WORKER
    public string RequestId { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public string PlayerSymbol { get; set; } = string.Empty;
    public string[,] Board { get; set; } = new string[15, 15];
    public MoveData LastMove { get; set; } = new MoveData();
    public WorkerInfo WorkerInfo { get; set; } = new WorkerInfo();
}

public class WorkerResponse
{
    public string RequestId { get; set; } = string.Empty;
    public string ResponseType { get; set; } = string.Empty; // AI_MOVE_RESULT, MOVE_VALIDATION_RESULT
    public bool IsSuccess { get; set; } = true;
    public string ErrorMessage { get; set; } = string.Empty;
    public MoveData Move { get; set; } = new MoveData();
    public bool IsWinningMove { get; set; } = false;
}

public class MoveData
{
    public int row { get; set; }
    public int col { get; set; }
}
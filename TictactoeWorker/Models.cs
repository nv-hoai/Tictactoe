using System.Text.Json.Serialization;

namespace TictactoeWorker;

// Models shared between Server and Worker
public class MoveData
{
    public int row { get; set; }
    public int col { get; set; }
}

public class PlayerInfo
{
    public string playerName { get; set; } = string.Empty;
    public string avatarUrl { get; set; } = string.Empty;
}

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

// Utility class for game logic
public static class GameLogic
{
    public static bool CheckWin(string[,] board, int row, int col, string symbol)
    {
        // Horizontal check
        if (CheckDirection(board, row, col, 0, 1, symbol) + CheckDirection(board, row, col, 0, -1, symbol) >= 4)
            return true;
        
        // Vertical check
        if (CheckDirection(board, row, col, 1, 0, symbol) + CheckDirection(board, row, col, -1, 0, symbol) >= 4)
            return true;
        
        // Diagonal (top-left to bottom-right)
        if (CheckDirection(board, row, col, 1, 1, symbol) + CheckDirection(board, row, col, -1, -1, symbol) >= 4)
            return true;
        
        // Diagonal (top-right to bottom-left)
        if (CheckDirection(board, row, col, 1, -1, symbol) + CheckDirection(board, row, col, -1, 1, symbol) >= 4)
            return true;
        
        return false;
    }

    private static int CheckDirection(string[,] board, int startRow, int startCol, int rowDir, int colDir, string symbol)
    {
        int count = 0;
        int row = startRow + rowDir;
        int col = startCol + colDir;
        
        while (row >= 0 && row < 15 && col >= 0 && col < 15 && board[row, col] == symbol)
        {
            count++;
            row += rowDir;
            col += colDir;
        }
        
        return count;
    }

    public static bool IsBoardFull(string[,] board)
    {
        for (int i = 0; i < 15; i++)
        {
            for (int j = 0; j < 15; j++)
            {
                if (string.IsNullOrEmpty(board[i, j]))
                    return false;
            }
        }
        return true;
    }

    public static bool IsValidMove(string[,] board, int row, int col)
    {
        if (row < 0 || row >= 15 || col < 0 || col >= 15)
            return false;

        return string.IsNullOrEmpty(board[row, col]);
    }
}
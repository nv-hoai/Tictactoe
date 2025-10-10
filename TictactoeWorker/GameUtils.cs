using System;
using System.Collections.Generic;

namespace TictactoeWorker;

/// <summary>
/// Utility methods for game operations
/// </summary>
public static class GameUtils
{
    /// <summary>
    /// Generate a random valid move on the board
    /// </summary>
    /// <param name="board">The current game board</param>
    /// <returns>A valid MoveData</returns>
    public static MoveData GenerateRandomMove(string[,] board)
    {
        // Fast scan for empty cells
        var empties = new List<(int r, int c)>(225);
        for (int r = 0; r < 15; r++)
        {
            for (int c = 0; c < 15; c++)
            {
                if (string.IsNullOrEmpty(board[r, c]))
                {
                    empties.Add((r, c));
                }
            }
        }

        // If no empty cells, return an arbitrary position
        if (empties.Count == 0)
        {
            return new MoveData { row = 0, col = 0 };
        }

        // Pick a random empty cell
        var pick = Random.Shared.Next(empties.Count);
        var (row, col) = empties[pick];
        
        return new MoveData { row = row, col = col };
    }
}
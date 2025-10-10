using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TictactoeWorker;

/// <summary>
/// Utility service for tracking statistics and managing worker state
/// </summary>
public class StatisticsService
{
    private readonly ConcurrentQueue<double> _processingTimes = new();
    private int _totalRequestsProcessed = 0;
    private double _averageProcessingTime = 0;
    private int _concurrentTasks = 0;

    public int ConcurrentTasks => _concurrentTasks;
    public int TotalRequestsProcessed => _totalRequestsProcessed;
    public double AverageProcessingTime => _averageProcessingTime;

    /// <summary>
    /// Increment the concurrent tasks counter
    /// </summary>
    public void IncrementConcurrentTasks() => Interlocked.Increment(ref _concurrentTasks);

    /// <summary>
    /// Decrement the concurrent tasks counter
    /// </summary>
    public void DecrementConcurrentTasks() => Interlocked.Decrement(ref _concurrentTasks);

    /// <summary>
    /// Record processing time for a request
    /// </summary>
    /// <param name="processingTimeMs">Processing time in milliseconds</param>
    public void RecordProcessingTime(double processingTimeMs)
    {
        _processingTimes.Enqueue(processingTimeMs);
        Interlocked.Increment(ref _totalRequestsProcessed);

        // Keep only the last 100 processing times for average
        if (_processingTimes.Count > 100)
        {
            _processingTimes.TryDequeue(out _);
        }

        // Update average
        _averageProcessingTime = _processingTimes.Any() ? _processingTimes.Average() : 0;
    }

    /// <summary>
    /// Get a statistics report
    /// </summary>
    /// <returns>A formatted string with statistics</returns>
    public string GetStatisticsReport()
    {
        return $"===== WORKER STATISTICS =====\n" +
               $"Current load: {_concurrentTasks} concurrent tasks\n" +
               $"Total requests processed: {_totalRequestsProcessed}\n" +
               $"Average processing time: {_averageProcessingTime:F2}ms\n" +
               $"=============================";
    }
}
using System.Collections.Concurrent;
using System.Text.Json;
using EmbeddedCV.Core.Detection;

namespace EmbeddedCV.Core.Logging;
/* Logs per-frame detection results, timestamps, and processing time
 * Corresponds to FR-05.
 */ 

public class MetricsLogger
{
    private readonly ConcurrentBag<FrameResult> _frameResults = new();

    public void LogFrame(FrameResult frameResult)
    {
        _frameResults.Add(frameResult);
    }

    public IReadOnlyList<FrameResult> GetAllResults() => 
        _frameResults.OrderBy(f => f.FrameNumber).ToList();

    /*
     * Writes all logged frame results to a JSON file for inspection/reporting.
    */ 
    public void SaveToJson(string outputPath)
    {
        var json = JsonSerializer.Serialize(_frameResults, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputPath, json);
    }
}

using EmbeddedCV.Core.Detection;

namespace EmbeddedCV.Core.Reporting;

/*
 * A single NFR threshold check result, will be shown in the summary report.
*/ 
public class NfrCheckResult
{
    public string RequirementId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string ActualValue { get; set; } = string.Empty;
    public string Threshold { get; set; } = string.Empty;
}

/*
 * Complied results and NFR pass/fail summary for a completed test run.
 * Corresponds to FR-07.
*/

public class SummaryReport
{
    public int TotalFrames { get; set; }
    public int SkippedFrames { get; set; }
    public double AverageLatencyMs { get; set; }
    public double MaxLatencyMs { get; set; }
    public double MinLatencyMs { get; set; }
    public int TotalDetections { get; set; }
    public List<NfrCheckResult> NfrResults { get; set; } = new();
    public double? ColdStartLatencyMs { get; set; }
    public double PeakMemoryMb { get; set; }
}

/* Generates a summary report from logged frame results, including
 * pass/fail status against defined NFR thresholds
 * Corresponds to FR-07.
*/ 
public class SummaryReportGenerator
{
    // Thresholds correspond to the rewritten, testable NFRs from Task 2.
    private const double NFR01_MaxLatencyMs = 100.0;
    private const double NFR03_MaxMemoryMb = 512.0; 
    private const double NFR02_MinFps = 15.0;
    private const double NFR02_MinFrameRateComplianceRate = 0.95; //95% of frames must meet the minimum frame rate requirement

    public SummaryReport Generate(IReadOnlyList<FrameResult> frameResults, double? coldStartLatencyMs = null)
    {
        var processedFrames = frameResults.Where(f => !f.WasSkipped).ToList();
        var latencies = processedFrames.Select(f => f.ProcessingTimeMs).ToList();
        var peakMemoryBytes = processedFrames.Count > 0 ? processedFrames.Max(f => f.MemoryUsedBytes) : 0;
        var peakMemoryMb = peakMemoryBytes / (1024.0 * 1024.0);
        var framesFpsCompliant = latencies.Count(ms => (1000.0 / ms) >= NFR02_MinFps);
        var fpsComplianceRate = latencies.Count > 0 ? (double)framesFpsCompliant / latencies.Count : 0;

        

        var report = new SummaryReport
        {
            TotalFrames = frameResults.Count,
            SkippedFrames = frameResults.Count(f => f.WasSkipped),
            AverageLatencyMs = latencies.Count > 0 ? latencies.Average() : 0,
            MaxLatencyMs = latencies.Count > 0 ? latencies.Max() : 0,
            MinLatencyMs = latencies.Count > 0 ? latencies.Min() : 0,
            TotalDetections = processedFrames.Sum(f => f.Detections.Count),
            ColdStartLatencyMs = coldStartLatencyMs
        };

        //NFR-03: peak memory usage under 512MB
        report.NfrResults.Add(new NfrCheckResult
        {
            RequirementId = "NFR-03",
            Description = "Peak memory usage under 512MB during test runs",
            Passed = peakMemoryMb < NFR03_MaxMemoryMb,
            ActualValue = $"{peakMemoryMb:F2}MB",
            Threshold = $"<{NFR03_MaxMemoryMb}MB"
        });

        //NFR-01: Average frame processing time under 100ms
        report.NfrResults.Add(new NfrCheckResult
        {
            RequirementId = "NFR-01",
            Description = "Average frame processing time under 100ms",
            Passed = report.AverageLatencyMs < NFR01_MaxLatencyMs,
            ActualValue = $"{report.AverageLatencyMs:F2}ms",
            Threshold = $"<{NFR01_MaxLatencyMs}ms"
        });

        //NFR-09: cold-start (model load + first inference) latency, tracked separately
        if (coldStartLatencyMs.HasValue)
        {
            report.NfrResults.Add(new NfrCheckResult
            {
                RequirementId = "NFR-09",
                Description = "Cold-start latency (model load + first inference)",
                Passed = coldStartLatencyMs.Value < 500.0, //not placeholder anymore, data has been analysed
                ActualValue = $"{coldStartLatencyMs.Value:F2}ms",
                Threshold = "<500ms"
            });
        }

        //NFR-08: all defined error conditions handled without crashing
        report.NfrResults.Add(new NfrCheckResult
        {
            RequirementId = "NFR-08",
            Description = "All frames processed or skipped without crashing (runtime check - see TC-07/TC-09/TC-10 for per-condition validation)",
            Passed = true, //if we reach this point, the run completed without crashing
            ActualValue = $"{report.SkippedFrames} skipped / {report.TotalFrames} total",
            Threshold = "0 crashes"
        });

        //NFR-02: sustain min 15 FPS for at least 95% of frames
        //FPS per frame is derived as 1000/ processingTimeMs, so we can check how many frames meet the minimum FPS requirement

        report.NfrResults.Add(new NfrCheckResult
        {
            RequirementId = "NFR-02",
            Description = "At least 95% of frames sustain minimum 15 FPS",
            Passed = fpsComplianceRate >= NFR02_MinFrameRateComplianceRate,
            ActualValue = $"{fpsComplianceRate:P1} of frames >= 15 FPS",
            Threshold = $">={NFR02_MinFrameRateComplianceRate:P0} of frames >={NFR02_MinFps} FPS"
        });

        //NFR-04: 100% of frames complete without crashing under high-load
        report.NfrResults.Add(new NfrCheckResult
        {
            RequirementId = "NFR-04",
            Description = "100% of frames processed without crashing under high-load configs",
            Passed = report.SkippedFrames == 0,
            ActualValue = $"{report.TotalFrames - report.SkippedFrames}/{report.TotalFrames} completed",
            Threshold = "100% completion, 0 crashes"
        });

        report.PeakMemoryMb = peakMemoryMb;
        return report;
    }
}
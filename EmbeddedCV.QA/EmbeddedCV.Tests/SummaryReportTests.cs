using Microsoft.VisualStudio.TestTools.UnitTesting;
using EmbeddedCV.Core;
using EmbeddedCV.Core.Detection;
using EmbeddedCV.Core.Logging;
using EmbeddedCV.Core.Reporting;

namespace EmbeddedCV.Tests;

[TestClass]
public class SummaryReportTests
{
    private static string GetModelPath() =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "models", "yolov8n.onnx");
    private static string GetSampleImagePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "SampleData", fileName);

    [TestInitialize]
    public void ResetProcessorAffinity()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        long fullMask = (1L << Environment.ProcessorCount) - 1;
        process.ProcessorAffinity = (IntPtr)fullMask;
    }

    [TestMethod]
    public void Generate_AfterProcessingFrames_ProducesNfrPassFailSummary()
    {
        //Arrange
        using var detector = new OnnxYoloDetector(GetModelPath());

        var coldStartStopwatch = System.Diagnostics.Stopwatch.StartNew();
        detector.DetectFrame(GetSampleImagePath("bus.jpg"));
        coldStartStopwatch.Stop();
        var coldStartLatency = coldStartStopwatch.Elapsed.TotalMilliseconds;

        var logger = new MetricsLogger();
        var runner = new DetectionPipelineRunner(detector, logger);

        runner.ProcessFrames(GetSampleImagePath("bus.jpg"), 1);
        runner.ProcessFrames(GetSampleImagePath("zidane.jpg"), 2);

        //Act
        var reportGenerator = new SummaryReportGenerator();
        var report = reportGenerator.Generate(logger.GetAllResults(), coldStartLatency);

        //Assert
        Console.WriteLine($"Cold-start latency: {report.ColdStartLatencyMs:F2}ms");
        Console.WriteLine($"Total frames: {report.TotalFrames}, Skipped: {report.SkippedFrames}");
        Console.WriteLine($"Avg latency: {report.AverageLatencyMs:F2}ms, Max: {report.MaxLatencyMs:F2}ms, Min: {report.MinLatencyMs:F2}ms");
        Console.WriteLine($"Total detections: {report.TotalDetections}");

        foreach (var nfr in report.NfrResults)
        {
            Console.WriteLine($"{nfr.RequirementId}: {(nfr.Passed ? "Passed" : "Failed")} - {nfr.Description} (actual: {nfr.ActualValue}, threshold: {nfr.Threshold})");
        }

        Assert.AreEqual(2, report.TotalFrames);
        Assert.IsTrue(report.TotalDetections > 0);
    }


    //Uses a fixed, small sample set (bus.jpg/zidane.jpg repeated) for now. Future improvement (See Task 10 recommendations
    //Will allow users to upload their own dataset for bigger, better NFR Testing. Not doing it for this phase due to
    //time constraints and because it isnt required by Assesment 2 tasks.
    [TestMethod]
    public void Generate_WithLargerFrameBatch_EvaluatesNfr02FrameRateCompliance()
    {
        using var detector = new OnnxYoloDetector(GetModelPath());
        detector.DetectFrame(GetSampleImagePath("bus.jpg")); // Warm-up

        var logger = new MetricsLogger();
        var runner = new DetectionPipelineRunner(detector, logger);

        var imagePaths = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            imagePaths.Add(GetSampleImagePath("bus.jpg"));
            imagePaths.Add(GetSampleImagePath("zidane.jpg"));
        }

        runner.ProcessBatch(imagePaths, EmbeddedCV.Core.Constraints.LoadCondition.Baseline);

        foreach (var result in logger.GetAllResults())
        {
            var fps = 1000.0 / result.ProcessingTimeMs;
            Console.WriteLine($"Frame {result.FrameNumber}: {result.ProcessingTimeMs:F2}ms ({fps:F1} FPS)");
        }

        var reportGenerator = new SummaryReportGenerator();
        var report = reportGenerator.Generate(logger.GetAllResults());

        var nfr02 = report.NfrResults.First(n => n.RequirementId == "NFR-02");
        Console.WriteLine($"NFR-02: {(nfr02.Passed ? "Passed" : "Failed")} - {nfr02.Description} (actual: {nfr02.ActualValue}, threshold: {nfr02.Threshold})");

        Assert.AreEqual(20, report.TotalFrames);
    }
}

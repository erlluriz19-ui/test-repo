using Microsoft.VisualStudio.TestTools.UnitTesting;
using EmbeddedCV.Core;
using EmbeddedCV.Core.Detection;
using EmbeddedCV.Core.Logging;
using EmbeddedCV.Core.Constraints;

namespace EmbeddedCV.Tests;

[TestClass]
public class LoadConditionTests
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
    public void ProcessBatch_UnderBaselineAndHighLoad_BothCompleteAllFrames()
    {
        //Arrange
        using var detector = new OnnxYoloDetector(GetModelPath());
        detector.DetectFrame(GetSampleImagePath("bus.jpg")); //warm up

        var imagePath = new List<string>
        {
            GetSampleImagePath("bus.jpg"),
            GetSampleImagePath("zidane.jpg"),
            GetSampleImagePath("bus.jpg"),
            GetSampleImagePath("zidane.jpg")
        };

        //Baseline run
        var baselineLogger = new MetricsLogger();
        var baselineRunner = new DetectionPipelineRunner(detector, baselineLogger);
        var baselineStopwatch = System.Diagnostics.Stopwatch.StartNew();
        baselineRunner.ProcessBatch(imagePath, LoadCondition.Baseline);
        baselineStopwatch.Stop();

        //High load run
        var highLoadLogger = new MetricsLogger();
        var highLoadRunner = new DetectionPipelineRunner(detector, highLoadLogger);
        var highLoadStopwatch = System.Diagnostics.Stopwatch.StartNew();
        highLoadRunner.ProcessBatch(imagePath, LoadCondition.HighLoad);
        highLoadStopwatch.Stop();

        //Assert
        var baselineResults = baselineLogger.GetAllResults();
        var highLoadResults = highLoadLogger.GetAllResults();

        Console.WriteLine($"Baseline total wall time: {baselineStopwatch.Elapsed.TotalMilliseconds:F2}ms, frames: {baselineResults.Count}");
        Console.WriteLine($"Highload total wall time: {highLoadStopwatch.Elapsed.TotalMilliseconds:F2}ms, frames: {highLoadResults.Count}");

        Assert.AreEqual(imagePath.Count, baselineResults.Count);
        Assert.AreEqual(imagePath.Count, highLoadResults.Count);
        Assert.IsTrue(baselineResults.All(r => !r.WasSkipped));
        Assert.IsTrue(highLoadResults.All(r => !r.WasSkipped));
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using EmbeddedCV.Core;
using EmbeddedCV.Core.Detection;
using EmbeddedCV.Core.Logging;

namespace EmbeddedCV.Tests;

[TestClass]
public class PipelineRunnerTests
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
    public void ProcessFrame_OnMultipleImages_LogsResultsAndSavesJson()
    {
        //Arrange
        using var detector = new OnnxYoloDetector(GetModelPath());
        var logger = new MetricsLogger();
        var runner = new DetectionPipelineRunner(detector, logger);

        //Act
        runner.ProcessFrames(GetSampleImagePath("bus.jpg"), frameNumber: 1);
        runner.ProcessFrames(GetSampleImagePath("zidane.jpg"), frameNumber: 2);

        var outputPath = Path.Combine(AppContext.BaseDirectory, "test-run-output.json");
        logger.SaveToJson(outputPath);

        //Assert
        var results = logger.GetAllResults();
        Assert.AreEqual(2, results.Count);
        Assert.IsFalse(results[0].WasSkipped);
        Assert.IsTrue(results[0].ProcessingTimeMs > 0);
        Assert.IsTrue(results[0].Detections.Count > 0);

        Assert.IsTrue(File.Exists(outputPath), "Expected JSON output file to be created");

        var jsonContent = File.ReadAllText(outputPath);
        Console.WriteLine(jsonContent);
        Assert.IsTrue(jsonContent.Contains("bus"), "Expected bus label in logged results");
    }

    [TestMethod]
    public void SaveToJson_AfterProcessingFrames_ProducesValidParsableJson()
    {
        //Arrange
        using var detector = new OnnxYoloDetector(GetModelPath());
        var logger = new MetricsLogger();
        var runner = new DetectionPipelineRunner(detector, logger);

        runner.ProcessFrames(GetSampleImagePath("bus.jpg"), 1);

        var outputPath = Path.Combine(AppContext.BaseDirectory, "json-validity-test-output.json");

        //Act
        logger.SaveToJson(outputPath);

        //Assert
        Assert.IsTrue(File.Exists(outputPath));

        var jsonContent = File.ReadAllText(outputPath);
        List<FrameResult>? parsed = null;
        
        try
        {
            parsed = System.Text.Json.JsonSerializer.Deserialize<List<FrameResult>>(jsonContent);
        }
        catch (System.Text.Json.JsonException ex)
        {
            Assert.Fail($"JSON output was not valid/parsable: {ex.Message}");
        }

        Assert.IsNotNull(parsed);
        Assert.AreEqual(1, parsed!.Count);
        Assert.AreEqual(1, parsed[0].FrameNumber);

        File.Delete(outputPath); // Clean up after test
    }
}

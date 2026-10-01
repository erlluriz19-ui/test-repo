using Microsoft.VisualStudio.TestTools.UnitTesting;
using EmbeddedCV.Core.Detection;

namespace EmbeddedCV.Tests;

[TestClass]
public class DetectionTests
{
    private static string GetModelPath() =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "models", "yolov8n.onnx");

    private static string GetSampleImagePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "SampleData", fileName);

    [TestMethod]
    public void DetectFrame_OnBusImage_ReturnsDetections()
    {
        //Arrange
        var modelPath = GetModelPath();
        var imagePath = GetSampleImagePath("bus.jpg");
        using var detector = new OnnxYoloDetector(modelPath);

        //Act
        var results = detector.DetectFrame(imagePath);

        //Assert
        Assert.IsNotNull(results);
        Assert.IsTrue(results.Count > 0, "Expected at least one detection in bus.jpg");

        foreach (var r in results)
        {
            Console.WriteLine($"{r.Label} - {r.Confidence:P1} at ({r.X},{r.Y}) {r.Width}x{r.Height}");
        }
    }

    [TestMethod]
    public void DetectFrame_RunMultipleTimes_ProducesConsistentDetectionCounts()
    {
        using var detector = new OnnxYoloDetector(GetModelPath());
        var imagePath = GetSampleImagePath("bus.jpg");
        detector.DetectFrame(imagePath); // Warm-up

        var detectionCounts = new List<int>();
        for (int i = 0; i < 5; i++)
        {
            var results = detector.DetectFrame(imagePath);
            detectionCounts.Add(results.Count);
        }

        Console.WriteLine($"Detection counts across 5 runs: {string.Join(", ", detectionCounts)}");

        var maxCount = detectionCounts.Max();
        var minCount = detectionCounts.Min();
        var variance = maxCount > 0 ? (double)(maxCount - minCount) / maxCount : 0;

        Console.WriteLine($"Variance: {variance:P1}");
        Assert.IsTrue(variance <= 0.02, $"Expected detection count variance within 2%, got {variance:P1}");
    }
}
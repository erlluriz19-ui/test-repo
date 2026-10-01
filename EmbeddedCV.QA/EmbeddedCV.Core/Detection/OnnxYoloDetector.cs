using SkiaSharp;
using YoloDotNet;
using YoloDotNet.Models;
using YoloDotNet.ExecutionProvider.Cpu;

namespace EmbeddedCV.Core.Detection;

/* Wraps YoloDotNet to do object detection using a YOLOv8 ONNX model. 
 * Corresponds to FR-01, FR-02, FR-03.
 */

public class OnnxYoloDetector : IDetector, IDisposable
{
    private readonly Yolo _yolo;

    public OnnxYoloDetector(string modelPath)
    {
        if (!File.Exists(modelPath))
        {
            //Corresponds to FR-09: stop and log error if model cant be loaded
            throw new FileNotFoundException($"YOLO model not found at path: {modelPath}");
        }
        try
        {
            _yolo = new Yolo(new YoloOptions
            {
                // FIX: Use the correct CPU execution provider for YoloDotNet
                ExecutionProvider = new CpuExecutionProvider(modelPath)
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load YOLO model from '{modelPath}'. File may be corrupt or incompatible. Test run stopped. Details: {ex.Message}", ex);
        }
    }

    public List<DetectionResult> DetectFrame(string imagePath)
    {
        using var image = SKBitmap.Decode(imagePath);
        var results = _yolo.RunObjectDetection(image);
        return results.Select(r => new DetectionResult
        {
            Label = r.Label.Name,
            Confidence = (float)r.Confidence,
            X = r.BoundingBox.Left,
            Y = r.BoundingBox.Top,
            Width = r.BoundingBox.Width,
            Height = r.BoundingBox.Height
        }).ToList();
    }

    public void Dispose()
    {
        _yolo.Dispose();
    }
}


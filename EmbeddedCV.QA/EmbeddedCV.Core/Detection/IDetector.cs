namespace EmbeddedCV.Core.Detection;
/* Abstraction over the detection engine so the pipeline and tests
 * dont depend directly on YoloDotNet/ONNX Runtime specifics
*/ 

public interface IDetector
{
    List<DetectionResult> DetectFrame(string imagePath);
}

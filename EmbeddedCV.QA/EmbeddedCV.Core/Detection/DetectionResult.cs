namespace EmbeddedCV.Core.Detection;

    /* Shows a single detected object within a processed frame
     * Corresponds to FR-03: bounding box, class label, and confidence score
    */

   public class DetectionResult
    {
        public string Label { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
    }
    /* Shows all detections and timing/metadata for a single processed frame
     * Corresponds to FR-05: per-frame timestamp and detection results
    */
    public class FrameResult
    {
        public int FrameNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public double ProcessingTimeMs { get; set; }
        public List<DetectionResult> Detections { get; set; } = new();
        public bool WasSkipped { get; set; } = false;
        public string? SkippedReason { get; set; }
        public long MemoryUsedBytes { get; set; } 
}


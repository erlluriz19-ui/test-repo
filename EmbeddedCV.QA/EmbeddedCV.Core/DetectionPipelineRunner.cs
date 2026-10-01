using System.Diagnostics;
using EmbeddedCV.Core.Detection;
using EmbeddedCV.Core.Logging;
using EmbeddedCV.Core.Constraints;

namespace EmbeddedCV.Core;
/*
 * Runs detection on a sequence of frames, measuring per-frame processing time and logging results.
 * Corresponds to FR-04, FR-05.
 */

public class DetectionPipelineRunner
{
    private readonly IDetector _detector;
    private readonly MetricsLogger _logger;
    private readonly ResourceConstraintSimulator? _constraintSimulator;

    public DetectionPipelineRunner(IDetector detector, MetricsLogger logger, ResourceConstraintSimulator? constraintSimulator = null)
    {
        _detector = detector;
        _logger = logger;
        _constraintSimulator = constraintSimulator;
    }

    public static void ValidateInputPaths(IReadOnlyList<string> imagePath)
    {
        if (imagePath == null || imagePath.Count == 0)
        {
            throw new ArgumentException("No input frames provided. Test run stopped before starting.");
        }
        var missing = imagePath.Where(p => !File.Exists(p)).ToList();
        if (missing.Count > 0)
        {
            throw new FileNotFoundException($"Test run rejected: {missing.Count} input file(s) not found: {string.Join(", ", missing)}");
        }
    }

    public void ProcessFrames(string imagePath, int frameNumber)
    {
        var stopwatch = Stopwatch.StartNew();
        var frameResult = new FrameResult
        {
            FrameNumber = frameNumber,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            frameResult.Detections = _detector.DetectFrame(imagePath);
            _constraintSimulator?.ApplySimulatedLatency();
            frameResult.MemoryUsedBytes = Process.GetCurrentProcess().WorkingSet64; //gives total process memory, not just the operation alloction
        }
        catch (Exception ex)
        {
            //Corresponds to FR-10: skip corrupted/unreadable frames, log failure, continue.
            frameResult.WasSkipped = true;
            frameResult.SkippedReason = ex.Message;
        }

        stopwatch.Stop();
        frameResult.ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds;

        _logger.LogFrame(frameResult);
    }

    /*
     * Processes a batch of frames under a given load condition
     * Baseline: sequential. Highload: concurrent, simulating high traffic
    */
    public void ProcessBatch(IReadOnlyList<string> imagePath, LoadCondition loadCondition)
    {
        ValidateInputPaths(imagePath);
        if (loadCondition == LoadCondition.Baseline)
        {
            for (int i = 0; i < imagePath.Count; i++)
            {
                ProcessFrames(imagePath[i], frameNumber: i + 1);
            }
        }
        else //High load
        {
            Parallel.For(0, imagePath.Count, i =>
            {
                ProcessFrames(imagePath[i], frameNumber: i + 1);
            });
        }
    }
}

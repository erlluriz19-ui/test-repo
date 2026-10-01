using EmbeddedCV.Core;
using EmbeddedCV.Core.Detection;
using EmbeddedCV.Core.Logging;
using EmbeddedCV.Core.Constraints;
using EmbeddedCV.Core.Reporting;

namespace EmbeddedCV.MAUI;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnRunClicked(object sender, EventArgs e)
    {
        RunButton.IsEnabled = false;
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        StatusLabel.Text = "Running...";
        ResultsLabel.Text = "";

        try
        {
            await Task.Run(() => RunTestScenario());
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
        }
        finally
        {
            RunButton.IsEnabled = true;
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private void RunTestScenario()
    {
        var modelPath = Path.Combine(AppContext.BaseDirectory, "Assets", "models", "yolov8n.onnx");
        var sampleDir = Path.Combine(AppContext.BaseDirectory, "Assets", "sample-data");

        var imagePath = new List<string>
        {
            Path.Combine(sampleDir, "bus.jpg"),
            Path.Combine(sampleDir, "zidane.jpg")
        };

        using var detector = new OnnxYoloDetector(modelPath);
        detector.DetectFrame(imagePath[0]); //warm up

        var logger = new MetricsLogger();

        var constraintProfile = ConstraintPicker.SelectedIndex == 1
            ? ResourceConstraintProfile.Constrained
            : ResourceConstraintProfile.Baseline;

        var simulator = new ResourceConstraintSimulator();
        simulator.Apply(constraintProfile);

        var loadCondition = LoadConditionPicker.SelectedIndex == 1
            ? LoadCondition.HighLoad
            : LoadCondition.Baseline;

        var runner = new DetectionPipelineRunner(detector, logger, simulator);
        runner.ProcessBatch(imagePath, loadCondition);

        var reportGenerator = new SummaryReportGenerator();
        var report = reportGenerator.Generate(logger.GetAllResults());

        var summary = $"Profile: {constraintProfile.Name} | Load: {loadCondition}\n\n" +
                     $"Frames: {report.TotalFrames} (Skipped: {report.SkippedFrames})\n" +
                     $"Avg latency: {report.AverageLatencyMs:F2}ms\n" +
                     $"Max latency: {report.MaxLatencyMs:F2}ms\n" +
                     $"Min latency: {report.MinLatencyMs:F2}ms\n" +
                     $"Total detections: {report.TotalDetections}\n\n" +
                     "NFR Results:\n" +
                     string.Join("\n", report.NfrResults.Select(n =>
                         $"  {n.RequirementId}: {(n.Passed ? "PASS" : "FAIL")} — {n.Description} (actual: {n.ActualValue}, threshold: {n.Threshold})"));
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = "Run complete.";
            ResultsLabel.Text = summary;
        });
    }
}
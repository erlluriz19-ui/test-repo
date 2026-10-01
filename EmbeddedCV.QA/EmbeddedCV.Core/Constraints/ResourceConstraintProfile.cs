using System.Diagnostics;

namespace EmbeddedCV.Core.Constraints;
/*
 * Defines a named resource constraint profile used to simulate
 * embedded/constrained hardware conditions in software.
 * Corresponds to FR-06.
*/ 

public class ResourceConstraintProfile
{
    public string Name { get; set; } = string.Empty;
    public int CpuCoreLimit { get; set; } = Environment.ProcessorCount;
    public int SimulatedLatencyMs { get; set; } = 0;

    public static ResourceConstraintProfile Baseline => new()
    {
        Name = "Baseline",
        CpuCoreLimit = Environment.ProcessorCount,
        SimulatedLatencyMs = 0
    };

    public static ResourceConstraintProfile Constrained => new()
    {
        Name = "Constrained (Embedded-like)",
        CpuCoreLimit = 1,
        SimulatedLatencyMs = 50
    };
}

/*
 * Applies a resource constraint profile to the current process to
 * simulate running on limited embedded hardware.
 * Corresponds to FR-06, NFR-07 (portability across simulated hardware profiles)
*/ 

public class ResourceConstraintSimulator
{
    private ResourceConstraintProfile? _activeProfile;
    public void Apply(ResourceConstraintProfile profile)
    {
        _activeProfile = profile;

        try
        {
            var process = Process.GetCurrentProcess();
            //Limit which CPU cores the process is allowed to use,
            //simulating a low-power embedded CPU.
            long mask = (1L << profile.CpuCoreLimit) - 1;
            process.ProcessorAffinity = (IntPtr)mask;
        }
        catch (Exception ex)
        {
            //Processor affinity isnt supported on all platforms/permission levels
            //this allows it to fail gracefully rather than crashing the whole run
            Console.WriteLine($"Warning: could not apply CPU affinity constraint: {ex.Message}");
        }
    }

    /*
     * Applies the active profiles simulated latency, if any, to emulate
     * slower per-frame processing on constrained hardware
    */
    public void ApplySimulatedLatency()
    {
        if (_activeProfile?.SimulatedLatencyMs > 0)
        {
            Thread.Sleep(_activeProfile.SimulatedLatencyMs);
        }
    }

    public ResourceConstraintProfile? ActiveProfile => _activeProfile;
}

namespace EmbeddedCV.Core.Constraints;

/*
 * Defines a load condition controlling how many frames are processed
 * at the same time during a test run. Corresponds to FR-08.
*/

public enum LoadCondition
{
    Baseline,  //frames processed sequentially, one at a time
    HighLoad   //frames processed concurrently, simulating big load
}

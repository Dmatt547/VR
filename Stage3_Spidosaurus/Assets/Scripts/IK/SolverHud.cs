using UnityEngine;
using TMPro;

/// <summary>
/// Draws the solver's live measurements onto a world-space label so the numbers appear
/// in Play-mode screenshots rather than only in the console.
/// </summary>
public class SolverHud : MonoBehaviour
{
    public BranchedFabrikSolver solver;
    public GaitStub gait;
    public TMP_Text label;

    float worstSeen;

    void Update()
    {
        if (solver == null || label == null) return;
        worstSeen = Mathf.Max(worstSeen, solver.maxPlantedErrorMm);

        label.text =
            $"planted error: {solver.maxPlantedErrorMm:F1} mm  (worst {worstSeen:F1})\n" +
            $"threshold:     {solver.contactToleranceMm:F0} mm\n" +
            $"iterations:    {solver.iterations} (fixed)\n" +
            $"separations:   {solver.separationsThisFrame}\n" +
            $"repairs:       {solver.repairsThisFrame}\n" +
            $"frame:         {Time.unscaledDeltaTime * 1000f:F2} ms";
    }
}

using MechJebLib.PSG;
using UnityEngine;

namespace MechJebLibBindings.PSG
{
    public static class FlightPlanExtensions
    {
        public static void DrawStageGUI(this FlightPlan flightPlan)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            foreach (FlightPlanStage stage in flightPlan.Stages)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{stage.KSPStage,3} {stage.DeltaV:##,###0} m/s");
                if (stage.Unguided)
                    GUILayout.Label(" (unguided)");
                if (!stage.AllowShutdown)
                    GUILayout.Label(" (fixed)");
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }
    }
}

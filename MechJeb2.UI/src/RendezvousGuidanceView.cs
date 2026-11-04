using KodeKit;
using KodeUI;
using KSP.Localization;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleRendezvousGuidance ("Rendezvous Planner") in KodeKit.
    /// The maneuver planning actions are internal methods of the module.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleRendezvousGuidance))]
    public class RendezvousGuidanceView : KodeView
    {
        readonly MechJebModuleRendezvousGuidance guidance;

        public RendezvousGuidanceView(MechJebModuleRendezvousGuidance guidance) : base(guidance) => this.guidance = guidance;

        protected override float MinWidth => 300;

        MechJebCore Core => guidance.Core;

        int Mode() => GuidanceUi.RendezvousMode(Core, guidance.Orbit);

        int NodeCount => guidance.Vessel.patchedConicSolver != null ? guidance.Vessel.patchedConicSolver.maneuverNodes.Count : 0;

        // computed once per frame, the readouts below all use it
        double closestApproachTime;

        protected override void BeforeSync()
        {
            if (Mode() == 2) closestApproachTime = guidance.Orbit.MinimumSeparationTime(Core.Target.TargetOrbit, guidance.VesselState.Time);
        }

        // a burn that can't be computed says so, where it would otherwise only reach the log
        static System.Action Plan(System.Action plan) => () =>
        {
            try
            {
                plan();
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogException(e);
                ScreenMessages.PostScreenMessage(Localizer.Format("#MechJeb_RZplan_failed", e.Message), 5);
            }
        };

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            //"Select a target to rendezvous with.", "Rendezvous target must be in the same sphere of influence."
            Layout main = GuidanceUi.RendezvousGate(w, r, Mode, "#MechJeb_RZplan_label1", "#MechJeb_RZplan_label2");
            System.Func<bool> on = () => Mode() == 2;

            UIText ground = GuidanceUi.Warning(w, main, "#MechJeb_RZplan_onGround"); //"The target or this vessel is on the ground: these maneuvers need both in orbit."
            w.Visible(ground.gameObject, () => Mode() == 2 && guidance.OnTheGround);

            //Information readouts:
            GuidanceUi.Pair(w, main, "#MechJeb_RZplan_label3", "Kerbin Station Alpha Mk2", ct => ct.Append(Core.Target.Name), on); //"Rendezvous target"
            GuidanceUi.Pair(w, main, "#MechJeb_RZplan_label4", "-1.23 Mm x 123 Gm", ct => //"Target orbit"
                ct.AppendSI(Core.Target.TargetOrbit.PeA, 3).Append("m x ").AppendSI(Core.Target.TargetOrbit.ApA, 3).Append('m'), on);
            GuidanceUi.Pair(w, main, "#MechJeb_RZplan_label5", "-1.23 Mm x 123 Gm", ct => //"Current orbit"
                ct.AppendSI(guidance.Orbit.PeA, 3).Append("m x ").AppendSI(guidance.Orbit.ApA, 3).Append('m'), on);
            GuidanceUi.Pair(w, main, "#MechJeb_RZplan_label6", "-180.00°", ct => //"Relative inclination"
                ct.Append(guidance.Orbit.RelativeInclination(Core.Target.TargetOrbit), 2).Append('°'), on);
            GuidanceUi.Pair(w, main, "#MechJeb_RZplan_label7", "123y 123d 12h 12m 12s", ct => //"Time until closest approach"
                ct.AppendDHMS(closestApproachTime - guidance.VesselState.Time, 0, GuidanceUi.HoursPerDay, GuidanceUi.DaysPerYear), on);
            GuidanceUi.Pair(w, main, "#MechJeb_RZplan_label8", "1234 Mm", ct => //"Separation at closest approach"
                ct.AppendSI(guidance.Orbit.Separation(Core.Target.TargetOrbit, closestApproachTime)).Append('m'), on);
            GuidanceUi.Pair(w, main, "#MechJeb_RZplan_label10", "1234 km/s", ct => //"Relative speed at closest approach"
                ct.AppendSI(OrbitalManeuverCalculator.DeltaVToMatchVelocities(guidance.Orbit, closestApproachTime, Core.Target.TargetOrbit).magnitude)
                    .Append("m/s"), on);

            //Maneuver planning buttons:
            KodeWindow.Button(main, "#MechJeb_RZplan_button1", Plan(guidance.AlignPlanes)); //"Align Planes"

            Layout orbit = KodeWindow.Row(main);
            KodeWindow.Button(orbit, "#MechJeb_RZplan_button2", Plan(guidance.EstablishNewOrbit), true); //"Establish new orbit at"
            EditFields.Field(w, orbit, () => guidance.phasingOrbitAltitude.Text, s => guidance.phasingOrbitAltitude.Text = s, 70,
                () => guidance.phasingOrbitAltitude.IsParsed);
            KodeWindow.Label(orbit, "km");

            UIText unsafeOrbit = GuidanceUi.Warning(w, main, "#MechJeb_RZplan_unsafeOrbit"); //"That altitude meets the atmosphere, the terrain, a moon or the edge of the sphere of influence."
            w.Visible(unsafeOrbit.gameObject, () => Mode() == 2 && !guidance.PhasingOrbitIsSafe);

            KodeWindow.Button(main, "#MechJeb_RZplan_button3", Plan(guidance.InterceptWithHohmann)); //"Intercept with Hohmann transfer"
            KodeWindow.Button(main, "#MechJeb_RZplan_button4", Plan(guidance.MatchVelocitiesAtClosestApproach)); //"Match velocities at closest approach"
            KodeWindow.Button(main, "#MechJeb_RZplan_button5", Plan(guidance.GetCloser)); //"Get closer"
            KodeWindow.Button(main, "#MechJeb_RZplan_button9", () => guidance.Vessel.RemoveAllManeuverNodes(), ButtonRole.Danger); //Remove ALL nodes

            Layout node = KodeWindow.Panel(main);
            w.Visible(node.gameObject, () => Core.Node != null);
            NodeUi.ExecuteButtons(w, node, Core, guidance, "#MechJeb_RZplan_button6", "#MechJeb_RZplan_button7", "#MechJeb_RZplan_button8",
                () => NodeCount > 0, () => NodeCount > 1, false); //"Execute next node", "Execute all nodes", "Abort node execution"
            NodeUi.AutowarpToggle(w, node, Core, "#MechJeb_RZplan_checkbox"); //"Auto-warp"
        }
    }
}

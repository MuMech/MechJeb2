using KodeKit;
using KodeUI;
using KSP.Localization;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleRendezvousGuidance ("Rendezvous Planner") in KodeKit, row for row with its WindowGUI.
    /// The maneuver planning actions are internal methods of the module, shared with the IMGUI window.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleRendezvousGuidance))]
    public class RendezvousGuidanceView : KodeView
    {
        readonly MechJebModuleRendezvousGuidance guidance;

        public RendezvousGuidanceView(MechJebModuleRendezvousGuidance guidance) : base(guidance) => this.guidance = guidance;

        protected override float MinWidth => 300;

        MechJebCore Core => guidance.Core;

        // 0 = no target, 1 = target in another sphere of influence, 2 = usable target
        int Mode()
        {
            if (Core.Target == null || !Core.Target.NormalTargetExists) return 0;
            return Core.Target.TargetOrbit.referenceBody != guidance.Orbit.referenceBody ? 1 : 2;
        }

        int NodeCount => guidance.Vessel.patchedConicSolver != null ? guidance.Vessel.patchedConicSolver.maneuverNodes.Count : 0;

        // computed once per frame, the readouts below all use it
        double closestApproachTime;

        protected override void BeforeSync()
        {
            if (Mode() == 2) closestApproachTime = guidance.Orbit.NextClosestApproachTime(Core.Target.TargetOrbit, guidance.VesselState.Time);
        }

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            Layout none = KodeWindow.Panel(r);
            GuidanceUi.Wrapped(none, "#MechJeb_RZplan_label1"); //"Select a target to rendezvous with."
            w.Visible(none.gameObject, () => Mode() == 0);

            Layout other = KodeWindow.Panel(r);
            GuidanceUi.Wrapped(other, "#MechJeb_RZplan_label2"); //"Rendezvous target must be in the same sphere of influence."
            w.Visible(other.gameObject, () => Mode() == 1);

            Layout main = KodeWindow.Panel(r);
            w.Visible(main.gameObject, () => Mode() == 2);
            System.Func<bool> on = () => Mode() == 2;

            //Information readouts:
            GuidanceUi.Pair(w, main, "#MechJeb_RZplan_label3", "Kerbin Station Alpha Mk2", ct => ct.Append(Core.Target.Name), on); //"Rendezvous target"
            GuidanceUi.Pair(w, main, "#MechJeb_RZplan_label4", "-1.23 Mm x 123 Gm", ct => //"Target orbit"
                ct.AppendSI(Core.Target.TargetOrbit.PeA, 3).Append("m x ").AppendSI(Core.Target.TargetOrbit.ApA, 3).Append('m'), on);
            GuidanceUi.Pair(w, main, "#MechJeb_RZplan_label5", "-1.23 Mm x 123 Gm", ct => //"Current orbit"
                ct.AppendSI(guidance.Orbit.PeA, 3).Append("m x ").AppendSI(guidance.Orbit.ApA, 3).Append('m'), on);
            GuidanceUi.Pair(w, main, "#MechJeb_RZplan_label6", "-180.00º", ct => //"Relative inclination"
                ct.Append(guidance.Orbit.RelativeInclination(Core.Target.TargetOrbit), 2).Append('º'), on);
            GuidanceUi.Pair(w, main, "#MechJeb_RZplan_label7", "123y 123d 12h 12m 12s", ct => //"Time until closest approach"
                ct.AppendDHMS(closestApproachTime - guidance.VesselState.Time, 0, GuidanceUi.HoursPerDay, GuidanceUi.DaysPerYear), on);
            GuidanceUi.Pair(w, main, "#MechJeb_RZplan_label8", "1234 Mm", ct => //"Separation at closest approach"
                ct.AppendSI(guidance.Orbit.Separation(Core.Target.TargetOrbit, closestApproachTime)).Append('m'), on);
            GuidanceUi.Pair(w, main, "#MechJeb_RZplan_label10", "1234 km/s", ct => //"Relative speed at closest approach"
                ct.AppendSI(OrbitalManeuverCalculator.DeltaVToMatchVelocities(guidance.Orbit, closestApproachTime, Core.Target.TargetOrbit).magnitude)
                    .Append("m/s"), on);

            //Maneuver planning buttons:
            KodeWindow.Button(main, "#MechJeb_RZplan_button1", guidance.AlignPlanes); //"Align Planes"

            Layout orbit = KodeWindow.Row(main);
            KodeWindow.Button(orbit, "#MechJeb_RZplan_button2", guidance.EstablishNewOrbit, true); //"Establish new orbit at"
            EditFields.Field(w, orbit, () => guidance.phasingOrbitAltitude.Text, s => guidance.phasingOrbitAltitude.Text = s, 70,
                () => guidance.phasingOrbitAltitude.IsParsed);
            KodeWindow.Label(orbit, "km");

            KodeWindow.Button(main, "#MechJeb_RZplan_button3", guidance.InterceptWithHohmann); //"Intercept with Hohmann transfer"
            KodeWindow.Button(main, "#MechJeb_RZplan_button4", guidance.MatchVelocitiesAtClosestApproach); //"Match velocities at closest approach"
            KodeWindow.Button(main, "#MechJeb_RZplan_button5", guidance.GetCloser); //"Get closer"
            KodeWindow.Button(main, "#MechJeb_RZplan_button9", () => guidance.Vessel.RemoveAllManeuverNodes(), KodeWindow.ModernLayout ? ButtonRole.Danger : ButtonRole.Normal); //Remove ALL nodes

            Layout node = KodeWindow.Panel(main);
            w.Visible(node.gameObject, () => Core.Node != null);
            UIButton execOne = KodeWindow.Button(node, "#MechJeb_RZplan_button6", () => Core.Node.ExecuteOneNode(guidance), KodeWindow.ModernLayout ? ButtonRole.Primary : ButtonRole.Normal); //"Execute next node"
            w.Visible(execOne.gameObject, () => Core.Node != null && NodeCount > 0 && !Core.Node.Enabled);
            UIButton execAll = KodeWindow.Button(node, "#MechJeb_RZplan_button7", () => Core.Node.ExecuteAllNodes(guidance)); //"Execute all nodes"
            w.Visible(execAll.gameObject, () => Core.Node != null && NodeCount > 1 && !Core.Node.Enabled);
            UIButton abort = KodeWindow.Button(node, "#MechJeb_RZplan_button8", () => Core.Node.Abort(), KodeWindow.ModernLayout ? ButtonRole.Danger : ButtonRole.Normal); //"Abort node execution"
            w.Visible(abort.gameObject, () => Core.Node != null && Core.Node.Enabled);
            w.Toggle(node, "#MechJeb_RZplan_checkbox", () => Core.Node != null && Core.Node.Autowarp, v => //"Auto-warp"
            {
                if (Core.Node != null) Core.Node.Autowarp = v;
            });
        }
    }
}

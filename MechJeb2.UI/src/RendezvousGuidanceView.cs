using KodeKit;
using KodeUI;
using KSP.Localization;
using UnityEngine;

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

        static bool Landed(Vessel v) => v != null && (v.LandedOrSplashed || v.situation == Vessel.Situations.PRELAUNCH);

        // "on the ground" in place of an orbit that is only the ground track of a landed vessel
        static void AppendOrbit(CharText ct, Orbit o, Vessel v, string onGround)
        {
            if (Landed(v)) ct.Append(onGround);
            else ct.AppendSI(o.PeA, 3).Append("m x ").AppendSI(o.ApA, 3).Append('m');
        }

        readonly GuidanceUi.TargetName targetName = new GuidanceUi.TargetName();

        // the readouts below all use it; the search marches a whole orbit, so it runs five times a second, and at once for
        // another target or when the approach it found has passed (time warp)
        double closestApproachTime;
        float nextClosestApproach;
        Orbit closestApproachTarget;

        protected override void BeforeSync()
        {
            if (Mode() != 2) return;
            Orbit target = Core.Target.TargetOrbit;
            double now = guidance.VesselState.Time;
            if (target == closestApproachTarget && Time.unscaledTime < nextClosestApproach && closestApproachTime >= now) return;
            closestApproachTarget = target;
            nextClosestApproach = Time.unscaledTime + 0.2f;
            closestApproachTime = guidance.Orbit.MinimumSeparationTime(target, now);
        }

        // a burn that can't be planned says why; one that can't be computed says so, where it would otherwise only reach the log
        static System.Action Plan(System.Func<string> plan) => () =>
        {
            try
            {
                string problem = plan();
                if (problem != null) ScreenMessages.PostScreenMessage(problem, 5);
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
            GuidanceUi.Pair(w, main, "#MechJeb_RZplan_label3", "Kerbin Station Alpha Mk2", ct => ct.Append(targetName.Get(Core.Target)), on); //"Rendezvous target"
            string onGround = Localizer.Format("#MechJeb_NewUI_OnTheGround"); //on the ground
            GuidanceUi.Pair(w, main, "#MechJeb_RZplan_label4", "-1.23 Mm x 123 Gm", ct => //"Target orbit"
                AppendOrbit(ct, Core.Target.TargetOrbit, Core.Target.Target?.GetVessel(), onGround), on);
            GuidanceUi.Pair(w, main, "#MechJeb_RZplan_label5", "-1.23 Mm x 123 Gm", ct => //"Current orbit"
                AppendOrbit(ct, guidance.Orbit, guidance.Vessel, onGround), on);
            GuidanceUi.Pair(w, main, "#MechJeb_RZplan_label6", "-180.00°", ct => //"Relative inclination"
                ct.Append(guidance.Orbit.RelativeInclination(Core.Target.TargetOrbit), 2).Append('°'), on);
            GuidanceUi.Pair(w, main, "#MechJeb_RZplan_label7", "123y 123d 12h 12m 12s", ct => //"Time until closest approach"
                ct.AppendDHMS(closestApproachTime - guidance.VesselState.Time, 0), on);
            GuidanceUi.Pair(w, main, "#MechJeb_RZplan_label8", "1234 Mm", ct => //"Separation at closest approach"
                ct.AppendSI(guidance.Orbit.Separation(Core.Target.TargetOrbit, closestApproachTime)).Append('m'), on);
            GuidanceUi.Pair(w, main, "#MechJeb_RZplan_label10", "1234 km/s", ct => //"Relative speed at closest approach"
                ct.AppendSI(OrbitalManeuverCalculator.DeltaVToMatchVelocities(guidance.Orbit, closestApproachTime, Core.Target.TargetOrbit).magnitude)
                    .Append("m/s"), on);

            // the planning buttons, off while a vessel is on the ground (the warning above says so)
            KodeWindow.Header(main, "#MechJeb_NewUI_PlanAManeuver"); //Plan a maneuver
            bool CanPlan() => !guidance.OnTheGround;
            GuidanceUi.Enable(w, KodeWindow.Button(main, "#MechJeb_RZplan_button1", Plan(guidance.AlignPlanes)), CanPlan); //"Align Planes"

            Layout orbit = KodeWindow.Row(main);
            GuidanceUi.Enable(w, KodeWindow.Button(orbit, "#MechJeb_RZplan_button2", Plan(guidance.EstablishNewOrbit), true), CanPlan); //"Establish new orbit at"
            EditFields.Field(w, orbit, () => guidance.phasingOrbitAltitude.Text, s => guidance.phasingOrbitAltitude.Text = s, 70,
                () => guidance.phasingOrbitAltitude.IsParsed);
            KodeWindow.Label(orbit, "km");

            UIText unsafeOrbit = GuidanceUi.Warning(w, main, "#MechJeb_RZplan_unsafeOrbit"); //"That altitude meets the atmosphere, the terrain, a moon or the edge of the sphere of influence."
            w.Visible(unsafeOrbit.gameObject, () => Mode() == 2 && !guidance.PhasingOrbitIsSafe);

            GuidanceUi.Enable(w, KodeWindow.Button(main, "#MechJeb_RZplan_button3", Plan(guidance.InterceptWithHohmann)), CanPlan); //"Intercept with Hohmann transfer"
            GuidanceUi.Enable(w, KodeWindow.Button(main, "#MechJeb_RZplan_button4", Plan(guidance.MatchVelocitiesAtClosestApproach)), CanPlan); //"Match velocities at closest approach"
            GuidanceUi.Enable(w, KodeWindow.Button(main, "#MechJeb_RZplan_button5", Plan(guidance.GetCloser)), CanPlan); //"Get closer"

            // the executor; Remove ALL nodes beside it, small and off without nodes
            Layout node = KodeWindow.Panel(main);
            w.Visible(node.gameObject, () => Core.Node != null);
            KodeWindow.Header(node, "#MechJeb_NewUI_Execute"); //Execute
            Layout execRow = KodeWindow.Row(node);
            NodeUi.ExecuteButtons(w, execRow, Core, guidance, "#MechJeb_RZplan_button6", "#MechJeb_RZplan_button7", "#MechJeb_RZplan_button8",
                () => NodeCount > 0, () => NodeCount > 1, true); //"Execute next node", "Execute all nodes", "Abort node execution"
            UIButton removeAll = KodeWindow.Button(execRow, "#MechJeb_RZplan_button9", () => guidance.Vessel.RemoveAllManeuverNodes(), ButtonRole.Danger); //Remove ALL nodes
            GuidanceUi.Enable(w, removeAll, () => NodeCount > 0);
            NodeUi.AutowarpToggle(w, node, Core, "#MechJeb_RZplan_checkbox"); //"Auto-warp"
        }
    }
}

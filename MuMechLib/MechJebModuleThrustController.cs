using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace MuMech
{
    //ThrustController should be enabled/disabled through .users, not .enabled.
    public class MechJebModuleThrustController : DisplayModule
    {
        public MechJebModuleThrustController(MechJebCore core)
            : base(core)
        {
            priority = 200;
        }

        // UI stuff
        protected override void FlightWindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            enabled = GUILayout.Toggle(enabled, "Control throttle");

            targetThrottle = GUILayout.HorizontalSlider(targetThrottle, 0, 1);
            limitToTerminalVelocity = GUILayout.Toggle(limitToTerminalVelocity, "Limit To Terminal Velocity");
            limitToPreventOverheats = GUILayout.Toggle(limitToPreventOverheats, "Prevent Overheat");
            limitToPreventFlameout  = GUILayout.Toggle(limitToPreventFlameout,  "Prevent Jet Flameout");
            manageIntakes           = GUILayout.Toggle(manageIntakes,           "Manage Air Intakes");
            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            try {
                GUILayout.Label ("Jet Safety Margin");
                flameoutSafetyPctString = GUILayout.TextField(flameoutSafetyPctString, 5);
                Double.TryParse(flameoutSafetyPctString, out flameoutSafetyPct); // no change if parse fails
                GUILayout.Label("%");
            } finally {
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            base.FlightWindowGUI(windowID);
        }
        public override GUILayoutOption[] FlightWindowOptions()
        {
            return new GUILayoutOption[]{
                GUILayout.Width(200), GUILayout.Height(30)
            };
        }

        public override string GetName()
        {
            return "Throttle Control";
        }




        //turn these into properties? implement settings saving
        public float trans_spd_act = 0;
        public float trans_prev_thrust = 0;
        public bool trans_kill_h = false;
        public bool trans_land = false;
        public bool trans_land_gears = false;
        [ToggleInfoItem(name="Limit throttle to keep below terminal velocity")]
        public bool limitToTerminalVelocity = true;
        [ToggleInfoItem(name="Limit throttle to prevent overheats")]
        public bool limitToPreventOverheats = true;
        [ToggleInfoItem(name="Limit throttle to prevent jet flameout")]
        public bool limitToPreventFlameout = true;
        [ToggleInfoItem(name="Manage air intakes")]
        public bool manageIntakes = true;
        public bool limitAcceleration = false;
        public EditableDouble maxAcceleration = 40;

        public float targetThrottle = 0;

        // 5% safety margin on flameouts
        private double flameoutSafetyPct = 5;
        private string flameoutSafetyPctString = "5";

        private bool tmode_changed = false;

        private double t_integral = 0;
        private double t_prev_err = 0;

        public float t_Kp = 0.05F;
        public float t_Ki = 0.000001F;
        public float t_Kd = 0.05F;

        float lastThrottle = 0;

        public enum TMode
        {
            OFF,
            KEEP_ORBITAL,
            KEEP_SURFACE,
            KEEP_VERTICAL,
            DIRECT
        }

        private TMode prev_tmode = TMode.OFF;
        private TMode _tmode = TMode.OFF;
        public TMode tmode
        {
            get
            {
                return _tmode;
            }
            set
            {
                if (_tmode != value)
                {
                    prev_tmode = _tmode;
                    _tmode = value;
                    tmode_changed = true;
                }
            }
        }



        public override void Drive(FlightCtrlState s)
        {
            targetThrottle = Mathf.Clamp01((s.mainThrottle - lastThrottle) + targetThrottle);

            if ((tmode != TMode.OFF) && (vesselState.thrustAvailable > 0))
            {
                double spd = 0;

                switch (tmode)
                {
                    case TMode.KEEP_ORBITAL:
                        spd = vesselState.speedOrbital;
                        break;
                    case TMode.KEEP_SURFACE:
                        spd = vesselState.speedSurface;
                        break;
                    case TMode.KEEP_VERTICAL:
                        spd = vesselState.speedVertical;
                        Vector3d rot = Vector3d.up;
                        if (trans_kill_h)
                        {
                            Vector3 hsdir = Vector3.Exclude(vesselState.up, vesselState.velocityVesselSurface);
                            Vector3 dir = -hsdir + vesselState.up * Math.Max(Math.Abs(spd), 20 * mainBody.GeeASL);
                            if ((Math.Min(vesselState.altitudeASL, vesselState.altitudeTrue) > 5000) && (hsdir.magnitude > Math.Max(Math.Abs(spd), 100 * mainBody.GeeASL) * 2))
                            {
                                tmode = TMode.DIRECT;
                                trans_spd_act = 100;
                                rot = -hsdir;
                            }
                            else
                            {
                                if (spd > 0)
                                {
                                    rot = vesselState.up;
                                }
                                else
                                {
                                    rot = dir.normalized;
                                }
                            }
                            core.attitude.attitudeTo(rot, AttitudeReference.INERTIAL, null);
                        }
                        break;
                }

                double t_err = (trans_spd_act - spd) / vesselState.maxThrustAccel;
                if ((tmode == TMode.KEEP_ORBITAL && Vector3d.Dot(vesselState.forward, vesselState.velocityVesselOrbit) < 0) ||
                   (tmode == TMode.KEEP_SURFACE && Vector3d.Dot(vesselState.forward, vesselState.velocityVesselSurface) < 0))
                {
                    //allow thrust to declerate 
                    t_err *= -1;
                }
                t_integral += t_err * TimeWarp.fixedDeltaTime;
                double t_deriv = (t_err - t_prev_err) / TimeWarp.fixedDeltaTime;
                double t_act = (t_Kp * t_err) + (t_Ki * t_integral) + (t_Kd * t_deriv);
                t_prev_err = t_err;

                if ((tmode != TMode.KEEP_VERTICAL)
                    || !trans_kill_h
                    || (core.attitude.attitudeError < 2)
                    || ((Math.Min(vesselState.altitudeASL, vesselState.altitudeTrue) < 1000) && (core.attitude.attitudeError < 90)))
                {
                    if (tmode == TMode.DIRECT)
                    {
                        trans_prev_thrust = s.mainThrottle = trans_spd_act / 100.0F;
                    }
                    else
                    {
                        trans_prev_thrust = s.mainThrottle = Mathf.Clamp(trans_prev_thrust + (float)t_act, 0, 1.0F);
                    }
                }
                else
                {
                    if ((core.attitude.attitudeError >= 2) && (vesselState.torqueThrustPYAvailable > vesselState.torquePYAvailable * 10))
                    {
                        trans_prev_thrust = s.mainThrottle = 0.1F;
                    }
                    else
                    {
                        trans_prev_thrust = s.mainThrottle = 0;
                    }
                }
            }

            // TODO: ideally, if the user sets the throttle via shift/ctrl, or an autopilot sets the throttle, we automagically update targetThrottle.
            // Most important is the 'X' cutoff command.
            s.mainThrottle = targetThrottle;

            if (limitToTerminalVelocity)
            {
                s.mainThrottle = Mathf.Min(s.mainThrottle, TerminalVelocityThrottle());
            }

            if (limitToPreventOverheats)
            {
                s.mainThrottle = Mathf.Min(s.mainThrottle, TemperatureSafetyThrottle());
            }

            if (limitAcceleration)
            {
                s.mainThrottle = Mathf.Min(s.mainThrottle, AccelerationLimitedThrottle());
            }

            if (limitToPreventFlameout)
            {
                // This clause benefits being last: if we don't need much air
                // due to prior limits, we can close some intakes.
                s.mainThrottle = Mathf.Min(s.mainThrottle, FlameoutSafetyThrottle());
            }

            lastThrottle = s.mainThrottle;
        }

        //A throttle setting that throttles down when the vertical velocity of the ship exceeds terminal velocity
        float TerminalVelocityThrottle()
        {
            if (vesselState.altitudeASL > mainBody.RealMaxAtmosphereAltitude()) return 1.0F;

            double velocityRatio = Vector3d.Dot(vesselState.velocityVesselSurface, vesselState.up) / vesselState.TerminalVelocity();

            if (velocityRatio < 1.0) return 1.0F; //full throttle if under terminal velocity

            //throttle down quickly as we exceed terminal velocity:
            double falloff = 15.0;
            return Mathf.Clamp((float)(1.0 - falloff * (velocityRatio - 1.0)), 0.0F, 1.0F);
        }


        //a throttle setting that throttles down if something is close to overheating
        float TemperatureSafetyThrottle()
        {
            float maxTempRatio = vessel.parts.Max(p => p.temperature / p.maxTemp);

            //reduce throttle as the max temp. ratio approaches 1 within the safety margin
            float tempSafetyMargin = 0.05f;
            if (maxTempRatio < 1 - tempSafetyMargin) return 1.0F;
            else return (1 - maxTempRatio) / tempSafetyMargin;
        }

        float FlameoutSafetyThrottle()
        {
            float throttle = 1;

            // For every resource that is provided by intakes, find how
            // much we need at full throttle, add a safety margin, and
            // that's the max throttle for that resource.  Take the min of
            // the max throttles.
            foreach(var resource in vesselState.resources.Values)
            {
                if (resource.intakes.Length == 0) {
                    // No intakes provide this resource; not our problem.
                    continue;
                }

                // Compute the amount of air we will need, plus a safety
                // margin.  Make sure it's at least enough for last frame's
                // requirement, since it takes time for engines to spin down.
                // Note: this could be zero, e.g. if we have intakes but no
                // jets.

                double margin = (1 + 0.01 * flameoutSafetyPct);
                double safeRequirement = margin * resource.requiredAtMaxThrottle;
                safeRequirement = Math.Max (safeRequirement, resource.required);

                // Open the right number of intakes.
                if (manageIntakes) {
                    OptimizeIntakes(resource, safeRequirement);
                }

                double provided = resource.intakeProvided;
                if (resource.required >= provided) {
                    // We must cut throttle immediately, otherwise we are
                    // flaming out immediately.  Continue doing the rest of the
                    // loop anyway, but we'll be at zero throttle.
                    throttle = 0;
                } else {
                    double maxthrottle = provided / safeRequirement;
                    throttle = Mathf.Min (throttle, (float)maxthrottle);
                }
            }
            return throttle;
        }

        // Open and close intakes so that they provide the required flow but no
        // more.
        void OptimizeIntakes(VesselState.ResourceInfo info, double requiredFlow)
        {
            // The highly imperfect algorithm here is:
            // - group intakes by symmetry
            // - sort the groups by their size
            // - open a group at a time until we have enough airflow
            // - close the remaining groups

            // TODO: improve the algorithm:
            // 1. Take cost-benefit into account.  We want to open ram intakes
            //    before any others, and we probably never want to open
            //    nacelles.  We need more info to know how much thrust we lose
            //    for losing airflow, versus how much drag we gain for opening
            //    an intake.
            // 2. Take the center of drag into account.  We don't need to open
            //    symmetry groups all at once; we just need the center of drag
            //    to be in a good place.  Symmetry in the SPH also doesn't
            //    guarantee anything; we could be opening all pairs that are
            //    above the plane through the center of mass and none below,
            //    which makes control harder.
            // Even just point (1) means we want to solve knapsack.

            // Group intakes by symmetry, and collect the information by intake.
            var groups = new List<List<ModuleResourceIntake>>();
            var groupIds = new Dictionary<ModuleResourceIntake, int>();
            var data = new Dictionary<ModuleResourceIntake, VesselState.ResourceInfo.IntakeData>();
            foreach(var intakeData in info.intakes) {
                ModuleResourceIntake intake = intakeData.intake;
                data[intake] = intakeData;
                if (groupIds.ContainsKey(intake)) { continue; }

                var intakes = new List<ModuleResourceIntake>();
                intakes.Add(intake);
                foreach(var part in intake.part.symmetryCounterparts) {
                    foreach(var symintake in part.Modules.OfType<ModuleResourceIntake>()) {
                        intakes.Add (symintake);
                    }
                }

                int grpId = groups.Count;
                groups.Add (intakes);
                foreach(var member in intakes) {
                    groupIds[member] = grpId;
                }
            }

            // For each group in sorted order, if we need more air, open any
            // closed intakes.  If we have enough air, close any open intakes.
            // OrderBy is stable, so we'll always be opening the same intakes.
            double airFlowSoFar = 0;
            KSPActionParam param = new KSPActionParam(KSPActionGroup.None, 
                    KSPActionType.Activate);
            foreach(var grp in groups.OrderBy(grp => grp.Count)) {
                if (airFlowSoFar < requiredFlow) {
                    foreach(var intake in grp) {
                        double airFlowThisIntake = data[intake].predictedMassFlow;
                        if (!intake.intakeEnabled) {
                            intake.ToggleAction(param);
                        }
                        airFlowSoFar += airFlowThisIntake;
                    }
                } else {
                    foreach(var intake in grp) {
                        if (intake.intakeEnabled) {
                            intake.ToggleAction(param);
                        }
                    }
                }
            }
        }

        //The throttle setting that will give an acceleration of maxAcceleration
        float AccelerationLimitedThrottle()
        {
            double throttleForMaxAccel = (maxAcceleration - vesselState.minThrustAccel) / (vesselState.maxThrustAccel - vesselState.minThrustAccel);
            return Mathf.Clamp((float)throttleForMaxAccel, 0, 1);
        }

        public override void OnUpdate()
        {
            if (tmode_changed)
            {
                if (trans_kill_h && (tmode == TMode.OFF))
                {
                    core.attitude.attitudeDeactivate(null);
                    trans_land = false;
                }
                t_integral = 0;
                t_prev_err = 0;
                tmode_changed = false;
                FlightInputHandler.SetNeutralControls();
            }
        }
    }
}

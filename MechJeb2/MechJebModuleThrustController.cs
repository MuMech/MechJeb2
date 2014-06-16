using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleThrustController : ComputerModule
    {
        public MechJebModuleThrustController(MechJebCore core)
            : base(core)
        {
            priority = 200;
        }

        public float trans_spd_act = 0;
        public float trans_prev_thrust = 0;
        public bool trans_kill_h = false;

        [Persistent(pass = (int)Pass.Global)]
        public bool limitToTerminalVelocity = false;
        [GeneralInfoItem("Limit to terminal velocity", InfoItem.Category.Thrust)]
        public void LimitToTerminalVelocityInfoItem()
        {
            GUIStyle s = new GUIStyle(GUI.skin.toggle);
            if (limiter == LimitMode.TerminalVelocity) s.onHover.textColor = s.onNormal.textColor = Color.green;
            limitToTerminalVelocity = GUILayout.Toggle(limitToTerminalVelocity, "Limit to terminal velocity", s);
        }

        [Persistent(pass = (int)Pass.Global)]
        public bool limitToPreventOverheats = false;

        [GeneralInfoItem("Prevent overheats", InfoItem.Category.Thrust)]
        public void LimitToPreventOverheatsInfoItem()
        {
            GUIStyle s = new GUIStyle(GUI.skin.toggle);
            if (limiter == LimitMode.Temperature) s.onHover.textColor = s.onNormal.textColor = Color.green;
            limitToPreventOverheats = GUILayout.Toggle(limitToPreventOverheats, "Prevent overheats", s);
        }

        [ToggleInfoItem("Smooth throttle", InfoItem.Category.Thrust)]
        [Persistent(pass = (int)Pass.Global)]
        public bool smoothThrottle = false;

        [Persistent(pass = (int)Pass.Global)]
        public double throttleSmoothingTime = 1.0;

        [Persistent(pass = (int)Pass.Global)]
        public bool limitToPreventFlameout = false;

        [GeneralInfoItem("Prevent jet flameout", InfoItem.Category.Thrust)]
        public void LimitToPreventFlameoutInfoItem()
        {
            GUIStyle s = new GUIStyle(GUI.skin.toggle);
            if (limiter == LimitMode.Flameout) s.onHover.textColor = s.onNormal.textColor = Color.green;
            limitToPreventFlameout = GUILayout.Toggle(limitToPreventFlameout, "Prevent jet flameout", s);
        }


        // 5% safety margin on flameouts
        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble flameoutSafetyPct = 5;

        [ToggleInfoItem("Manage air intakes", InfoItem.Category.Thrust)]
        [Persistent(pass = (int)Pass.Global)]
        public bool manageIntakes = false;

        [Persistent(pass = (int)Pass.Global)]
        public bool limitAcceleration = false;

        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble maxAcceleration = 40;

        [GeneralInfoItem("Limit acceleration", InfoItem.Category.Thrust)]
        public void LimitAccelerationInfoItem()
        {
            GUILayout.BeginHorizontal();
            GUIStyle s = new GUIStyle(GUI.skin.toggle);
            if (limiter == LimitMode.Acceleration) s.onHover.textColor = s.onNormal.textColor = Color.green;
            limitAcceleration = GUILayout.Toggle(limitAcceleration, "Limit acceleration to", s, GUILayout.Width(140));
            maxAcceleration.text = GUILayout.TextField(maxAcceleration.text, GUILayout.Width(30));
            GUILayout.Label("m/s²", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }

        [Persistent(pass = (int)Pass.Local)]
        public bool limitThrottle = false;

        [Persistent(pass = (int)Pass.Local)]
        public EditableDoubleMult maxThrottle = new EditableDoubleMult(1, 0.01);

        [GeneralInfoItem("Limit throttle", InfoItem.Category.Thrust)]
        public void LimitThrottleInfoItem()
        {
            GUILayout.BeginHorizontal();
            GUIStyle s = new GUIStyle(GUI.skin.toggle);
            if (limiter == LimitMode.Throttle) s.onHover.textColor = s.onNormal.textColor = Color.green;
            limitThrottle = GUILayout.Toggle(limitThrottle, "Limit throttle to", s, GUILayout.Width(110));
            maxThrottle.text = GUILayout.TextField(maxThrottle.text, GUILayout.Width(30));
            GUILayout.Label("%", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }

        public enum LimitMode { None, TerminalVelocity, Temperature, Flameout, Acceleration, Throttle }
        public LimitMode limiter = LimitMode.None;

        public float targetThrottle = 0;

        protected bool tmode_changed = false;
        

        public PIDController pid;

        float lastThrottle = 0;

        public enum TMode
        {
            OFF,
            KEEP_ORBITAL,
            KEEP_SURFACE,
            KEEP_VERTICAL,
            KEEP_RELATIVE,
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

        public override void OnStart(PartModule.StartState state)
        {
            pid = new PIDController(0.05, 0.000001, 0.05);
            users.Add(this);

            base.OnStart(state);
        }

        public void ThrustOff()
        {
            targetThrottle = 0;
            vessel.ctrlState.mainThrottle = 0;
            tmode = TMode.OFF;
            if (vessel == FlightGlobals.ActiveVessel)
            {
                FlightInputHandler.state.mainThrottle = 0; //so that the on-screen throttle gauge reflects the autopilot throttle
            }
        }

        // Call this function to set the throttle for a burn with dV delta-V remaining.
        // timeConstant controls how quickly we throttle down toward the end of the burn.
        // This function is nice because it will correctly handle engine spool-up/down times.
        public void ThrustForDV(double dV, double timeConstant)
        {
            timeConstant += vesselState.maxEngineResponseTime;
            double spooldownDV = vesselState.currentThrustAccel * vesselState.maxEngineResponseTime;
            double desiredAcceleration = (dV - spooldownDV) / timeConstant;

            targetThrottle = Mathf.Clamp01((float)(desiredAcceleration / vesselState.maxThrustAccel));
        }

        public override void Drive(FlightCtrlState s)
        {
        	if (core.GetComputerModule<MechJebModuleThrustWindow>().hidden && core.GetComputerModule<MechJebModuleAscentGuidance>().hidden) { return; }
        	
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
//                            tmode = TMode.DIRECT;
//                            trans_spd_act = 100;
                            Vector3 hsdir = Vector3.Exclude(vesselState.up, vessel.srf_velocity);
                            Vector3 dir = -hsdir + vesselState.up * Math.Max(Math.Abs(spd), 20 * mainBody.GeeASL) * (150 / vesselState.torqueAvailable.magnitude);
                            if ((Math.Min(vesselState.altitudeASL, vesselState.altitudeTrue) > 5000) && (hsdir.magnitude > Math.Max(Math.Abs(spd), 100 * mainBody.GeeASL) * 2))
                            {
                                rot = -hsdir;
                            }
                            else
                            {
                                rot = dir.normalized;
                            }
                            core.attitude.attitudeTo(rot, AttitudeReference.INERTIAL, null);
                        }
                        break;
                }

                double t_err = (trans_spd_act - spd) / vesselState.maxThrustAccel;
                if ((tmode == TMode.KEEP_ORBITAL && Vector3d.Dot(vesselState.forward, vessel.obt_velocity) < 0) ||
                   (tmode == TMode.KEEP_SURFACE && Vector3d.Dot(vesselState.forward, vessel.srf_velocity) < 0))
                {
                    //allow thrust to declerate 
                    t_err *= -1;
                }

                double t_act = pid.Compute(t_err);

                if ((tmode != TMode.KEEP_VERTICAL)
                    || !trans_kill_h
                    || (core.attitude.attitudeError < 3)
                    || ((Math.Min(vesselState.altitudeASL, vesselState.altitudeTrue) < 1000) && (core.attitude.attitudeError < 90)))
                {
                    if (tmode == TMode.DIRECT)
                    {
                        trans_prev_thrust = targetThrottle = trans_spd_act / 100.0F;
                    }
                    else
                    {
                        trans_prev_thrust = targetThrottle = Mathf.Clamp01(trans_prev_thrust + (float)t_act);
                    }
                }
                else
                {
                    if ((core.attitude.attitudeError >= 3) && (vesselState.torqueThrustPYAvailable > Math.Min( vesselState.torqueAvailable.x, vesselState.torqueAvailable.z) * 10))
                    {
                        trans_prev_thrust = targetThrottle = 0.1F;
                    }
                    else
                    {
                        trans_prev_thrust = targetThrottle = 0;
                    }
                }
            }

            // Only set throttle if a module need it. Othewise let the user or other mods set it
            // There is always at least 1 user : the module itself (why ?)
            if (users.Count() > 1)
                s.mainThrottle = targetThrottle;

            float throttleLimit = 1;

            limiter = LimitMode.None;

            if (limitThrottle)
            {
                if (maxThrottle < throttleLimit) limiter = LimitMode.Throttle;
                throttleLimit = Mathf.Min(throttleLimit, (float)maxThrottle);
            }

            if (limitToTerminalVelocity)
            {
                float limit = TerminalVelocityThrottle();
                if (limit < throttleLimit) limiter = LimitMode.TerminalVelocity;
                throttleLimit = Mathf.Min(throttleLimit, limit);
            }

            if (limitToPreventOverheats)
            {
                float limit = TemperatureSafetyThrottle();
                if(limit < throttleLimit) limiter = LimitMode.Temperature;
                throttleLimit = Mathf.Min(throttleLimit, limit);
            }

            if (limitAcceleration)
            {
                float limit = AccelerationLimitedThrottle();
                if(limit < throttleLimit) limiter = LimitMode.Acceleration;
                throttleLimit = Mathf.Min(throttleLimit, limit);
            }

            if (limitToPreventFlameout)
            {
                // This clause benefits being last: if we don't need much air
                // due to prior limits, we can close some intakes.
                float limit = FlameoutSafetyThrottle();
                if (limit < throttleLimit) limiter = LimitMode.Flameout;
                throttleLimit = Mathf.Min(throttleLimit, limit);
            }

            if (double.IsNaN(throttleLimit)) throttleLimit = 0;
            throttleLimit = Mathf.Clamp01(throttleLimit);

            vesselState.throttleLimit = throttleLimit;

            if (s.mainThrottle < throttleLimit) limiter = LimitMode.None;

            s.mainThrottle = Mathf.Min(s.mainThrottle, throttleLimit);

            if (smoothThrottle)
            {
                s.mainThrottle = SmoothThrottle(s.mainThrottle);
            }

            if (double.IsNaN(s.mainThrottle)) s.mainThrottle = 0;
            s.mainThrottle = Mathf.Clamp01(s.mainThrottle);
            
            if (s.Z == 0 && core.rcs.rcsThrottle && vesselState.rcsThrust) s.Z = -s.mainThrottle;

            lastThrottle = s.mainThrottle;
        }

        //A throttle setting that throttles down when the vertical velocity of the ship exceeds terminal velocity
        float TerminalVelocityThrottle()
        {
            if (vesselState.altitudeASL > mainBody.RealMaxAtmosphereAltitude()) return 1.0F;

            double velocityRatio = Vector3d.Dot(vessel.srf_velocity, vesselState.up) / vesselState.TerminalVelocity();

            if (velocityRatio < 1.0) return 1.0F; //full throttle if under terminal velocity

            //throttle down quickly as we exceed terminal velocity:
            const double falloff = 15.0;
            return Mathf.Clamp((float)(1.0 - falloff * (velocityRatio - 1.0)), 0.0F, 1.0F);
        }

        //a throttle setting that throttles down if something is close to overheating
        float TemperatureSafetyThrottle()
        {
            float maxTempRatio = vessel.parts.Max(p => p.temperature / p.maxTemp);

            //reduce throttle as the max temp. ratio approaches 1 within the safety margin
            const float tempSafetyMargin = 0.05f;
            if (maxTempRatio < 1 - tempSafetyMargin) return 1.0F;
            else return (1 - maxTempRatio) / tempSafetyMargin;
        }

        float SmoothThrottle(float mainThrottle)
        {
            return Mathf.Clamp(mainThrottle,
                               (float)(lastThrottle - vesselState.deltaT / throttleSmoothingTime),
                               (float)(lastThrottle + vesselState.deltaT / throttleSmoothingTime));
        }

        float FlameoutSafetyThrottle()
        {
            float throttle = 1;

            // For every resource that is provided by intakes, find how
            // much we need at full throttle, add a safety margin, and
            // that's the max throttle for that resource.  Take the min of
            // the max throttles.
            foreach (var resource in vesselState.resources.Values)
            {
                if (resource.intakes.Length == 0)
                {
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
                safeRequirement = Math.Max(safeRequirement, resource.required);

                // Open the right number of intakes.
                if (manageIntakes)
                {
                    OptimizeIntakes(resource, safeRequirement);
                }

                double provided = resource.intakeProvided;
                if (resource.required >= provided)
                {
                    // We must cut throttle immediately, otherwise we are
                    // flaming out immediately.  Continue doing the rest of the
                    // loop anyway, but we'll be at zero throttle.
                    throttle = 0;
                }
                else
                {
                    double maxthrottle = provided / safeRequirement;
                    throttle = Mathf.Min(throttle, (float)maxthrottle);
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
            foreach (var intakeData in info.intakes)
            {
                ModuleResourceIntake intake = intakeData.intake;
                data[intake] = intakeData;
                if (groupIds.ContainsKey(intake)) { continue; }

                // Create a group for this symmetry
                int grpId = groups.Count;
                var intakes = new List<ModuleResourceIntake>();
                groups.Add(intakes);

                // In DFS order, get all the symmetric parts.
                // We can't rely on symmetryCounterparts; see bug #52 by tavert:
                // https://github.com/MuMech/MechJeb2/issues/52
                var stack = new Stack<Part>();
                stack.Push(intake.part);
                while(stack.Count > 0) {
                    var part = stack.Pop();
                    var partIntake = part.Modules.OfType<ModuleResourceIntake>().FirstOrDefault();
                    if (partIntake == null || groupIds.ContainsKey(partIntake)) {
                        continue;
                    }

                    groupIds[partIntake] = grpId;
                    intakes.Add(partIntake);

                    foreach (var sympart in part.symmetryCounterparts) {
                        stack.Push(sympart);
                    }
                }
            }

            // For each group in sorted order, if we need more air, open any
            // closed intakes.  If we have enough air, close any open intakes.
            // OrderBy is stable, so we'll always be opening the same intakes.
            double airFlowSoFar = 0;
            KSPActionParam param = new KSPActionParam(KSPActionGroup.None,
                    KSPActionType.Activate);
            foreach (var grp in groups.OrderBy(grp => grp.Count))
            {
                if (airFlowSoFar < requiredFlow)
                {
                    foreach (var intake in grp)
                    {
                        double airFlowThisIntake = data[intake].predictedMassFlow;
                        if (!intake.intakeEnabled)
                        {
                            intake.ToggleAction(param);
                        }
                        airFlowSoFar += airFlowThisIntake;
                    }
                }
                else
                {
                    foreach (var intake in grp)
                    {
                        if (intake.intakeEnabled)
                        {
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
        	if (core.GetComputerModule<MechJebModuleThrustWindow>().hidden && core.GetComputerModule<MechJebModuleAscentGuidance>().hidden) { return; }
        	
            if (tmode_changed)
            {
                if (trans_kill_h && (tmode == TMode.OFF))
                {
                    core.attitude.attitudeDeactivate();
                }
                pid.Reset();
                tmode_changed = false;
                FlightInputHandler.SetNeutralControls();
            }
        }
    }
}

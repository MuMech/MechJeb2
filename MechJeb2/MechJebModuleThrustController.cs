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

        [Persistent(pass = (int)Pass.Type)]
        public bool differentialThrottle = false;

        [GeneralInfoItem("Differential throttle", InfoItem.Category.Thrust)]
        public void  DifferentialThrottle()
        {
            bool oldDifferentialThrottle = core.thrust.differentialThrottle;
            GUIStyle s = new GUIStyle(GUI.skin.toggle);
            if (differentialThrottle && vessel.LiftedOff())
            {
                s.onHover.textColor = s.onNormal.textColor = core.thrust.differentialThrottleSuccess ? Color.green : Color.yellow;
            }
            differentialThrottle = GUILayout.Toggle(differentialThrottle, "Differential throttle", s);
            if (oldDifferentialThrottle && !core.thrust.differentialThrottle)
                core.thrust.DisableDifferentialThrottle();
        }

        public Vector3d differentialThrottleDemandedTorque = new Vector3d();

        // true if differential throttle is active and a solution has been found i.e. at least 3 engines are on and they are not aligned
        public bool differentialThrottleSuccess = false;

        public enum LimitMode { None, TerminalVelocity, Temperature, Flameout, Acceleration, Throttle }
        public LimitMode limiter = LimitMode.None;

        public float targetThrottle = 0;

        protected bool tmode_changed = false;
        

        public PIDController pid;

        float lastThrottle = 0;
        bool userCommandingRotation { get { return userCommandingRotationSmoothed > 0; } }
        int userCommandingRotationSmoothed = 0;
        bool lastDisableThrusters = false;

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
            float threshold = 0.1F;
            bool _userCommandingRotation = !(Mathfx.Approx(s.pitch, s.pitchTrim, threshold)
                    && Mathfx.Approx(s.yaw, s.yawTrim, threshold)
                    && Mathfx.Approx(s.roll, s.rollTrim, threshold));
            bool _userCommandingTranslation = !(Math.Abs(s.X) < threshold
                        && Math.Abs(s.Y) < threshold
                        && Math.Abs(s.Z) < threshold);

            if (_userCommandingRotation && !_userCommandingTranslation)
            {
                userCommandingRotationSmoothed = 2;
            }
            else if (userCommandingRotationSmoothed > 0)
            {
                userCommandingRotationSmoothed--;
            }

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
                            Vector3 hsdir = Vector3.ProjectOnPlane(vesselState.surfaceVelocity, vesselState.up);
                            Vector3 dir = -hsdir + vesselState.up * Math.Max(Math.Abs(spd), 20 * mainBody.GeeASL);
                            if ((Math.Min(vesselState.altitudeASL, vesselState.altitudeTrue) > 5000) && (hsdir.magnitude > Math.Max(Math.Abs(spd), 100 * mainBody.GeeASL) * 2))
                            {
                                tmode = TMode.DIRECT;
                                trans_spd_act = 100;
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
                if ((tmode == TMode.KEEP_ORBITAL && Vector3d.Dot(vesselState.forward, vesselState.orbitalVelocity) < 0) ||
                   (tmode == TMode.KEEP_SURFACE && Vector3d.Dot(vesselState.forward, vesselState.surfaceVelocity) < 0))
                {
                    //allow thrust to declerate 
                    t_err *= -1;
                }

                double t_act = pid.Compute(t_err);

                if ((tmode != TMode.KEEP_VERTICAL)
                    || !trans_kill_h
                    || (core.attitude.attitudeError < 2)
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
                    bool useGimbal = (vesselState.torqueFromEngine.x / vessel.ctrlState.mainThrottle > vesselState.torqueAvailable.x * 10) ||
                                     (vesselState.torqueFromEngine.z / vessel.ctrlState.mainThrottle > vesselState.torqueAvailable.z * 10);

                    bool useDiffThrottle = (vesselState.torqueFromDiffThrottle.x > vesselState.torqueAvailable.x * 10) ||
                                           (vesselState.torqueFromDiffThrottle.z > vesselState.torqueAvailable.z * 10);

                    if ((core.attitude.attitudeError >= 2) && (useGimbal || (useDiffThrottle && core.thrust.differentialThrottle)))
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
                float limit = (float)TemperatureSafetyThrottle();
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

            if (!core.attitude.enabled)
            {
                Vector3d act = new Vector3d(s.pitch, s.yaw, s.roll);
                differentialThrottleDemandedTorque = -Vector3d.Scale(act.xzy, vesselState.torqueFromDiffThrottle * s.mainThrottle * 0.5f);
            }
        }

        public override void OnFixedUpdate()
        {
            if (differentialThrottle)
            {
                differentialThrottleSuccess = ComputeDifferentialThrottle(differentialThrottleDemandedTorque);
            }
            else
            {
                differentialThrottleSuccess = false;
            }
        }

        //A throttle setting that throttles down when the vertical velocity of the ship exceeds terminal velocity
        float TerminalVelocityThrottle()
        {
            if (vesselState.altitudeASL > mainBody.RealMaxAtmosphereAltitude()) return 1.0F;

            double velocityRatio = Vector3d.Dot(vesselState.surfaceVelocity, vesselState.up) / vesselState.TerminalVelocity();

            if (velocityRatio < 1.0) return 1.0F; //full throttle if under terminal velocity

            //throttle down quickly as we exceed terminal velocity:
            const double falloff = 15.0;
            return Mathf.Clamp((float)(1.0 - falloff * (velocityRatio - 1.0)), 0.0F, 1.0F);
        }

        //a throttle setting that throttles down if something is close to overheating
        double TemperatureSafetyThrottle()
        {
            double maxTempRatio = vessel.parts.Max(p => p.temperature / p.maxTemp);

            //reduce throttle as the max temp. ratio approaches 1 within the safety margin
            const double tempSafetyMargin = 0.05f;
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

                    for (int i = 0; i < part.symmetryCounterparts.Count; i++)
                    {
                        var sympart = part.symmetryCounterparts[i];
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
                    for (int i = 0; i < grp.Count; i++)
                    {
                        var intake = grp[i];
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
                    for (int j = 0; j < grp.Count; j++)
                    {
                        var intake = grp[j];
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
            if (core.GetComputerModule<MechJebModuleThrustWindow>().hidden && core.GetComputerModule<MechJebModuleAscentGuidance>().hidden)
            {
                return;
            }

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

            bool disableThrusters = (userCommandingRotation && !core.rcs.rcsForRotation);
            if (disableThrusters != lastDisableThrusters)
            {
                lastDisableThrusters = disableThrusters;
                var rcsModules = vessel.FindPartModulesImplementing<ModuleRCS>();
                foreach (var pm in rcsModules)
                {
                    if (disableThrusters)
                    {
                        pm.Disable();
                    }
                    else
                    {
                        pm.Enable();
                    }
                }
            }
        }

#warning from here remove all the EngineWrapper stuff and move the useful stuff to VesselState.EngineInfo

        static void MaxThrust(double[] x, ref double func, double[] grad, object obj)
        {
            List<EngineWrapper> el = (List<EngineWrapper>)obj;

            func = 0;

            for (int i = 0, j = 0; j < el.Count; j++)
            {
                EngineWrapper e = el[j];
                if (!e.engine.throttleLocked)
                {
                    func -= el[j].maxVariableForce.y * x[i];
                    grad[i] = -el[j].maxVariableForce.y;
                    i++;
                }
            }
        }

        public bool ComputeDifferentialThrottle(Vector3d torque)
        {
            List<EngineWrapper> engines = new List<EngineWrapper>();
            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];
                for (int j = 0; j < p.Modules.Count; j++)
                {
                    PartModule pm = p.Modules[j];
                    if (pm is ModuleEngines)
                    {
                        ModuleEngines e = (ModuleEngines)pm;
                        EngineWrapper engine = new EngineWrapper(e);
                        if (e.EngineIgnited && !e.flameout && e.enabled)
                        {
                            engine.UpdateForceAndTorque(vesselState.CoM);
                            engines.Add(engine);
                        }
                    }
                }
            }

            int n = engines.Count(eng => !eng.engine.throttleLocked);
            if (n < 3)
            {
                for (int i = 0; i < engines.Count; i++)
                {
                    EngineWrapper e = engines[i];
                    e.thrustRatio = 1;
                }
                return false;
            }

            double[,] C = new double[2+2*n,n+1];
            int[] CT = new int[2+2*n];
            float mainThrottle = vessel.ctrlState.mainThrottle;

            // FIXME: the solver will throw an exception if the commanded torque is not realisable,
            // clamp the commanded torque to half the possible torque for now
            if (double.IsNaN(vesselState.torqueFromDiffThrottle.x)) vesselState.torqueFromDiffThrottle.x = 0;
            if (double.IsNaN(vesselState.torqueFromDiffThrottle.z)) vesselState.torqueFromDiffThrottle.z = 0;
            C[0, n] = Mathf.Clamp((float)torque.x, -(float)vesselState.torqueFromDiffThrottle.x * mainThrottle / 2, (float)vesselState.torqueFromDiffThrottle.x * mainThrottle / 2);
            C[1, n] = Mathf.Clamp((float)torque.z, -(float)vesselState.torqueFromDiffThrottle.z * mainThrottle / 2, (float)vesselState.torqueFromDiffThrottle.z * mainThrottle / 2);

            for (int i = 0, j = 0; j < engines.Count; j++)
            {
                var e = engines[j];

                C[0,n] -= e.constantTorque.x;
                C[1,n] -= e.constantTorque.z;

                if (!e.engine.throttleLocked)
                {
                    C[0,i] = e.maxVariableTorque.x;
                    //C[1,j] = e.maxVariableTorque.y;
                    C[1,i] = e.maxVariableTorque.z;

                    C[2+2*i,i] = 1;
                    C[2+2*i,n] = 1;
                    CT[2+2*i] = -1;

                    C[3+2*i,i] = 1;
                    C[3+2*i,n] = 0;
                    CT[3+2*i] = 1;

                    i++;
                }
            }

            double[] w = new double[0];
            double[,] u = new double[0,0];
            double[,] vt = new double[0,0];
            alglib.svd.rmatrixsvd(C, 2, n, 0, 0, 2, ref w, ref u, ref vt);
            if (w[0] >= 10 * w[1])
            {
                for (int i = 0; i < engines.Count; i++)
                {
                    EngineWrapper e = engines[i];
                    e.thrustRatio = 1;
                }
                return false;
            }

            // Multiply by mainThrottle later to compute the singular value decomposition correctly
            for (int i = 0; i < n; i++)
            {
                C[0,i] *= mainThrottle;
                C[1,i] *= mainThrottle;
            }

            double[] x = new double[n];
            alglib.minbleicstate state;
            alglib.minbleicreport rep;

            try
            {
                alglib.minbleiccreate(x, out state);
                alglib.minbleicsetlc(state, C, CT);
                alglib.minbleicoptimize(state, MaxThrust, null, engines);
                alglib.minbleicresults(state, out x, out rep);
            }
            catch
            {
                return false;
            }

            if (x.Any(val => double.IsNaN(val)))
                return false;

            for (int i = 0, j = 0; j < engines.Count; j++)
            {
                if (!engines[j].engine.throttleLocked)
                {
                    engines[j].thrustRatio = (float)x[i];
                    i++;
                }
            }

            return true;
        }

        public void DisableDifferentialThrottle()
        {
            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];
                for (int j = 0; j < p.Modules.Count; j++)
                {
                    PartModule pm = p.Modules[j];
                    if (pm is ModuleEngines)
                    {
                        ModuleEngines e = (ModuleEngines)pm;
                        EngineWrapper engine = new EngineWrapper(e);
                        engine.thrustRatio = 1;
                    }
                }
            }
        }
    }
}

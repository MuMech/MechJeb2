using System;
using System.Collections.Generic;
using System.Linq;
using KSP.UI.Screens;
using UnityEngine;
using KSP.Localization;

namespace MuMech
{
    public class MechJebModuleThrustController : ComputerModule
    {
        public enum DifferentialThrottleStatus
        {
            Success,
            AllEnginesOff,
            MoreEnginesRequired,
            SolverFailed
        }

        public MechJebModuleThrustController(MechJebCore core)
            : base(core)
        {
            priority = 200;
        }

        public float trans_spd_act = 0;
        public float trans_prev_thrust = 0;
        public bool trans_kill_h = false;


        // The Terminal Velocity limiter is removed to not have to deal with users who
        // think that seeing the aerodynamic FX means they reached it.
        // And it s really high since 1.0.x anyway so the Dynamic Pressure limiter is better now
        //[Persistent(pass = (int)Pass.Global)]
        public bool limitToTerminalVelocity = false;

        //[GeneralInfoItem("Limit to terminal velocity", InfoItem.Category.Thrust)]
        //public void LimitToTerminalVelocityInfoItem()
        //{
        //    GUIStyle s = new GUIStyle(GUI.skin.toggle);
        //    if (limiter == LimitMode.TerminalVelocity) s.onHover.textColor = s.onNormal.textColor = Color.green;
        //    limitToTerminalVelocity = GUILayout.Toggle(limitToTerminalVelocity, "Limit to terminal velocity", s);
        //}

        [Persistent(pass = (int)Pass.Global)]
        public bool limitDynamicPressure = false;

        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble maxDynamicPressure = 20000;

        [GeneralInfoItem("#MechJeb_LimittoMaxQ", InfoItem.Category.Thrust)]//Limit to Max Q
        public void LimitToMaxDynamicPressureInfoItem()
        {
            GUILayout.BeginHorizontal();
            GUIStyle s = new GUIStyle(GUI.skin.toggle);
            if (limiter == LimitMode.DynamicPressure) s.onHover.textColor = s.onNormal.textColor = Color.green;
            limitDynamicPressure = GUILayout.Toggle(limitDynamicPressure, Localizer.Format("#MechJeb_Ascent_checkbox11"), s, GUILayout.Width(140));//"Limit Q to"
            maxDynamicPressure.text = GUILayout.TextField(maxDynamicPressure.text, GUILayout.Width(80));
            GUILayout.Label("pa", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }

        [Persistent(pass = (int)Pass.Global)]
        public bool limitToPreventOverheats = false;

        [GeneralInfoItem("#MechJeb_PreventEngineOverheats", InfoItem.Category.Thrust)]//Prevent engine overheats
        public void LimitToPreventOverheatsInfoItem()
        {
            GUIStyle s = new GUIStyle(GUI.skin.toggle);
            if (limiter == LimitMode.Temperature) s.onHover.textColor = s.onNormal.textColor = Color.green;
            limitToPreventOverheats = GUILayout.Toggle(limitToPreventOverheats, Localizer.Format("#MechJeb_Ascent_checkbox12"), s);//"Prevent engine overheats"
        }
     
        [ToggleInfoItem("#MechJeb_SmoothThrottle", InfoItem.Category.Thrust)]//Smooth throttle
        [Persistent(pass = (int)Pass.Global)]
        public bool smoothThrottle = false;

        [Persistent(pass = (int)Pass.Global)]
        public double throttleSmoothingTime = 1.0;

        [Persistent(pass = (int)Pass.Global)]
        public bool limitToPreventFlameout = false;

        [GeneralInfoItem("#MechJeb_PreventJetFlameout", InfoItem.Category.Thrust)]//Prevent jet flameout
        public void LimitToPreventFlameoutInfoItem()
        {
            GUIStyle s = new GUIStyle(GUI.skin.toggle);
            if (limiter == LimitMode.Flameout) s.onHover.textColor = s.onNormal.textColor = Color.green;
            limitToPreventFlameout = GUILayout.Toggle(limitToPreventFlameout, Localizer.Format("#MechJeb_Ascent_checkbox13"), s);//"Prevent jet flameout"
        }

        [Persistent(pass = (int)Pass.Global)]
        public bool limitToPreventUnstableIgnition = false;

        [GeneralInfoItem("#MechJeb_PreventUnstableIgnition", InfoItem.Category.Thrust)]//Prevent unstable ignition
        public void LimitToPreventUnstableIgnitionInfoItem()
        {
            GUIStyle s = new GUIStyle(GUI.skin.toggle);
            if (limiter == LimitMode.UnstableIgnition) s.onHover.textColor = s.onNormal.textColor = Color.green;
            limitToPreventUnstableIgnition = GUILayout.Toggle(limitToPreventUnstableIgnition, Localizer.Format("#MechJeb_Ascent_checkbox14"), s);//"Prevent unstable ignition"
        }

        [Persistent(pass = (int)Pass.Global)]
        public bool autoRCSUllaging = true;

        [GeneralInfoItem("#MechJeb_UseRCStoullage", InfoItem.Category.Thrust)]//Use RCS to ullage
        public void AutoRCsUllageInfoItem()
        {
            GUIStyle s = new GUIStyle(GUI.skin.toggle);
            if (limiter == LimitMode.AutoRCSUllage) s.onHover.textColor = s.onNormal.textColor = Color.green;
            autoRCSUllaging = GUILayout.Toggle(autoRCSUllaging, Localizer.Format("#MechJeb_Ascent_checkbox15"), s);//"Use RCS to ullage"
        }

        // 5% safety margin on flameouts
        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble flameoutSafetyPct = 5;

        [ToggleInfoItem("#MechJeb_ManageAirIntakes", InfoItem.Category.Thrust)]//Manage air intakes
        [Persistent(pass = (int)Pass.Global)]
        public bool manageIntakes = false;

        [Persistent(pass = (int)Pass.Global)]
        public bool limitAcceleration = false;

        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble maxAcceleration = 40;

        [GeneralInfoItem("#MechJeb_LimitAcceleration", InfoItem.Category.Thrust)]//Limit acceleration
        public void LimitAccelerationInfoItem()
        {
            GUILayout.BeginHorizontal();
            GUIStyle s = new GUIStyle(GUI.skin.toggle);
            if (limiter == LimitMode.Acceleration) s.onHover.textColor = s.onNormal.textColor = Color.green;
            limitAcceleration = GUILayout.Toggle(limitAcceleration, Localizer.Format("#MechJeb_Ascent_checkbox16"), s, GUILayout.Width(140));//"Limit acceleration to"
            maxAcceleration.text = GUILayout.TextField(maxAcceleration.text, GUILayout.Width(30));
            GUILayout.Label("m/s²", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }

        [Persistent(pass = (int)Pass.Local)]
        public bool limitThrottle = false;

        [Persistent(pass = (int)Pass.Local)]
        public EditableDoubleMult maxThrottle = new EditableDoubleMult(1, 0.01);

        [GeneralInfoItem("#MechJeb_LimitThrottle", InfoItem.Category.Thrust)]//Limit throttle
        public void LimitThrottleInfoItem()
        {
            GUILayout.BeginHorizontal();
            GUIStyle s = new GUIStyle(GUI.skin.toggle);
            if (limiter == LimitMode.Throttle) s.onHover.textColor = s.onNormal.textColor = maxThrottle > 0d ? Color.green : Color.red;
            limitThrottle = GUILayout.Toggle(limitThrottle, Localizer.Format("#MechJeb_Ascent_checkbox17"), s, GUILayout.Width(110));//"Limit throttle to"
            maxThrottle.text = GUILayout.TextField(maxThrottle.text, GUILayout.Width(30));
            GUILayout.Label("%", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }

        [Persistent(pass = (int) (Pass.Local | Pass.Type | Pass.Global))]
        public bool limiterMinThrottle = false;

        [Persistent(pass = (int) (Pass.Local | Pass.Type | Pass.Global))]
        public EditableDoubleMult minThrottle = new EditableDoubleMult(0.05, 0.01);

        [GeneralInfoItem("#MechJeb_LowerThrottleLimit", InfoItem.Category.Thrust)]//Lower throttle limit
        public void LimiterMinThrottleInfoItem()
        {
            GUILayout.BeginHorizontal();
            GUIStyle s = new GUIStyle(GUI.skin.toggle);
            if (limiter == LimitMode.MinThrottle) s.onHover.textColor = s.onNormal.textColor = Color.green;
            limiterMinThrottle = GUILayout.Toggle(limiterMinThrottle, Localizer.Format("#MechJeb_Ascent_checkbox18"), s, GUILayout.Width(160));//"Keep limited throttle over"
            minThrottle.text = GUILayout.TextField(minThrottle.text, GUILayout.Width(30));
            GUILayout.Label("%", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }

        [Persistent(pass = (int)Pass.Type)]
        public bool differentialThrottle = false;

        [GeneralInfoItem("#MechJeb_DifferentialThrottle", InfoItem.Category.Thrust)]//Differential throttle
        public void  DifferentialThrottle()
        {
            bool oldDifferentialThrottle = core.thrust.differentialThrottle;
            GUIStyle s = new GUIStyle(GUI.skin.toggle);
            if (differentialThrottle && vessel.LiftedOff())
            {
                s.onHover.textColor = s.onNormal.textColor = core.thrust.differentialThrottleSuccess == DifferentialThrottleStatus.Success ? Color.green : Color.yellow;
            }
            differentialThrottle = GUILayout.Toggle(differentialThrottle, Localizer.Format("#MechJeb_Ascent_checkbox19"), s);//"Differential throttle"

            if (oldDifferentialThrottle && !core.thrust.differentialThrottle)
                core.thrust.DisableDifferentialThrottle();
        }

        public Vector3d differentialThrottleDemandedTorque = new Vector3d();

        public DifferentialThrottleStatus differentialThrottleSuccess = DifferentialThrottleStatus.Success;

        [Persistent(pass = (int)Pass.Local)]
        public bool electricThrottle = false;

        [Persistent(pass = (int)Pass.Local)]
        public EditableDoubleMult electricThrottleLo = new EditableDoubleMult(0.05, 0.01);
        [Persistent(pass = (int)Pass.Local)]
        public EditableDoubleMult electricThrottleHi = new EditableDoubleMult(0.15, 0.01);

        [GeneralInfoItem("#MechJeb_ElectricLimit", InfoItem.Category.Thrust)]//Electric limit
        public void LimitElectricInfoItem()
        {
            GUILayout.BeginHorizontal();
            GUIStyle s = new GUIStyle(GUI.skin.toggle);
            if (limiter == LimitMode.Electric)
            {
                if (vesselState.throttleLimit <0.001)
                    s.onHover.textColor = s.onNormal.textColor = Color.red;
                else
                    s.onHover.textColor = s.onNormal.textColor = Color.yellow;
            }
            else if (ElectricEngineRunning()) s.onHover.textColor = s.onNormal.textColor = Color.green;

            electricThrottle = GUILayout.Toggle(electricThrottle, Localizer.Format("#MechJeb_Ascent_checkbox20"), s, GUILayout.Width(110));//"Electric limit Lo"
            electricThrottleLo.text = GUILayout.TextField(electricThrottleLo.text, GUILayout.Width(30));
            GUILayout.Label("% Hi", GUILayout.ExpandWidth(false));
            electricThrottleHi.text = GUILayout.TextField(electricThrottleHi.text, GUILayout.Width(30));
            GUILayout.Label("%", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }

        public enum LimitMode { None, TerminalVelocity, Temperature, Flameout, Acceleration, Throttle, DynamicPressure, MinThrottle, Electric, UnstableIgnition, AutoRCSUllage }
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

        ScreenMessage preventingUnstableIgnitionsMessage;

        public override void OnStart(PartModule.StartState state)
        {
            preventingUnstableIgnitionsMessage = new ScreenMessage(Localizer.Format("#MechJeb_Ascent_srcmsg1"), 2f, ScreenMessageStyle.UPPER_CENTER);//"<color=orange>[MechJeb]: Killing throttle to prevent unstable ignition</color>"
            pid = new PIDController(0.05, 0.000001, 0.05);
            users.Add(this);

            base.OnStart(state);
        }

        public void ThrustOff()
        {
            if (vessel == null || vessel.ctrlState == null)
                return;

            targetThrottle = 0;
            vessel.ctrlState.mainThrottle = 0;
            tmode = TMode.OFF;
            SetFlightGlobals(0);
        }

        private void SetFlightGlobals(double throttle)
        {
            if (FlightGlobals.ActiveVessel != null && vessel == FlightGlobals.ActiveVessel)
            {
                FlightInputHandler.state.mainThrottle = (float) throttle; //so that the on-screen throttle gauge reflects the autopilot throttle
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

        /* the current throttle limit, this may include transient condition such as limiting to zero due to unstable propellants in RF */
        public float throttleLimit { get; private set; }
        /* the fixed throttle limit (i.e. user limited in the GUI), does not include transient conditions as limiting to zero due to unstable propellants in RF */
        public float throttleFixedLimit { get; private set; }

        /* This is an API for limits which are "temporary" or "conditional" (things like ullage status which will change very soon).
           This will often be temporarily zero, which means the computed accelleration of the ship will be zero, which would cause
           consumers (like the NodeExecutor) to compute infinite burntime, so this value should not be used by those consumers */
        private void setTempLimit(float limit, LimitMode mode)
        {
            throttleLimit = limit;
            limiter = mode;
        }

        /* This is an API for limits which are not temporary (like the throttle limit set in the GUI)
           The throttleFixedLimit is what consumers like the NodeExecutor should use to compute acceleration and burntime.
           This deliberately sets both values.  The actually applied throttle limit may be lower than thottleFixedLimit
           (i.e. there may be a more limiting temp limit) */
        private void setFixedLimit(float limit, LimitMode mode)
        {
            if (throttleLimit > limit) {
                throttleLimit = limit;
            }
            throttleFixedLimit = limit;
            limiter = mode;
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
                    bool useGimbal = (vesselState.torqueGimbal.positive.x > vesselState.torqueAvailable.x * 10) ||
                                     (vesselState.torqueGimbal.positive.z > vesselState.torqueAvailable.z * 10);

                    bool useDiffThrottle = (vesselState.torqueDiffThrottle.x > vesselState.torqueAvailable.x * 10) ||
                                           (vesselState.torqueDiffThrottle.z > vesselState.torqueAvailable.z * 10);

                    if ((core.attitude.attitudeError >= 2) && (useGimbal || (useDiffThrottle && core.thrust.differentialThrottle)))
                    {
                        trans_prev_thrust = targetThrottle = 0.1F;
                        print(" targetThrottle = 0.1F");
                    }
                    else
                    {
                        trans_prev_thrust = targetThrottle = 0;
                    }
                }
            }

            // Only set throttle if a module need it. Otherwise let the user or other mods set it
            // There is always at least 1 user : the module itself (why ?)
            if (users.Count > 1)
                s.mainThrottle = targetThrottle;

            throttleLimit = 1;
            throttleFixedLimit = 1;

            limiter = LimitMode.None;

            if (limitThrottle)
            {
                if (maxThrottle < throttleLimit)
                {
                    setFixedLimit((float)maxThrottle, LimitMode.Throttle);
                }
            }

            if (limitToTerminalVelocity)
            {
                float limit = TerminalVelocityThrottle();
                if (limit < throttleLimit)
                {
                    setFixedLimit(limit, LimitMode.TerminalVelocity);
                }
            }

            if (limitDynamicPressure)
            {
                float limit = MaximumDynamicPressureThrottle();
                if (limit < throttleLimit)
                {
                    setFixedLimit(limit, LimitMode.DynamicPressure);
                }
            }

            if (limitToPreventOverheats)
            {
                float limit = (float)TemperatureSafetyThrottle();
                if (limit < throttleLimit) {
                    setFixedLimit(limit, LimitMode.Temperature);
                }
            }

            if (limitAcceleration)
            {
                float limit = AccelerationLimitedThrottle();
                if (limit < throttleLimit) {
                    setFixedLimit(limit, LimitMode.Acceleration);
                }
            }

            if (electricThrottle && ElectricEngineRunning())
            {
                float limit = ElectricThrottle();
                if (limit < throttleLimit) {
                    setFixedLimit(limit, LimitMode.Electric);
                }
            }

            if (limitToPreventFlameout)
            {
                // This clause benefits being last: if we don't need much air
                // due to prior limits, we can close some intakes.
                float limit = FlameoutSafetyThrottle();
                if (limit < throttleLimit) {
                    setFixedLimit(limit, LimitMode.Flameout);
                }
            }

            // Any limiters which can limit to non-zero values must come before this, any
            // limiters (like ullage) which enforce zero throttle should come after.  The
            // minThrottle setting has authority over any other limiter that sets non-zero throttle.
            if (limiterMinThrottle && limiter != LimitMode.None)
            {
                if (minThrottle > throttleFixedLimit)
                {
                    setFixedLimit((float) minThrottle, LimitMode.MinThrottle);
                }
                if (minThrottle > throttleLimit)
                {
                    setTempLimit((float) minThrottle, LimitMode.MinThrottle);
                }
            }

            /* auto-RCS ullaging up to very stable */
            if (autoRCSUllaging && s.mainThrottle > 0.0F && throttleLimit > 0.0F )
            {
                if (vesselState.lowestUllage < VesselState.UllageState.VeryStable)
                {
                    Debug.Log("MechJeb RCS auto-ullaging: found state below very stable: " + vesselState.lowestUllage);
                    if (vessel.hasEnabledRCSModules())
                    {
                        if (!vessel.ActionGroups[KSPActionGroup.RCS])
                        {
                            Debug.Log("MechJeb RCS auto-ullaging: enabling RCS action group for automatic ullaging");
                            vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);
                        }
                        Debug.Log("MechJeb RCS auto-ullaging: firing RCS to stabilize ulllage");
                        setTempLimit(0.0F, LimitMode.UnstableIgnition);
                        s.Z = -1.0F;
                    } else {
                        Debug.Log("MechJeb RCS auto-ullaging: vessel has no enabled/staged RCS modules");
                    }
                }
            }

            /* prevent unstable ignitions */
            if (limitToPreventUnstableIgnition && s.mainThrottle > 0.0F && throttleLimit > 0.0F )
            {
                if (vesselState.lowestUllage < VesselState.UllageState.Stable)
                {
                    ScreenMessages.PostScreenMessage(preventingUnstableIgnitionsMessage);
                    Debug.Log("MechJeb Unstable Ignitions: preventing ignition in state: " + vesselState.lowestUllage);
                    setTempLimit(0.0F, LimitMode.UnstableIgnition);
                }
            }

            // we have to force the throttle here so that rssMode can work, otherwise we don't get our last throttle command
            // back on the next tick after disabling.  we save this before applying the throttle limits so that we preserve
            // the requested throttle, and not the limited throttle.
            if (core.rssMode)
            {
                SetFlightGlobals(s.mainThrottle);
            }

            if (double.IsNaN(throttleLimit)) throttleLimit = 1.0F;
            throttleLimit = Mathf.Clamp01(throttleLimit);

            /* we do not _apply_ the "fixed" limit, the actual throttleLimit should always be the more limited and lower one */
            /* the purpose of the "fixed" limit is for external consumers like the node executor to consume */
            if (double.IsNaN(throttleFixedLimit)) throttleFixedLimit = 1.0F;
            throttleFixedLimit = Mathf.Clamp01(throttleFixedLimit);

            vesselState.throttleLimit = throttleLimit;
            vesselState.throttleFixedLimit = throttleFixedLimit;

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
                differentialThrottleDemandedTorque = -Vector3d.Scale(act.xzy, vesselState.torqueDiffThrottle * s.mainThrottle * 0.5f);
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
                differentialThrottleSuccess = DifferentialThrottleStatus.Success;
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

        //A throttle setting that throttles down when the dynamic pressure exceed a set value
        float MaximumDynamicPressureThrottle()
        {
            if (maxDynamicPressure <= 0) return 1.0F;

            double pressureRatio = vesselState.dynamicPressure / maxDynamicPressure;

            if (pressureRatio < 1.0) return 1.0F; //full throttle if under maximum dynamic pressure

            //throttle down quickly as we exceed maximum dynamic pressure:
            const double falloff = 15.0;
            return Mathf.Clamp((float)(1.0 - falloff * (pressureRatio - 1.0)), 0.0F, 1.0F);
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
                if (resource.intakes.Count == 0)
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

        bool ElectricEngineRunning() {
            var activeEngines = vessel.parts.Where(p => p.inverseStage >= vessel.currentStage && p.IsEngine() && !p.IsSepratron());
            var engineModules = activeEngines.Select(p => p.Modules.OfType<ModuleEngines>().First(e => e.isEnabled));

            return engineModules.SelectMany(eng => eng.propellants).Any(p => p.id == PartResourceLibrary.ElectricityHashcode);
        }

        float ElectricThrottle()
        {
            double totalElectric = vessel.MaxResourceAmount(PartResourceLibrary.ElectricityHashcode);
            double lowJuice = totalElectric * electricThrottleLo;
            double highJuice = totalElectric * electricThrottleHi;
            double curElectric = vessel.TotalResourceAmount(PartResourceLibrary.ElectricityHashcode);

            if (curElectric <= lowJuice)
                return 0;
            if (curElectric >= highJuice)
                return 1;
            // Avoid divide by zero
            if (Math.Abs(highJuice - lowJuice) < 0.01)
                return 1;
            return Mathf.Clamp((float) ((curElectric - lowJuice) / (highJuice - lowJuice)), 0, 1);
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
                        pm.enablePitch = pm.enableRoll = pm.enableYaw = false;
                    }
                    else
                    {
                        // TODO : Check the protopart for the original values (slow) ? Or use a dict to save them (hard with save) ?
                        pm.enablePitch = pm.enableRoll = pm.enableYaw = true;
                    }
                }
            }
        }

        static void MaxThrust(double[] x, ref double func, double[] grad, object obj)
        {
            List<VesselState.EngineWrapper> el = (List<VesselState.EngineWrapper>)obj;

            func = 0;

            for (int i = 0, j = 0; j < el.Count; j++)
            {
                VesselState.EngineWrapper e = el[j];
                if (!e.engine.throttleLocked)
                {
                    func -= el[j].maxVariableForce.y * x[i];
                    grad[i] = -el[j].maxVariableForce.y;
                    i++;
                }
            }
        }

        private DifferentialThrottleStatus ComputeDifferentialThrottle(Vector3d torque)
        {
            //var stopwatch = new Stopwatch();
            //stopwatch.Start();

            float mainThrottle = vessel.ctrlState.mainThrottle;

            if (mainThrottle == 0)
            {
                torque = Vector3d.zero;
                mainThrottle = 1;
            }

            int nb_engines = vesselState.enginesWrappers.Count;

            double torqueScale = 0;
            double forceScale = 0;
            Vector3d force = new Vector3d();

            for (int i = 0; i < nb_engines; i++)
            {
                torque -= vesselState.enginesWrappers[i].constantTorque;
                torqueScale += vesselState.enginesWrappers[i].maxVariableTorque.magnitude;

                force += Vector3d.Dot(mainThrottle * vesselState.enginesWrappers[i].maxVariableForce, Vector3d.up) * Vector3d.up;
                forceScale += vesselState.enginesWrappers[i].maxVariableForce.magnitude * 10;
            }

            var engines = vesselState.enginesWrappers.Where(eng => !eng.engine.throttleLocked).ToList();
            var n = engines.Count;

            if (nb_engines == 0)
                return DifferentialThrottleStatus.AllEnginesOff;

            if (nb_engines == 1 || n == 0)
                return DifferentialThrottleStatus.MoreEnginesRequired;


            double[,] a = new double[n, n];
            double[] b = new double[n];
            double[] boundL = new double[n];
            double[] boundU = new double[n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    a[i, j] = Vector3d.Dot(engines[i].maxVariableTorque, engines[j].maxVariableTorque) / (torqueScale * torqueScale)
                              + Vector3d.Dot(engines[i].maxVariableForce, engines[j].maxVariableForce) / (forceScale * forceScale);
                }
                b[i] = -Vector3d.Dot(engines[i].maxVariableTorque, torque) / (torqueScale * torqueScale)
                       - Vector3d.Dot(engines[i].maxVariableForce, force) / (forceScale * forceScale);

                boundL[i] = 0;
                boundU[i] = mainThrottle;
            }
            alglib.minqpstate state;
            alglib.minqpcreate(n, out state);
            alglib.minqpsetquadraticterm(state, a);
            alglib.minqpsetlinearterm(state, b);
            alglib.minqpsetbc(state, boundL, boundU);
            alglib.minqpsetalgobleic(state, 0.0, 0.0, 0.0, 0);
            //var t1 = stopwatch.ElapsedMilliseconds;
            alglib.minqpoptimize(state);
            //var t2 = stopwatch.ElapsedMilliseconds;
            double[] x;
            alglib.minqpreport report;
            alglib.minqpresults(state, out x, out report);
            //var t3 = stopwatch.ElapsedMilliseconds;
            //UnityEngine.Debug.LogFormat("[DiffThrottle] t1: {0}, t2: {1}, t3: {2}", t1, t2 - t1, t3 - t2);

            if (x.Any(val => double.IsNaN(val)))
                return DifferentialThrottleStatus.SolverFailed;

            for (int i = 0; i < n; i++)
            {
                engines[i].thrustRatio = (float)(x[i] / mainThrottle);
            }

            return DifferentialThrottleStatus.Success;
        }

        public void DisableDifferentialThrottle()
        {
            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];
                for (int j = 0; j < p.Modules.Count; j++)
                {
                    PartModule pm = p.Modules[j];
                    ModuleEngines engine = pm as ModuleEngines;
                    if (engine != null)
                    {
                        engine.thrustPercentage = 100;
                    }
                }
            }
        }
    }
}

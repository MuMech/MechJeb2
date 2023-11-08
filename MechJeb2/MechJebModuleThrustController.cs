using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleThrustController : ComputerModule
    {
        public enum DifferentialThrottleStatus
        {
            SUCCESS,
            ALL_ENGINES_OFF,
            MORE_ENGINES_REQUIRED,
            SOLVER_FAILED
        }

        public MechJebModuleThrustController(MechJebCore core)
            : base(core)
        {
            Priority = 200;
        }

        public  float TransSpdAct;
        private float _transPrevThrust;
        public  bool  TransKillH = false;

        // The Terminal Velocity limiter is removed to not have to deal with users who
        // think that seeing the aerodynamic FX means they reached it.
        // And it s really high since 1.0.x anyway so the Dynamic Pressure limiter is better now
        //[Persistent(pass = (int)Pass.Global)]
        public bool LimitToTerminalVelocity = false;

        //[GeneralInfoItem("Limit to terminal velocity", InfoItem.Category.Thrust)]
        //public void LimitToTerminalVelocityInfoItem()
        //{
        //    GUIStyle s = new GUIStyle(GUI.skin.toggle);
        //    if (limiter == LimitMode.TerminalVelocity) s.onHover.textColor = s.onNormal.textColor = Color.green;
        //    limitToTerminalVelocity = GUILayout.Toggle(limitToTerminalVelocity, "Limit to terminal velocity", s);
        //}

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool LimitDynamicPressure;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble MaxDynamicPressure = 20000;

        [GeneralInfoItem("#MechJeb_LimittoMaxQ", InfoItem.Category.Thrust)] //Limit to Max Q
        public void LimitToMaxDynamicPressureInfoItem()
        {
            GUIStyle s = Limiter == LimitMode.DYNAMIC_PRESSURE ? GuiUtils.GreenToggle : null;
            GuiUtils.ToggledTextBox(ref LimitDynamicPressure, CachedLocalizer.Instance.MechJebAscentCheckbox11, MaxDynamicPressure, "pa", s, 80);
        }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool LimitToPreventOverheats;

        [GeneralInfoItem("#MechJeb_PreventEngineOverheats", InfoItem.Category.Thrust)] //Prevent engine overheats
        public void LimitToPreventOverheatsInfoItem()
        {
            GUIStyle s = Limiter == LimitMode.TEMPERATURE ? GuiUtils.GreenToggle : GuiUtils.Skin.toggle;
            LimitToPreventOverheats =
                GUILayout.Toggle(LimitToPreventOverheats, CachedLocalizer.Instance.MechJebAscentCheckbox12, s); //"Prevent engine overheats"
        }

        [ToggleInfoItem("#MechJeb_SmoothThrottle", InfoItem.Category.Thrust)] //Smooth throttle
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool SmoothThrottle;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public double ThrottleSmoothingTime = 1.0;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool LimitToPreventFlameout;

        [GeneralInfoItem("#MechJeb_PreventJetFlameout", InfoItem.Category.Thrust)] //Prevent jet flameout
        public void LimitToPreventFlameoutInfoItem()
        {
            GUIStyle s = Limiter == LimitMode.FLAMEOUT ? GuiUtils.GreenToggle : GuiUtils.Skin.toggle;
            LimitToPreventFlameout =
                GUILayout.Toggle(LimitToPreventFlameout, CachedLocalizer.Instance.MechJebAscentCheckbox13, s); //"Prevent jet flameout"
        }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool LimitToPreventUnstableIgnition;

        [GeneralInfoItem("#MechJeb_PreventUnstableIgnition", InfoItem.Category.Thrust)] //Prevent unstable ignition
        public void LimitToPreventUnstableIgnitionInfoItem()
        {
            GUIStyle s = Limiter == LimitMode.UNSTABLE_IGNITION ? GuiUtils.GreenToggle : GuiUtils.Skin.toggle;
            LimitToPreventUnstableIgnition =
                GUILayout.Toggle(LimitToPreventUnstableIgnition, CachedLocalizer.Instance.MechJebAscentCheckbox14, s); //"Prevent unstable ignition"
        }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool AutoRCSUllaging = true;

        [GeneralInfoItem("#MechJeb_UseRCStoullage", InfoItem.Category.Thrust)] //Use RCS to ullage
        public void AutoRCsUllageInfoItem()
        {
            GUIStyle s = Limiter == LimitMode.AUTO_RCS_ULLAGE ? GuiUtils.GreenToggle : GuiUtils.Skin.toggle;
            AutoRCSUllaging = GUILayout.Toggle(AutoRCSUllaging, CachedLocalizer.Instance.MechJebAscentCheckbox15, s); //"Use RCS to ullage"
        }

        // 5% safety margin on flameouts
        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble FlameoutSafetyPct = 5;

        [ToggleInfoItem("#MechJeb_ManageAirIntakes", InfoItem.Category.Thrust)] //Manage air intakes
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool ManageIntakes;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool LimitAcceleration;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble MaxAcceleration = 40;

        [GeneralInfoItem("#MechJeb_LimitAcceleration", InfoItem.Category.Thrust)] //Limit Acceleration
        public void LimitAccelerationInfoItem()
        {
            GUIStyle s = Limiter == LimitMode.ACCELERATION ? GuiUtils.GreenToggle : null;
            GuiUtils.ToggledTextBox(ref LimitAcceleration, CachedLocalizer.Instance.MechJebAscentCheckbox16, MaxAcceleration, "m/s²", s,
                30); //"Limit acceleration to"
        }

        [Persistent(pass = (int)Pass.LOCAL)]
        public bool LimitThrottle;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.LOCAL)]
        public readonly EditableDoubleMult MaxThrottle = new EditableDoubleMult(1, 0.01);

        [GeneralInfoItem("#MechJeb_LimitThrottle", InfoItem.Category.Thrust)] //Limit throttle
        public void LimitThrottleInfoItem()
        {
            GUIStyle s = Limiter == LimitMode.THROTTLE ? MaxThrottle > 0d ? GuiUtils.GreenToggle : GuiUtils.RedToggle : null;
            GuiUtils.ToggledTextBox(ref LimitThrottle, CachedLocalizer.Instance.MechJebAscentCheckbox17, MaxAcceleration, "%", s,
                30); //"Limit throttle to"
        }

        [Persistent(pass = (int)(Pass.LOCAL | Pass.TYPE | Pass.GLOBAL))]
        public bool LimiterMinThrottle = true;

        [Persistent(pass = (int)(Pass.LOCAL | Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult MinThrottle = new EditableDoubleMult(0.05, 0.01);

        [GeneralInfoItem("#MechJeb_LowerThrottleLimit", InfoItem.Category.Thrust)] //Lower throttle limit
        public void LimiterMinThrottleInfoItem()
        {
            GUIStyle s = Limiter == LimitMode.MIN_THROTTLE ? GuiUtils.GreenToggle : null;
            GuiUtils.ToggledTextBox(ref LimiterMinThrottle, CachedLocalizer.Instance.MechJebAscentCheckbox18, MinThrottle, "%", s,
                30); //"Keep limited throttle over"
        }

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.TYPE)]
        public bool DifferentialThrottle;

        [GeneralInfoItem("#MechJeb_DifferentialThrottle", InfoItem.Category.Thrust)] //Differential throttle
        public void DifferentialThrottleMenu()
        {
            bool oldDifferentialThrottle = Core.Thrust.DifferentialThrottle;
            GUIStyle s = DifferentialThrottle && Vessel.LiftedOff()
                ? Core.Thrust.DifferentialThrottleSuccess == DifferentialThrottleStatus.SUCCESS ? GuiUtils.GreenToggle : GuiUtils.YellowToggle
                : GuiUtils.Skin.toggle;
            DifferentialThrottle =
                GUILayout.Toggle(DifferentialThrottle, CachedLocalizer.Instance.MechJebAscentCheckbox19, s); //"Differential throttle"

            if (oldDifferentialThrottle && !Core.Thrust.DifferentialThrottle)
                Core.Thrust.DisableDifferentialThrottle();
        }

        public Vector3d DifferentialThrottleDemandedTorque;

        public DifferentialThrottleStatus DifferentialThrottleSuccess = DifferentialThrottleStatus.SUCCESS;

        [Persistent(pass = (int)Pass.LOCAL)]
        public bool ElectricThrottle;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.LOCAL)]
        public readonly EditableDoubleMult ElectricThrottleLo = new EditableDoubleMult(0.05, 0.01);

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.LOCAL)]
        public readonly EditableDoubleMult ElectricThrottleHi = new EditableDoubleMult(0.15, 0.01);

        [GeneralInfoItem("#MechJeb_ElectricLimit", InfoItem.Category.Thrust)] //Electric limit
        public void LimitElectricInfoItem()
        {
            GUILayout.BeginHorizontal();
            GUIStyle s = GuiUtils.Skin.label;
            if (Limiter == LimitMode.ELECTRIC)
            {
                s = VesselState.throttleLimit < 0.001 ? GuiUtils.RedLabel : GuiUtils.YellowLabel;
            }
            else if (ElectricEngineRunning()) s = GuiUtils.GreenLabel;

            ElectricThrottle =
                GUILayout.Toggle(ElectricThrottle, CachedLocalizer.Instance.MechJebAscentCheckbox20, s,
                    GuiUtils.LayoutWidth(110)); //"Electric limit Lo"
            GuiUtils.SimpleTextField(ElectricThrottleLo, 30);
            GUILayout.Label("% Hi", GuiUtils.ExpandWidth(false));
            GuiUtils.SimpleTextField(ElectricThrottleHi, 30);
            GUILayout.Label("%", GuiUtils.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }

        public enum LimitMode
        {
            NONE, TERMINAL_VELOCITY, TEMPERATURE, FLAMEOUT, ACCELERATION, THROTTLE, DYNAMIC_PRESSURE, MIN_THROTTLE, ELECTRIC, UNSTABLE_IGNITION,
            AUTO_RCS_ULLAGE
        }

        public LimitMode Limiter = LimitMode.NONE;

        public float TargetThrottle;

        private bool _tmodeChanged;

        private double _ullageUntil;

        private PIDController _pid;

        public  float LastThrottle;
        private bool  _userCommandingRotation => _userCommandingRotationSmoothed > 0;
        private int   _userCommandingRotationSmoothed;
        private bool  _lastDisableThrusters;

        public enum TMode
        {
            OFF,
            KEEP_ORBITAL,
            KEEP_SURFACE,
            KEEP_VERTICAL,
            DIRECT
        }

        private TMode _tmode = TMode.OFF;

        public TMode Tmode
        {
            get => _tmode;
            set
            {
                if (_tmode != value)
                {
                    _tmode        = value;
                    _tmodeChanged = true;
                }
            }
        }

        private ScreenMessage _preventingUnstableIgnitionsMessage;

        public override void OnStart(PartModule.StartState state)
        {
            _preventingUnstableIgnitionsMessage =
                new ScreenMessage(Localizer.Format("#MechJeb_Ascent_srcmsg1"), 2f,
                    ScreenMessageStyle.UPPER_CENTER); //"<color=orange>[MechJeb]: Killing throttle to prevent unstable ignition</color>"
            _pid = new PIDController(0.05, 0.000001, 0.05);
            Users.Add(this);

            base.OnStart(state);
        }

        public void ThrustOff()
        {
            if (Vessel == null || Vessel.ctrlState == null)
                return;

            TargetThrottle                = 0;
            Vessel.ctrlState.mainThrottle = 0;
            Tmode                         = TMode.OFF;
            SetFlightGlobals(0);
        }

        private void SetFlightGlobals(double throttle)
        {
            if (FlightGlobals.ActiveVessel != null && Vessel == FlightGlobals.ActiveVessel)
            {
                FlightInputHandler.state.mainThrottle = (float)throttle; //so that the on-screen throttle gauge reflects the autopilot throttle
            }
        }

        // Call this function to set the throttle for a burn with dV delta-V remaining.
        // timeConstant controls how quickly we throttle down toward the end of the burn.
        // This function is nice because it will correctly handle engine spool-up/down times.
        public void ThrustForDV(double dV, double timeConstant)
        {
            timeConstant += VesselState.maxEngineResponseTime;
            double spooldownDV = VesselState.currentThrustAccel * VesselState.maxEngineResponseTime;
            double desiredAcceleration = (dV - spooldownDV) / timeConstant;

            TargetThrottle = Mathf.Clamp01((float)(desiredAcceleration / VesselState.maxThrustAccel));
        }

        /* the current throttle limit, this may include transient condition such as limiting to zero due to unstable propellants in RF */
        [UsedImplicitly]
        public float ThrottleLimit { get; set; }

        /* the fixed throttle limit (i.e. user limited in the GUI), does not include transient conditions as limiting to zero due to unstable propellants in RF */
        [UsedImplicitly]
        public float ThrottleFixedLimit { get; set; }

        /* This is an API for limits which are "temporary" or "conditional" (things like ullage status which will change very soon).
           This will often be temporarily zero, which means the computed accelleration of the ship will be zero, which would cause
           consumers (like the NodeExecutor) to compute infinite burntime, so this value should not be used by those consumers */
        private void SetTempLimit(float limit, LimitMode mode)
        {
            ThrottleLimit = limit;
            Limiter       = mode;
        }

        /* This is an API for limits which are not temporary (like the throttle limit set in the GUI)
           The throttleFixedLimit is what consumers like the NodeExecutor should use to compute acceleration and burntime.
           This deliberately sets both values.  The actually applied throttle limit may be lower than thottleFixedLimit
           (i.e. there may be a more limiting temp limit) */
        private void SetFixedLimit(float limit, LimitMode mode)
        {
            if (ThrottleLimit > limit)
            {
                ThrottleLimit = limit;
            }

            ThrottleFixedLimit = limit;
            Limiter            = mode;
        }

        public override void Drive(FlightCtrlState s)
        {
            const float THRESHOLD = 0.1F;
            bool userCommandingRotation = !(Mathfx.Approx(s.pitch, s.pitchTrim, THRESHOLD)
                                            && Mathfx.Approx(s.yaw, s.yawTrim, THRESHOLD)
                                            && Mathfx.Approx(s.roll, s.rollTrim, THRESHOLD));
            bool userCommandingTranslation = !(Math.Abs(s.X) < THRESHOLD
                                               && Math.Abs(s.Y) < THRESHOLD
                                               && Math.Abs(s.Z) < THRESHOLD);

            if (userCommandingRotation && !userCommandingTranslation)
            {
                _userCommandingRotationSmoothed = 2;
            }
            else if (_userCommandingRotationSmoothed > 0)
            {
                _userCommandingRotationSmoothed--;
            }

            if (Core.GetComputerModule<MechJebModuleThrustWindow>().Hidden && Core.GetComputerModule<MechJebModuleAscentMenu>().Hidden) { return; }

            if (Tmode != TMode.OFF && VesselState.thrustAvailable > 0)
            {
                double spd = 0;

                switch (Tmode)
                {
                    case TMode.KEEP_ORBITAL:
                        spd = VesselState.speedOrbital;
                        break;
                    case TMode.KEEP_SURFACE:
                        spd = VesselState.speedSurface;
                        break;
                    case TMode.KEEP_VERTICAL:
                        spd = VesselState.speedVertical;
                        if (TransKillH)
                        {
                            var hsdir = Vector3.ProjectOnPlane(VesselState.surfaceVelocity, VesselState.up);
                            Vector3 dir = -hsdir + VesselState.up * Math.Max(Math.Abs(spd), 20 * MainBody.GeeASL);
                            Vector3d rot;
                            if (Math.Min(VesselState.altitudeASL, VesselState.altitudeTrue) > 5000 &&
                                hsdir.magnitude > Math.Max(Math.Abs(spd), 100 * MainBody.GeeASL) * 2)
                            {
                                Tmode       = TMode.DIRECT;
                                TransSpdAct = 100;
                                rot         = -hsdir;
                            }
                            else
                            {
                                rot = dir.normalized;
                            }

                            Core.Attitude.attitudeTo(rot, AttitudeReference.INERTIAL, null);
                        }

                        break;
                }

                double tErr = (TransSpdAct - spd) / VesselState.maxThrustAccel;
                if ((Tmode == TMode.KEEP_ORBITAL && Vector3d.Dot(VesselState.forward, VesselState.orbitalVelocity) < 0) ||
                    (Tmode == TMode.KEEP_SURFACE && Vector3d.Dot(VesselState.forward, VesselState.surfaceVelocity) < 0))
                {
                    //allow thrust to declerate
                    tErr *= -1;
                }

                double tAct = _pid.Compute(tErr);

                if (Tmode != TMode.KEEP_VERTICAL
                    || !TransKillH
                    || Core.Attitude.attitudeError < 2
                    || (Math.Min(VesselState.altitudeASL, VesselState.altitudeTrue) < 1000 && Core.Attitude.attitudeError < 90))
                {
                    if (Tmode == TMode.DIRECT)
                    {
                        _transPrevThrust = TargetThrottle = TransSpdAct / 100.0F;
                    }
                    else
                    {
                        _transPrevThrust = TargetThrottle = Mathf.Clamp01(_transPrevThrust + (float)tAct);
                    }
                }
                else
                {
                    bool useGimbal = VesselState.torqueGimbal.Positive.x > VesselState.torqueAvailable.x * 10 ||
                                     VesselState.torqueGimbal.Positive.z > VesselState.torqueAvailable.z * 10;

                    bool useDiffThrottle = VesselState.torqueDiffThrottle.x > VesselState.torqueAvailable.x * 10 ||
                                           VesselState.torqueDiffThrottle.z > VesselState.torqueAvailable.z * 10;

                    if (Core.Attitude.attitudeError >= 2 && (useGimbal || (useDiffThrottle && Core.Thrust.DifferentialThrottle)))
                    {
                        _transPrevThrust = TargetThrottle = 0.1F;
                        Print(" targetThrottle = 0.1F");
                    }
                    else
                    {
                        _transPrevThrust = TargetThrottle = 0;
                    }
                }
            }

            // Only set throttle if a module need it. Otherwise let the user or other mods set it
            // There is always at least 1 user : the module itself (why ?)
            if (Users.Count > 1)
                s.mainThrottle = TargetThrottle;

            ThrottleLimit      = 1;
            ThrottleFixedLimit = 1;

            Limiter = LimitMode.NONE;

            if (LimitThrottle)
            {
                if (MaxThrottle < ThrottleLimit)
                {
                    SetFixedLimit((float)MaxThrottle, LimitMode.THROTTLE);
                }
            }

            if (LimitToTerminalVelocity)
            {
                float limit = TerminalVelocityThrottle();
                if (limit < ThrottleLimit)
                {
                    SetFixedLimit(limit, LimitMode.TERMINAL_VELOCITY);
                }
            }

            if (LimitDynamicPressure)
            {
                float limit = MaximumDynamicPressureThrottle();
                if (limit < ThrottleLimit)
                {
                    SetFixedLimit(limit, LimitMode.DYNAMIC_PRESSURE);
                }
            }

            if (LimitToPreventOverheats)
            {
                float limit = (float)TemperatureSafetyThrottle();
                if (limit < ThrottleLimit)
                {
                    SetFixedLimit(limit, LimitMode.TEMPERATURE);
                }
            }

            if (LimitAcceleration)
            {
                float limit = AccelerationLimitedThrottle();
                if (limit < ThrottleLimit)
                {
                    SetFixedLimit(limit, LimitMode.ACCELERATION);
                }
            }

            if (ElectricThrottle && ElectricEngineRunning())
            {
                float limit = ElectricThrottleLimit();
                if (limit < ThrottleLimit)
                {
                    SetFixedLimit(limit, LimitMode.ELECTRIC);
                }
            }

            if (LimitToPreventFlameout)
            {
                // This clause benefits being last: if we don't need much air
                // due to prior limits, we can close some intakes.
                float limit = FlameoutSafetyThrottle();
                if (limit < ThrottleLimit)
                {
                    SetFixedLimit(limit, LimitMode.FLAMEOUT);
                }
            }

            // Any limiters which can limit to non-zero values must come before this, any
            // limiters (like ullage) which enforce zero throttle should come after.  The
            // minThrottle setting has authority over any other limiter that sets non-zero throttle.
            if (LimiterMinThrottle && Limiter != LimitMode.NONE)
            {
                if (MinThrottle > ThrottleFixedLimit)
                {
                    SetFixedLimit((float)MinThrottle, LimitMode.MIN_THROTTLE);
                }

                if (MinThrottle > ThrottleLimit)
                {
                    SetTempLimit((float)MinThrottle, LimitMode.MIN_THROTTLE);
                }
            }

            ProcessUllage(s);

            /* prevent unstable ignitions */
            if (LimitToPreventUnstableIgnition && s.mainThrottle > 0.0F && ThrottleLimit > 0.0F)
            {
                if (VesselState.lowestUllage < 0.95) // needs to match 'unstable' value in RF
                {
                    ScreenMessages.PostScreenMessage(_preventingUnstableIgnitionsMessage);
                    Debug.Log("MechJeb Unstable Ignitions: preventing ignition in state: " + VesselState.lowestUllage);
                    SetTempLimit(0.0F, LimitMode.UNSTABLE_IGNITION);
                }
            }

            // we have to force the throttle here so that rssMode can work, otherwise we don't get our last throttle command
            // back on the next tick after disabling.  we save this before applying the throttle limits so that we preserve
            // the requested throttle, and not the limited throttle.
            if (Core.RssMode)
            {
                SetFlightGlobals(s.mainThrottle);
            }

            if (double.IsNaN(ThrottleLimit)) ThrottleLimit = 1.0F;
            ThrottleLimit = Mathf.Clamp01(ThrottleLimit);

            /* we do not _apply_ the "fixed" limit, the actual throttleLimit should always be the more limited and lower one */
            /* the purpose of the "fixed" limit is for external consumers like the node executor to consume */
            if (double.IsNaN(ThrottleFixedLimit)) ThrottleFixedLimit = 1.0F;
            ThrottleFixedLimit = Mathf.Clamp01(ThrottleFixedLimit);

            VesselState.throttleLimit      = ThrottleLimit;
            VesselState.throttleFixedLimit = ThrottleFixedLimit;

            if (s.mainThrottle < ThrottleLimit) Limiter = LimitMode.NONE;

            s.mainThrottle = Mathf.Min(s.mainThrottle, ThrottleLimit);

            if (SmoothThrottle)
            {
                s.mainThrottle = ApplySmoothThrottle(s.mainThrottle);
            }

            if (double.IsNaN(s.mainThrottle)) s.mainThrottle = 0;

            s.mainThrottle = Mathf.Clamp01(s.mainThrottle);


            if (s.Z == 0 && Core.RCS.rcsThrottle && VesselState.rcsThrust) s.Z = -s.mainThrottle;

            LastThrottle = s.mainThrottle;

            if (!Core.Attitude.Enabled)
            {
                var act = new Vector3d(s.pitch, s.yaw, s.roll);
                DifferentialThrottleDemandedTorque = -Vector3d.Scale(act.xzy, VesselState.torqueDiffThrottle * s.mainThrottle * 0.5f);
            }
        }

        public override void OnFixedUpdate() =>
            DifferentialThrottleSuccess =
                DifferentialThrottle ? ComputeDifferentialThrottle(DifferentialThrottleDemandedTorque) : DifferentialThrottleStatus.SUCCESS;

        //A throttle setting that throttles down when the vertical velocity of the ship exceeds terminal velocity
        private float TerminalVelocityThrottle()
        {
            if (VesselState.altitudeASL > MainBody.RealMaxAtmosphereAltitude()) return 1.0F;

            double velocityRatio = Vector3d.Dot(VesselState.surfaceVelocity, VesselState.up) / VesselState.TerminalVelocity();

            if (velocityRatio < 1.0) return 1.0F; //full throttle if under terminal velocity

            //throttle down quickly as we exceed terminal velocity:
            const double FALLOFF = 15.0;
            return Mathf.Clamp((float)(1.0 - FALLOFF * (velocityRatio - 1.0)), 0.0F, 1.0F);
        }

        //A throttle setting that throttles down when the dynamic pressure exceed a set value
        private float MaximumDynamicPressureThrottle()
        {
            if (MaxDynamicPressure <= 0) return 1.0F;

            double pressureRatio = VesselState.dynamicPressure / MaxDynamicPressure;

            if (pressureRatio < 1.0) return 1.0F; //full throttle if under maximum dynamic pressure

            //throttle down quickly as we exceed maximum dynamic pressure:
            const double FALLOFF = 15.0;
            return Mathf.Clamp((float)(1.0 - FALLOFF * (pressureRatio - 1.0)), 0.0F, 1.0F);
        }

        //a throttle setting that throttles down if something is close to overheating
        private double TemperatureSafetyThrottle()
        {
            double maxTempRatio = Vessel.parts.Max(p => p.temperature / p.maxTemp);

            //reduce throttle as the max temp. ratio approaches 1 within the safety margin
            const double TEMP_SAFETY_MARGIN = 0.05f;
            if (maxTempRatio < 1 - TEMP_SAFETY_MARGIN) return 1.0F;
            return (1 - maxTempRatio) / TEMP_SAFETY_MARGIN;
        }

        private float ApplySmoothThrottle(float mainThrottle) =>
            Mathf.Clamp(mainThrottle,
                (float)(LastThrottle - VesselState.deltaT / ThrottleSmoothingTime),
                (float)(LastThrottle + VesselState.deltaT / ThrottleSmoothingTime));

        private float FlameoutSafetyThrottle()
        {
            float throttle = 1;

            // For every resource that is provided by intakes, find how
            // much we need at full throttle, add a safety margin, and
            // that's the max throttle for that resource.  Take the min of
            // the max throttles.
            foreach (VesselState.ResourceInfo resource in VesselState.resources.Values)
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

                double margin = 1 + 0.01 * FlameoutSafetyPct;
                double safeRequirement = margin * resource.requiredAtMaxThrottle;
                safeRequirement = Math.Max(safeRequirement, resource.required);

                // Open the right number of intakes.
                if (ManageIntakes)
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
        private void OptimizeIntakes(VesselState.ResourceInfo info, double requiredFlow)
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
            foreach (VesselState.ResourceInfo.IntakeData intakeData in info.intakes)
            {
                ModuleResourceIntake intake = intakeData.intake;
                data[intake] = intakeData;
                if (groupIds.ContainsKey(intake)) { continue; }

                // Create a group for this symmetry
                int grpId = groups.Count;
                var intakes = new List<ModuleResourceIntake>();
                groups.Add(intakes);

                // In DFS order, get all the symmetric parts.
                // We can't rely on symmetryCounterparts; see issue #52 by tavert:
                // https://github.com/MuMech/MechJeb2/issues/52
                var stack = new Stack<Part>();
                stack.Push(intake.part);
                while (stack.Count > 0)
                {
                    Part part = stack.Pop();
                    ModuleResourceIntake partIntake = part.Modules.OfType<ModuleResourceIntake>().FirstOrDefault();
                    if (partIntake == null || groupIds.ContainsKey(partIntake))
                    {
                        continue;
                    }

                    groupIds[partIntake] = grpId;
                    intakes.Add(partIntake);

                    for (int i = 0; i < part.symmetryCounterparts.Count; i++)
                    {
                        Part sympart = part.symmetryCounterparts[i];
                        stack.Push(sympart);
                    }
                }
            }

            // For each group in sorted order, if we need more air, open any
            // closed intakes.  If we have enough air, close any open intakes.
            // OrderBy is stable, so we'll always be opening the same intakes.
            double airFlowSoFar = 0;
            var param = new KSPActionParam(KSPActionGroup.None,
                KSPActionType.Activate);
            foreach (List<ModuleResourceIntake> grp in groups.OrderBy(grp => grp.Count))
            {
                if (airFlowSoFar < requiredFlow)
                {
                    for (int i = 0; i < grp.Count; i++)
                    {
                        ModuleResourceIntake intake = grp[i];
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
                        ModuleResourceIntake intake = grp[j];
                        if (intake.intakeEnabled)
                        {
                            intake.ToggleAction(param);
                        }
                    }
                }
            }
        }

        private bool ElectricEngineRunning()
        {
            IEnumerable<Part> activeEngines = Vessel.parts.Where(p => p.inverseStage >= Vessel.currentStage && p.IsEngine() && !p.IsSepratron());
            IEnumerable<ModuleEngines> engineModules = activeEngines.Select(p => p.Modules.OfType<ModuleEngines>().First(e => e.isEnabled));

            return engineModules.SelectMany(eng => eng.propellants).Any(p => p.id == PartResourceLibrary.ElectricityHashcode);
        }

        private float ElectricThrottleLimit()
        {
            double totalElectric = Vessel.MaxResourceAmount(PartResourceLibrary.ElectricityHashcode);
            double lowJuice = totalElectric * ElectricThrottleLo;
            double highJuice = totalElectric * ElectricThrottleHi;
            double curElectric = Vessel.TotalResourceAmount(PartResourceLibrary.ElectricityHashcode);

            if (curElectric <= lowJuice)
                return 0;
            if (curElectric >= highJuice)
                return 1;
            // Avoid divide by zero
            if (Math.Abs(highJuice - lowJuice) < 0.01)
                return 1;
            return Mathf.Clamp((float)((curElectric - lowJuice) / (highJuice - lowJuice)), 0, 1);
        }

        //The throttle setting that will give an acceleration of maxAcceleration
        private float AccelerationLimitedThrottle()
        {
            double throttleForMaxAccel = (MaxAcceleration - VesselState.minThrustAccel) / (VesselState.maxThrustAccel - VesselState.minThrustAccel);
            return Mathf.Clamp((float)throttleForMaxAccel, 0, 1);
        }

        public override void OnUpdate()
        {
            if (Core.GetComputerModule<MechJebModuleThrustWindow>().Hidden && Core.GetComputerModule<MechJebModuleAscentMenu>().Hidden)
            {
                return;
            }

            if (_tmodeChanged)
            {
                if (TransKillH && Tmode == TMode.OFF)
                {
                    Core.Attitude.attitudeDeactivate();
                }

                _pid.Reset();
                _tmodeChanged = false;
                FlightInputHandler.SetNeutralControls();
            }

            bool disableThrusters = _userCommandingRotation && !Core.RCS.rcsForRotation;
            if (disableThrusters != _lastDisableThrusters)
            {
                _lastDisableThrusters = disableThrusters;
                List<ModuleRCS> rcsModules = Vessel.FindPartModulesImplementing<ModuleRCS>();
                foreach (ModuleRCS pm in rcsModules)
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

        private static void MaxThrust(double[] x, ref double func, double[] grad, object obj)
        {
            var el = (List<VesselState.EngineWrapper>)obj;

            func = 0;

            for (int i = 0, j = 0; j < el.Count; j++)
            {
                VesselState.EngineWrapper e = el[j];
                if (e.engine.throttleLocked) continue;

                func    -= el[j].maxVariableForce.y * x[i];
                grad[i] =  -el[j].maxVariableForce.y;
                i++;
            }
        }

        /// <summary>
        ///     Handles auto-RCS ullaging up to very stable.
        /// </summary>
        /// <param name="s"></param>
        private void ProcessUllage(FlightCtrlState s)
        {
            // seconds to continue to apply RCS after ullage has settled to VeryStable
            const double MIN_RCS_TIME = 0.50;
            // seconds after VeryStable to wait before applying throttle
            const double IGNITION_DELAY = 0.10;

            if (!AutoRCSUllaging || s.mainThrottle <= 0F || ThrottleLimit <= 0F)
                return;

            if (!Vessel.hasEnabledRCSModules())
                return;

            if (VesselState.lowestUllage >= 1.0 && VesselState.time > _ullageUntil)
                return;

            if (VesselState.lowestUllage < 1.0)
                _ullageUntil = VesselState.time + MIN_RCS_TIME;

            // limit the throttle only if we aren't already burning (don't waste ignitions)
            // also apply RCS for IGNITION_DELAY seconds above VeryStable point.
            if (LastThrottle <= 0 && _ullageUntil - VesselState.time < MIN_RCS_TIME - IGNITION_DELAY)
                SetTempLimit(0.0F, LimitMode.AUTO_RCS_ULLAGE);

            if (!Vessel.ActionGroups[KSPActionGroup.RCS])
                Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);

            s.Z = -1.0F;
        }

        private DifferentialThrottleStatus ComputeDifferentialThrottle(Vector3d torque)
        {
            //var stopwatch = new  Stopwatch();
            //stopwatch.Start();

            float mainThrottle = Vessel.ctrlState.mainThrottle;

            if (mainThrottle == 0)
            {
                torque       = Vector3d.zero;
                mainThrottle = 1;
            }

            int nbEngines = VesselState.enginesWrappers.Count;

            double torqueScale = 0;
            double forceScale = 0;
            var force = new Vector3d();

            for (int i = 0; i < nbEngines; i++)
            {
                torque      -= VesselState.enginesWrappers[i].constantTorque;
                torqueScale += VesselState.enginesWrappers[i].maxVariableTorque.magnitude;

                force      += Vector3d.Dot(mainThrottle * VesselState.enginesWrappers[i].maxVariableForce, Vector3d.up) * Vector3d.up;
                forceScale += VesselState.enginesWrappers[i].maxVariableForce.magnitude * 10;
            }

            var engines = VesselState.enginesWrappers.Where(eng => !eng.engine.throttleLocked).ToList();
            int n = engines.Count;

            if (nbEngines == 0)
                return DifferentialThrottleStatus.ALL_ENGINES_OFF;

            if (nbEngines == 1 || n == 0)
                return DifferentialThrottleStatus.MORE_ENGINES_REQUIRED;


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

            alglib.minqpcreate(n, out alglib.minqpstate state);
            alglib.minqpsetquadraticterm(state, a, false);
            alglib.minqpsetlinearterm(state, b);
            alglib.minqpsetbc(state, boundL, boundU);
            alglib.minqpsetalgobleic(state, 0.0, 0.0, 0.0, 0);
            //var t1 = stopwatch.ElapsedMilliseconds;
            alglib.minqpoptimize(state);
            //var t2 = stopwatch.ElapsedMilliseconds;
            alglib.minqpresults(state, out double[] x, out alglib.minqpreport _);
            //var t3 = stopwatch.ElapsedMilliseconds;
            //UnityEngine.Debug.LogFormat("[DiffThrottle] t1: {0}, t2: {1}, t3: {2}", t1, t2 - t1, t3 - t2);

            if (x.Any(double.IsNaN))
                return DifferentialThrottleStatus.SOLVER_FAILED;

            for (int i = 0; i < n; i++)
            {
                engines[i].thrustRatio = (float)(x[i] / mainThrottle);
            }

            return DifferentialThrottleStatus.SUCCESS;
        }

        private void DisableDifferentialThrottle()
        {
            for (int i = 0; i < Vessel.parts.Count; i++)
            {
                Part p = Vessel.parts[i];
                for (int j = 0; j < p.Modules.Count; j++)
                {
                    PartModule pm = p.Modules[j];
                    var engine = pm as ModuleEngines;
                    if (engine != null)
                    {
                        engine.thrustPercentage = 100;
                    }
                }
            }
        }
    }
}

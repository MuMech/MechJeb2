// CTGIGLandingStep.cs
// Powered + terminal descent guidance for MechJeb2 dev branch.
//
// Drop-in landing step for MechJebModuleLandingAutopilot.
// Handles everything from PDI detection through touchdown.
// Replace DecelerationBurn + FinalDescent with this one class.
//
// Usage in MechJebModuleLandingAutopilot after plane-change / deorbit:
//     _currentPhase = new CTGIGLandingStep(Core) { LandAtTarget = LandAtTarget };
//
// Solvers (verbatim from standalone CTGIGPlugin):
//   - DCTGIG powered descent (SolveDCTGIG_Outer / IterateCTGCore)
//   - Gravity-turn terminal law (Han, Jo & Ho arXiv:2409.01465)
//   - Apollo zero-jerk terminal law

using System;
using System.Text;
using UnityEngine;

namespace MuMech.Landing
{
    public partial class CTGIGLandingStep : AutopilotStep
    {
        // -----------------------------------------------------------------------
        // State machine
        // -----------------------------------------------------------------------

        private enum CTGIGState { Coasting, PoweredDescent, TerminalDescent }
        private CTGIGState _ctgState = CTGIGState.Coasting;

        // -----------------------------------------------------------------------
        // Mode flags — set by MechJebModuleLandingAutopilot before handing off
        // -----------------------------------------------------------------------

        public bool LandAtTarget      = true;   // true → use Core.target coords; false → use worldTargetPos
        public bool UseApolloTerminal = false;   // Apollo zero-jerk vs gravity-turn terminal law

        // -----------------------------------------------------------------------
        // Solver parameters
        // -----------------------------------------------------------------------

        public double TargetClearance = 100.0;  // terrain clearance above landing site [m]
        public double PlanThrottle    = 0.95;   // nominal throttle fraction used in solver
        public double MinThrottle = 0.375;
        public double TerminalHandoverDownrange = 1000.0;  
        public double TerminalGlideConstraint = 15.0; // Constraint cone for approach    

        private const double GuidancePeriodDescent  = 1.0;   // solver cadence [s]
        private const double GuidancePeriodTerminal = 0.1;

        private double _dt_tol    = 1e-2;
        private double _f_tf_tol  = 1e-2;

        private bool _landAnywhereTargetSet = false;
        private double _landAnywhereLat = double.NaN;
        private double _landAnywhereLon = double.NaN;
        bool _terminalClearanceZeroed = false;

        // PWM Test
        public bool PulseThrottleMode = false;
        public double PulsePeriod = 1.0;       // seconds
        public double PulseMinOnTime = 0.10;   // seconds
        public double PulseMinOffTime = 0.10;  // seconds
        private double _pulseTimer = 0.0;

        // -----------------------------------------------------------------------
        // Public telemetry — read by MechJebModuleLandingGuidance window
        // -----------------------------------------------------------------------

        public double  TimeToGo        { get; private set; }
        public double  RequiredThrust  { get; private set; }
        public double  DvUsed          { get; private set; }
        public double  DownrangeToTgt  { get; private set; }
        public double  PredDownrange   { get; private set; }
        public double  DownrangeError  { get; private set; }
        public double  CurrentTWR      { get; private set; }
        public double  AvailableTWR    { get; private set; }
        public double  LocalG          { get; private set; }
        public float   CurrentThrottle { get; private set; }

        // Verbose solver telemetry
        public int    DbgInnerIter  { get; private set; }
        public int    DbgOuterIter  { get; private set; }
        public double DbgTfInitial  { get; private set; }
        public double DbgTfLast     { get; private set; }
        public double DbgFTf        { get; private set; }
        public double DbgDfDtf      { get; private set; }
        public double DbgDxDt       { get; private set; }
        public double DbgTMagBest   { get; private set; }
        public string DbgNullReason { get; private set; } = "";
        public string DbgStage      { get; private set; } = "idle";
        public string DbgIterLog    { get; private set; } = "";

        // -----------------------------------------------------------------------
        // Private guidance state
        // -----------------------------------------------------------------------

        private double  _current_tf = 0.0;
        private double  _current_T  = 0.0;
        private float   _targetThrottle = 0f;
        private Vector3d _targetPointing = Vector3d.up;
        private Vector3d _worldTargetPos = Vector3d.zero;  // body-relative; used in LandAnywhere mode

        private double _guidanceStartMass = -1.0;
        private double _nextGuidanceUpdateUT = 0.0;
        private double _guidancePeriod = GuidancePeriodDescent;

        // Solver scratch — updated each guidance tick
        private double   _availableThrust = 0.0;
        private double   _rawMaxThrust    = 0.0;
        private double   _Ve              = 0.0;
        private double   _mass            = 0.0;

        // Debug scratch written during solver
        private double   _dbgDet         = 0.0;
        private double   _dbgYfNominal   = 0.0;
        private double   _dbgYfUsed      = 0.0;
        private double   _dbgLastFTf     = 0.0;
        private double   _dbgLastDfDtf   = 0.0;
        private Vector3d _dbgFHat        = Vector3d.zero;  // solver x_hat snapshot for nudge fallback
        private Vector3d _dbgCHat        = Vector3d.zero;  // solver z_hat snapshot

        // -----------------------------------------------------------------------
        // Constructor
        // -----------------------------------------------------------------------

        public CTGIGLandingStep(MechJebCore core) : base(core)
        {
            Status = "CTGIG: initialized";
        }

        // -----------------------------------------------------------------------
        // AutopilotStep interface
        // -----------------------------------------------------------------------

        /// <summary>
        /// Called once per physics tick by MechJebModuleLandingAutopilot.
        /// Returns this to continue; returns null when landed / guidance should stop.
        /// </summary>
        public override AutopilotStep OnFixedUpdate()
        {
            Vessel vessel    = Vessel;
            VesselState vs   = VesselState;
            CelestialBody cb = MainBody;

            if (vessel == null || cb == null) return this;

            if (vessel.LandedOrSplashed)
            {
                _targetThrottle = 0f;
                Core.Thrust.ThrustOff();
                Core.Landing.StopLanding();
                Status = "Touchdown. Guidance complete.";
                return null;
            }

            if (!UpdateShipStats())
            {
                _targetThrottle = 0f;
                Core.Thrust.ThrustOff();
                Status = "CTGIG: waiting for active engine.";
                return this;
            }

            double ut       = vs.time;
            Vector3d pos    = vs.CoM - cb.position;
            Vector3d vel    = vs.surfaceVelocity;
            double altRadar = Math.Max(0.0, vs.altitudeBottom);
            double R        = cb.Radius;
            double mu       = cb.gravParameter;

            UpdatePerformanceStats(pos, _mass, _availableThrust, _Ve);

            // --- Solve target position / frame ---
            Vector3d targetPos;
            Vector3d x_hat, y_hat, z_hat;
            bool landAnywhereNeedsTarget = !LandAtTarget && _worldTargetPos.sqrMagnitude <= 1e-6;

            if (landAnywhereNeedsTarget)
            {
                // Land Anywhere: no fixed target exists yet.
                // Use the original CTGIG concept: build a velocity-aligned frame,
                // run one coast solve, then use that solution's x_f as the target.
                if (!VelocityAlignedFrame(pos, vel, out x_hat, out y_hat, out z_hat))
                {
                    Status = "Land Anywhere: horizontal velocity too small.";
                    return this;
                }

                // Dummy radial target used only to define terminal radial velocity and radius.
                // It is replaced as soon as the coast solve produces a valid x_f.
                targetPos = (cb.Radius + TargetClearance) * y_hat;
            }
            else
            {
                if (!TryGetEffectiveTarget(out _, out _, out targetPos, out _))
                    return this;

                if (!SolverFrame(pos, vel, targetPos, out x_hat, out y_hat, out z_hat))
                {
                    x_hat = Vector3d.right;
                    y_hat = Vector3d.up;
                    z_hat = Vector3d.forward;
                }
            }

            _dbgFHat = x_hat;
            _dbgCHat = z_hat;

            Vector3d r_loc = new Vector3d(Vector3d.Dot(pos, x_hat), Vector3d.Dot(pos, y_hat), Vector3d.Dot(pos, z_hat));
            Vector3d v_loc = new Vector3d(Vector3d.Dot(vel, x_hat), Vector3d.Dot(vel, y_hat), Vector3d.Dot(vel, z_hat));

            // touchdown target velocity: small inward radial speed
            Vector3d vf_global = targetPos.magnitude > 1e-6 ? -3.0 * targetPos.normalized : Vector3d.zero;
            Vector3d vf_loc    = new Vector3d(Vector3d.Dot(vf_global, x_hat),
                                              Vector3d.Dot(vf_global, y_hat),
                                              Vector3d.Dot(vf_global, z_hat));

            double target_x_loc = Vector3d.Dot(targetPos - pos, x_hat);
            double R_target = targetPos.magnitude;

            if (landAnywhereNeedsTarget)
            {
                // No target exists yet; the first valid coast solve will create one.
                target_x_loc = double.PositiveInfinity;
                R_target = cb.Radius + TargetClearance;
            }

            DownrangeToTgt = target_x_loc;

            // --- Guidance cadence gate ---
            bool runSolver = ut >= _nextGuidanceUpdateUT;

            if (runSolver)
            {
                _nextGuidanceUpdateUT = ut + _guidancePeriod;

                // ================================================================
                // COASTING — probe solver; ignite when x_loc closes to target
                // ================================================================
                if (_ctgState == CTGIGState.Coasting)
                {
                    _targetThrottle = 0f;
                    if (vel.sqrMagnitude > 1e-6) _targetPointing = -vel.normalized;

                    double testThrust = _availableThrust * PlanThrottle;
                    double alpha = (_mass * _Ve) / Math.Max(1.0, testThrust);

                    if (_current_tf <= 0.0 || !IsFinite(_current_tf))
                    {
                        double safeVel = Math.Max(50.0, Math.Abs(v_loc.x));
                        _current_tf = alpha * (1.0 - Math.Exp(-safeVel / _Ve));
                    }

                    CTGResult coast = IterateCTG(_current_tf, r_loc, v_loc, vf_loc, _Ve, alpha, testThrust, mu, R_target, 3);

                    if (coast != null && coast.converged && IsFinite(coast.x_f) && IsFinite(coast.tf) && coast.tf > 0.0)
                    {
                        _current_tf = coast.tf;
                        PredDownrange = coast.x_f;
                        DownrangeError = IsFinite(target_x_loc) ? PredDownrange - target_x_loc : 0.0;
                        _current_T = testThrust;

                        if (!LandAtTarget && !_landAnywhereTargetSet)
                        {
                            if (SetLandAnywhereTargetFromCoastSolution(PredDownrange, x_hat, y_hat, cb))
                            {
                                // Land Anywhere concept: one valid coast solution defines the target;
                                // after that, immediately continue into powered guidance on the next tick.
                                if (_guidanceStartMass < 0.0) _guidanceStartMass = _mass;
                                _guidancePeriod = GuidancePeriodDescent;
                                _ctgState = CTGIGState.PoweredDescent;
                                _nextGuidanceUpdateUT = ut;
                                Status = $"Land Anywhere target set. PDI initiated. x={PredDownrange:F0}m";
                                return this;
                            }

                            Status = "Land Anywhere: coast solve valid but target creation failed.";
                            return this;
                        }

                        bool targetReady = LandAtTarget || _landAnywhereTargetSet;
                        if (targetReady && target_x_loc <= PredDownrange + 10.0 && altRadar > 100.0)
                        {
                            if (_guidanceStartMass < 0.0) _guidanceStartMass = _mass;
                            _guidancePeriod = GuidancePeriodDescent;
                            _ctgState = CTGIGState.PoweredDescent;
                            Status = "PDI initiated.";
                        }
                        else
                        {
                            Status = IsFinite(target_x_loc)
                                ? $"Coasting. Pred={PredDownrange:F0}m Tgt={target_x_loc:F0}m"
                                : $"Coasting. Pred={PredDownrange:F0}m";
                        }
                    }
                    else
                    {
                        PredDownrange = double.NaN;
                        DownrangeError = double.NaN;
                        Status = $"Coast solver invalid. tf0={_current_tf:F3}, Ttest={testThrust:F1}";
                    }
                }

                // ================================================================
                // POWERED DESCENT — DCTGIG solver
                // ================================================================
                else if (_ctgState == CTGIGState.PoweredDescent)
                {
                    CTGResult res = SolveDCTGIG_Outer(r_loc, v_loc, vf_loc, target_x_loc, _mass, _current_T, _Ve, mu, R_target, 3);

                    if (res != null) ApplyDbgFromResult(res);

                    if (res != null && res.converged && IsFinite(res.x_f) && IsFinite(res.tf) && IsFinite(res.T_mag))
                    {
                        _current_T = res.T_mag;
                        _current_tf = res.tf;
                        DownrangeError = res.x_f - target_x_loc;
                        PredDownrange = res.x_f;

                        _targetThrottle = _availableThrust > 1e-6
                            ? Mathf.Clamp((float)(_current_T / _availableThrust), 0.01f, 1.0f)
                            : 0f;

                        // Build world-space thrust direction from guidance coefficients
                        double lam4_0     = 1.0 / Math.Sqrt(1.0 + res.C2 * res.C2 + res.C3 * res.C3);
                        Vector3d u_cmd_loc = new Vector3d(-lam4_0, res.C2 * lam4_0, res.C3 * lam4_0);
                        _targetPointing   = (u_cmd_loc.x * x_hat + u_cmd_loc.y * y_hat + u_cmd_loc.z * z_hat).normalized;

                        Status = $"Powered. TGO={_current_tf:F1}s T={_current_T / 1000.0:F2}kN";

                        // Transition to terminal when close to target
                        if (IsFinite(_current_tf) && (target_x_loc < TerminalHandoverDownrange || altRadar < 30.0))
                        {
                            TargetClearance = 0.0;

                            _guidancePeriod = GuidancePeriodTerminal;
                            _ctgState = CTGIGState.TerminalDescent;
                            Status = "Terminal descent initiated.";
                        }
                    }
                    else
                    {
                        Status = $"Solver null. x_tgt={target_x_loc:F0}m";
                    }
                }

                // ================================================================
                // TERMINAL DESCENT — GT or Apollo law
                // ================================================================
                else if (_ctgState == CTGIGState.TerminalDescent)
                {
                    if (vessel.LandedOrSplashed)
                    {
                        _targetThrottle = 0f;
                        Status = "Touchdown. Guidance complete.";
                        return null; // signal autopilot we're done
                    }
                    if (altRadar < 0.5 && vel.magnitude < 1.0)
                    {
                        _targetThrottle = 0f;
                        Core.Thrust.ThrustOff();
                        Core.Landing.StopLanding();
                        Status = "Contact. Disengaging.";
                        return null;
                    }

                    if (UseApolloTerminal)
                    {
                        ApolloGuidanceOutput ap = ComputeApolloZeroJerkCommand(
                            pos, vel, targetPos, x_hat, y_hat, z_hat, mu, _mass, _availableThrust,
                            vfWorldOpt:    -0.1 * y_hat,
                            aFinalWorldOpt: 0.2 * y_hat,
                            jFinalWorldOpt: Vector3d.zero,
                            tgoGuess: 10.0, tgoAxis: 1);

                        bool badApollo = !ap.valid
                            || !IsFinite(ap.tgo) || ap.tgo <= 0.0 || ap.tgo > 500.0
                            || !IsFinite(ap.requiredThrust);
                        bool badKin = !IsFinite(altRadar) || altRadar < -5.0
                            || vel.magnitude > 500.0
                            || Vector3d.Dot(vel, y_hat) < -100.0;

                        if (badApollo || badKin || _availableThrust <= 1e-3)
                        {
                            _targetThrottle = 0f;
                            Status = $"Apollo aborted. v={ap.valid} tgo={ap.tgo:F1} alt={altRadar:F1}";
                            return this;
                        }

                        _targetPointing = ap.thrustDirWorld;
                        _targetThrottle = ap.throttle;
                        _current_tf     = ap.tgo;
                        _current_T      = ap.requiredThrust;
                        Status = $"Apollo TGO={ap.tgo:F1}s";
                    }
                    else
                    {
                        // Gravity-turn terminal law (Han, Jo & Ho arXiv:2409.01465)

                        GTGuidanceOutput gt = ComputeGravityTurnCommand(
                            pos, vel, targetPos, x_hat, y_hat, z_hat,
                            mu, _mass, _availableThrust, _Ve,
                            Tmin_abs:    _availableThrust * MinThrottle,
                            C_b:         0.95,   k_gain:      2.4,
                            C_e:         20.0,   phi_rad:     TerminalGlideConstraint * Math.PI / 180.0,
                            delta:       5.0,    C_col_lower: 0.75,
                            C_col_upper: 0.95);

                        bool badGT = !gt.valid
                            || !IsFinite(gt.tgo) || gt.tgo <= 0.0 || gt.tgo > 500.0
                            || !IsFinite(gt.requiredThrust);

                        if (badGT || _availableThrust <= 1e-3)
                        {
                            _targetThrottle = 0f;
                            Status = $"GT aborted: {gt.reason}";
                            return this;
                        }

                        // Guidance
                        _targetPointing = gt.thrustDirWorld;
                        _targetThrottle = gt.throttle;
                        _current_tf     = gt.tgo;
                        _current_T      = gt.requiredThrust;

                        Status = $"GT: {gt.reason} gammaErr={target_x_loc:F1} clr={TargetClearance:F1}m";

                    }
                }
            } // end runSolver gate

            // --- Coasting attitude: point retrograde every tick ---
            if (_ctgState == CTGIGState.Coasting && vel.sqrMagnitude > 1e-6)
                _targetPointing = -vel.normalized;

            TimeToGo       = _current_tf;
            RequiredThrust = _current_T;
            CurrentThrottle = _targetThrottle;

            // --- Attitude command via MechJeb attitude controller ---
            if (_targetPointing.sqrMagnitude > 1e-6)
                Core.Attitude.attitudeTo(
                    _targetPointing.normalized,
                    AttitudeReference.INERTIAL,
                    Core.Landing,
                    killRollRotation: true);

            return this;
        }

        /// <summary>
        /// Drive is called every physics tick after OnFixedUpdate.
        /// Sets throttle. Attitude is driven by Core.Attitude.
        /// </summary>
        public override AutopilotStep Drive(FlightCtrlState s)
        {
            float rawThrottle = _targetThrottle;
            float throttleCmd = rawThrottle;

            if (PulseThrottleMode)
                throttleCmd = PulseThrottleCommand(rawThrottle);

            Core.Thrust.RequestActiveThrottle(throttleCmd, allowZero: true);

            CurrentThrottle = throttleCmd;
            // Status = // debug only
            //     $"PWM={PulseThrottleMode} raw={rawThrottle:F2} cmd={throttleCmd:F2} " +
            //     $"period={PulsePeriod:F2} timer={_pulseTimer:F2}";

            return this;
        }



        // -----------------------------------------------------------------------
        // Public helpers — called by LandingGuidance or LandingAutopilot
        // -----------------------------------------------------------------------

        private float PulseThrottleCommand(float commandedThrottle)
        {
            commandedThrottle = Mathf.Clamp01(commandedThrottle);

            if (commandedThrottle <= 0.0f)
            {
                _pulseTimer = 0.0;
                return 0.0f;
            }

            if (commandedThrottle >= 0.999f)
            {
                _pulseTimer = 0.0;
                return 1.0f;
            }

            double period = Math.Max(0.2, PulsePeriod);

            _pulseTimer += TimeWarp.fixedDeltaTime;
            while (_pulseTimer >= period)
                _pulseTimer -= period;

            double onTime = commandedThrottle * period;

            if (onTime < 0.05)
                return 0.0f;

            if ((period - onTime) < 0.05)
                return 1.0f;

            return _pulseTimer < onTime ? 1.0f : 0.0f;
        }

        /// <summary>
        /// Override target in LandAnywhere mode using velocity-aligned frame.
        /// Call from MechJebModuleLandingAutopilot after LandUntargeted() is invoked.
        /// </summary>
        



        /// <summary>
        /// Nudge the landing target downrange (+) / uprange (-) and left (-) / right (+) of LOS.
        /// Uses a fresh LOS from vessel to target so buttons are always frame-correct.
        /// </summary>
        

        // -----------------------------------------------------------------------
        // Internal helpers
        // -----------------------------------------------------------------------

       private static string Fmt(Vector3d v)
        {
            return PDGMathUtils.FormatVector(v);
        }

        private static bool IsFinite(double v)
        {
            return PDGMathUtils.IsFinite(v);
        }

        /// <summary>
        /// Returns the current guidance target in body-relative space.
        /// In LandAtTarget mode reads Core.Target; otherwise uses worldTargetPos.
        /// </summary>
        

        

        

        /// <summary>
        /// Reads engine stats directly from KSP part modules.
        /// Returns false if no active engines.
        /// </summary>
        private bool UpdateShipStats()
        {
            PDGEngineState engineState = PDGEngine.ReadEngineState(Vessel);

            if (!engineState.Valid)
                return false;

            _mass = engineState.Mass;
            _rawMaxThrust = engineState.RawMaxThrust;
            _availableThrust = engineState.AvailableThrust;
            _Ve = engineState.EffectiveExhaustVelocity;

            return true;
        }

        private void UpdatePerformanceStats(Vector3d pos, double mass, double availThrust, double ve)
        {
            double r  = Math.Max(1.0, pos.magnitude);
            double mu = MainBody.gravParameter;
            LocalG = mu / (r * r);

            AvailableTWR = availThrust > 1e-6 ? availThrust / (mass * LocalG) : 0.0;
            CurrentTWR   = _current_T > 1e-6 ? _current_T  / (mass * LocalG) : 0.0;
            DvUsed       = (_guidanceStartMass > mass && mass > 1e-6 && ve > 1e-6)
                            ? ve * Math.Log(_guidanceStartMass / mass) : 0.0;

        }

        private void ApplyDbgFromResult(CTGResult res)
        {
            if (res == null) return;
            DbgInnerIter  = res.iterations;
            DbgTfInitial  = res.tf_initial;
            DbgTfLast     = res.tf;
            DbgFTf        = res.last_f_tf;
            DbgDfDtf      = res.last_df_dtf;
            DbgNullReason = res.null_reason ?? "";
            DbgStage      = res.stage ?? "";
            DbgIterLog    = res.iteration_log ?? "";
        }

        private void ResetInnerDebug(double tf0)
        {
            DbgInnerIter = 0; DbgTfInitial = tf0; DbgTfLast = tf0;
            DbgFTf = 0; DbgDfDtf = 0; _dbgDet = 0;
            _dbgYfNominal = 0; _dbgYfUsed = 0; DbgNullReason = ""; DbgStage = "running"; DbgIterLog = "";
        }

        // -----------------------------------------------------------------------
        // Ground marker (flight view and map view)
        // Uses MechJeb's GLUtils so we don't need the custom material setup.
        // Call from OnRenderObject of the parent MechJebModuleLandingAutopilot
        // by exposing TargetLatLon as public properties.
        // -----------------------------------------------------------------------



        // -----------------------------------------------------------------------
        // SOLVERS (verbatim from CTGIGPlugin.cs — no changes)
        // -----------------------------------------------------------------------

    }
}
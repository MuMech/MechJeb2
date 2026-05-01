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
    public class CTGIGLandingStep : AutopilotStep
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
        private bool SetLandAnywhereTargetFromCoastSolution(
            double downrange,
            Vector3d xHat,
            Vector3d yHat,
            CelestialBody body)
        {
            if (LandAtTarget)
                return false;

            if (!IsFinite(downrange) || downrange <= 0.0)
            {
                Status = $"Land Anywhere: invalid coast solution x={downrange:F3}";
                return false;
            }

            if (body == null || xHat.magnitude < 1e-6 || yHat.magnitude < 1e-6)
            {
                Status = "Land Anywhere: invalid frame.";
                return false;
            }

            xHat = xHat.normalized;
            yHat = yHat.normalized;

            // 1. Use predicted downrange to get an initial surface lat/lon guess.
            Vector3d relGuess = downrange * xHat + body.Radius * yHat;
            Vector3d worldGuess = body.position + relGuess;

            body.GetLatLonAlt(worldGuess, out double guessLat, out double guessLon, out _);

            if (!IsFinite(guessLat) || !IsFinite(guessLon))
            {
                Status = "Land Anywhere: invalid guessed lat/lon.";
                return false;
            }

            // 2. Query terrain at the guessed lat/lon.
            double guessTerrainAlt = GetTerrainAltitude(body, guessLat, guessLon);

            // 3. Use terrain-adjusted radius only to refine the final lat/lon.
            double guessTargetRadius = body.Radius + guessTerrainAlt + TargetClearance;
            Vector3d relTargetGuess = downrange * xHat + guessTargetRadius * yHat;
            Vector3d worldTargetGuess = body.position + relTargetGuess;

            body.GetLatLonAlt(worldTargetGuess, out double finalLat, out double finalLon, out _);

            if (!IsFinite(finalLat) || !IsFinite(finalLon))
            {
                Status = "Land Anywhere: invalid final lat/lon.";
                return false;
            }

            // 4. Re-query terrain at the final lat/lon.
            double finalTerrainAlt = GetTerrainAltitude(body, finalLat, finalLon);

            // 5. Build the actual held target from lat/lon + terrain + clearance.
            // This avoids relying on the solver-frame vector to preserve altitude.
            _worldTargetPos = body.GetWorldSurfacePosition(
                finalLat,
                finalLon,
                finalTerrainAlt + TargetClearance
            );

            // 6. Store once. From now on TryGetEffectiveTarget() holds this lat/lon.
            _landAnywhereLat = finalLat;
            _landAnywhereLon = finalLon;
            _landAnywhereTargetSet = true;

            Core.Target.SetPositionTarget(body, finalLat, finalLon);

            Status =
                $"Land Anywhere target set. x={downrange:F0}m " +
                $"terrain={finalTerrainAlt:F1}m clearance={TargetClearance:F1}m " +
                $"lat={finalLat:F4} lon={finalLon:F4}";

            return true;
        }

        private double GetTerrainAltitude(CelestialBody body, double lat, double lon)
        {
            if (body.pqsController != null)
            {
                Vector3d radial = body.GetRelSurfaceNVector(lat, lon);
                return body.pqsController.GetSurfaceHeight(radial) - body.pqsController.radius;
            }

            return body.TerrainAltitude(lat, lon);
        }

        /// <summary>
        /// Nudge the landing target downrange (+) / uprange (-) and left (-) / right (+) of LOS.
        /// Uses a fresh LOS from vessel to target so buttons are always frame-correct.
        /// </summary>
        public void NudgeTargetMeters(double downrangeMeters, double crossrangeMeters)
        {
            Vessel vessel    = Vessel;
            CelestialBody cb = vessel.mainBody;
            if (!TryGetEffectiveTarget(out _, out _, out Vector3d targetPosBody, out _)) return;

            Vector3d targetUp = targetPosBody.normalized;
            if (targetUp.sqrMagnitude < 1e-10) return;

            // Fresh LOS from vessel to target, projected onto target tangent plane
            Vector3d vesselBCI = vessel.GetWorldPos3D() - cb.position;
            Vector3d los       = targetPosBody - vesselBCI;
            Vector3d downDir   = Vector3d.Exclude(targetUp, los);
            if (downDir.sqrMagnitude < 1e-10) downDir = Vector3d.Exclude(targetUp, _dbgFHat);
            if (downDir.sqrMagnitude < 1e-10) return;
            downDir.Normalize();

            Vector3d crossDir = Vector3d.Cross(targetUp, downDir);
            if (crossDir.sqrMagnitude < 1e-10) return;
            crossDir.Normalize();

            Vector3d shifted = targetPosBody + downrangeMeters * downDir + crossrangeMeters * crossDir;
            shifted = shifted.normalized * targetPosBody.magnitude;

            cb.GetLatLonAlt(cb.position + shifted, out double newLat, out double newLon, out _);
            double terrAlt = cb.TerrainAltitude(newLat, newLon);
            _worldTargetPos = cb.GetWorldSurfacePosition(newLat, newLon, terrAlt + TargetClearance);

            // Also update MechJeb target so the map marker moves
            if (LandAtTarget)
                Core.Target.SetPositionTarget(cb, newLat, newLon);
        }

        // -----------------------------------------------------------------------
        // Internal helpers
        // -----------------------------------------------------------------------

        private static string Fmt(Vector3d v) { return $"[{v.x:F3},{v.y:F3},{v.z:F3}]"; }
        private static bool IsFinite(double v) { return !double.IsNaN(v) && !double.IsInfinity(v); }

        /// <summary>
        /// Returns the current guidance target in body-relative space.
        /// In LandAtTarget mode reads Core.Target; otherwise uses worldTargetPos.
        /// </summary>
        private bool TryGetEffectiveTarget(
            out double lat,
            out double lon,
            out Vector3d targetPosBody,
            out Vector3d targetWorld)
        {
            CelestialBody cb = MainBody;
            lat = lon = 0.0;
            targetPosBody = targetWorld = Vector3d.zero;

            if (cb == null)
                return false;

            

            // Targeted mode: use MechJeb's selected target.
            if (LandAtTarget)
            {
                lat = Core.Target.targetLatitude;
                lon = Core.Target.targetLongitude;

                if (!IsFinite(lat) || !IsFinite(lon))
                    return false;

                double terrAlt = GetTerrainAltitude(cb, lat, lon);
                targetWorld = cb.GetWorldSurfacePosition(lat, lon, terrAlt + TargetClearance);
                targetPosBody = targetWorld - cb.position;
                return true;
            }

            // Land Anywhere mode: once selected, hold the lat/lon fixed.
            if (_landAnywhereTargetSet &&
                IsFinite(_landAnywhereLat) &&
                IsFinite(_landAnywhereLon))
            {
                lat = _landAnywhereLat;
                lon = _landAnywhereLon;

                double terrAlt = GetTerrainAltitude(cb, lat, lon);
                targetWorld = cb.GetWorldSurfacePosition(lat, lon, terrAlt + TargetClearance);
                targetPosBody = targetWorld - cb.position;

                _worldTargetPos = targetWorld;
                return true;
            }

            // Land Anywhere has not been solved/locked yet.
            return false;
        }

        private bool SolverFrame(Vector3d pos, Vector3d vel, Vector3d targetPos, out Vector3d xHat, out Vector3d yHat, out Vector3d zHat)
        {
            yHat = pos.magnitude > 1e-6 ? pos.normalized : Vector3d.up;
            Vector3d d = targetPos - pos;
            Vector3d d_horiz  = d - Vector3d.Dot(d, yHat) * yHat;
            Vector3d horizVel = Vector3d.Exclude(yHat, vel);
            xHat = d_horiz.magnitude > 0.1
                ? d_horiz.normalized
                : (horizVel.magnitude > 1e-6 ? horizVel.normalized : Vector3d.right);
            zHat = Vector3d.Cross(yHat, xHat);
            if (zHat.magnitude < 1e-6) zHat = Vector3d.forward;
            zHat = zHat.normalized;
            xHat = Vector3d.Cross(zHat, yHat).normalized;
            return xHat.magnitude > 1e-6 && yHat.magnitude > 1e-6 && zHat.magnitude > 1e-6;
        }

        private bool VelocityAlignedFrame(Vector3d pos, Vector3d vel, out Vector3d xHat, out Vector3d yHat, out Vector3d zHat)
        {
            yHat = pos.magnitude > 1e-6 ? pos.normalized : Vector3d.up;

            Vector3d horizVel = Vector3d.Exclude(yHat, vel);
            if (horizVel.magnitude < 1e-3)
            {
                xHat = Vector3d.right;
                zHat = Vector3d.forward;
                return false;
            }

            xHat = horizVel.normalized;
            zHat = Vector3d.Cross(yHat, xHat);

            if (zHat.magnitude < 1e-6)
            {
                zHat = Vector3d.forward;
                return false;
            }

            zHat = zHat.normalized;
            xHat = Vector3d.Cross(zHat, yHat).normalized;

            return xHat.magnitude > 1e-6 && yHat.magnitude > 1e-6 && zHat.magnitude > 1e-6;
        }

        /// <summary>
        /// Reads engine stats directly from KSP part modules.
        /// Returns false if no active engines.
        /// </summary>
        private bool UpdateShipStats()
        {
            Vessel vessel = Vessel;
            if (vessel == null) return false;

            _mass = vessel.totalMass * 1000.0;  // tonnes → kg
            _rawMaxThrust    = 0.0;
            _availableThrust = 0.0;
            _Ve              = 0.0;

            double massFlow = 0.0;
            foreach (Part p in vessel.parts)
            {
                foreach (ModuleEngines eng in p.FindModulesImplementing<ModuleEngines>())
                {
                    if (!(eng.EngineIgnited && eng.isOperational && eng.maxThrust > 0)) continue;
                    double rawN      = eng.maxThrust * 1000.0;
                    double limitFrac = Math.Max(0.0, Math.Min(1.0, eng.thrustPercentage * 0.01));
                    double thrustN   = rawN * limitFrac;
                    double isp       = eng.realIsp > 0 ? eng.realIsp : eng.atmosphereCurve.Evaluate(0);
                    if (!IsFinite(isp) || isp <= 1e-6 || thrustN <= 1e-6) continue;
                    _rawMaxThrust    += rawN;
                    _availableThrust += thrustN;
                    massFlow         += thrustN / (isp * 9.80665);
                }
            }
            if (_availableThrust <= 0 || massFlow <= 0) return false;
            _Ve = _availableThrust / massFlow;
            return IsFinite(_Ve) && _mass > 0;
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

        public bool TryGetTargetLatLon(out double lat, out double lon)
        {
            if (!TryGetEffectiveTarget(out lat, out lon, out _, out _)) return false;
            return true;
        }

        // -----------------------------------------------------------------------
        // SOLVERS (verbatim from CTGIGPlugin.cs — no changes)
        // -----------------------------------------------------------------------

        #region CTGResult struct

        private class CTGResult
        {
            public double   tf, T_mag, x_f, dx_dT;
            public double[] rho;
            public Vector3d[] g_coeffs;
            public double   lam2, lam3, C2, C3;
            public bool     converged;
            public int      iterations;
            public double   tf_initial, last_f_tf, last_df_dtf, last_det;
            public double   y_f_nominal, y_f_used, R_effective;
            public string   stage, null_reason, iteration_log;
        }

        #endregion

        #region DCTGIG outer loop

        private CTGResult SolveDCTGIG_Outer(
            Vector3d r0, Vector3d v0, Vector3d vf,
            double target_x, double m0, double T_guess, double Ve,
            double mu, double R, int poly_order)
        {
            double T_mag = Math.Max(_availableThrust * 0.1,
                           Math.Min(_availableThrust,
                                    T_guess > 0 ? T_guess : _availableThrust * PlanThrottle));
            double tf_current = _current_tf > 0 ? _current_tf : 1.0;
            CTGResult best = null;

            for (int k = 0; k < 10; k++)
            {
                DbgOuterIter = k + 1;
                double alpha = (m0 * Ve) / Math.Max(1.0, T_mag);
                CTGResult res = IterateCTG(tf_current, r0, v0, vf, Ve, alpha, T_mag, mu, R, poly_order);
                if (res == null || !res.converged || !IsFinite(res.x_f) || !IsFinite(res.dx_dT)) break;

                tf_current = res.tf;
                best = res;

                double err = res.x_f - target_x;
                if (Math.Abs(err) < 0.1) break;

                DbgDxDt = res.dx_dT;
                // Limit T change to 1% per outer step to smooth throttle
                T_mag = T_mag - Math.Max(T_mag * -0.01, Math.Min(T_mag * 0.01, err / res.dx_dT));
                T_mag = Math.Max(_availableThrust * 0.1, Math.Min(_availableThrust, T_mag));
                best.T_mag = T_mag;
                DbgTMagBest = T_mag;
            }
            return (best != null && best.converged) ? best : null;
        }

        #endregion

        #region DCTGIG inner iterator

        private CTGResult IterateCTG(
            double tf0, Vector3d r0, Vector3d v0, Vector3d vf,
            double Ve, double alpha, double T, double mu, double R, int poly_order)
        {
            ResetInnerDebug(tf0);
            CTGResult res = IterateCTGCore(tf0, r0, v0, vf, Ve, alpha, T, mu, R, poly_order);
            return res;
        }

        private CTGResult IterateCTGCore(
            double tf0, Vector3d r0, Vector3d v0, Vector3d vf,
            double Ve, double alpha, double T, double mu, double R, int poly_order)
        {
            // Local helper to build a failed result
            CTGResult Fail(string stage, string reason, int iters, double tfC, double fTf, double dfDtf, double det, double yNom, double yUsed, string log)
            {
                return new CTGResult {
                    converged = false, tf = tfC, tf_initial = tf0, iterations = iters,
                    last_f_tf = fTf, last_df_dtf = dfDtf, last_det = det,
                    y_f_nominal = yNom, y_f_used = yUsed, R_effective = R,
                    stage = stage, null_reason = reason, iteration_log = log ?? ""
                };
            }

            if (!IsFinite(tf0) || !IsFinite(alpha) || !IsFinite(T) || !IsFinite(Ve) ||
                tf0 <= 0 || alpha <= 0 || T <= 0 || Ve <= 0)
                return Fail("input", "invalid scalar input", 0, tf0, 0, 0, 0, 0, 0, "");

            double tf = Math.Min(tf0, alpha - 1e-3);
            if (tf <= 1e-3) return Fail("input", "tf <= 1e-3", 0, tf, 0, 0, 0, 0, 0, "");

            double[]   rho      = new double[poly_order + 1]; rho[0] = 1.0;
            Vector3d[] g_coeffs = new Vector3d[poly_order + 1];
            double     lam2 = 0, lam3 = 0, C2 = 0, C3 = 0;
            double     dx_dT_total = 0;
            StringBuilder iterSb = new StringBuilder();

            for (int j = 0; j < 50; j++)
            {
                DbgInnerIter = j + 1;
                double tf_old = tf;
                DbgTfLast     = tf;

                GetBaseIntegrals(tf, Ve, alpha, T, poly_order + 1,
                    out double[] Ib, out double[] Jb, out double[] Is, out double[] Js);
                Vector3d dv_g = CalcGravVel(tf, g_coeffs, poly_order);
                Vector3d dr_g = CalcGravPos(tf, g_coeffs, poly_order);

                double Ir0 = 0, Ir1 = 0, Jr0 = 0, Jr1 = 0;
                for (int i = 0; i <= poly_order; i++)
                {
                    Ir0 += rho[i] * Ib[i]; Ir1 += rho[i] * Ib[i + 1];
                    Jr0 += rho[i] * Jb[i]; Jr1 += rho[i] * Jb[i + 1];
                }

                double x_f = r0.x + v0.x * tf + dr_g.x - Jr0;
                double z_f = 0.0;
                double r_horiz_sq = x_f * x_f + z_f * z_f;

                _dbgYfNominal = r_horiz_sq < R * R
                    ?  Math.Sqrt(R * R - r_horiz_sq)
                    : -Math.Sqrt(r_horiz_sq - R * R);
                _dbgYfUsed = _dbgYfNominal;

                Vector3d V_diff = vf - v0 - dv_g;
                double   Y      = _dbgYfUsed - r0.y - v0.y * tf - dr_g.y;
                double   Z      = z_f        - r0.z - v0.z * tf - dr_g.z;

                double det = (Jr1 * Ir0) - (Ir1 * Jr0);
                _dbgDet = det;
                if (!IsFinite(det) || Math.Abs(det) < 1e-12)
                    return Fail("matrix", "det too small", j + 1, tf, 0, 0, det, _dbgYfNominal, _dbgYfUsed, iterSb.ToString());

                lam2 = (Jr0 * V_diff.y - Ir0 * Y) / det;
                C2   = (Jr1 * V_diff.y - Ir1 * Y) / det;
                lam3 = (Jr0 * V_diff.z - Ir0 * Z) / det;
                C3   = (Jr1 * V_diff.z - Ir1 * Z) / det;

                int    n_samples = 12;
                double[] t_samp  = new double[n_samples];
                for (int i = 0; i < n_samples; i++) t_samp[i] = tf * i / (n_samples - 1.0);

                double rho_0 = 1.0 / Math.Sqrt(1.0 + C2 * C2 + C3 * C3);
                rho[0] = rho_0;

                if (poly_order > 0)
                {
                    double[] t_nonzero = new double[n_samples - 1];
                    double[] tgt_shifted = new double[n_samples - 1];
                    for (int i = 1; i < n_samples; i++)
                    {
                        t_nonzero[i - 1]  = t_samp[i];
                        double lam4_bar   = 1.0 / Math.Sqrt(1.0
                            + Math.Pow(-lam2 * t_samp[i] + C2, 2)
                            + Math.Pow(-lam3 * t_samp[i] + C3, 2));
                        tgt_shifted[i - 1] = (lam4_bar - rho_0) / t_samp[i];
                    }
                    double[] rho_rest = ManualPolyFit(t_nonzero, tgt_shifted, poly_order - 1);
                    if (rho_rest == null)
                        return Fail("rho", "polyfit rho failed", j + 1, tf, 0, 0, det, _dbgYfNominal, _dbgYfUsed, iterSb.ToString());
                    for (int i = 0; i < poly_order; i++) rho[i + 1] = rho_rest[i];
                }

                double[] gx = new double[n_samples], gy = new double[n_samples], gz = new double[n_samples];
                for (int i = 0; i < n_samples; i++)
                {
                    Vector3d rt = PredictPositionAtT(t_samp[i], r0, v0, rho, g_coeffs, lam2, C2, lam3, C3, Ve, alpha, T);
                    Vector3d g  = -(mu / Math.Pow(Math.Max(1.0, rt.magnitude), 3)) * rt;
                    gx[i] = g.x; gy[i] = g.y; gz[i] = g.z;
                }

                double[] gxFit = ManualPolyFit(t_samp, gx, poly_order);
                double[] gyFit = ManualPolyFit(t_samp, gy, poly_order);
                double[] gzFit = ManualPolyFit(t_samp, gz, poly_order);
                if (gxFit == null || gyFit == null || gzFit == null)
                    return Fail("gravity", "polyfit g failed", j + 1, tf, 0, 0, det, _dbgYfNominal, _dbgYfUsed, iterSb.ToString());
                for (int i = 0; i <= poly_order; i++) g_coeffs[i] = new Vector3d(gxFit[i], gyFit[i], gzFit[i]);

                Vector3d newDvg  = CalcGravVel(tf, g_coeffs, poly_order);
                double   newVx   = vf.x - v0.x - newDvg.x;
                double   newIr0  = 0;
                for (int i = 0; i <= poly_order; i++) newIr0 += rho[i] * Ib[i];

                double f_tf   = newVx + newIr0;
                double tau_tf = Ve / Math.Max(1e-6, alpha - tf);
                double rho_tf = 0, gx_tf = 0;
                for (int i = 0; i <= poly_order; i++)
                {
                    rho_tf += rho[i] * Math.Pow(tf, i);
                    gx_tf  += g_coeffs[i].x * Math.Pow(tf, i);
                }
                double df_dtf = tau_tf * rho_tf - gx_tf;

                _dbgLastFTf  = f_tf; _dbgLastDfDtf = df_dtf;
                iterSb.AppendLine(string.Format("j={0} tf={1:F6} f={2:E3} df={3:E3}", j, tf, f_tf, df_dtf));

                if (!IsFinite(df_dtf) || Math.Abs(df_dtf) < 1e-12)
                    return Fail("newton", "df_dtf too small", j + 1, tf, f_tf, df_dtf, det, _dbgYfNominal, _dbgYfUsed, iterSb.ToString());

                double tf_new = tf - (f_tf / df_dtf);
                if (!IsFinite(tf_new))
                    return Fail("newton", "tf_new non-finite", j + 1, tf, f_tf, df_dtf, det, _dbgYfNominal, _dbgYfUsed, iterSb.ToString());

                tf_new        = Math.Max(1e-3, Math.Min(alpha - 1e-3, tf_new));
                double dt_tf  = tf_new - tf_old;
                tf            = tf_new;
                DbgTfLast     = tf;

                if (Math.Abs(dt_tf) < _dt_tol && Math.Abs(f_tf) < _f_tf_tol)
                {
                    double Sv = 0, Sr = 0;
                    for (int i = 0; i <= poly_order; i++) { Sv += rho[i] * Is[i]; Sr -= rho[i] * Js[i]; }
                    double dtf_dT   = -Sv / df_dtf;
                    dx_dT_total     = Sr + vf.x * dtf_dT;

                    return new CTGResult {
                        tf = tf, rho = rho, g_coeffs = g_coeffs,
                        lam2 = lam2, lam3 = lam3, C2 = C2, C3 = C3,
                        x_f = r0.x + v0.x * tf + dr_g.x - Jr0,
                        dx_dT = dx_dT_total, T_mag = T, converged = true, iterations = j + 1,
                        tf_initial = tf0, last_f_tf = f_tf, last_df_dtf = df_dtf, last_det = det,
                        y_f_nominal = _dbgYfNominal, y_f_used = _dbgYfUsed, R_effective = R,
                        stage = "converged", null_reason = "", iteration_log = iterSb.ToString()
                    };
                }
            }
            return Fail("iterate", "max iterations", DbgInnerIter, tf, _dbgLastFTf, _dbgLastDfDtf, _dbgDet, _dbgYfNominal, _dbgYfUsed, iterSb.ToString());
        }

        #endregion

        #region Integral and polynomial helpers

        private void GetBaseIntegrals(double t, double Ve, double alpha, double T, int max_order,
            out double[] Ib, out double[] Jb, out double[] Is, out double[] Js)
        {
            Ib = new double[max_order + 2]; Is = new double[max_order + 2];
            Jb = new double[max_order + 1]; Js = new double[max_order + 1];

            double denom = Math.Max(0.01, alpha - t);
            Ib[0] = Ve * Math.Log(alpha / denom);
            Is[0] = (Ve * t) / (T * denom);

            for (int n = 1; n < max_order + 2; n++)
            {
                Ib[n] = alpha * Ib[n - 1] - (Ve * Math.Pow(t, n) / n);
                Is[n] = alpha * Is[n - 1] - (alpha / T) * Ib[n - 1];
            }
            for (int n = 0; n < max_order + 1; n++)
            {
                Jb[n] = t * Ib[n] - Ib[n + 1];
                Js[n] = t * Is[n] - Is[n + 1];
            }
        }

        private double[] ManualPolyFit(double[] xData, double[] yData, int order)
        {
            if (xData == null || yData == null || xData.Length != yData.Length ||
                xData.Length == 0 || order < 0) return null;

            int n = xData.Length, nv = order + 1;
            double xMax = 0.0;
            for (int k = 0; k < n; k++) if (Math.Abs(xData[k]) > xMax) xMax = Math.Abs(xData[k]);
            if (xMax < 1e-12) xMax = 1.0;

            double[] xn = new double[n];
            for (int k = 0; k < n; k++) xn[k] = xData[k] / xMax;

            double[,] A = new double[nv, nv];
            double[]  B = new double[nv];
            for (int i = 0; i < nv; i++)
            {
                for (int j = 0; j < nv; j++) { double s = 0; for (int k = 0; k < n; k++) s += Math.Pow(xn[k], i + j); A[i, j] = s; }
                double sxy = 0; for (int k = 0; k < n; k++) sxy += yData[k] * Math.Pow(xn[k], i); B[i] = sxy;
            }
            for (int i = 0; i < nv; i++)
            {
                int pRow = i; double pAbs = Math.Abs(A[i, i]);
                for (int k = i + 1; k < nv; k++) { double c = Math.Abs(A[k, i]); if (c > pAbs) { pAbs = c; pRow = k; } }
                if (pAbs < 1e-25) return null;
                if (pRow != i)
                {
                    for (int j = i; j < nv; j++) { double tmp = A[i, j]; A[i, j] = A[pRow, j]; A[pRow, j] = tmp; }
                    double tmpB = B[i]; B[i] = B[pRow]; B[pRow] = tmpB;
                }
                for (int k = i + 1; k < nv; k++)
                {
                    double fac = A[k, i] / A[i, i];
                    for (int j = i; j < nv; j++) A[k, j] -= fac * A[i, j];
                    B[k] -= fac * B[i];
                }
            }
            double[] bc = new double[nv];
            for (int i = nv - 1; i >= 0; i--)
            {
                double sum = 0; for (int j = i + 1; j < nv; j++) sum += A[i, j] * bc[j];
                if (Math.Abs(A[i, i]) < 1e-25) return null;
                bc[i] = (B[i] - sum) / A[i, i];
            }
            double[] ac = new double[nv];
            for (int i = 0; i < nv; i++) ac[i] = bc[i] / Math.Pow(xMax, i);
            return ac;
        }

        private Vector3d PredictPositionAtT(double t, Vector3d r0, Vector3d v0, double[] rho,
            Vector3d[] g_coeffs, double lam2, double C2, double lam3, double C3, double Ve, double alpha, double T)
        {
            int poly_order = rho.Length - 1;
            Vector3d dr_gt = CalcGravPos(t, g_coeffs, poly_order);
            GetBaseIntegrals(t, Ve, alpha, T, poly_order + 1, out _, out double[] J_base_t, out _, out _);
            double Jr0_t = 0, Jr1_t = 0;
            for (int i = 0; i <= poly_order; i++) { Jr0_t += rho[i] * J_base_t[i]; Jr1_t += rho[i] * J_base_t[i + 1]; }
            return new Vector3d(
                r0.x + v0.x * t + dr_gt.x - Jr0_t,
                r0.y + v0.y * t + dr_gt.y + (-lam2 * Jr1_t + C2 * Jr0_t),
                r0.z + v0.z * t + dr_gt.z + (-lam3 * Jr1_t + C3 * Jr0_t));
        }

        private Vector3d CalcGravVel(double tf, Vector3d[] g, int p)
        {
            Vector3d v = Vector3d.zero;
            for (int n = 0; n <= p; n++) v += g[n] * (Math.Pow(tf, n + 1) / (n + 1));
            return v;
        }

        private Vector3d CalcGravPos(double tf, Vector3d[] g, int p)
        {
            Vector3d r = Vector3d.zero;
            for (int n = 0; n <= p; n++) r += g[n] * (Math.Pow(tf, n + 2) / ((n + 1) * (n + 2)));
            return r;
        }

        #endregion

        #region Apollo zero-jerk terminal law

        private struct ApolloGuidanceOutput
        {
            public bool     valid; public string reason;
            public double   tgo, requiredThrust;
            public Vector3d accelCmdWorld, thrustAccelWorld, thrustDirWorld;
            public float    throttle;
        }

        private ApolloGuidanceOutput ComputeApolloZeroJerkCommand(
            Vector3d pos, Vector3d vel, Vector3d targetPos,
            Vector3d x_hat, Vector3d y_hat, Vector3d z_hat,
            double mu, double mass, double availThrust,
            Vector3d? vfWorldOpt = null, Vector3d? aFinalWorldOpt = null, Vector3d? jFinalWorldOpt = null,
            double tgoGuess = 10.0, int tgoAxis = 1, int maxIter = 15, double tol = 1e-3)
        {
            ApolloGuidanceOutput outp = new ApolloGuidanceOutput {
                valid = false, reason = "Unknown", tgo = double.NaN,
                accelCmdWorld = Vector3d.zero, thrustAccelWorld = Vector3d.zero,
                thrustDirWorld = Vector3d.zero, requiredThrust = 0.0, throttle = 0f
            };

            if (mass <= 0.0) { outp.reason = "Mass <= 0"; return outp; }
            if (availThrust <= 1e-6) { outp.reason = "No thrust"; return outp; }
            if (x_hat.magnitude < 1e-6 || y_hat.magnitude < 1e-6 || z_hat.magnitude < 1e-6)
                { outp.reason = "Invalid frame"; return outp; }

            Vector3d rel  = pos - targetPos;
            Vector3d RG   = new Vector3d(Vector3d.Dot(rel, x_hat), Vector3d.Dot(rel, y_hat), Vector3d.Dot(rel, z_hat));
            Vector3d VG   = new Vector3d(Vector3d.Dot(vel, x_hat), Vector3d.Dot(vel, y_hat), Vector3d.Dot(vel, z_hat));

            Vector3d vfW  = vfWorldOpt ?? Vector3d.zero;
            Vector3d afW  = aFinalWorldOpt ?? Vector3d.zero;
            Vector3d jfW  = jFinalWorldOpt ?? Vector3d.zero;
            Vector3d VDG  = new Vector3d(Vector3d.Dot(vfW, x_hat), Vector3d.Dot(vfW, y_hat), Vector3d.Dot(vfW, z_hat));
            Vector3d ADG  = new Vector3d(Vector3d.Dot(afW, x_hat), Vector3d.Dot(afW, y_hat), Vector3d.Dot(afW, z_hat));
            Vector3d JDG  = new Vector3d(Vector3d.Dot(jfW, x_hat), Vector3d.Dot(jfW, y_hat), Vector3d.Dot(jfW, z_hat));
            Vector3d RDG  = Vector3d.zero;

            double T = Math.Max(1.0, tgoGuess);
            double Raxis  = (tgoAxis == 0) ? RG.x : (tgoAxis == 1 ? RG.y : RG.z);
            double Vaxis  = (tgoAxis == 0) ? VG.x : (tgoAxis == 1 ? VG.y : VG.z);
            double RdAxis = 0.0;
            double VdAxis = (tgoAxis == 0) ? VDG.x : (tgoAxis == 1 ? VDG.y : VDG.z);
            double AdAxis = (tgoAxis == 0) ? ADG.x : (tgoAxis == 1 ? ADG.y : ADG.z);
            double JdAxis = (tgoAxis == 0) ? JDG.x : (tgoAxis == 1 ? JDG.y : JDG.z);

            bool converged = false;
            for (int i = 0; i < maxIter; i++)
            {
                double dR = RdAxis - Raxis;
                double F  = JdAxis * T * T * T - 6.0 * AdAxis * T * T + 6.0 * Vaxis * T + 18.0 * VdAxis * T - 24.0 * dR;
                double dF = 3.0 * JdAxis * T * T - 12.0 * AdAxis * T + 6.0 * Vaxis + 18.0 * VdAxis;
                if (Math.Abs(dF) < 1e-9) { outp.reason = "Apollo dF too small"; return outp; }
                double dT = -F / dF; T += dT;
                if (!IsFinite(T) || T <= 0.0) { outp.reason = "Apollo T invalid"; return outp; }
                if (Math.Abs(dT) < tol) { converged = true; break; }
            }
            if (!converged) { outp.reason = "Apollo TGO no converge"; return outp; }
            T = Math.Max(1.0, T);
            outp.tgo = T;

            Vector3d ACG          = ADG + (12.0 / (T * T)) * (RDG - RG) - (6.0 / T) * (VDG + VG);
            Vector3d accelCmdW    = ACG.x * x_hat + ACG.y * y_hat + ACG.z * z_hat;
            double   rmag         = pos.magnitude;
            if (rmag < 1.0) { outp.reason = "pos magnitude too small"; return outp; }
            Vector3d gVec         = -(mu / (rmag * rmag * rmag)) * pos;
            Vector3d thrustAccelW = accelCmdW - gVec;
            double   taMag        = thrustAccelW.magnitude;
            if (!IsFinite(taMag) || taMag < 1e-6) { outp.reason = "thrust accel too small"; return outp; }

            outp.valid          = true;
            outp.reason         = "OK";
            outp.accelCmdWorld  = accelCmdW;
            outp.thrustAccelWorld = thrustAccelW;
            outp.thrustDirWorld = thrustAccelW / taMag;
            outp.requiredThrust = mass * taMag;
            outp.throttle       = (float)Math.Min(1.0, Math.Max(0.0, outp.requiredThrust / availThrust));
            return outp;
        }

        #endregion

        #region Gravity-turn terminal law (Han, Jo & Ho arXiv:2409.01465)

        private struct GTGuidanceOutput
        {
            public bool     valid; public string reason;
            public double   tgo, beta, gammaStar;
            public Vector3d thrustDirWorld;
            public double   requiredThrust;
            public float    throttle;
        }

        private static double GT_Sigma(double x, double lo, double hi)
        {
            if (x <= lo) return 0.0; if (x >= hi) return 1.0;
            return (x - lo) / (hi - lo);
        }

        private static Vector3d GT_Sat(Vector3d x, double lo, double hi)
        {
            double mag = x.magnitude;
            if (mag < 1e-12) return Vector3d.zero;
            if (mag < lo)    return (lo  / mag) * x;
            if (mag > hi)    return (hi  / mag) * x;
            return x;
        }

        private static Vector3d GT_Fit(Vector3d x, Vector3d y, double c)
        {
            double xmag = x.magnitude;
            if (xmag > c)     return Vector3d.zero;
            if (xmag < 1e-12) return GT_Sat(y, 0.0, c);
            Vector3d xhat = x / xmag;
            if (Vector3d.Dot(x, y) < 0.0)
            {
                Vector3d yperp = y - Vector3d.Dot(y, xhat) * xhat;
                double   rim   = Math.Sqrt(Math.Max(0.0, c * c - xmag * xmag));
                return GT_Sat(yperp, 0.0, rim);
            }
            double ymag     = y.magnitude; if (ymag < 1e-12) return Vector3d.zero;
            double xDotYhat = Vector3d.Dot(x, y / ymag);
            double upper    = xDotYhat + Math.Sqrt(Math.Max(0.0, xDotYhat * xDotYhat + c * c - xmag * xmag));
            return GT_Sat(y, 0.0, upper);
        }

        private GTGuidanceOutput ComputeGravityTurnCommand(
            Vector3d pos, Vector3d vel, Vector3d targetPos,
            Vector3d x_hat, Vector3d y_hat, Vector3d z_hat,
            double mu, double mass, double availThrust, double Ve,
            double Tmin_abs, double C_b = 0.95, double k_gain = 2.4, double C_e = 20.0,
            double phi_rad = 0.0, double delta = 5.0, double C_col_lower = 0.75, double C_col_upper = 0.95)
        {
            GTGuidanceOutput outp = new GTGuidanceOutput {
                valid = false, reason = "Unknown", tgo = double.NaN, beta = double.NaN,
                gammaStar = double.NaN, thrustDirWorld = Vector3d.zero, requiredThrust = 0, throttle = 0f
            };

            if (mass <= 0.0 || availThrust <= 1e-6 || Ve <= 1e-6)
                { outp.reason = "Invalid mass/thrust/Ve"; return outp; }

            double Tmax = availThrust; double Tmin = Tmin_abs;
            double rmag = pos.magnitude; if (rmag < 1.0) { outp.reason = "Invalid pos"; return outp; }
            double g    = mu / (rmag * rmag);

            // Frame mapping: paper L ← KSP solver frame
            // paper x=downrange(x_hat), paper y=lateral(z_hat), paper z=vertical(y_hat)
            Vector3d rel  = pos - targetPos;
            double r_xL   = Vector3d.Dot(rel, x_hat);
            double r_yL   = Vector3d.Dot(rel, z_hat);
            double r_zL   = Vector3d.Dot(rel, y_hat);
            double v_xL   = Vector3d.Dot(vel, x_hat);
            double v_yL   = Vector3d.Dot(vel, z_hat);
            double v_zL   = Vector3d.Dot(vel, y_hat);

            // Step 1 — β, β̇, G-frame axes (Eq. 21-22, 44-45)
            double Beta     = C_b * Tmax / (mass * g);
            double Beta_dot = Beta * Beta * g / Ve;
            if (Beta <= 1.0) { outp.reason = $"β={Beta:F3}≤1"; return outp; }

            double x_go_raw = Math.Sqrt(r_xL * r_xL + r_yL * r_yL);
            Vector3d xG, yG, zG;
            if (x_go_raw > 0.5)
            {
                xG = (-r_xL * x_hat - r_yL * z_hat) / x_go_raw;
                zG = y_hat; yG = Vector3d.Cross(zG, xG);
            }
            else { xG = x_hat; zG = y_hat; yG = z_hat; }

            Vector3d v_G  = new Vector3d(Vector3d.Dot(vel, xG), Vector3d.Dot(vel, yG), Vector3d.Dot(vel, zG));
            Vector3d gVec = -(mu / (rmag * rmag * rmag)) * pos;
            Vector3d g_G  = new Vector3d(Vector3d.Dot(gVec, xG), Vector3d.Dot(gVec, yG), Vector3d.Dot(gVec, zG));

            // Step 2 — x_go, z_go, γ*, v* (Eq. 13-17)
            double x_go = x_go_raw;
            double z_go = -r_zL;
            double gamma_star, v_star;
            if (x_go < 0.5)
            {
                gamma_star = -Math.PI / 2.0;
                v_star     = Math.Sqrt(Math.Max(0.0, 2.0 * (Beta - 1.0) * g * Math.Abs(z_go)));
            }
            else
            {
                double kappa = ((4.0 * Beta * Beta - 4.0) * z_go) / ((4.0 * Beta * Beta - 1.0) * x_go);
                double gamma = Math.Atan2(z_go, x_go);
                bool   conv  = false;
                for (int i = 0; i < 100; i++)
                {
                    double sy = Math.Sin(gamma), cy = Math.Cos(gamma);
                    double dh_denom = 2.0 * Beta * cy - sy * cy;
                    if (Math.Abs(dh_denom) < 1e-12) break;
                    double h  = (2.0 * Beta * sy - sy * sy - 1.0) / dh_denom - kappa;
                    double dh = (3.0 * (Beta - sy) * (Beta - sy) + Beta * Beta - 1.0) / (dh_denom * dh_denom);
                    if (!IsFinite(h) || !IsFinite(dh) || Math.Abs(dh) < 1e-12) break;
                    double gnew = gamma - h / dh;
                    if (!IsFinite(gnew)) break;
                    bool done = Math.Abs(gnew - gamma) < 1e-10;
                    gamma = gnew;
                    if (done) { conv = true; break; }
                }
                if (!conv) { outp.reason = "γ* no converge"; return outp; }
                gamma_star = gamma;
                double sy2 = Math.Sin(gamma_star), cy2 = Math.Cos(gamma_star);
                double vSq = (4.0 * Beta * Beta - 1.0) * g * x_go / ((2.0 * Beta - sy2) * cy2);
                if (vSq <= 0.0) { outp.reason = "v* imaginary"; return outp; }
                v_star = Math.Sqrt(vSq);
            }
            if (!IsFinite(gamma_star) || !IsFinite(v_star)) { outp.reason = "γ*/v* not finite"; return outp; }

            // Step 3 — v_d^G, tracking error, t_go (Eq. 24, 33)
            double vx_star = v_star * Math.Cos(gamma_star);
            double vz_star = v_star * Math.Sin(gamma_star);
            Vector3d vd_G  = new Vector3d(vx_star, 0.0, vz_star);
            Vector3d e_G   = vd_G - v_G;
            double t_go    = (Beta * v_star - vz_star) / ((Beta * Beta - 1.0) * g) + e_G.magnitude / (Beta * g);
            t_go = Math.Max(t_go, 1e-3);

            // Step 4 — Jacobians, tracking command (Eq. 27-34)
            double dfx_dvx = (1.0 / v_star) * (2.0 * Beta * vx_star * vx_star + 2.0 * Beta * v_star * v_star - v_star * vz_star);
            double dfx_dvz = (1.0 / v_star) * (2.0 * Beta * vx_star * vz_star - v_star * vx_star);
            double dfz_dvx = (1.0 / v_star) * (2.0 * Beta * vx_star * vz_star - 2.0 * v_star * vx_star);
            double dfz_dvz = (1.0 / v_star) * (2.0 * Beta * vz_star * vz_star + 2.0 * Beta * v_star * v_star - 4.0 * v_star * vz_star);
            double det     = dfx_dvx * dfz_dvz - dfz_dvx * dfx_dvz;
            if (!IsFinite(det) || Math.Abs(det) < 1e-12) { outp.reason = "F†_vd singular"; return outp; }

            double dfx_dxgo  = -(4.0 * Beta * Beta - 1.0) * g;
            double dfz_dzgo  = -(4.0 * Beta * Beta - 4.0) * g;
            double dfx_dbeta = 2.0 * v_star * vx_star - 8.0 * Beta * g * x_go;
            double dfz_dbeta = 2.0 * v_star * vz_star - 8.0 * Beta * g * z_go;
            double omega_z   = (x_go > 1e-9) ? -v_G.y / x_go : 0.0;
            Vector3d omCrossVd = new Vector3d(0.0, omega_z * vx_star, 0.0);

            double ff00 =  dfz_dvz * dfx_dxgo / det;
            double ff02 = -dfx_dvz * dfz_dzgo / det;
            double ff20 = -dfz_dvx * dfx_dxgo / det;
            double ff22 =  dfx_dvx * dfz_dzgo / det;
            Vector3d FdagFrgoVg = new Vector3d(ff00 * v_G.x + ff02 * v_G.z, 0.0, ff20 * v_G.x + ff22 * v_G.z);
            double fb_x  = ( dfz_dvz * dfx_dbeta - dfx_dvz * dfz_dbeta) / det;
            double fb_z  = (-dfz_dvx * dfx_dbeta + dfx_dvx * dfz_dbeta) / det;
            Vector3d FdagFbeta = new Vector3d(fb_x, 0.0, fb_z);

            Vector3d a_G_trk     = FdagFrgoVg - FdagFbeta * Beta_dot + omCrossVd - g_G + (k_gain / t_go) * e_G;
            Vector3d a_trk_world = a_G_trk.x * xG + a_G_trk.y * yG + a_G_trk.z * zG;

            // Step 5 — Collision avoidance (Eq. 46-52)
            Vector3d a_col_world = Vector3d.zero;
            if (e_G.magnitude >= C_e || a_trk_world.magnitude >= Tmax / mass)
            {
                double sin2 = Math.Sin(phi_rad) * Math.Sin(phi_rad);
                double cos2 = Math.Cos(phi_rad) * Math.Cos(phi_rad);
                double rDotV = Vector3d.Dot(rel, vel), rSq = rel.sqrMagnitude, vSq = vel.sqrMagnitude;
                double a_p   = v_zL * v_zL - vSq * sin2;
                double b_p   = r_zL * v_zL - rDotV * sin2;
                double c_p   = r_zL * r_zL - rSq   * sin2;
                double disc  = b_p * b_p - a_p * c_p;
                double t_p   = (Math.Abs(a_p) > 1e-12 && disc >= 0.0)
                               ? Math.Max(0.0, (-b_p - Math.Sqrt(disc)) / a_p) : 0.0;
                Vector3d r_p = rel + vel * t_p;

                double r_px_L = Vector3d.Dot(r_p, x_hat);
                double r_py_L = Vector3d.Dot(r_p, z_hat);
                double r_pz_L = Vector3d.Dot(r_p, y_hat);
                double np_den = Math.Max(1e-6, Math.Sqrt(
                    (r_px_L * r_px_L + r_py_L * r_py_L) * sin2 * sin2 + r_pz_L * r_pz_L * cos2 * cos2));
                Vector3d n_p  = (1.0 / np_den) * (-r_px_L * sin2 * x_hat - r_py_L * sin2 * z_hat + r_pz_L * cos2 * y_hat);

                double s     = Math.Max(Vector3d.Dot(rel - r_p, n_p) - delta, 0.1);
                double vDotN = Vector3d.Dot(vel, n_p);
                Vector3d a_n = Vector3d.zero;
                if (vDotN < 0.0) { double gDotN = Vector3d.Dot(gVec, n_p); a_n = (-gDotN + (vDotN * vDotN) / (2.0 * s)) * n_p; }

                double sig   = GT_Sigma(a_n.magnitude, C_col_lower * Tmax / mass, C_col_upper * Tmax / mass);
                a_col_world  = sig * a_n;
            }

            // Step 6 — Final sat/fit command (Eq. 55)
            Vector3d u_world = GT_Sat(a_col_world + GT_Fit(a_col_world, a_trk_world, Tmax / mass), Tmin / mass, Tmax / mass);
            double   u_mag   = u_world.magnitude;
            if (!IsFinite(u_mag) || u_mag < 1e-6) { outp.reason = "u_mag invalid"; return outp; }

            outp.valid          = true;
            outp.reason         = $"OK β={Beta:F3} γ*={gamma_star * 180.0 / Math.PI:F1}° tgo={t_go:F1}s";
            outp.tgo            = t_go;
            outp.beta           = Beta;
            outp.gammaStar      = gamma_star * 180.0 / Math.PI;
            outp.thrustDirWorld = u_world / u_mag;
            outp.requiredThrust = mass * u_mag;
            outp.throttle       = (float)Math.Min(1.0, Math.Max(0.0, outp.requiredThrust / Tmax));
            return outp;
        }

        #endregion
    }
}
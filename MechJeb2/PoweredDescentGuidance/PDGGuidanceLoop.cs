// PDGGuidanceLoop.cs
// Powered + terminal descent guidance AutopilotStep for MechJeb2.
// Standalone powered-descent guidance loop used by MechJebModulePoweredDescentGuidance.
//
// Implements a three-phase state machine:
//   Coasting        — probes the DCTGIG solver; waits for powered descent initiation (PDI).
//   PoweredDescent  — runs the DCTGIG outer solver loop every GuidancePeriodDescent seconds.
//   TerminalDescent — hands off to either Apollo zero-jerk or gravity-turn terminal law.
//
// Solvers implemnented in separate classes:
//   PdgSolver          — DCTGIG powered-descent (IterateCTG / SolveDCTGIG): Zhang et al. 2025, DOI: 10.1016/j.cja.2025.104030. 
//   PdgTerminalSolver  — 
//          Apollo (zero-jerk solve for t_go)
//          Near-Optimal Gravity-turn terminal law - arXiv:2409.01465


using System;
using UnityEngine;

namespace MuMech.Landing
{
    public partial class PDGGuidanceLoop : AutopilotStep
    {
        // -------------------------------------------------------------------------
        // State machine
        // -------------------------------------------------------------------------

        private enum GuidanceState { Coasting, PoweredDescent, TerminalDescent }
        private GuidanceState _state = GuidanceState.Coasting;

        // -------------------------------------------------------------------------
        // Mode flags — set by the caller before use
        // -------------------------------------------------------------------------

        /// <summary>true → land at the MechJeb position target; false → land anywhere.</summary>
        public bool LandAtTarget      = true;

        /// <summary>true → use Apollo zero-jerk terminal law; false → use gravity-turn law.</summary>
        public bool UseApolloTerminal = false;

        // -------------------------------------------------------------------------
        // Solver parameters — exposed for the GUI and the parent module
        // -------------------------------------------------------------------------

        /// <summary>Terrain clearance added above the landing site [m].</summary>
        public double TargetClearance = 100.0;

        /// <summary>Nominal throttle fraction used to seed the DCTGIG solver (0–1).</summary>
        public double PlanThrottle = 0.95;

        /// <summary>Minimum throttle fraction applied during terminal descent.</summary>
        public double MinThrottle = 0.375;

        /// <summary>Downrange distance at which terminal descent is triggered [m].</summary>
        public double TerminalHandoverDownrange = 1000.0;

        /// <summary>Half-angle of the gravity-turn approach cone [deg].</summary>
        public double TerminalGlideConstraint = 15.0;

        /// <summary>Enables pulse-width-modulated throttle output.</summary>
        public bool PulseThrottleMode = false;

        /// <summary>PWM period [s]. Only used when <see cref="PulseThrottleMode"/> is true.</summary>
        public double PulsePeriod = 1.0;

        // -------------------------------------------------------------------------
        // Public telemetry — read by the GUI window
        // -------------------------------------------------------------------------

        public double TimeToGo        { get; private set; }
        public double RequiredThrust  { get; private set; }
        public double DvUsed          { get; private set; }
        public double DownrangeToTgt  { get; private set; }
        public double PredDownrange   { get; private set; }
        public double DownrangeError  { get; private set; }
        public double CurrentTWR      { get; private set; }
        public double AvailableTWR    { get; private set; }
        public double LocalG          { get; private set; }
        public float  CurrentThrottle { get; private set; }

        // Extended solver diagnostics (for debug overlays / logging).
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

        // -------------------------------------------------------------------------
        // Private guidance state
        // -------------------------------------------------------------------------

        private const double GuidancePeriodDescent  = 1.0;  // solver cadence during powered descent [s]
        private const double GuidancePeriodTerminal = 0.1;  // solver cadence during terminal descent [s]

        private double  _currentTf              = 0.0;
        private double  _currentT               = 0.0;
        private float   _targetThrottle         = 0f;
        private Vector3d _targetPointing        = Vector3d.up;
        private Vector3d _worldTargetPos        = Vector3d.zero; // held in "land anywhere" mode

        private double _guidanceStartMass       = -1.0;
        private double _nextGuidanceUpdateUT    = 0.0;
        private double _guidancePeriod          = GuidancePeriodDescent;
        private double _pulseTimer              = 0.0;

        // Propulsion state — refreshed each fixed update.
        private double _availableThrust = 0.0;
        private double _ve              = 0.0;
        private double _mass            = 0.0;

        // "Land anywhere" target lock.
        private bool   _landAnywhereTargetSet = false;
        private double _landAnywhereLat       = double.NaN;
        private double _landAnywhereLon       = double.NaN;

        // Solver-frame snapshots kept for NudgeTargetMeters fallback.
        private Vector3d _dbgFHat = Vector3d.zero;

        // Reference to the controller that owns this step (passed to Core.Attitude).
        private readonly object _controller;

        // -------------------------------------------------------------------------
        // Constructor
        // -------------------------------------------------------------------------

        public PDGGuidanceLoop(MechJebCore core, object controller) : base(core)
        {
            _controller = controller;
            Status      = "PDG: initialized";
        }

        // =========================================================================
        // AutopilotStep interface
        // =========================================================================

        /// <summary>
        /// Called once per physics tick by <see cref="MechJebModulePoweredDescentGuidance"/>.
        /// Returns <c>this</c> to keep running; returns <c>null</c> when guidance is complete.
        /// </summary>
        public override AutopilotStep OnFixedUpdate()
        {
            Vessel vessel    = Vessel;
            VesselState vs   = VesselState;
            CelestialBody cb = MainBody;

            if (vessel == null || cb == null) return this;

            if (cb.atmosphere)
            {
                _targetThrottle = 0f;
                Core.Thrust.ThrustOff();
                Status = "PDG is intended for airless bodies.";
                return null;
            }

            if (vessel.LandedOrSplashed)
            {
                _targetThrottle = 0f;
                Core.Thrust.ThrustOff();
                Status = "Touchdown. Guidance complete.";
                return null;
            }

            if (!RefreshPropulsionState())
            {
                _targetThrottle = 0f;
                Core.Thrust.ThrustOff();
                Status = "PDG: waiting for active engine.";
                return this;
            }

            double   ut       = vs.time;
            Vector3d pos      = vs.CoM - cb.position;
            Vector3d vel      = vs.surfaceVelocity;
            double   altRadar = Math.Max(0.0, vs.altitudeBottom);
            double   mu       = cb.gravParameter;

            UpdatePerformanceStats(pos, _mass, _availableThrust, _ve);

            // --- Resolve target position and build solver frame ---

            Vector3d targetPos;
            Vector3d xHat, yHat, zHat;
            bool needsInitialTarget = !LandAtTarget && _worldTargetPos.sqrMagnitude <= 1e-6;

            if (needsInitialTarget)
            {
                // Land Anywhere: no target yet — use a velocity-aligned frame and coast-solve
                // to predict where we will reach the surface, then lock that as the target.
                if (!VelocityAlignedFrame(pos, vel, out xHat, out yHat, out zHat))
                {
                    Status = "Land Anywhere: horizontal velocity too small.";
                    return this;
                }
                targetPos = (cb.Radius + TargetClearance) * yHat; // placeholder radial only
            }
            else
            {
                if (!TryGetEffectiveTarget(out _, out _, out targetPos, out _))
                    return this;

                if (!SolverFrame(pos, vel, targetPos, out xHat, out yHat, out zHat))
                {
                    xHat = Vector3d.right;
                    yHat = Vector3d.up;
                    zHat = Vector3d.forward;
                }
            }

            _dbgFHat = xHat;

            // Project state into the solver local frame.
            Vector3d rLoc = new Vector3d(
                Vector3d.Dot(pos, xHat), Vector3d.Dot(pos, yHat), Vector3d.Dot(pos, zHat));
            Vector3d vLoc = new Vector3d(
                Vector3d.Dot(vel, xHat), Vector3d.Dot(vel, yHat), Vector3d.Dot(vel, zHat));

            // Desired terminal velocity: small inward radial bleed.
            Vector3d vfGlobal = targetPos.magnitude > 1e-6 ? -3.0 * targetPos.normalized : Vector3d.zero;
            Vector3d vfLoc    = new Vector3d(
                Vector3d.Dot(vfGlobal, xHat),
                Vector3d.Dot(vfGlobal, yHat),
                Vector3d.Dot(vfGlobal, zHat));

            double targetXLoc = needsInitialTarget
                ? double.PositiveInfinity
                : Vector3d.Dot(targetPos - pos, xHat);

            double rTarget = needsInitialTarget ? cb.Radius + TargetClearance : targetPos.magnitude;

            DownrangeToTgt = targetXLoc;

            // --- Guidance cadence gate ---

            bool runSolver = ut >= _nextGuidanceUpdateUT;
            if (runSolver)
            {
                _nextGuidanceUpdateUT = ut + _guidancePeriod;
                RunGuidanceStep(vessel, cb, pos, vel, rLoc, vLoc, vfLoc,
                                targetPos, xHat, yHat, zHat,
                                targetXLoc, rTarget, mu, altRadar, needsInitialTarget);
            }

            // Coasting: point retrograde every tick regardless of guidance cadence.
            if (_state == GuidanceState.Coasting && vel.sqrMagnitude > 1e-6)
                _targetPointing = -vel.normalized;

            TimeToGo        = _currentTf;
            RequiredThrust  = _currentT;
            CurrentThrottle = _targetThrottle;

            if (_targetPointing.sqrMagnitude > 1e-6)
                Core.Attitude.attitudeTo(
                    _targetPointing.normalized,
                    AttitudeReference.INERTIAL,
                    _controller,
                    killRollRotation: true);

            return this;
        }

        // =========================================================================
        // Guidance state machine (called inside the cadence gate)
        // =========================================================================

        private void RunGuidanceStep(
            Vessel vessel, CelestialBody cb,
            Vector3d pos, Vector3d vel,
            Vector3d rLoc, Vector3d vLoc, Vector3d vfLoc,
            Vector3d targetPos,
            Vector3d xHat, Vector3d yHat, Vector3d zHat,
            double targetXLoc, double rTarget, double mu,
            double altRadar, bool needsInitialTarget)
        {
            switch (_state)
            {
                case GuidanceState.Coasting:
                    RunCoastingStep(cb, pos, vel, rLoc, vLoc, vfLoc,
                                    xHat, yHat, targetXLoc, rTarget, mu, altRadar, needsInitialTarget);
                    break;

                case GuidanceState.PoweredDescent:
                    RunPoweredDescentStep(pos, vel, rLoc, vLoc, vfLoc,
                                          xHat, yHat, zHat, targetXLoc, rTarget, mu, altRadar);
                    break;

                case GuidanceState.TerminalDescent:
                    RunTerminalDescentStep(vessel, pos, vel, targetPos, xHat, yHat, zHat, mu, altRadar);
                    break;
            }
        }

        // --- Coasting ---

        private void RunCoastingStep(
            CelestialBody cb,
            Vector3d pos, Vector3d vel,
            Vector3d rLoc, Vector3d vLoc, Vector3d vfLoc,
            Vector3d xHat, Vector3d yHat,
            double targetXLoc, double rTarget, double mu,
            double altRadar, bool needsInitialTarget)
        {
            _targetThrottle = 0f;
            if (vel.sqrMagnitude > 1e-6) _targetPointing = -vel.normalized;

            double testThrust = _availableThrust * PlanThrottle;
            double alpha      = (_mass * _ve) / Math.Max(1.0, testThrust);

            if (_currentTf <= 0.0 || !IsFinite(_currentTf))
            {
                double safeVel = Math.Max(50.0, Math.Abs(vLoc.x));
                _currentTf     = alpha * (1.0 - Math.Exp(-safeVel / _ve));
            }

            PdgSolverResult coast = PdgSolver.Iterate(
                _currentTf, rLoc, vLoc, vfLoc, _ve, alpha, testThrust, mu, rTarget, 3);

            if (coast != null && coast.converged && IsFinite(coast.x_f) && IsFinite(coast.tf) && coast.tf > 0.0)
            {
                _currentTf    = coast.tf;
                PredDownrange = coast.x_f;
                DownrangeError = IsFinite(targetXLoc) ? PredDownrange - targetXLoc : 0.0;
                _currentT     = testThrust;

                if (!LandAtTarget && !_landAnywhereTargetSet)
                {
                    if (SetLandAnywhereTargetFromCoastSolution(PredDownrange, xHat, yHat, cb))
                    {
                        // Immediately transition into powered descent now that we have a target.
                        if (_guidanceStartMass < 0.0) _guidanceStartMass = _mass;
                        _guidancePeriod           = GuidancePeriodDescent;
                        _state                    = GuidanceState.PoweredDescent;
                        _nextGuidanceUpdateUT     = VesselState.time; // re-run next tick
                        Status = $"Land Anywhere target set. PDI. x={PredDownrange:F0}m";
                    }
                    else
                    {
                        Status = "Land Anywhere: coast solve valid but target creation failed.";
                    }
                    return;
                }

                bool targetReady = LandAtTarget || _landAnywhereTargetSet;
                if (targetReady && targetXLoc <= PredDownrange + 10.0 && altRadar > 100.0)
                {
                    if (_guidanceStartMass < 0.0) _guidanceStartMass = _mass;
                    _guidancePeriod = GuidancePeriodDescent;
                    _state          = GuidanceState.PoweredDescent;
                    Status          = "PDI initiated.";
                }
                else
                {
                    Status = IsFinite(targetXLoc)
                        ? $"Coasting. Pred={PredDownrange:F0}m Tgt={targetXLoc:F0}m"
                        : $"Coasting. Pred={PredDownrange:F0}m";
                }
            }
            else
            {
                PredDownrange  = double.NaN;
                DownrangeError = double.NaN;
                Status = $"Coast solver invalid. tf0={_currentTf:F3} T={testThrust:F1}";
            }
        }

        // --- Powered descent ---

        private void RunPoweredDescentStep(
            Vector3d pos, Vector3d vel,
            Vector3d rLoc, Vector3d vLoc, Vector3d vfLoc,
            Vector3d xHat, Vector3d yHat, Vector3d zHat,
            double targetXLoc, double rTarget, double mu, double altRadar)
        {
            PdgSolverResult res = PdgSolver.Solve(
                rLoc, vLoc, vfLoc, targetXLoc, _mass, _currentTf, _ve,
                _availableThrust, PlanThrottle, mu, rTarget, 3);

            ApplyDbgFromResult(res);

            if (res != null && res.converged && IsFinite(res.x_f) && IsFinite(res.tf) && IsFinite(res.T_mag))
            {
                _currentT      = res.T_mag;
                _currentTf     = res.tf;
                DownrangeError = res.x_f - targetXLoc;
                PredDownrange  = res.x_f;

                _targetThrottle = _availableThrust > 1e-6
                    ? Mathf.Clamp((float)(_currentT / _availableThrust), 0.01f, 1.0f)
                    : 0f;

                // Compute the world-space pointing direction from guidance co-states.
                double   lam40   = 1.0 / Math.Sqrt(1.0 + res.C2 * res.C2 + res.C3 * res.C3);
                Vector3d uCmdLoc = new Vector3d(-lam40, res.C2 * lam40, res.C3 * lam40);
                _targetPointing  = (uCmdLoc.x * xHat + uCmdLoc.y * yHat + uCmdLoc.z * zHat).normalized;

                Status = $"Powered. TGO={_currentTf:F1}s T={_currentT / 1000.0:F2}kN";

                if (IsFinite(_currentTf) && (targetXLoc < TerminalHandoverDownrange || altRadar < 30.0))
                {
                    TargetClearance  = 0.0;
                    _guidancePeriod  = GuidancePeriodTerminal;
                    _state           = GuidanceState.TerminalDescent;
                    Status           = "Terminal descent initiated.";
                }
            }
            else
            {
                Status = $"Solver null. x_tgt={targetXLoc:F0}m";
            }
        }

        // --- Terminal descent ---

        private void RunTerminalDescentStep(
            Vessel vessel,
            Vector3d pos, Vector3d vel, Vector3d targetPos,
            Vector3d xHat, Vector3d yHat, Vector3d zHat,
            double mu, double altRadar)
        {
            if (vessel.LandedOrSplashed)
            {
                _targetThrottle = 0f;
                Status = "Touchdown. Guidance complete.";
                return;
            }
            if (altRadar < 0.5 && vel.magnitude < 1.0)
            {
                _targetThrottle = 0f;
                Core.Thrust.ThrustOff();
                Status = "Contact. Disengaging.";
                return;
            }

            if (UseApolloTerminal)
                RunApolloTerminalStep(pos, vel, targetPos, xHat, yHat, zHat, mu, altRadar);
            else
                RunGravityTurnTerminalStep(pos, vel, targetPos, xHat, yHat, zHat, mu, altRadar);
        }

        private void RunApolloTerminalStep(
            Vector3d pos, Vector3d vel, Vector3d targetPos,
            Vector3d xHat, Vector3d yHat, Vector3d zHat,
            double mu, double altRadar)
        {
            ApolloGuidanceOutput ap = PdgTerminalSolver.ComputeApolloZeroJerkCommand(
                pos, vel, targetPos, xHat, yHat, zHat, mu, _mass, _availableThrust,
                vfWorld:    -0.1 * yHat,
                aFinalWorld: 0.2 * yHat,
                jFinalWorld: Vector3d.zero,
                tgoGuess: 10.0, tgoAxis: 1);

            bool badResult = !ap.valid
                || !IsFinite(ap.tgo) || ap.tgo <= 0.0 || ap.tgo > 500.0
                || !IsFinite(ap.requiredThrust);
            bool badKin = !IsFinite(altRadar) || altRadar < -5.0
                || vel.magnitude > 500.0
                || Vector3d.Dot(vel, yHat) < -100.0;

            if (badResult || badKin || _availableThrust <= 1e-3)
            {
                _targetThrottle = 0f;
                Status = $"Apollo aborted. v={ap.valid} tgo={ap.tgo:F1} alt={altRadar:F1}";
                return;
            }

            _targetPointing = ap.thrustDirWorld;
            _targetThrottle = ap.throttle;
            _currentTf      = ap.tgo;
            _currentT       = ap.requiredThrust;
            Status          = $"Apollo TGO={ap.tgo:F1}s";
        }

        private void RunGravityTurnTerminalStep(
            Vector3d pos, Vector3d vel, Vector3d targetPos,
            Vector3d xHat, Vector3d yHat, Vector3d zHat,
            double mu, double altRadar)
        {
            GTGuidanceOutput gt = PdgTerminalSolver.ComputeGravityTurnCommand(
                pos, vel, targetPos, xHat, yHat, zHat,
                mu, _mass, _availableThrust, _ve,
                tMinAbs:     _availableThrust * MinThrottle,
                cB:          0.95,
                kGain:       2.4,
                cE:          20.0,
                phiRad:      TerminalGlideConstraint * Math.PI / 180.0,
                delta:       5.0,
                cColLower:   0.75,
                cColUpper:   0.95);

            bool badResult = !gt.valid
                || !IsFinite(gt.tgo) || gt.tgo <= 0.0 || gt.tgo > 500.0
                || !IsFinite(gt.requiredThrust);

            if (badResult || _availableThrust <= 1e-3)
            {
                _targetThrottle = 0f;
                Status = $"GT aborted: {gt.reason}";
                return;
            }

            _targetPointing = gt.thrustDirWorld;
            _targetThrottle = gt.throttle;
            _currentTf      = gt.tgo;
            _currentT       = gt.requiredThrust;
            Status          = $"GT TGO={gt.tgo:F1}s {gt.reason}";
        }

        

        // =========================================================================
        // Propulsion state
        // =========================================================================

        /// <summary>
        /// Reads the vessel's current engine state into the cached propulsion fields.
        /// Returns false if no usable engines are found.
        /// </summary>
        private bool RefreshPropulsionState()
        {
            PDGEngineState eng = PDGEngine.ReadEngineState(Vessel);
            if (!eng.Valid) return false;
            _mass            = eng.Mass;
            _availableThrust = eng.AvailableThrust;
            _ve              = eng.EffectiveExhaustVelocity;
            return true;
        }

        // =========================================================================
        // Utility
        // =========================================================================

        private static bool IsFinite(double v) => PDGMathUtils.IsFinite(v);
    }
}
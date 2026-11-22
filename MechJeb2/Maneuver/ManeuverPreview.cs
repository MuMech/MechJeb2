#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace MuMech
{
    /// <summary>
    /// The Maneuver Planner's preview of an operation hovered in its list (the new UI's <c>PreviewModel</c> says when): the orbits its
    /// burns would give, drawn dashed on the map, with the burns' total Δv and the first burn's time for the row's card. A display
    /// only: it computes through <see cref="Operation.PreviewNodes"/>, which places no node, changes no message and logs nothing, with
    /// the operation's current settings, and it draws with <see cref="GLUtils"/> from MechJeb's post-draw queue.
    /// <list type="bullet">
    /// <item>Only an operation that opts in (<see cref="Operation.Previewable"/>) and only with the setting on
    /// (<see cref="MechJebModuleSettings.UiPlannerPreview"/>, off by default).</item>
    /// <item>Drawn only in the map view, only while the planner's window is shown, for the vessel it was computed for.</item>
    /// <item>Removed on <see cref="Clear"/> (the list's popup closes, its owner is disabled or destroyed), on a vessel change and
    /// on a scene change.</item>
    /// <item>Computed again when its inputs change: the orbit it starts from, the target, the planner's nodes or mode, and once a
    /// second of game time (burns timed from now), but at most every quarter second of real time (high warp with the map open). A
    /// burn that is past by then is not drawn.</item>
    /// </list>
    /// </summary>
    public sealed class ManeuverPreview
    {
        /// <summary>The ghost orbits' colour: a light blue, apart from KSP's own orbit lines and MechJeb's red and yellow paths.</summary>
        public static readonly Color GhostColor = new Color(0.45f, 0.85f, 1f, 0.95f);

        private const double RECOMPUTE_SECONDS = 1;

        // the shortest real time between two computations (an Eccentricity preview allocates about 60 KB)
        private const float MIN_REAL_SECONDS = 0.25f;

        private readonly MechJebModuleManeuverPlanner _planner;
        private readonly List<Orbit>                  _orbits = new List<Orbit>();
        private readonly Stopwatch                    _watch  = new Stopwatch();

        // the inputs it was computed from
        private Vessel?     _vessel;
        private Orbit?      _from;
        private ITargetable? _target;
        private double      _fromUT, _computedAt, _sma, _ecc, _inc, _lan, _argPe;
        private float       _computedAtReal;
        private int         _nodes;
        private MechJebModuleManeuverPlanner.NodeMode _mode;

        internal ManeuverPreview(MechJebModuleManeuverPlanner planner) => _planner = planner;

        /// <summary>The operation previewed; null when none.</summary>
        public Operation? Operation { get; private set; }

        /// <summary>Whether a preview is held (it draws while the map view is shown).</summary>
        public bool Shown => Operation != null;

        /// <summary>Whether the operation made its burns (false: <see cref="Error"/> says why, when it said).</summary>
        public bool Valid => _orbits.Count > 0;

        /// <summary>The burns' total Δv (m/s).</summary>
        public double DeltaV { get; private set; }

        /// <summary>The first burn's universal time.</summary>
        public double BurnUT { get; private set; }

        /// <summary>How many burns the operation makes.</summary>
        public int Burns { get; private set; }

        /// <summary>The operation's reason it made no burn, or its warning; null or empty when none.</summary>
        public string? Error { get; private set; }

        /// <summary>The orbits after each burn, in order.</summary>
        public IReadOnlyList<Orbit> Orbits => _orbits;

        /// <summary>How many times it was computed, and the last computation's time (ms).</summary>
        public int Computes { get; private set; }

        public double LastComputeMs { get; private set; }

        /// <summary>How many frames drew the ghost, and the last draw's time (ms).</summary>
        public int Draws { get; private set; }

        public double LastDrawMs { get; private set; }

        private bool On => _planner.Core.Settings != null && _planner.Core.Settings.UiPlannerPreview;

        /// <summary>Previews <paramref name="op"/> now (nothing for an operation that did not opt in, or with the setting off).</summary>
        public void Show(Operation op)
        {
            if (!op.Previewable || !On || _planner.Vessel == null || _planner.Vessel.patchedConicSolver == null)
            {
                Clear();
                return;
            }

            Operation = op;
            Compute();
        }

        /// <summary>Removes the preview.</summary>
        public void Clear()
        {
            Operation = null;
            _orbits.Clear();
            Error     = null;
            DeltaV    = 0;
            Burns     = 0;
            _vessel   = null;
            _from     = null;
            _target   = null;
        }

        private void Compute()
        {
            if (Operation == null) return;
            _watch.Reset();
            _watch.Start();
            _planner.GetOrbitAndUT(out Orbit o, out double ut);
            Remember(o, ut);
            _orbits.Clear();
            DeltaV = 0;
            Burns  = 0;
            List<ManeuverParameters>? burns = Operation.PreviewNodes(o, ut, _planner.Core.Target, out string? error);
            Error = error;
            if (burns != null)
            {
                try
                {
                    Orbit at = o;
                    BurnUT = burns[0].UT;
                    foreach (ManeuverParameters b in burns)
                    {
                        if (b.UT < BurnUT) BurnUT = b.UT;
                        at = at.PerturbedOrbit(b.UT, b.dV);
                        _orbits.Add(at);
                        DeltaV += b.dV.magnitude;
                    }

                    Burns = burns.Count;
                }
                catch (System.Exception)
                {
                    // a display: no ghost rather than a log line every recomputation
                    _orbits.Clear();
                    DeltaV = 0;
                    Burns  = 0;
                }
            }

            _watch.Stop();
            LastComputeMs = _watch.Elapsed.TotalMilliseconds;
            Computes++;
        }

        private void Remember(Orbit o, double ut)
        {
            _vessel     = _planner.Vessel;
            _from       = o;
            _fromUT     = ut;
            _computedAt = Planetarium.GetUniversalTime();
            _computedAtReal = Time.realtimeSinceStartup;
            _target     = _planner.Core.Target.Target;
            _nodes      = _planner.PlannerNodeCount();
            _mode       = _planner.Mode;
            _sma        = o.semiMajorAxis;
            _ecc        = o.eccentricity;
            _inc        = o.inclination;
            _lan        = o.LAN;
            _argPe      = o.argumentOfPeriapsis;
        }

        // the inputs changed since the last computation
        private bool Stale()
        {
            if (Time.realtimeSinceStartup - _computedAtReal < MIN_REAL_SECONDS) return false;
            if (Planetarium.GetUniversalTime() - _computedAt >= RECOMPUTE_SECONDS) return true;
            if (_planner.Core.Target.Target != _target || _planner.PlannerNodeCount() != _nodes || _planner.Mode != _mode) return true;
            _planner.GetOrbitAndUT(out Orbit o, out double ut);
            if (o != _from || (ut != _fromUT && _nodes > 0)) return true;
            return System.Math.Abs(o.semiMajorAxis - _sma) > 1e-5 * System.Math.Abs(_sma) || System.Math.Abs(o.eccentricity - _ecc) > 1e-5 ||
                   System.Math.Abs(o.inclination - _inc) > 0.01 || System.Math.Abs(o.LAN - _lan) > 0.01 || System.Math.Abs(o.argumentOfPeriapsis - _argPe) > 0.01;
        }

        /// <summary>The post-draw queue's call (each frame of the camera shown): draws the ghost in the map view.</summary>
        internal void Draw()
        {
            if (Operation == null) return;
            // the owner closed, the vessel changed, the setting went off: the ghost goes
            if (!_planner.Enabled || !On || _vessel != _planner.Vessel || _vessel != FlightGlobals.ActiveVessel)
            {
                Clear();
                return;
            }

            if (!MapView.MapIsEnabled) return;
            if (Stale()) Compute();
            if (_orbits.Count == 0) return;
            // a burn timed from now that is past by now: dropped (the next computation finds the next one)
            if (BurnUT < Planetarium.GetUniversalTime() - 1) return;

            _watch.Reset();
            _watch.Start();
            for (int i = 0; i < _orbits.Count; i++)
                GLUtils.DrawOrbit(_orbits[i], GhostColor, true);
            _watch.Stop();
            LastDrawMs = _watch.Elapsed.TotalMilliseconds;
            Draws++;
        }
    }
}

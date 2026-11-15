using UnityEngine;

namespace MuMech.UI
{
    /// <summary>
    /// PSG settings that need the player's attention: their rows are marked in the PSG Settings window, and the Ascent window's PSG
    /// Settings button carries a badge while any of them is.
    /// </summary>
    static class PsgAttention
    {
        /// <summary>Aref 0 (the default) on a body with an atmosphere: PSG then plans the ascent with no drag at all.</summary>
        public static bool ArefUnset(MechJebCore core) => core.PSGSimulation.Aref <= 0 && Atmosphere(core);

        /// <summary>A minimum coast longer than the maximum, which PSG refuses to plan with (only read when a coast stage is set).</summary>
        public static bool CoastTimesInverted(MechJebCore core) =>
            core.AscentSettings.CoastStage >= 0 && core.PSGSimulation.MinCoast > core.PSGSimulation.MaxCoast;

        /// <summary>A Qα limit PSG clamps to between 0 and 10000 Pa-rad.</summary>
        public static bool QaClamped(MechJebCore core) => core.PSGSimulation.LimitQa < 0 || core.PSGSimulation.LimitQa > 10000;

        /// <summary>
        ///     A coast during a stage (it burns, coasts, then burns again) with an engine that can't light a second time: a stock engine
        ///     that doesn't allow a restart, or a RealFuels engine without the ignitions left. The burn after the coast loses its thrust.
        /// </summary>
        public static bool CoastNoRelight(MechJebCore core)
        {
            // the check walks every module of the vessel and reads RealFuels' ignitions by reflection, and the window asks several
            // times a frame: computed at most every half second
            if (Time.unscaledTime < _relightNext && core == _relightCore)
                return _relight;

            _relightCore = core;
            _relightNext = Time.unscaledTime + 0.5f;
            _relight = ComputeCoastNoRelight(core);
            return _relight;
        }

        static MechJebCore _relightCore;
        static float _relightNext;
        static bool _relight;

        static bool ComputeCoastNoRelight(MechJebCore core)
        {
            MechJebModuleAscentSettings s = core.AscentSettings;
            int stage = s.CoastStage;
            // once the coast is over, its relight is behind: an engine with no ignitions left in the burn after it is fine
            if (stage < 0 || s.CoastLocation != 0 || core.Guidance.hasCoasted)
                return false;

            System.Collections.Generic.List<Part> parts = core.vessel != null ? core.vessel.parts : EditorLogic.fetch?.ship?.parts;
            if (parts == null)
                return false;

            for (int i = 0; i < parts.Count; i++)
            {
                Part p = parts[i];
                if (p.inverseStage != stage)
                    continue;
                for (int m = 0; m < p.Modules.Count; m++)
                    if (p.Modules[m] is ModuleEngines e && !CanRelight(e))
                        return true;
            }

            return false;
        }

        // RealFuels counts ignitions (-1: unlimited), and an engine not lit yet needs one for its first burn; stock engines say
        // whether they allow a restart
        static bool CanRelight(ModuleEngines e)
        {
            if (VesselState.IsRealFuelsCorrectlyInitialized && VesselState.RFModuleEnginesRFType.IsInstance(e))
            {
                int ignitions = VesselState.RFignitionsField.GetValue<int>(e);
                return ignitions < 0 || ignitions >= (e.EngineIgnited ? 1 : 2);
            }

            return e.allowRestart;
        }

        public static bool Any(MechJebCore core) => ArefUnset(core) || CoastTimesInverted(core) || QaClamped(core) || CoastNoRelight(core);

        // the vessel's body in flight, Kerbin (or the home body) in the editor
        static bool Atmosphere(MechJebCore core)
        {
            CelestialBody body = core.vessel != null ? core.vessel.mainBody : FlightGlobals.GetHomeBody();
            return body != null && body.atmosphere;
        }
    }
}

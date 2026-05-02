// PDGEngine.cs
// Reads the current propulsion state of a vessel.
// Aggregates thrust and mass-flow from all ignited engines and
// RCS thrusters that have "fore by throttle" enabled.
//      Approximation: counts full module thrust when fore-by-throttle is enabled.
// Output: PDGEngineState — mass, available thrust, effective exhaust velocity.

using System;

namespace MuMech.Landing
{
    internal static class PDGEngine
    {
        /// <summary>
        /// Builds a <see cref="PDGEngineState"/> snapshot for <paramref name="vessel"/>.
        /// Returns an invalid state (with a reason string) if no usable engines are found.
        /// </summary>
        public static PDGEngineState ReadEngineState(Vessel vessel)
        {
            PDGEngineState state = new PDGEngineState();

            if (vessel == null)
            {
                state.Valid         = false;
                state.FailureReason = "No vessel.";
                return state;
            }

            state.Mass = vessel.totalMass * 1000.0; // kg

            double massFlow = 0.0;

            foreach (Part part in vessel.parts)
            {
                // --- Main engines ---
                foreach (ModuleEngines engine in part.FindModulesImplementing<ModuleEngines>())
                {
                    if (!engine.EngineIgnited || !engine.isOperational || engine.maxThrust <= 0)
                        continue;

                    double rawThrust    = engine.maxThrust * 1000.0;
                    double limitFrac    = Math.Max(0.0, Math.Min(1.0, engine.thrustPercentage * 0.01));
                    double thrust       = rawThrust * limitFrac;
                    double isp          = engine.realIsp > 0 ? engine.realIsp : engine.atmosphereCurve.Evaluate(0);

                    if (!PDGMathUtils.IsFinite(isp) || isp <= 1e-6 || thrust <= 1e-6)
                        continue;

                    state.RawMaxThrust    += rawThrust;
                    state.AvailableThrust += thrust;
                    massFlow              += thrust / (isp * 9.80665);
                }

                // --- RCS thrusters (fore-by-throttle mode only) ---
                foreach (ModuleRCS rcs in part.FindModulesImplementing<ModuleRCS>())
                {
                    if (!rcs.rcsEnabled || !rcs.useThrottle || rcs.thrusterPower <= 0.0f)
                        continue;

                    double isp = rcs.atmosphereCurve.Evaluate(0.0f);
                    if (!PDGMathUtils.IsFinite(isp) || isp <= 1e-6)
                        continue;

                    double rawThrust = rcs.thrusterPower * 1000.0;
                    double limitFrac = Math.Max(0.0, Math.Min(1.0, rcs.thrustPercentage * 0.01));
                    double thrust    = rawThrust * limitFrac;

                    if (!PDGMathUtils.IsFinite(thrust) || thrust <= 1e-6)
                        continue;

                    state.RawMaxThrust    += rawThrust;
                    state.AvailableThrust += thrust;
                    massFlow              += thrust / (isp * 9.80665);
                }
            }

            state.MassFlow = massFlow;

            if (state.AvailableThrust <= 0.0 || massFlow <= 0.0)
            {
                state.Valid         = false;
                state.FailureReason = "No active engine.";
                return state;
            }

            state.EffectiveExhaustVelocity = state.AvailableThrust / massFlow;
            state.Valid = PDGMathUtils.IsFinite(state.EffectiveExhaustVelocity) && state.Mass > 0.0;

            if (!state.Valid)
                state.FailureReason = "Invalid engine state.";

            return state;
        }
    }
}
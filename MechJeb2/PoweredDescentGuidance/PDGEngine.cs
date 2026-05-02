// Reads both operational engines and rcs  thrusters with 'fore by throttle' enabled
// Outputs total mass, available thrust, mass flow, and effective exhaust velocity for the vessel's current engine configuration
// Attached to Type - PDGEngineState

using System;

namespace MuMech.Landing
{
    internal static class PDGEngine
    {
        public static PDGEngineState ReadEngineState(Vessel vessel)
        {
            PDGEngineState state = new PDGEngineState();

            if (vessel == null)
            {
                state.Valid = false;
                state.FailureReason = "No vessel.";
                return state;
            }

            state.Mass = vessel.totalMass * 1000.0;

            double massFlow = 0.0;

            foreach (Part part in vessel.parts)
            {
                foreach (ModuleEngines engine in part.FindModulesImplementing<ModuleEngines>())
                {
                    if (!(engine.EngineIgnited && engine.isOperational && engine.maxThrust > 0)) continue;

                    double rawThrust = engine.maxThrust * 1000.0;
                    double limitFraction = Math.Max(0.0, Math.Min(1.0, engine.thrustPercentage * 0.01));
                    double thrust = rawThrust * limitFraction;
                    double isp = engine.realIsp > 0 ? engine.realIsp : engine.atmosphereCurve.Evaluate(0);

                    if (!PDGMathUtils.IsFinite(isp) || isp <= 1e-6 || thrust <= 1e-6) continue;

                    state.RawMaxThrust += rawThrust;
                    state.AvailableThrust += thrust;
                    massFlow += thrust / (isp * 9.80665);
                }

                foreach (ModuleRCS rcs in part.FindModulesImplementing<ModuleRCS>())
                {
                    if (!rcs.rcsEnabled) continue;
                    if (!rcs.useThrottle) continue;
                    if (rcs.thrusterPower <= 0.0f) continue;

                    double isp = rcs.atmosphereCurve.Evaluate(0.0f);
                    if (!PDGMathUtils.IsFinite(isp) || isp <= 1e-6) continue;

                    double rawThrust = rcs.thrusterPower * 1000.0;
                    double limitFraction = Math.Max(0.0, Math.Min(1.0, rcs.thrustPercentage * 0.01));
                    double thrust = rawThrust * limitFraction;

                    if (!PDGMathUtils.IsFinite(thrust) || thrust <= 1e-6) continue;

                    state.RawMaxThrust += rawThrust;
                    state.AvailableThrust += thrust;
                    massFlow += thrust / (isp * 9.80665);
                }
            }

            state.MassFlow = massFlow;

            if (state.AvailableThrust <= 0.0 || massFlow <= 0.0)
            {
                state.Valid = false;
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
using System;
using UnityEngine;

namespace MuMech
{
    public static class CelestialBodyExtensions
    {
        public static double TerrainAltitude(this CelestialBody body, Vector3d worldPosition)
        {
            return body.TerrainAltitude(body.GetLatitude(worldPosition), body.GetLongitude(worldPosition));
        }
        
        //The KSP drag law is dv/dt = -b * v^2 where b is proportional to the air density and
        //the ship's drag coefficient. In this equation b has units of inverse length. So 1/b
        //is a characteristic length: a ship that travels this distance through air will lose a significant
        //fraction of its initial velocity
        public static double DragLength(this CelestialBody body, Vector3d pos, double dragCoeff, double mass)
        {
            double airDensity = FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(pos, body), FlightGlobals.getExternalTemperature(pos, body));

            if (airDensity <= 0) return Double.MaxValue;

            //MechJebCore.print("DragLength " + airDensity.ToString("F5") + " " +  dragCoeff.ToString("F5"));

            return mass / (0.0005 * PhysicsGlobals.DragMultiplier * airDensity * dragCoeff);
        }

        public static double DragLength(this CelestialBody body, double altitudeASL, double dragCoeff, double mass)
        {
            return body.DragLength(body.GetWorldSurfacePosition(0, 0, altitudeASL), dragCoeff, mass);
        }

        public static double RealMaxAtmosphereAltitude(this CelestialBody body)
        {
            return !body.atmosphere ? 0 : body.atmosphereDepth;
        }


        public static double AltitudeForPressure(this CelestialBody body, double pressure)
        {
            if (!body.atmosphere)
                return 0;
            double upperAlt = body.atmosphereDepth;
            double lowerAlt = 0;
            while (upperAlt - lowerAlt > 10)
            {
                double testAlt = (upperAlt + lowerAlt) * 0.5;
                double testPressure = FlightGlobals.getStaticPressure(testAlt, body);
                if (testPressure < pressure)
                {
                    upperAlt = testAlt;
                }
                else
                {
                    lowerAlt = testAlt;
                }
            }
            return (upperAlt + lowerAlt) * 0.5;
        }

        // Stock version throws an IndexOutOfRangeException when the body biome map is not defined
        public static string GetExperimentBiomeSafe(this CelestialBody body, double lat, double lon)
        {
            if (body.BiomeMap == null || body.BiomeMap.Attributes.Length == 0)
                return string.Empty;
            return ScienceUtil.GetExperimentBiome(body, lat, lon);
        }
    }
}

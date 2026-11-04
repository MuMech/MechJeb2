extern alias JetBrainsAnnotations;
using System;
using JetBrainsAnnotations::JetBrains.Annotations;
using UnityEngine;

namespace MuMech
{
    public static class CelestialBodyExtensions
    {
        public static double TerrainAltitude(this CelestialBody body, Vector3d worldPosition) =>
            body.TerrainAltitude(body.GetLatitude(worldPosition), body.GetLongitude(worldPosition));

        public static void GetLatLngAltAtUT(this CelestialBody body, double ut, Vector3d localPosition, out double lat, out double lon, out double alt)
        {
            Vector3d derotated = body.GetCurrentSurfacePositionFromUT(ut, localPosition);
            LatLon.GetLatLongAlt(body.BodyFrame, Vector3d.zero, body.Radius, derotated, out lat, out lon, out alt);
        }

        public static Vector3d GetCurrentSurfacePositionFromUT(this CelestialBody body, double ut, Vector3d localPosition)
        {
            double deltaT = ut - Planetarium.GetUniversalTime();
            double theta = 360.0 / body.rotationPeriod * deltaT;

            var derotation = QuaternionD.AngleAxis(-theta, new Vector3d(0, -1, 0));
            Vector3d derotated = derotation * localPosition;
            return derotated;
        }

        //The KSP drag law is dv/dt = -b * v^2 where b is proportional to the air density and
        //the ship's drag coefficient. In this equation b has units of inverse length. So 1/b
        //is a characteristic length: a ship that travels this distance through air will lose a significant
        //fraction of its initial velocity
        [UsedImplicitly]
        public static double DragLength(this CelestialBody body, Vector3d pos, double dragCoeff, double mass)
        {
            double airDensity =
                FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(pos, body), FlightGlobals.getExternalTemperature(pos, body));

            if (airDensity <= 0) return double.MaxValue;

            //MechJebCore.print("DragLength " + airDensity.ToString("F5") + " " +  dragCoeff.ToString("F5"));

            return mass / (0.0005 * PhysicsGlobals.DragMultiplier * airDensity * dragCoeff);
        }

        public static double DragLength(this CelestialBody body, double altitudeASL, double dragCoeff, double mass) =>
            body.DragLength(body.GetWorldSurfacePosition(0, 0, altitudeASL), dragCoeff, mass);

        public static double RealMaxAtmosphereAltitude(this CelestialBody body) => !body.atmosphere ? 0 : body.atmosphereDepth;

        // The radius of the lowest circular orbit clear of the atmosphere and the highest terrain
        public static double SafeOrbitRadius(this CelestialBody body)
        {
            double terrain = body.pqsController != null ? Math.Max(body.pqsController.mapMaxHeight, body.pqsController.radiusMax - body.Radius) : 0;
            return body.Radius + Math.Max(body.RealMaxAtmosphereAltitude(), terrain) + 3000;
        }

        // Whether a circular orbit of this radius also stays well inside the sphere of influence and away from the moons
        public static bool IsSafeOrbitRadius(this CelestialBody body, double radius)
        {
            if (radius < body.SafeOrbitRadius() || radius > 0.9 * body.sphereOfInfluence)
                return false;

            foreach (CelestialBody moon in body.orbitingBodies)
            {
                if (radius > moon.orbit.PeR - moon.sphereOfInfluence && radius < moon.orbit.ApR + moon.sphereOfInfluence)
                    return false;
            }

            return true;
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
            return ScienceUtil.GetExperimentBiomeLocalized(body, lat, lon);
        }

        public static float GetPQSSlopeDegrees(
            this CelestialBody body,
            double latitude, double longitude,
            double sampleRadiusMeters = 50.0)
        {
            double metersPerDeg = body.Radius * Math.PI / 180.0;
            double epsilon = sampleRadiusMeters / metersPerDeg;

            // a degree of longitude is only cos(latitude) as long, so widen the longitude step to keep it sampleRadiusMeters
            double cosLat = Math.Max(Math.Cos(latitude * Math.PI / 180.0), 0.01);
            double epsilonLon = epsilon / cosLat;

            // do not step past the poles, the actual north-south distance is used below
            double latN = Math.Min(latitude + epsilon, 90.0);
            double latS = Math.Max(latitude - epsilon, -90.0);

            double hN = body.TerrainAltitude(latN, longitude, true);
            double hS = body.TerrainAltitude(latS, longitude, true);
            double hE = body.TerrainAltitude(latitude, longitude + epsilonLon, true);
            double hW = body.TerrainAltitude(latitude, longitude - epsilonLon, true);

            double dhdx = (hE - hW) / (2.0 * epsilonLon * metersPerDeg * cosLat);
            double dhdy = latN > latS ? (hN - hS) / ((latN - latS) * metersPerDeg) : 0;

            return (float)(Math.Atan(Math.Sqrt(dhdx * dhdx + dhdy * dhdy)) * 180.0 / Math.PI);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public static class CelestialBodyExtensions
    {
        public static double TerrainAltitude(this CelestialBody body, Vector3d worldPosition)
        {
            return body.TerrainAltitude(body.GetLatitude(worldPosition), body.GetLongitude(worldPosition));
        }

        public static double TerrainAltitude(this CelestialBody body, double latitude, double longitude)
        {
            if (body.pqsController == null) return 0;

            Vector3d pqsRadialVector = QuaternionD.AngleAxis(longitude, Vector3d.down) * QuaternionD.AngleAxis(latitude, Vector3d.forward) * Vector3d.right;
            double ret = body.pqsController.GetSurfaceHeight(pqsRadialVector) - body.pqsController.radius;
            if (ret < 0) ret = 0;
            return ret;
        }

        //formula for drag seems to be drag force = (1/2) * DragMultiplier * (air density) * (mass * max_drag) * (airspeed)^2
        //so drag acceleration is (1/2) * DragMultiplier * (air density) * (average max_drag) * (airspeed)^2
        //where the max_drag average over parts is weighted by the part mass
        public static Vector3d DragAccel(this CelestialBody body, Vector3d pos, Vector3d orbitVel, double dragCoeffOverMass)
        {
            double airPressure = FlightGlobals.getStaticPressure(pos, body);

            //Atmosphere cuts out abruptly when pressure falls below 1e-6 * sea level pressure.
            if (airPressure < body.atmosphereMultiplier * 1e-6) return Vector3d.zero;

            double airDensity = FlightGlobals.getAtmDensity(airPressure);
            Vector3d airVel = orbitVel - body.getRFrmVel(pos);
            return -0.5 * FlightGlobals.DragMultiplier * airDensity * dragCoeffOverMass * airVel.magnitude * airVel;
        }

        //The KSP drag law is dv/dt = -b * v^2 where b is proportional to the air density and
        //the ship's drag coefficient. In this equation b has units of inverse length. So 1/b
        //is a characteristic length: a ship that travels this distance through air will lose a significant
        //fraction of its initial velocity
        public static double DragLength(this CelestialBody body, Vector3d pos, double dragCoeffOverMass)
        {
            double airDensity = FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(pos, body));
            if (airDensity <= 0) return Double.MaxValue;
            return 1.0 / (0.5 * FlightGlobals.DragMultiplier * airDensity * dragCoeffOverMass);
        }

        public static double DragLength(this CelestialBody body, double altitudeASL, double dragCoeffOverMass)
        {
            return body.DragLength(body.GetWorldSurfacePosition(0, 0, altitudeASL), dragCoeffOverMass);
        }

        //CelestialBody.maxAtmosphereAltitude doesn't actually give the upper edge of
        //the atmosphere. Use this function instead.
        public static double RealMaxAtmosphereAltitude(this CelestialBody body)
        {
            if (!body.atmosphere) return 0;
            if (body.useLegacyAtmosphere)
            {
                //Atmosphere actually cuts out when exp(-altitude / scale height) = 1e-6
                return -body.atmosphereScaleHeight * 1000 * Math.Log(1e-6);
            }
            else
            {
                return body.pressureCurve.keys.Last().time * 1000;
            }
        }
    }
}

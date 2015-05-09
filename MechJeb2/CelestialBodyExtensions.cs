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
    }
}

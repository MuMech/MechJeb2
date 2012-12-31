using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    //A static class that makes it slightly easier to get info about the current target
    public static class Target
    {
        public static bool Exists()
        {
            ITargetable t = FlightGlobals.fetch.VesselTarget;
            return (t != null && (t is Vessel || t is CelestialBody || t is ModuleDockingNode));
        }

        public static Orbit Orbit()
        {
            return FlightGlobals.fetch.VesselTarget.GetOrbit();
        }

        public static Vector3 Position()
        {
            return FlightGlobals.fetch.VesselTarget.GetTransform().position;
        }

        public static float Distance(Vessel v)
        {
            return Vector3.Distance(Position(), v.GetTransform().position);
        }

        public static Vector3d RelativeVelocity(Vessel v)
        {
            return (v.orbit.GetVel() - Orbit().GetVel());
        }

        public static Vector3d RelativePosition(Vessel v)
        {
            return (v.GetTransform().position - Position());
        }


        public static Transform Transform()
        {
            return FlightGlobals.fetch.VesselTarget.GetTransform();
        }

        public static string Name()
        {
            return FlightGlobals.fetch.VesselTarget.GetName();
        }
    }

    public class PositionTarget : ITargetable
    {
        GameObject g = new GameObject();
        string name;
        Vector3d position;

        public PositionTarget(string name)
        {
            this.name = name;
        }

        public void Update(CelestialBody body, double latitude, double longitude)
        {
            double altitude = MuUtils.TerrainAltitude(body, latitude, longitude);
            Update(body, latitude, longitude, altitude);
        }


        public void Update(CelestialBody body, double latitude, double longitude, double altitude)
        {
            Update(body.position + (body.Radius + altitude) * body.GetSurfaceNVector(latitude, longitude));
        }

        //Call this every frame to make sure the target transform stays up to date
        public void Update(Vector3d position)
        {
            this.position = position;
            g.transform.position = position;
        }

        public Vector3 GetFwdVector() { return Vector3d.up; }
        public string GetName() { return name; }
        public Vector3 GetObtVelocity() { return Vector3.zero; }
        public Orbit GetOrbit() { return null; }
        public OrbitDriver GetOrbitDriver() { return null; }
        public Vector3 GetSrfVelocity() { return Vector3.zero; }
        public Transform GetTransform() { return g.transform; }
        public Vessel GetVessel() { return null; }
    }

    public class DirectionTarget : PositionTarget
    {
        Vector3d direction;

        public DirectionTarget(string name) : base(name) { }

        //Call this every frame to make sure the target transform stays up to date
        public new void Update(Vector3d direction)
        {
            this.direction = direction;
            base.Update(FlightGlobals.ActiveVessel.transform.position + 10000 * direction);
        }
    }

}

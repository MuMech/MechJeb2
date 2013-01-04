using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    class ReentrySimulation
    {
        //parameters of the problem:
        Orbit initialOrbit;
        double seaLevelAtmospheres;
        double scaleHeight;
        double bodyRadius;
        double gravParameter;
        double dragCoefficient; //massDrag / mass
        Vector3d bodyAngularVelocity;
        IDescentSpeedPolicy descentSpeedPolicy;
        double landedRadius;
        double aerobrakedRadius;
        double startUT;
        CelestialBody mainBody; //we're not actually allowed to call any functions on this from our separate thread, we just keep it as reference

        Vector3d lat0lon0AtStart;
        Vector3d lat0lon90AtStart;
        Vector3d lat90AtStart;
        double bodyRotationPeriod;

        const double baseDt = 0.2;
        double dt;
        const double maxSimulatedTime = 2000; //in seconds


        //Dynamical variables 
        Vector3d x; //coordinate system used is centered on main body
        Vector3d v;
        double t;

        //Accumulated results
        double maxDragGees;
        double deltaVExpended;
        Trajectory trajectory;

        public ReentrySimulation(Orbit orbit, double UT, double dragCoefficient,
            IDescentSpeedPolicy descentSpeedPolicy, double endAltitudeASL, double coarseness)
        {
            //make a separate Orbit instance for ourselves:
            initialOrbit = MuUtils.OrbitFromStateVectors(orbit.SwappedAbsolutePositionAtUT(UT), orbit.SwappedOrbitalVelocityAtUT(UT), orbit.referenceBody, UT);

            CelestialBody body = orbit.referenceBody;
            seaLevelAtmospheres = body.atmosphereMultiplier;
            scaleHeight = 1000 * body.atmosphereScaleHeight;
            bodyRadius = body.Radius;
            gravParameter = body.gravParameter;
            this.dragCoefficient = dragCoefficient;
            bodyAngularVelocity = body.angularVelocity;
            this.descentSpeedPolicy = descentSpeedPolicy;
            landedRadius = bodyRadius + endAltitudeASL;
            aerobrakedRadius = bodyRadius + body.maxAtmosphereAltitude;
            mainBody = body;
            
            lat0lon0AtStart = body.GetSurfaceNVector(0, 0);
            lat0lon90AtStart = body.GetSurfaceNVector(0, 90);
            lat90AtStart = body.GetSurfaceNVector(90, 0);
            bodyRotationPeriod = body.rotationPeriod;
            
            dt = baseDt * coarseness;
            
            x = initialOrbit.SwappedRelativePositionAtUT(UT);
            v = initialOrbit.SwappedOrbitalVelocityAtUT(UT);
            startUT = UT;
            
            maxDragGees = 0;
            deltaVExpended = 0;
            trajectory = new Trajectory();
        }


        public Result RunSimulation()
        {
            Result result = new Result();

            if (!OrbitReenters()) { result.outcome = Outcome.NO_REENTRY; return result; }

            t = startUT;

            AdvanceToFreefallEnd();

            double maxT = t + maxSimulatedTime;
            while (true)
            {
                if (Landed()) { result.outcome = Outcome.LANDED; break; }
                if (Aerobraked()) { result.outcome = Outcome.AEROBRAKED; break; }
                if (t > maxT) { result.outcome = Outcome.TIMED_OUT; break; }

                RK4Step();
                LimitSpeed();
                RecordTrajectory();
            }

            result.endUT = t;
            result.maxDragGees = maxDragGees;
            result.deltaVExpended = deltaVExpended;
            result.endLatitude = Latitude(x, t);
            result.endLongitude = Longitude(x, t);
            result.trajectory = trajectory;
            return result;
        }

        bool OrbitReenters()
        {
            return (initialOrbit.PeR < landedRadius || initialOrbit.PeR < aerobrakedRadius);
        }

        bool Landed()
        {
            return x.magnitude < landedRadius;
        }

        bool Aerobraked()
        {
            return (x.magnitude > aerobrakedRadius) && (Vector3d.Dot(x, v) > 0);
        }

        void AdvanceToFreefallEnd()
        {
            t = FindFreefallEndTime();
            x = initialOrbit.SwappedRelativePositionAtUT(t);
            v = initialOrbit.SwappedOrbitalVelocityAtUT(t);
        }

        //This is a convenience function used by the reentry simulation. It does a binary search for the first UT
        //in the interval (lowerUT, upperUT) for which condition(UT, relative position, orbital velocity) is true
        double FindFreefallEndTime()
        {
            double lowerUT = t;
            double upperUT = initialOrbit.NextPeriapsisTime(t);

            const double PRECISION = 1.0;
            while (upperUT - lowerUT > PRECISION)
            {
                double testUT = (upperUT + lowerUT) / 2;
                if (FreefallEnded(testUT)) upperUT = testUT;
                else lowerUT = testUT;
            }
            return (upperUT + lowerUT) / 2;
        }

        //Freefall orbit ends when either
        // - we enter the atmosphere, or
        // - our vertical velocity is negative and either
        //    - we've landed or
        //    - the descent speed policy says to start braking
        bool FreefallEnded(double UT)
        {
            Vector3d pos = initialOrbit.SwappedRelativePositionAtUT(UT);
            Vector3d surfaceVelocity = SurfaceVelocity(pos, initialOrbit.SwappedOrbitalVelocityAtUT(UT));

            if (pos.magnitude < aerobrakedRadius) return true;
            if (Vector3d.Dot(surfaceVelocity, initialOrbit.Up(UT)) > 0) return false;
            if (pos.magnitude < landedRadius) return true;
            if (descentSpeedPolicy != null && surfaceVelocity.magnitude > descentSpeedPolicy.MaxAllowedSpeed(pos, surfaceVelocity)) return true;
            return false;
        }

        //one time step of RK4:
        void RK4Step()
        {
            maxDragGees = Math.Max(maxDragGees, DragAccel(x, v).magnitude / 9.81f);

            Vector3d dv1 = dt * TotalAccel(x, v);
            Vector3d dx1 = dt * v;

            Vector3d dv2 = dt * TotalAccel(x + 0.5 * dx1, v + 0.5 * dv1);
            Vector3d dx2 = dt * (v + 0.5 * dv1);

            Vector3d dv3 = dt * TotalAccel(x + 0.5 * dx2, v + 0.5 * dv2);
            Vector3d dx3 = dt * (v + 0.5 * dv2);

            Vector3d dv4 = dt * TotalAccel(x + dx3, v + dv3);
            Vector3d dx4 = dt * (v + dv3);

            Vector3d dx = (dx1 + 2 * dx2 + 2 * dx3 + dx4) / 6.0;
            Vector3d dv = (dv1 + 2 * dv2 + 2 * dv3 + dv4) / 6.0;

            x += dx;
            v += dv;
            t += dt;
        }

        //enforce the descent speed policy
        void LimitSpeed()
        {
            if (descentSpeedPolicy == null) return;

            double maxAllowedSpeed = descentSpeedPolicy.MaxAllowedSpeed(x, v);
            if (v.magnitude > maxAllowedSpeed)
            {
                deltaVExpended += v.magnitude - maxAllowedSpeed;
                v *= maxAllowedSpeed / v.magnitude;
            }
        }

        void RecordTrajectory()
        {
            trajectory.Add(new Trajectory.Point
                {
                    body = mainBody,
                    latitude = Latitude(x, t),
                    longitude = Longitude(x, t),
                    radius = x.magnitude,
                    UT = t
                });
        }


        Vector3d TotalAccel(Vector3d pos, Vector3d vel)
        {
            return GravAccel(pos) + DragAccel(pos, vel);
        }

        Vector3d GravAccel(Vector3d pos)
        {
            return -(gravParameter / pos.sqrMagnitude) * pos.normalized;
        }

        Vector3d DragAccel(Vector3d pos, Vector3d vel)
        {
            Vector3d airVel = SurfaceVelocity(pos, vel);
            return -0.5 * FlightGlobals.DragMultiplier * dragCoefficient * AirDensity(pos) * airVel.sqrMagnitude * airVel.normalized;
        }

        Vector3d SurfaceVelocity(Vector3d pos, Vector3d vel)
        {
            return vel - Vector3d.Cross(bodyAngularVelocity, pos);
        }

        double AirDensity(Vector3d pos)
        {
            double pressure = seaLevelAtmospheres * Math.Exp(-(pos.magnitude - bodyRadius) / scaleHeight);
            return FlightGlobals.getAtmDensity(pressure);
        }


        double Latitude(Vector3d pos, double UT)
        {
            return 180 / Math.PI * Math.Asin(Vector3d.Dot(pos.normalized, lat90AtStart));
        }

        double Longitude(Vector3d pos, double UT)
        {
            double ret = 180 / Math.PI * Math.Atan2(Vector3d.Dot(pos.normalized, lat0lon90AtStart), Vector3d.Dot(pos.normalized, lat0lon0AtStart));
            ret -= 360 * (UT - startUT) / bodyRotationPeriod;
            return MuUtils.ClampDegrees180(ret);
        }

        //An IDescentSpeedPolicy describes a strategy for doing the brakinb burn.
        //while landing. The function MaxAllowedSpeed is supposed to compute the maximum allowed speed
        //as a function of body-relative position and rotating frame, surface-relative velocity. 
        //This lets the ReentrySimulator simulate the descent of a vessel following this policy.
        //
        //Note: the IDescentSpeedPolicy object passed into the simulation will be used in the separate simulation
        //thread. It must not point to mutable objects that may change over the course of the simulation. Similarly,
        //you must be sure not to modify the IDescentSpeedPolicy object itself after passing it to the simulation.
        public interface IDescentSpeedPolicy
        {
            double MaxAllowedSpeed(Vector3d pos, Vector3d surfaceVel);
        }

        public enum Outcome { LANDED, AEROBRAKED, TIMED_OUT, NO_REENTRY }

        public struct Result
        {
            public Outcome outcome;
            public double endUT;
            public double endLatitude;
            public double endLongitude;
            public double maxDragGees;
            public double deltaVExpended;
            public Trajectory trajectory;
        }

        //A trajectory is a set of position vs. time data points, where the positions are 
        //stored as latitudes, longitudes, and altitudes.
        public class Trajectory
        {
            public struct Point
            {
                public CelestialBody body;
                public double UT;
                public double latitude;
                public double longitude;
                public double radius;

                public Vector3d AsVector()
                {
                    return body.position + radius * body.GetSurfaceNVector(latitude, longitude);
                }

                public Vector3d AsVectorWithoutRotation(double now)
                {
                    double unrotatedLongitude = MuUtils.ClampDegrees360(longitude + 360 * (UT - now) / body.rotationPeriod);
                    return body.position + radius * body.GetSurfaceNVector(latitude, unrotatedLongitude);
                }
            }

            public List<Point> points = new List<Point>();

            public void Add(Point p) { points.Add(p); }

            public void DrawOnMap(double timeStep, Color c, bool dashed, bool drawLandingMark, Color landingMarkColor)
            {
                if (points.Count() == 0) return;

                if (drawLandingMark)
                {
                    Point last = points.Last();
                    GLUtils.DrawMapViewGroundMarker(last.body, last.latitude, last.longitude, landingMarkColor, 60);
                }

                List<Vector3d> drawnPoints = new List<Vector3d>();
                double now = Planetarium.GetUniversalTime();
                double lastUT = Double.MinValue;
                foreach (Point p in points)
                {
                    if (p.UT > lastUT + timeStep)
                    {
                        drawnPoints.Add(p.AsVectorWithoutRotation(now));
                        lastUT = p.UT;
                    }
                }

                GLUtils.DrawPath(points[0].body, drawnPoints.ToArray(), c, dashed);
            }
        }
    }
}

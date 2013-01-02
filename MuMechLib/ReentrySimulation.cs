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
        double seaLevelAtmospheres;
        double scaleHeight;
        double bodyRadius;
        double gravParameter;
        double dragCoefficient; //massDrag / mass
        Vector3d bodyAngularVelocity;
        IDescentSpeedPolicy descentSpeedPolicy;
        double radiusLowerBound;
        double radiusUpperBound;
        double startUT;

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


        public ReentrySimulation(Vector3d position, Vector3d velocity, double UT, CelestialBody body, double dragCoefficient, 
            IDescentSpeedPolicy descentSpeedPolicy, double endAltitudeASL, double coarseness) 
        {
            seaLevelAtmospheres = body.atmosphereMultiplier;
            scaleHeight = 1000 * body.atmosphereScaleHeight;
            bodyRadius = body.Radius;
            gravParameter = body.gravParameter;
            this.dragCoefficient = dragCoefficient;
            bodyAngularVelocity = body.angularVelocity;
            this.descentSpeedPolicy = descentSpeedPolicy;
            radiusLowerBound = bodyRadius + endAltitudeASL;
            radiusUpperBound = bodyRadius + body.maxAtmosphereAltitude;

            lat0lon0AtStart = body.GetSurfaceNVector(0, 0);
            lat0lon90AtStart = body.GetSurfaceNVector(0, 90);
            lat90AtStart = body.GetSurfaceNVector(90, 0);
            bodyRotationPeriod = body.rotationPeriod;

            dt = baseDt * coarseness;

            x = position - body.position;
            v = velocity;
            startUT = UT;

            maxDragGees = 0;
            deltaVExpended = 0;
        }


        void RunSimulation()
        {
            Result result = new Result();

            t = startUT;
            double maxT = t + maxSimulatedTime;
            while(true)
            {
                if (x.magnitude < radiusLowerBound) { result.outcome = Outcome.LANDED; break; }
                if (x.magnitude > radiusUpperBound) { result.outcome = Outcome.AEROBRAKED; break; }
                if (t > maxT) { result.outcome = Outcome.TIMED_OUT; break; }

                RK4Step();
                LimitSpeed();
            }

            result.endUT = t;
            result.maxDragGees = maxDragGees;
            result.deltaVExpended = deltaVExpended;
            result.endLatitude = Latitude(x, t);
            result.endLongitude = Longitude(x, t);
        }


        //one time step of RK4:
        void RK4Step()
        {
            maxDragGees = Math.Max(maxDragGees, DragAccel(x, v).magnitude) / 9.81f;

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
            Vector3d airRotationVelocity = Vector3d.Cross(bodyAngularVelocity, pos);
            Vector3d airVel = vel - airRotationVelocity;
            return -0.5 * FlightGlobals.DragMultiplier * dragCoefficient * AirDensity(pos) * airVel.sqrMagnitude * airVel.normalized;
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
            double ret = 180 / Math.PI * Math.Atan2(Vector3d.Dot(pos.normalized, lat90AtStart), Vector3d.Dot(pos.normalized, lat0lon90AtStart));
            ret -= 360 * (UT - startUT) / bodyRotationPeriod;
            return MuUtils.ClampDegrees180(ret);
        }


        //An IDescentSpeedPolicy describes a strategy for reducing your speed as a function of altitude
        //while landing. The function MaxAllowedSpeed is supposed to compute the maximum allowed speed
        //as a function of (body-relative) position and velocity. This lets the ReentrySimulator simulate
        //the descent of a vessel following this policy.
        //
        //Note: the IDescentSpeedPolicy object passed into the simulation will be used in the separate simulation
        //thread. It must not point to mutable objects that may change over the course of the simulation. Similarly,
        //you must be sure not to modify the IDescentSpeedPolicy object itself after passing it to the simulation.
        public interface IDescentSpeedPolicy
        {
            double MaxAllowedSpeed(Vector3d pos, Vector3d vel);
        }

        public enum Outcome { LANDED, AEROBRAKED, TIMED_OUT }

        public struct Result
        {
            public Outcome outcome;
            public double endUT;
            public double endLatitude;
            public double endLongitude;
            public double maxDragGees;
            public double deltaVExpended;
        }
    }
}

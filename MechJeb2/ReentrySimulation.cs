using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class ReentrySimulation
    {
        //parameters of the problem:
        bool bodyHasAtmosphere;
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
        double maxThrustAccel;

        bool orbitReenters;

        ReferenceFrame referenceFrame;

        double dt = 0.2; //in seconds
        const double maxSimulatedTime = 2000; //in seconds


        //Dynamical variables 
        Vector3d x; //coordinate system used is centered on main body
        Vector3d v;
        double t;

        //Accumulated results
        double maxDragGees;
        double deltaVExpended;
        List<AbsoluteVector> trajectory;

        public ReentrySimulation(Orbit initialOrbit, double UT, double dragCoefficient,
            IDescentSpeedPolicy descentSpeedPolicy, double endAltitudeASL, double maxThrustAccel)
        {
            CelestialBody body = initialOrbit.referenceBody;
            bodyHasAtmosphere = body.atmosphere;
            seaLevelAtmospheres = body.atmosphereMultiplier;
            scaleHeight = 1000 * body.atmosphereScaleHeight;
            bodyRadius = body.Radius;
            gravParameter = body.gravParameter;
            this.dragCoefficient = dragCoefficient;
            bodyAngularVelocity = body.angularVelocity;
            this.descentSpeedPolicy = descentSpeedPolicy;
            landedRadius = bodyRadius + endAltitudeASL;
            aerobrakedRadius = bodyRadius + body.RealMaxAtmosphereAltitude();
            mainBody = body;
            this.maxThrustAccel = maxThrustAccel;

            referenceFrame = ReferenceFrame.CreateAtCurrentTime(initialOrbit.referenceBody);

            orbitReenters = OrbitReenters(initialOrbit);

            if (orbitReenters)
            {
                startUT = UT;
                t = startUT;
                AdvanceToFreefallEnd(initialOrbit);
            }

            maxDragGees = 0;
            deltaVExpended = 0;
            trajectory = new List<AbsoluteVector>();
        }

        public Result RunSimulation()
        {
            Result result = new Result();

            if (!orbitReenters) { result.outcome = Outcome.NO_REENTRY; return result; }

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

            result.body = mainBody;
            result.referenceFrame = referenceFrame;
            result.endUT = t;
            result.maxDragGees = maxDragGees;
            result.deltaVExpended = deltaVExpended;
            result.endPosition = referenceFrame.ToAbsolute(x, t);
            result.endVelocity = referenceFrame.ToAbsolute(v, t);
            result.trajectory = trajectory;
            return result;
        }

        bool OrbitReenters(Orbit initialOrbit)
        {
            return (initialOrbit.PeR < landedRadius || initialOrbit.PeR < aerobrakedRadius);
        }

        bool Landed()
        {
            return x.magnitude < landedRadius;
        }

        bool Aerobraked()
        {
            return bodyHasAtmosphere && (x.magnitude > aerobrakedRadius) && (Vector3d.Dot(x, v) > 0);
        }

        void AdvanceToFreefallEnd(Orbit initialOrbit)
        {

            t = FindFreefallEndTime(initialOrbit);

            x = initialOrbit.SwappedRelativePositionAtUT(t);
            v = initialOrbit.SwappedOrbitalVelocityAtUT(t);

            if (double.IsNaN(v.magnitude))
            {
                //For eccentricities close to 1, the Orbit class functions are unreliable and
                //v may come out as NaN. If that happens we estimate v from conservation
                //of energy and the assumption that v is vertical (since ecc. is approximately 1).

                //0.5 * v^2 - GM / r = E   =>    v = sqrt(2 * (E + GM / r))
                double GM = initialOrbit.referenceBody.gravParameter;
                double E = -GM / (2 * initialOrbit.semiMajorAxis);
                v = Math.Sqrt(Math.Abs(2 * (E + GM / x.magnitude))) * x.normalized;
                if (initialOrbit.MeanAnomalyAtUT(t) > Math.PI) v *= -1;
            }
        }

        //This is a convenience function used by the reentry simulation. It does a binary search for the first UT
        //in the interval (lowerUT, upperUT) for which condition(UT, relative position, orbital velocity) is true
        double FindFreefallEndTime(Orbit initialOrbit)
        {
            if (FreefallEnded(initialOrbit, t))
            {
                return t;
            }

            double lowerUT = t;
            double upperUT = initialOrbit.NextPeriapsisTime(t);

            const double PRECISION = 1.0;
            while (upperUT - lowerUT > PRECISION)
            {
                double testUT = (upperUT + lowerUT) / 2;
                if (FreefallEnded(initialOrbit, testUT)) upperUT = testUT;
                else lowerUT = testUT;
            }
            return (upperUT + lowerUT) / 2;
        }

        //Freefall orbit ends when either
        // - we enter the atmosphere, or
        // - our vertical velocity is negative and either
        //    - we've landed or
        //    - the descent speed policy says to start braking
        bool FreefallEnded(Orbit initialOrbit, double UT)
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

            Vector3d surfaceVel = SurfaceVelocity(x, v);
            double maxAllowedSpeed = descentSpeedPolicy.MaxAllowedSpeed(x, surfaceVel);
            if (surfaceVel.magnitude > maxAllowedSpeed)
            {
                double dV = Math.Min(surfaceVel.magnitude - maxAllowedSpeed, dt * maxThrustAccel);
                surfaceVel -= dV * surfaceVel.normalized;
                deltaVExpended += dV;
                v = surfaceVel + Vector3d.Cross(bodyAngularVelocity, x);
            }
        }

        void RecordTrajectory()
        {
            trajectory.Add(referenceFrame.ToAbsolute(x, t));
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
            if (!bodyHasAtmosphere) return Vector3d.zero;
            Vector3d airVel = SurfaceVelocity(pos, vel);
            return -0.5 * FlightGlobals.DragMultiplier * dragCoefficient * AirDensity(pos) * airVel.sqrMagnitude * airVel.normalized;
        }

        Vector3d SurfaceVelocity(Vector3d pos, Vector3d vel)
        {
            //if we're low enough, calculate the airspeed properly:
            return vel - Vector3d.Cross(bodyAngularVelocity, pos);
        }

        double AirDensity(Vector3d pos)
        {
            double ratio = Math.Exp(-(pos.magnitude - bodyRadius) / scaleHeight);
            if (ratio < 1e-6) return 0; //this is not a fudge, this is faithfully simulating the game, which
            //pretends the pressure is zero if it is less than 1e-6 times the sea level pressure
            double pressure = seaLevelAtmospheres * ratio;
            return FlightGlobals.getAtmDensity(pressure);
        }

        public enum Outcome { LANDED, AEROBRAKED, TIMED_OUT, NO_REENTRY }

        public class Result
        {
            public Outcome outcome;

            public CelestialBody body;
            public ReferenceFrame referenceFrame;
            public double endUT;
            public AbsoluteVector endPosition;
            public AbsoluteVector endVelocity;
            public List<AbsoluteVector> trajectory;

            public double maxDragGees;
            public double deltaVExpended;

            public Vector3d RelativeEndPosition()
            {
                return WorldEndPosition() - body.position;
            }

            public Vector3d WorldEndPosition()
            {
                return referenceFrame.WorldPositionAtCurrentTime(endPosition);
            }

            public Vector3d WorldEndVelocity()
            {
                return referenceFrame.WorldVelocityAtCurrentTime(endVelocity);
            }

            public Orbit EndOrbit()
            {
                return MuUtils.OrbitFromStateVectors(WorldEndPosition(), WorldEndVelocity(), body, endUT);
            }

            public List<Vector3d> WorldTrajectory(double timeStep)
            {
                if (trajectory.Count() == 0) return new List<Vector3d>();

                List<Vector3d> ret = new List<Vector3d>();
                ret.Add(referenceFrame.WorldPositionAtCurrentTime(trajectory[0]));
                double lastTime = trajectory[0].UT;
                foreach (AbsoluteVector absolute in trajectory)
                {
                    if (absolute.UT > lastTime + timeStep)
                    {
                        ret.Add(referenceFrame.WorldPositionAtCurrentTime(absolute));
                        lastTime = absolute.UT;
                    }
                }
                return ret;
            }
        }
    }

    //An IDescentSpeedPolicy describes a strategy for doing the braking burn.
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

    //Why do AbsoluteVector and ReferenceFrame exist? What problem are they trying to solve? Here is the problem.
    //
    //The reentry simulation runs in a separate thread from the rest of the game. In principle, the reentry simulation
    //could take quite a while to complete. Meanwhile, some time has elapsed in the game. One annoying that that happens
    //as time progresses is that the origin of the world coordinate system shifts (due to the floating origin feature).
    //Furthermore, the axes of the world coordinate system rotate when you are near the surface of a rotating celestial body.
    //
    //So, one thing we do in the reentry simulation is be careful not to refer to external objects that may change
    //with time. Once the constructor finishes, the ReentrySimulation stores no reference to any CelestialBody, or Vessel,
    //or Orbit. It just stores the numbers that it needs to crunch. Then it crunches them, and comes out with a deterministic answer
    //that will never be affected by what happened in the game while it was crunching.
    //
    //However, this is not enough. What does the answer that the simulation produces mean? Suppose the reentry
    //simulation chugs through its calculations and determines that the vessel is going to land at the position
    //(400, 500, 600). That's fine, but where is that, exactly? The origin of the world coordinate system may have shifted,
    //and its axes may have rotated, since the simulation began. So (400, 500, 600) now refers to a different place
    //than it did when the simulation started.
    //
    //To deal with this, any vectors (that is, positions and velocities) that the reentry simulation produces as output need to
    //be provided in some unambiguous format, so that we can interpret these positions and velocities correctly at a later
    //time, regardless of what sort of origin shifts and axis rotations have occurred.
    //
    //Now, it doesn't particularly matter what unambiguous format we use, as long as it is in fact unambiguous. We choose to 
    //represent positions unambiguously via a latitude, a longitude, a radius, and a time. If we record these four data points
    //for an event, we can unambiguously reconstruct the position of the event at a later time. We just have to account for the
    //fact that the rotation of the planet means that the same position will have a different longitude.

    //An AbsoluteVector stores the information needed to unambiguously reconstruct a position or velocity at a later time.
    public struct AbsoluteVector
    {
        public double latitude;
        public double longitude;
        public double radius;
        public double UT;
    }

    //A ReferenceFrame is a scheme for converting Vector3d positions and velocities into AbsoluteVectors, and vice versa
    public class ReferenceFrame
    {
        private double epoch;
        private Vector3d lat0lon0AtStart;
        private Vector3d lat0lon90AtStart;
        private Vector3d lat90AtStart;
        private CelestialBody referenceBody;

        private ReferenceFrame() { }

        public static ReferenceFrame CreateAtCurrentTime(CelestialBody referenceBody)
        {
            ReferenceFrame ret = new ReferenceFrame();
            ret.lat0lon0AtStart = referenceBody.GetSurfaceNVector(0, 0);
            ret.lat0lon90AtStart = referenceBody.GetSurfaceNVector(0, 90);
            ret.lat90AtStart = referenceBody.GetSurfaceNVector(90, 0);
            ret.epoch = Planetarium.GetUniversalTime();
            ret.referenceBody = referenceBody;
            return ret;
        }

        //Vector3d must be either a position RELATIVE to referenceBody, or a velocity
        public AbsoluteVector ToAbsolute(Vector3d vector3d, double UT)
        {
            AbsoluteVector absolute = new AbsoluteVector();

            absolute.latitude = 180 / Math.PI * Math.Asin(Vector3d.Dot(vector3d.normalized, lat90AtStart));

            double longitude = 180 / Math.PI * Math.Atan2(Vector3d.Dot(vector3d.normalized, lat0lon90AtStart), Vector3d.Dot(vector3d.normalized, lat0lon0AtStart));
            longitude -= 360 * (UT - epoch) / referenceBody.rotationPeriod;
            absolute.longitude = MuUtils.ClampDegrees180(longitude);

            absolute.radius = vector3d.magnitude;

            absolute.UT = UT;

            return absolute;
        }

        //Interprets a given AbsoluteVector as a position, and returns the corresponding Vector3d position
        //in world coordinates.
        public Vector3d WorldPositionAtCurrentTime(AbsoluteVector absolute)
        {
            return referenceBody.position + WorldVelocityAtCurrentTime(absolute);
        }

        //Interprets a given AbsoluteVector as a velocity, and returns the corresponding Vector3d velocity
        //in world coordinates.
        public Vector3d WorldVelocityAtCurrentTime(AbsoluteVector absolute)
        {
            double now = Planetarium.GetUniversalTime();
            double unrotatedLongitude = MuUtils.ClampDegrees360(absolute.longitude - 360 * (now - absolute.UT) / referenceBody.rotationPeriod);
            return absolute.radius * referenceBody.GetSurfaceNVector(absolute.latitude, unrotatedLongitude);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class ReentrySimulation
    {
        // Input values
         Orbit input_initialOrbit;
         double input_UT; 
         double input_dragMassExcludingUsedParachutes; 
         List<SimulatedParachute> input_parachuteList; 
         double input_mass;
         IDescentSpeedPolicy input_descentSpeedPolicy; 
         double input_decelEndAltitudeASL; 
         double input_maxThrustAccel; 
         double input_parachuteSemiDeployMultiplier; 
         double input_probableLandingSiteASL; 
         bool input_multiplierHasError;
         double input_dt;

        //parameters of the problem:
        bool bodyHasAtmosphere;
        double seaLevelAtmospheres;
        double scaleHeight;
        double bodyRadius;
        double gravParameter;
        double dragMassExcludingUsedParachutes;         
        double mass;
        Vector3d bodyAngularVelocity;
        IDescentSpeedPolicy descentSpeedPolicy;
        double decelRadius;
        double aerobrakedRadius;
        double startUT;
        CelestialBody mainBody; //we're not actually allowed to call any functions on this from our separate thread, we just keep it as reference
        double maxThrustAccel;
        double probableLandingSiteASL; // This is the height of the ground at the point we think we will land. It is infact calculated by getting the height of the previous prediction. It is used to decide when the parachutes will be deployed.
        double probableLandingSiteRadius; // This is the height of the ground from the centre of the body at the point we think we will land. It is infact calculated by getting the height of the previous prediction. It is used to decide when the parachutes will be deployed, and when we have landed.

        bool orbitReenters;

        ReferenceFrame referenceFrame;
        
        double dt;
        double max_dt;
        double min_dt = 0.01; //in seconds
        const double maxSimulatedTime = 2000; //in seconds

        List<SimulatedParachute> parachutes;
        double parachuteSemiDeployMultiplier;
        bool multiplierHasError;

        //Dynamical variables 
        Vector3d x; //coordinate system used is centered on main body
        Vector3d v;
        double t;

        //Accumulated results
        double maxDragGees;
        double deltaVExpended;
        List<AbsoluteVector> trajectory;

        public ReentrySimulation(Orbit _initialOrbit, double _UT, double _dragMassExcludingUsedParachutes, List<SimulatedParachute> _parachuteList, double _mass,
            IDescentSpeedPolicy _descentSpeedPolicy, double _decelEndAltitudeASL, double _maxThrustAccel, double _parachuteSemiDeployMultiplier, double _probableLandingSiteASL, bool _multiplierHasError, double _dt)
        {
            // Store all the input values as they were given
            input_initialOrbit = _initialOrbit;
            input_UT = _UT;
            input_dragMassExcludingUsedParachutes = _dragMassExcludingUsedParachutes;
            input_parachuteList = _parachuteList;
            input_mass = _mass;
            input_descentSpeedPolicy = _descentSpeedPolicy;
            input_decelEndAltitudeASL = _decelEndAltitudeASL;
            input_maxThrustAccel = _maxThrustAccel;
            input_parachuteSemiDeployMultiplier = _parachuteSemiDeployMultiplier;
            input_probableLandingSiteASL = _probableLandingSiteASL;
            input_multiplierHasError = _multiplierHasError;
            input_dt = _dt;

            max_dt = _dt;
            dt = max_dt;             

            CelestialBody body = _initialOrbit.referenceBody;
            bodyHasAtmosphere = body.atmosphere;
            seaLevelAtmospheres = body.atmosphereMultiplier;
            scaleHeight = 1000 * body.atmosphereScaleHeight;
            bodyRadius = body.Radius;
            gravParameter = body.gravParameter;
            this.dragMassExcludingUsedParachutes = _dragMassExcludingUsedParachutes;

            this.parachutes = _parachuteList;
            this.parachuteSemiDeployMultiplier = _parachuteSemiDeployMultiplier;
            this.multiplierHasError = _multiplierHasError;

            this.mass = _mass;
            bodyAngularVelocity = body.angularVelocity;
            this.descentSpeedPolicy = _descentSpeedPolicy;
            decelRadius = bodyRadius + _decelEndAltitudeASL; 
            aerobrakedRadius = bodyRadius + body.RealMaxAtmosphereAltitude();
            mainBody = body;
            this.maxThrustAccel = _maxThrustAccel;
            this.probableLandingSiteASL = _probableLandingSiteASL;
            this.probableLandingSiteRadius = _probableLandingSiteASL + bodyRadius;

            referenceFrame = ReferenceFrame.CreateAtCurrentTime(_initialOrbit.referenceBody);

            orbitReenters = OrbitReenters(_initialOrbit);

            if (orbitReenters)
            {
                startUT = _UT;
                t = startUT;
                AdvanceToFreefallEnd(_initialOrbit);
            }

            maxDragGees = 0;
            deltaVExpended = 0;
            trajectory = new List<AbsoluteVector>();
        }

        public Result RunSimulation()
        {
            Result result = new Result();
            try
            {
                // First put all the problem parameters into the result, to aid debugging.
                result.input_initialOrbit = this.input_initialOrbit;
                result.input_UT = this.input_UT;
                result.input_dragMassExcludingUsedParachutes = this.input_dragMassExcludingUsedParachutes;
                result.input_parachuteList = this.input_parachuteList;
                result.input_mass = this.input_mass;
                result.input_descentSpeedPolicy = this.input_descentSpeedPolicy;
                result.input_decelEndAltitudeASL = this.input_decelEndAltitudeASL;
                result.input_maxThrustAccel = this.input_maxThrustAccel;
                result.input_parachuteSemiDeployMultiplier = this.input_parachuteSemiDeployMultiplier;
                result.input_probableLandingSiteASL = this.input_probableLandingSiteASL;
                result.input_multiplierHasError = this.input_multiplierHasError;
                result.input_dt = this.input_dt;
 

                if (!orbitReenters) 
                {
                    result.outcome = Outcome.NO_REENTRY; return result; 
                }

                result.startPosition = referenceFrame.ToAbsolute(x, t);

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

                result.id = System.Guid.NewGuid();
                result.body = mainBody;
                result.referenceFrame = referenceFrame;
                result.endUT = t;
                result.timeToComplete = t - input_UT;
                result.maxDragGees = maxDragGees;
                result.deltaVExpended = deltaVExpended;
                result.endPosition = referenceFrame.ToAbsolute(x, t);
                result.endVelocity = referenceFrame.ToAbsolute(v, t);
                result.trajectory = trajectory;
                result.parachuteMultiplier = this.parachuteSemiDeployMultiplier;
                result.multiplierHasError = this.multiplierHasError;
                result.maxdt = this.max_dt;

                // TODO I suspect we are not allowed to do this from this thread. commenting it out for testing.
                // result.endASL = Math.Max(0, result.body.TerrainAltitude(result.endPosition.latitude, result.endPosition.longitude));
            }
            catch (Exception ex)
            {
                Debug.LogError("Exception thrown during Rentry Simulation" + ex.StackTrace + ex.Message);
                result.outcome = Outcome.ERROR;
            }
            return result;
        }

        bool OrbitReenters(Orbit initialOrbit)
        {
            return (initialOrbit.PeR < decelRadius || initialOrbit.PeR < aerobrakedRadius);
        }

        bool Landed()
        {
            return x.magnitude < this.probableLandingSiteRadius;
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
            if (pos.magnitude < decelRadius) return true; // TODO should this be landed, not decelerated?
            if (descentSpeedPolicy != null && surfaceVelocity.magnitude > descentSpeedPolicy.MaxAllowedSpeed(pos, surfaceVelocity)) return true;
            return false;
        }

        // Function to call Simulate and rollback on all of the parachutes to see if any of them are expected to open in the current iteration.
        bool WillChutesDeploy(double altAGL, double altASL, double probableLandingSiteASL, double pressure, double t, double parachuteSemiDeployMultiplier)
        {
            foreach (SimulatedParachute p in parachutes)
            {
                if (p.SimulateAndRollback(altAGL, altASL, probableLandingSiteASL, pressure, t, this.parachuteSemiDeployMultiplier))
                {
                    return true;
                }
            }
            return false;
        }

        // one time step of RK4: There is logic to reduce the dt and repeat if a larger dt results in very large accelerations. Also the parachute opening logic is called from in order to allow the dt to be reduced BEFORE deploying parachutes to give more precision over the point of deployment.  
        void RK4Step()
        {
            bool repeatWithSmallerStep = false;
            bool parachutesDeploying = false;

            Vector3d dx;
            Vector3d dv;

            maxDragGees = Math.Max(maxDragGees, DragAccel(x, v).magnitude / 9.81f);

            do
            {
                repeatWithSmallerStep = false;

                // Perform the RK4 calculation
                {
                    Vector3d dv1 = dt * TotalAccel(x, v);
                    Vector3d dx1 = dt * v;

                    Vector3d dv2 = dt * TotalAccel(x + 0.5 * dx1, v + 0.5 * dv1);
                    Vector3d dx2 = dt * (v + 0.5 * dv1);

                    Vector3d dv3 = dt * TotalAccel(x + 0.5 * dx2, v + 0.5 * dv2);
                    Vector3d dx3 = dt * (v + 0.5 * dv2);

                    Vector3d dv4 = dt * TotalAccel(x + dx3, v + dv3);
                    Vector3d dx4 = dt * (v + dv3);

                    dx = (dx1 + 2 * dx2 + 2 * dx3 + dx4) / 6.0;
                    dv = (dv1 + 2 * dv2 + 2 * dv3 + dv4) / 6.0;
                }

                // If the change in velocity is more than half the current velocity, then we need to try again with a smaller delta-t
                // or if dt is already small enough then continue anyway.
                if (v.magnitude < dv.magnitude * 2 && dt >= min_dt * 2)
                {
                    dt = dt / 2;
                    repeatWithSmallerStep = true;
                }
                else
                {
                    // Consider opening the parachutes. If we do open them, and the dt is not as small as it could me, make it smaller and repeat,
                    Vector3 xForChuteSim = x + dx;
                    double altASL = xForChuteSim.magnitude - bodyRadius;
                    double altAGL = altASL - probableLandingSiteASL;
                    double pressure = Pressure(xForChuteSim);
                    bool willChutesOpen = WillChutesDeploy(altAGL, altASL, probableLandingSiteASL, pressure, t, this.parachuteSemiDeployMultiplier);

                    // If parachutes are about to open and we are running with a dt larger than the physics frame then drop dt to the physics frame rate and start again
                    if (willChutesOpen && dt > min_dt) // TODO test to see if we are better off just setting a minimum dt of the physics rame rate.
                    {
                        dt = Math.Max(dt/2,min_dt);
                        repeatWithSmallerStep = true;
                    }
                    else
                    {
                        foreach (SimulatedParachute p in parachutes)
                        {
                            p.Simulate(altAGL, altASL, probableLandingSiteASL, pressure, t, this.parachuteSemiDeployMultiplier);
                        }
                    }
                }
            }
            while (repeatWithSmallerStep);

            x += dx;
            v += dv;
            t += dt;
           
            // decide what the dt needs to be for the next iteration 
            // Is there a parachute in the process of opening? If so then we follow special rules - fix the dt at the physics frame rate. This is because the rate for deployment depends on the frame rate for stock parachutes.
            parachutesDeploying = this.ParachutesDeploying();

            // If parachutes are part way through deploying then we need to use the physics frame rate for the next step of the simulation
            if (parachutesDeploying)
            {
                dt = Time.fixedDeltaTime; // TODO There is a potential problem here. If the physics frame rate is so large that is causes too large a change in velocity, then we could get stuck in an infinte loop.
            }
            // If dt has been reduced, try increasing it, but only by one step. (but not if there is a parachute being deployed)
            else if(dt < max_dt)
            {
                dt = Math.Min(dt * 2, max_dt);
            }
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

            double realDragMass = this.dragMassExcludingUsedParachutes;

            foreach (SimulatedParachute p in parachutes)
            {
                realDragMass += p.AddedDragMass();
            }

            double realDragCoefficient = realDragMass / this.mass;

            return -0.5 * FlightGlobals.DragMultiplier * realDragCoefficient * AirDensity(pos) * airVel.sqrMagnitude * airVel.normalized;
        }

        void OpenParachutes(Vector3d pos)
        {
            double altASL = pos.magnitude - bodyRadius;
            double altAGL = altASL - probableLandingSiteASL;
            double pressure = Pressure(pos);
            if (bodyHasAtmosphere)
            {
                foreach (SimulatedParachute p in parachutes)
                {
                    p.Simulate(altAGL, altASL, probableLandingSiteASL , pressure, t, this.parachuteSemiDeployMultiplier);
                }
            }
        }

        bool ParachutesDeploying()
        {

            foreach (SimulatedParachute p in parachutes)
            {
                if (p.deploying)
                {
                    return true;
                }
            }
            return false;

        }

        double Pressure(Vector3d pos)
        {
            double ratio = Math.Exp(-(pos.magnitude - bodyRadius) / scaleHeight);
            if (ratio < 1e-6) return 0; //this is not a fudge, this is faithfully simulating the game, which
            //pretends the pressure is zero if it is less than 1e-6 times the sea level pressure
            return seaLevelAtmospheres * ratio;
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

        public enum Outcome { LANDED, AEROBRAKED, TIMED_OUT, NO_REENTRY, ERROR }

        public class Result
        {
            public double maxdt;

            public double timeToComplete;

            public System.Guid id; // give each set of results a new GUID so we can check to see if the result has changed.
            public Outcome outcome;

            public CelestialBody body;
            public ReferenceFrame referenceFrame;
            public double endUT;

            public AbsoluteVector startPosition;
            public AbsoluteVector endPosition;
            public AbsoluteVector endVelocity;
            public double endASL;
            public List<AbsoluteVector> trajectory;

            public double maxDragGees;
            public double deltaVExpended;

            public bool multiplierHasError;
            public double parachuteMultiplier;

            // Provide all the input paramaters to the simulation in the result to aid debugging
            public Orbit input_initialOrbit;
            public double input_UT;
            public double input_dragMassExcludingUsedParachutes;
            public List<SimulatedParachute> input_parachuteList;
            public double input_mass;
            public IDescentSpeedPolicy input_descentSpeedPolicy;
            public double input_decelEndAltitudeASL;
            public double input_maxThrustAccel;
            public double input_parachuteSemiDeployMultiplier;
            public double input_probableLandingSiteASL;
            public bool input_multiplierHasError;
            public double input_dt;

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

            // A method to calculate the overshoot (length of the component of the vector from the target to the actual landing position that is parallel to the vector from the start position to the target site.)
            public double GetOvershoot(EditableAngle targetLatitude,EditableAngle targetLongitude)
            {
                // Get the start, end and target positions as a set of 3d vectors that we can work with
                Vector3 end = this.body.GetRelSurfacePosition(endPosition.latitude, endPosition.longitude, 0);
                Vector3 target = this.body.GetRelSurfacePosition(targetLatitude, targetLongitude, 0); 
                Vector3 start = this.body.GetRelSurfacePosition(startPosition.latitude, startPosition.longitude, 0);

                // First we need to get two vectors that are non orthogonal to each other and to the vector from the start to the target. TODO can we simplify this code by using Vector3.Exclude?
                Vector3 start2Target = target - start;
                Vector3 orthog1 = Vector3.Cross(start2Target,Vector3.up);
                // check for the spaecial case where start2target is parrallel to up. If it is then the result will be zero,and we need to try again
                if(orthog1 == Vector3.up)
                {
                    orthog1 = Vector3.Cross(start2Target,Vector3.forward);
                }
                Vector3 orthog2 = Vector3.Cross(start2Target,orthog1);

                // Now that we have those two orthogonal vectors, we can project any vector onto the two planes defined by them to give us the vector component that is parallel to start2Target.
                Vector3 target2end = end - target;

                Vector3 overshoot = target2end.ProjectIntoPlane(orthog1).ProjectIntoPlane(orthog2);

                // finally how long is it? We know it is parrallel to start2target, so if we add it to start2target, and then get the difference of the lengths, that should give us a positive or negative
                double overshootLength = (start2Target+overshoot).magnitude - start2Target.magnitude;

                return overshootLength;
            }

            public override string ToString()
            {
                string resultText = "Simulation result\n{";

                resultText += "Inputs:\n{";
                if (null != input_initialOrbit) { resultText += "\n input_initialOrbit: " + input_initialOrbit.ToString(); }
                resultText += "\n input_UT: " + input_UT;
                resultText += "\n input_dragMassExcludingUsedParachutes: " + input_dragMassExcludingUsedParachutes;
                resultText += "\n input_mass: " + input_mass;
                if (null != input_descentSpeedPolicy) { resultText += "\n input_descentSpeedPolicy: " + input_descentSpeedPolicy.ToString(); }
                resultText += "\n input_decelEndAltitudeASL: " + input_decelEndAltitudeASL;
                resultText += "\n input_maxThrustAccel: " + input_maxThrustAccel;
                resultText += "\n input_parachuteSemiDeployMultiplier: " + input_parachuteSemiDeployMultiplier;
                resultText += "\n input_probableLandingSiteASL: " + input_probableLandingSiteASL;
                resultText += "\n input_multiplierHasError: " + input_multiplierHasError;
                resultText += "\n input_dt: " + input_dt;
                resultText += "\n}";

                if (null != id) { resultText += "\nid: " + id; }
                
                resultText += "\noutcome: " + outcome; 
                resultText += "\nmaxdt: " + maxdt;
                resultText += "\ntimeToComplete: " + timeToComplete;
                resultText += "\nendUT: " + endUT;
                if (null != referenceFrame) { resultText += "\nstartPosition: " + referenceFrame.WorldPositionAtCurrentTime(startPosition); }
                if (null != referenceFrame) { resultText += "\nendPosition: " + referenceFrame.WorldPositionAtCurrentTime(endPosition); }
                resultText += "\nendASL: " + endASL;
                resultText += "\nendVelocity: " + endVelocity.longitude + "," + endVelocity.latitude + "," + endVelocity.radius; 
                resultText += "\nmaxDragGees: " + maxDragGees;
                resultText += "\ndeltaVExpended: " + deltaVExpended;
                resultText += "\nmultiplierHasError: " + multiplierHasError;
                resultText += "\nparachuteMultiplier: " + parachuteMultiplier;
                resultText += "\n}";

                return (resultText);

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

    public class SimulatedParachute
    {
        ModuleParachute p;

        ModuleParachute.deploymentStates state;

        double openningTime;

        // Store some data about when and how the parachute opened during the simulation which will be useful for debugging
        double activatedASL = 0;
        double activatedAGL = 0;
        double semiDeployASL = 0;
        double semiDeployAGL = 0;
        double fullDeployASL = 0;
        double fullDeployAGL = 0;
        double targetASLAtSemiDeploy = 0;
        double targetASLAtFullDeploy = 0;
        double parachuteDrag = 0;
        double targetDrag = 0;
        public bool deploying = false;

        public SimulatedParachute(ModuleParachute p)
        {
            this.p = p;
            this.state = p.deploymentState;

            // Work out when the chute was put into its current state based on the current drag as compared to the stoed, semi deployed and fully deployed drag

            double timeSinceDeployment = 0;
            this.targetDrag = p.targetDrag;
            this.parachuteDrag = p.parachuteDrag;

            switch (p.deploymentState)
            {
                case ModuleParachute.deploymentStates.SEMIDEPLOYED:
                    // If the parachute is semi deployed calculate when it was semideployed by comparing the actual drag with the stowed drag and the semideployed drag.
                    timeSinceDeployment = (p.parachuteDrag - p.stowedDrag) / (p.semiDeployedDrag - p.stowedDrag) * p.semiDeploymentSpeed; // TODO there is an error in this, because the (semi)deployment does not increase the drag in a linear way. However this will only cause a problem for simulations run during the deployment and in unlikely to cause an error in the landing location.
                    break;

                case ModuleParachute.deploymentStates.DEPLOYED:
                    // If the parachute is deployed calculate when it was deployed by comparing the actual drag with the semideployed drag and the deployed drag.
                    timeSinceDeployment = (p.parachuteDrag - p.semiDeployedDrag) / (p.fullyDeployedDrag - p.semiDeployedDrag) * p.deploymentSpeed; // TODO there is an error in this, because the (semi)deployment does not increase the drag in a linear way. However this will only cause a problem for simulations run during the deployment and in unlikely to cause an error in the landing location.
                    break;

                case ModuleParachute.deploymentStates.STOWED:
                case ModuleParachute.deploymentStates.ACTIVE:
                    // If the parachute is stowed then for some reason p.parachuteDrag does not reflect the stowed drag. set this up by hand. 
                    this.parachuteDrag = this.targetDrag = p.stowedDrag;
                    timeSinceDeployment = 10000000;
                    break;

                default:
                    // otherwise set the time since deployment to be a very large number to indcate that it has been in that state for a long time (although we do not know how long!
                    timeSinceDeployment = 10000000;
                    break;
            }
           
            this.openningTime = -timeSinceDeployment;

            // Debug.Log("Parachute " + p.name + " parachuteDrag:" + p.parachuteDrag + " targetDrag:" + p.targetDrag + " stowedDrag:" + p.stowedDrag + " semiDeployedDrag:" + p.semiDeployedDrag + " fullyDeployedDrag:" + p.fullyDeployedDrag + " part.maximum_drag:" + p.part.maximum_drag + " part.minimum_drag:" + p.part.minimum_drag + " semiDeploymentSpeed:" + p.semiDeploymentSpeed + " deploymentSpeed:" + p.deploymentSpeed + " deploymentState:" + p.deploymentState + " timeSinceDeployment:" + timeSinceDeployment);       
        }

        public string GetDebugOutput()
        {
            string DebugOutput = "Parachute" + p.name + " activatedASL: " + activatedASL + " activatedAGL: " + activatedAGL + " semiDeployASL: " + semiDeployASL + " semiDeployAGL: " + semiDeployAGL + " fullDeployASL: " + fullDeployASL + " fullDeployAGL: " + fullDeployAGL + " targetASLAtSemiDeploy: " + targetASLAtSemiDeploy + " targetASLAtFullDeploy: " + targetASLAtFullDeploy + " this.targetDrag:" + this.targetDrag + " this.parachuteDrag:" + this.parachuteDrag + " p.part.minimum_drag:" + p.part.minimum_drag + " p.part.maximum_drag:" + p.part.maximum_drag; ; 
            return DebugOutput;
        }

        public double AddedDragMass()
        {
            double totalDrag;

            totalDrag = p.part.minimum_drag + this.parachuteDrag;

            return p.part.mass * totalDrag;
        }

        // Consider activating, semi deploying or deploying a parachute, but do not actually make any changes. returns true if the state has changed
        public bool SimulateAndRollback(double altATGL, double altASL, double endASL, double pressure, double time, double semiDeployMultiplier)
        {
            bool stateChanged = false;
            switch (state)
            {
                case ModuleParachute.deploymentStates.STOWED:
                    if (altATGL < semiDeployMultiplier * p.deployAltitude)
                    {
                        stateChanged = true;
                    }
                    break;
                case ModuleParachute.deploymentStates.ACTIVE:
                    if (pressure >= p.minAirPressureToOpen)
                    {
                        stateChanged = true;
                    }
                    break;
                case ModuleParachute.deploymentStates.SEMIDEPLOYED:
                    if (altATGL < p.deployAltitude)
                    {
                        stateChanged = true;
                    }
                    break;
            }
            return (stateChanged);
        }


        // Consider activating, semi deploying or deploying a parachute. returns true if the state has changed
        public bool Simulate(double altATGL, double altASL, double endASL ,double pressure, double time, double semiDeployMultiplier)
        {
            bool stateChanged = false;
            switch (state)
            {
                case ModuleParachute.deploymentStates.STOWED:
                    if (altATGL < semiDeployMultiplier * p.deployAltitude)
                    {
                        state = ModuleParachute.deploymentStates.ACTIVE;
                        stateChanged = true;
                        activatedAGL = altATGL;
                        activatedASL = altASL;
                        // Immediately check to see if the parachute should be semi deployed, rather than waiting for another iteration.
                        if (pressure >= p.minAirPressureToOpen)
                        {
                            state = ModuleParachute.deploymentStates.SEMIDEPLOYED;
                            openningTime = time;
                            semiDeployAGL = altATGL;
                            semiDeployASL = altASL;
                            targetASLAtSemiDeploy = endASL;
                            this.targetDrag = p.semiDeployedDrag;
                        }
                    }
                    break;
                case ModuleParachute.deploymentStates.ACTIVE:
                    if (pressure >= p.minAirPressureToOpen)
                    {
                        state = ModuleParachute.deploymentStates.SEMIDEPLOYED;
                        stateChanged = true;
                        openningTime = time;
                        semiDeployAGL = altATGL;
                        semiDeployASL = altASL;
                        targetASLAtSemiDeploy = endASL;
                        this.targetDrag = p.semiDeployedDrag;
                    }

                    break;
                case ModuleParachute.deploymentStates.SEMIDEPLOYED:
                    if (altATGL < p.deployAltitude)
                    {
                        state = ModuleParachute.deploymentStates.DEPLOYED;
                        stateChanged = true;
                        openningTime = time;
                        fullDeployAGL = altATGL;
                        fullDeployASL = altASL;
                        targetASLAtFullDeploy = endASL;
                        this.targetDrag = p.fullyDeployedDrag;
                    }
                    break;
            }

            // Now that we have potentially changed states calculate the current drag or the parachute in whatever state (or transition to a state) that it is in.
            float normalizedTime;

            // Depending on the state that we are in consider if we are part way through a deployment.
            if (state == ModuleParachute.deploymentStates.SEMIDEPLOYED)
            {
                normalizedTime = (float)Math.Min((time - openningTime) / p.semiDeploymentSpeed, 1);
            }
            else if (state == ModuleParachute.deploymentStates.DEPLOYED)
            {
                normalizedTime = (float)Math.Min((time - openningTime) / p.deploymentSpeed, 1);
            }
            else
            {
                normalizedTime = 1;
            }

            // Are we deploying in any way? We know if we are deploying or not if normalized time is less than 1
            if (normalizedTime < 1)
            {
                this.deploying = true;
            }
            else
            {
                this.deploying = false;
            }

            // If we are deploying or semi deploying then use Lerp to replicate the way the game increases the drag as we deploy.
            if (state == ModuleParachute.deploymentStates.DEPLOYED || state == ModuleParachute.deploymentStates.SEMIDEPLOYED)
            {
                this.parachuteDrag = Mathf.Lerp((float)this.parachuteDrag, (float)this.targetDrag, normalizedTime);
            }

            return (stateChanged);
        }
    }
}

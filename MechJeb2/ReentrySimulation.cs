using System;
using System.Collections.Generic;
using Smooth.Dispose;
using Smooth.Pools;
using UnityEngine;

namespace MuMech
{
    public class ReentrySimulation
    {
        // Input values
        Orbit input_initialOrbit;
        double input_UT;
        //double input_mass;
        IDescentSpeedPolicy input_descentSpeedPolicy;
        double input_decelEndAltitudeASL;
        double input_maxThrustAccel;
        double input_parachuteSemiDeployMultiplier;
        double input_probableLandingSiteASL;
        bool input_multiplierHasError;
        double input_dt;

        //parameters of the problem:
        Orbit initialOrbit = new Orbit();
        bool bodyHasAtmosphere;
        //double seaLevelAtmospheres;
        //double scaleHeight;
        double bodyRadius;
        double gravParameter;

        //double mass;
        SimulatedVessel vessel;
        Vector3d bodyAngularVelocity;
        IDescentSpeedPolicy descentSpeedPolicy;
        double decelRadius;
        double aerobrakedRadius;
        double startUT;
        CelestialBody mainBody; //we're not actually allowed to call any functions on this from our separate thread, we just keep it as reference
        double maxThrustAccel;
        double probableLandingSiteASL; // This is the height of the ground at the point we think we will land. It is infact calculated by getting the height of the previous prediction. It is used to decide when the parachutes will be deployed.
        double probableLandingSiteRadius; // This is the height of the ground from the centre of the body at the point we think we will land. It is infact calculated by getting the height of the previous prediction. It is used to decide when the parachutes will be deployed, and when we have landed.
        QuaternionD attitude;

        bool orbitReenters;

        ReferenceFrame referenceFrame = new ReferenceFrame();

        double dt;
        double max_dt;
        //private const double min_dt = 0.01; //in seconds
        public double min_dt; //in seconds
        double maxSimulatedTime; //in seconds
        // Maximum numbers of orbits we want to predict
        private double maxOrbits;

        private bool noSKiptoFreefall;

        double parachuteSemiDeployMultiplier;
        bool multiplierHasError;

        //Dynamical variables
        Vector3d x; //coordinate system used is centered on main body
        Vector3d startX; //start position
        Vector3d v;
        double t;

        private Vector3 lastRecordedDrag;

        //Accumulated results
        double maxDragGees;
        double deltaVExpended;
        List<AbsoluteVector> trajectory;

        private int steps;

        public static int activeStep;
        public static double activeDt;

        // FloatCurve (Unity Animation curve) are not thread safe so we need a local copy of the curves for the thread
        private SimCurves simCurves;

        // debug
        public static bool once = true;


        private static readonly Pool<ReentrySimulation> pool = new Pool<ReentrySimulation>(Create, Reset);

        public static int PoolSize
        {
            get { return pool.Size; }
        }

        private static ReentrySimulation Create()
        {
            return new ReentrySimulation();
        }

        public void Release()
        {
            pool.Release(this);
        }

        private static void Reset(ReentrySimulation obj)
        {
        }

        public static ReentrySimulation Borrow(Orbit _initialOrbit, double _UT, SimulatedVessel _vessel, SimCurves _simcurves, IDescentSpeedPolicy _descentSpeedPolicy, double _decelEndAltitudeASL, double _maxThrustAccel, double _parachuteSemiDeployMultiplier, double _probableLandingSiteASL, bool _multiplierHasError, double _dt, double _min_dt, double _maxOrbits, bool _noSKiptoFreefall)
        {
            ReentrySimulation sim = pool.Borrow();
            sim.Init(_initialOrbit, _UT, _vessel, _simcurves, _descentSpeedPolicy, _decelEndAltitudeASL, _maxThrustAccel, _parachuteSemiDeployMultiplier, _probableLandingSiteASL, _multiplierHasError, _dt, _min_dt, _maxOrbits, _noSKiptoFreefall);
            return sim;
        }


        public void Init(Orbit _initialOrbit, double _UT, SimulatedVessel _vessel, SimCurves _simcurves, IDescentSpeedPolicy _descentSpeedPolicy, double _decelEndAltitudeASL, double _maxThrustAccel, double _parachuteSemiDeployMultiplier, double _probableLandingSiteASL, bool _multiplierHasError, double _dt, double _min_dt, double _maxOrbits, bool _noSKiptoFreefall)
        {
            // Store all the input values as they were given
            input_initialOrbit = _initialOrbit;
            input_UT = _UT;

            vessel = _vessel;
            input_descentSpeedPolicy = _descentSpeedPolicy;
            input_decelEndAltitudeASL = _decelEndAltitudeASL;
            input_maxThrustAccel = _maxThrustAccel;
            input_parachuteSemiDeployMultiplier = _parachuteSemiDeployMultiplier;
            input_probableLandingSiteASL = _probableLandingSiteASL;
            input_multiplierHasError = _multiplierHasError;
            input_dt = _dt;
            // the vessel attitude relative to the surface vel. Fixed for now
            attitude = Quaternion.Euler(180,0,0);

            min_dt = _min_dt;
            max_dt = _dt;
            dt = max_dt;
            steps = 0;

            maxOrbits = _maxOrbits;

            noSKiptoFreefall = _noSKiptoFreefall;

            // Get a copy of the original orbit, to be more thread safe
            //initialOrbit = new Orbit();
            initialOrbit.UpdateFromOrbitAtUT(_initialOrbit, _UT, _initialOrbit.referenceBody);

            CelestialBody body = _initialOrbit.referenceBody;
            bodyHasAtmosphere = body.atmosphere;
            bodyRadius = body.Radius;
            gravParameter = body.gravParameter;

            this.parachuteSemiDeployMultiplier = _parachuteSemiDeployMultiplier;
            this.multiplierHasError = _multiplierHasError;

            bodyAngularVelocity = body.angularVelocity;
            this.descentSpeedPolicy = _descentSpeedPolicy;
            decelRadius = bodyRadius + _decelEndAltitudeASL;
            aerobrakedRadius = bodyRadius + body.RealMaxAtmosphereAltitude();
            mainBody = body;
            this.maxThrustAccel = _maxThrustAccel;
            this.probableLandingSiteASL = _probableLandingSiteASL;
            this.probableLandingSiteRadius = _probableLandingSiteASL + bodyRadius;
            referenceFrame.UpdateAtCurrentTime(_initialOrbit.referenceBody);
            orbitReenters = OrbitReenters(_initialOrbit);

            startX = initialOrbit.SwappedRelativePositionAtUT(startUT);
            // This calls some Unity function so it should be done outside the thread
            if (orbitReenters)
            {
                startUT = _UT;
                t = startUT;
                AdvanceToFreefallEnd(initialOrbit);
            }

            maxDragGees = 0;
            deltaVExpended = 0;
            trajectory = ListPool<AbsoluteVector>.Instance.Borrow();

            simCurves = _simcurves;

            once = true;
        }

        private Result result;
        private static ulong resultId = 0;

        public Result RunSimulation()
        {
            result = Result.Borrow();
            try
            {
                // First put all the problem parameters into the result, to aid debugging.
                result.input_initialOrbit = this.input_initialOrbit;
                result.input_UT = this.input_UT;
                result.input_descentSpeedPolicy = this.input_descentSpeedPolicy;
                result.input_decelEndAltitudeASL = this.input_decelEndAltitudeASL;
                result.input_maxThrustAccel = this.input_maxThrustAccel;
                result.input_parachuteSemiDeployMultiplier = this.input_parachuteSemiDeployMultiplier;
                result.input_probableLandingSiteASL = this.input_probableLandingSiteASL;
                result.input_multiplierHasError = this.input_multiplierHasError;
                result.input_dt = this.input_dt;

                //MechJebCore.print("Sim Start");

                if (!orbitReenters)
                {
                    result.outcome = Outcome.NO_REENTRY;
                    return result;
                }

                result.startPosition = referenceFrame.ToAbsolute(x, t);

                // Simulate a maximum of maxOrbits periods of a circular orbit at the entry altitude
                maxSimulatedTime = maxOrbits * 2.0 * Math.PI * Math.Sqrt(Math.Pow(Math.Abs(x.magnitude), 3.0) / gravParameter);

                RecordTrajectory();

                double maxT = t + maxSimulatedTime;
                while (true)
                {
                    if (Landed())
                    {
                        result.outcome = Outcome.LANDED;
                        break;
                    }
                    if (!result.aeroBrake && Aerobraked())
                    {
                        result.aeroBrake = true;
                        result.aeroBrakeUT = t;
                        result.aeroBrakePosition = referenceFrame.ToAbsolute(x, t);
                        result.aeroBrakeVelocity = referenceFrame.ToAbsolute(v, t);
                        //break;
                    }
                    if (t > maxT || Escaping() || steps > 50000)
                    {
                        result.outcome = result.aeroBrake ? Outcome.AEROBRAKED : Outcome.TIMED_OUT;
                        break;
                    }
                    //RK4Step();
                    BS34Step();
                    LimitSpeed();
                    RecordTrajectory();
                }

                //MechJebCore.print("Sim ready " + result.outcome + " " + (t - startUT).ToString("F2"));
                result.id = resultId++;
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
                result.steps = steps;
            }
            catch (Exception ex)
            {
                //Debug.LogError("Exception thrown during Rentry Simulation : " + ex.GetType() + ":" + ex.Message + "\n"+ ex.StackTrace);
                result.exception = ex;
                result.outcome = Outcome.ERROR;
            }
            finally
            {
                if (trajectory != result.trajectory)
                    ListPool<AbsoluteVector>.Instance.Release(trajectory);
                vessel.Release();
                simCurves.Release();
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

        bool Escaping()
        {
            double escapeVel = Math.Sqrt(2 * gravParameter / x.magnitude);
            return bodyHasAtmosphere && (v.magnitude > escapeVel) && (Vector3d.Dot(x, v) > 0);
        }


        void AdvanceToFreefallEnd(Orbit initialOrbit)
        {

            t = FindFreefallEndTime(initialOrbit);

            x = initialOrbit.SwappedRelativePositionAtUT(t);
            v = initialOrbit.SwappedOrbitalVelocityAtUT(t);

            if (Double.IsNaN(v.magnitude))
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
            if (noSKiptoFreefall || FreefallEnded(initialOrbit, t))
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
            if (pos.magnitude < decelRadius) return true;
            if (descentSpeedPolicy != null && surfaceVelocity.magnitude > descentSpeedPolicy.MaxAllowedSpeed(pos, surfaceVelocity)) return true;
            return false;
        }

        // one time step of RK4: There is logic to reduce the dt and repeat if a larger dt results in very large accelerations. Also the parachute opening logic is called from in order to allow the dt to be reduced BEFORE deploying parachutes to give more precision over the point of deployment.
        void RK4Step()
        {
            bool repeatWithSmallerStep = false;
            bool parachutesDeploying = false;

            Vector3d dx;
            Vector3d dv;

            //Log(x, v);

            do
            {
                steps++;
                activeStep = steps;
                activeDt = dt;
                repeatWithSmallerStep = false;

                // Perform the RK4 calculation
                {
                    Vector3d dv1 = dt * TotalAccel(x, v, true);
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
                    double vForChuteSim = (v + dv).magnitude;
                    double altASL = xForChuteSim.magnitude - bodyRadius;
                    double altAGL = altASL - probableLandingSiteASL;
                    double pressure = Pressure(xForChuteSim);

                    bool willChutesOpen = vessel.WillChutesDeploy(altAGL, altASL, probableLandingSiteASL, pressure, vForChuteSim, t, parachuteSemiDeployMultiplier);
                    maxDragGees = Math.Max(maxDragGees, lastRecordedDrag.magnitude / 9.81f);

                    // If parachutes are about to open and we are running with a dt larger than the physics frame then drop dt to the physics frame rate and start again
                    if (willChutesOpen && dt > min_dt) // TODO test to see if we are better off just setting a minimum dt of the physics frame rate.
                    {
                        dt = Math.Max(dt/2,min_dt);
                        repeatWithSmallerStep = true;
                    }
                    else
                    {
                        parachutesDeploying = vessel.Simulate(altAGL, altASL, probableLandingSiteASL, pressure, vForChuteSim, t, parachuteSemiDeployMultiplier);
                    }
                }
            }
            while (repeatWithSmallerStep);

            x += dx;
            v += dv;
            t += dt;

            // decide what the dt needs to be for the next iteration
            // Is there a parachute in the process of opening? If so then we follow special rules - fix the dt at the physics frame rate. This is because the rate for deployment depends on the frame rate for stock parachutes.
            // If parachutes are part way through deploying then we need to use the physics frame rate for the next step of the simulation
            if (parachutesDeploying)
            {
                dt = min_dt; // TODO There is a potential problem here. If the physics frame rate is so large that is causes too large a change in velocity, then we could get stuck in an infinte loop.
            }
            // If dt has been reduced, try increasing it, but only by one step. (but not if there is a parachute being deployed)
            else if(dt < max_dt)
            {
                dt = Math.Min(dt * 2, max_dt);
            }
        }

        // Bogacki–Shampine method
        void BS34Step()
        {
            bool repeatWithSmallerStep = false;
            bool parachutesDeploying = false;

            Vector3d dx;
            Vector3d dv;

            //Log(x, v);
            const double tol = 0.01;
            const double d9 = 1d / 9d;
            const double d24 = 1d / 24d;

            do
            {
                steps++;
                activeStep = steps;
                activeDt = dt;
                repeatWithSmallerStep = false;
                Vector3d errorv;
                // Perform the calculation
                {
                    Vector3d dv1 = dt * TotalAccel(x, v, true);
                    Vector3d dx1 = dt * v;

                    Vector3d dv2 = dt * TotalAccel(x + 0.5 * dx1, v + 0.5 * dv1);
                    Vector3d dx2 = dt * (v + 0.5 * dv1);

                    Vector3d dv3 = dt * TotalAccel(x + 0.75 * dx2, v + 0.75 * dv2);
                    Vector3d dx3 = dt * (v + 0.75 * dv2);

                    Vector3d dv4 = dt * TotalAccel(x + 2 * d9 * dx1 + 3 * d9 * dx2 + 4 * d9 * dx3, v + 2 * d9 * dv1 + 3 * d9 * dv2 + 4 * d9 * dv3);
                    //Vector3d dx4 = dt * (v + 2d / 9 * dv1 + 1d / 3 * dv2 + 4d / 9 * dv3);

                    dx = (2 * dx1 + 3 * dx2 + 4 * dx3 ) * d9;
                    dv = (2 * dv1 + 3 * dv2 + 4 * dv3 ) * d9;

                    //Vector3d zx = (7 * dx1 + 6 * dx2 + 8 * dx3 + 3 * dx4) * d24;
                    Vector3d zv = (7 * dv1 + 6 * dv2 + 8 * dv3 + 3 * dv4) * d24;
                    errorv = zv - dv;
                }


                // Consider opening the parachutes. If we do open them, and the dt is not as small as it could be, make it smaller and repeat,
                Vector3 xForChuteSim = x + dx;
                double altASL = xForChuteSim.magnitude - bodyRadius;
                double altAGL = altASL - probableLandingSiteASL;
                double pressure = Pressure(xForChuteSim);

                Vector3d airVel = SurfaceVelocity(xForChuteSim, v + dv);
                double airDensity = AirDensity(xForChuteSim, altASL);
                double speedOfSound = mainBody.GetSpeedOfSound(Pressure(xForChuteSim), airDensity);
                double velocity = airVel.magnitude;
                double mach = Math.Min(velocity / speedOfSound, 50f);
                double shockTemp = ShockTemperature(velocity, mach);

                // check if the parachute will open in the next frame or if that frame will be underground
                bool willChutesOpen = altASL < probableLandingSiteASL || vessel.WillChutesDeploy(altAGL, altASL, probableLandingSiteASL, pressure, shockTemp, t, parachuteSemiDeployMultiplier);

                double next_dt;
                var errorMagnitude = Math.Max(errorv.magnitude, 10e-6);
                if (!willChutesOpen)
                {
                    next_dt = dt * 0.9 * Math.Pow(tol / errorMagnitude, 1 / 3d);
                }
                else
                {
                    // The chute will open at the next dt so we want to make it shorter so they don't
                    // until we have dt = min_dt and we can safely open them
                    // The same system avoids skiping the ground with a large dt
                    next_dt = dt * 0.5;
                }

                // I don't see how it could append but it has shown up...
                if (double.IsNaN(next_dt))
                    next_dt = min_dt;

                next_dt = Math.Max(next_dt, min_dt);
                next_dt = Math.Min(next_dt, 10);

                var sqrStartDist = (x - startX).sqrMagnitude;
                // The first 1km is always high precision
                if (sqrStartDist < 1000 * 1000)
                    next_dt = Math.Min(next_dt, 0.02);
                else if (sqrStartDist < 5000 * 5000)
                    next_dt = Math.Min(next_dt, 0.5);
                else if (sqrStartDist < 10000 * 10000)
                    next_dt = Math.Min(next_dt, 1);

                if ((errorMagnitude > tol || willChutesOpen) && dt > min_dt)
                {
                    dt = next_dt;
                    repeatWithSmallerStep = true;
                }
                else
                {
                    maxDragGees = Math.Max(maxDragGees, lastRecordedDrag.magnitude / 9.81f);
                    parachutesDeploying = vessel.Simulate(altAGL, altASL, probableLandingSiteASL, pressure, shockTemp, t, parachuteSemiDeployMultiplier);
                    x += dx;
                    v += dv;
                    t += dt;
                    if (parachutesDeploying)
                    {
                        dt = min_dt;
                    }
                    else
                    {
                        // If a parachute is opening we need to lower dt to make sure we capture the opening sequence properly
                        dt = next_dt;
                    }
                }
            }
            while (repeatWithSmallerStep);
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

        Vector3d TotalAccel(Vector3d pos, Vector3d vel, bool record=false)
        {
            Vector3d airVel = SurfaceVelocity(pos, vel);
            double altitude = pos.magnitude - bodyRadius;

            double airDensity = AirDensity(pos, altitude);
            double pressure = Pressure(pos);
            double speedOfSound = mainBody.GetSpeedOfSound(pressure, airDensity);
            float mach = Mathf.Min((float)(airVel.magnitude / speedOfSound), 50f);

            float dynamicPressurekPa = (float)(0.0005 * airDensity * airVel.sqrMagnitude);

            double pseudoReynolds = airDensity * airVel.magnitude;
            double pseudoReDragMult = (double)simCurves.DragCurvePseudoReynolds.Evaluate((float)pseudoReynolds);

            if (once)
            {
                result.prediction.firstDrag = DragForce(pos, vel, dynamicPressurekPa, mach).magnitude / 9.81;
                result.prediction.firstLift = LiftForce(pos, vel, dynamicPressurekPa, mach).magnitude / 9.81;
                result.prediction.mach = mach;
                result.prediction.speedOfSound = speedOfSound;
                result.prediction.dynamicPressurekPa = dynamicPressurekPa;
            }
            Vector3d dragAccel = DragForce(pos, vel, dynamicPressurekPa, mach) * pseudoReDragMult / vessel.totalMass;

            if (record)
                lastRecordedDrag = dragAccel;

            Vector3d gravAccel = GravAccel(pos);
            Vector3d liftAccel = LiftForce(pos, vel, dynamicPressurekPa, mach) / vessel.totalMass;

            Vector3d totalAccel = gravAccel + dragAccel + liftAccel;

            if (once)
                once = false;

            return totalAccel;
        }

        Vector3d GravAccel(Vector3d pos)
        {
            return -(gravParameter / pos.sqrMagnitude) * pos.normalized;
        }

        Vector3d DragForce(Vector3d pos, Vector3d vel, float dynamicPressurekPa, float mach)
        {
            if (!bodyHasAtmosphere) return Vector3d.zero;

            Vector3d airVel = SurfaceVelocity(pos, vel);

            Vector3d localVel = attitude * Vector3d.up * airVel.magnitude;

            // TODO : check if it is forward, back, up or down...
            // Lift works with a velocity in SHIP coordinate and return a vector in ship coordinate
            Vector3d shipDrag = vessel.Drag(localVel, dynamicPressurekPa, mach);

            //if (once)
            //{
            //    string msg = "DragForce";
            //    msg += "\n " + mach.ToString("F3") + " " + vessel.parts[0].oPart.machNumber.ToString("F3");
            //    msg += "\n " + Pressure(pos).ToString("F7") + " " + vessel.parts[0].oPart.vessel.staticPressurekPa.ToString("F7");
            //    msg += "\n " + AirDensity(pos).ToString("F7") + " " + vessel.parts[0].oPart.atmDensity.ToString("F7");
            //    msg += "\n " + vessel.parts[0].oPart.vessel.latitude.ToString("F3");
            //
            //
            //    double altitude = pos.magnitude - bodyRadius;
            //    double temp = FlightGlobals.getExternalTemperature(altitude, mainBody)
            //                  + mainBody.atmosphereTemperatureSunMultCurve.Evaluate((float)altitude)
            //                  * (mainBody.latitudeTemperatureBiasCurve.Evaluate(0)
            //                     + mainBody.latitudeTemperatureSunMultCurve.Evaluate(0) * (1 + 1) * 0.5 // fix that 0 into latitude
            //                     + mainBody.axialTemperatureSunMultCurve.Evaluate(1));
            //
            //    msg += "\n " + temp.ToString("F3") + " " + vessel.parts[0].oPart.vessel.atmosphericTemperature.ToString("F3");
            //
            //
            //    //this.atmosphericTemperature =
            //    //    this.currentMainBody.GetTemperature(this.altitude) +
            //    //    (double)this.currentMainBody.atmosphereTemperatureSunMultCurve.Evaluate((float)this.altitude) *
            //    //        ((double)this.currentMainBody.latitudeTemperatureBiasCurve.Evaluate((float)(num1 * 57.2957801818848)) +
            //    //         (double)this.currentMainBody.latitudeTemperatureSunMultCurve.Evaluate((float)(num1 * 57.2957801818848)) * (1 + this.sunDot) * 0.5
            //    //          + (double)this.currentMainBody.axialTemperatureSunMultCurve.Evaluate(this.sunAxialDot));
            //    //
            //
            //    MechJebCore.print(msg);
            //}

            //MechJebCore.print("DragForce " + airVel.magnitude.ToString("F4") + " " + mach.ToString("F4") + " " + shipDrag.magnitude.ToString("F4"));
            //MechJebCore.print("DragForce " + AirDensity(pos).ToString("F4") + " " + airVel.sqrMagnitude.ToString("F4") + " " + dynamicPressurekPa.ToString("F4"));

            return - airVel.normalized * shipDrag.magnitude;
        }

        private Vector3d LiftForce(Vector3d pos, Vector3d vel, float dynamicPressurekPa, float mach)
        {
            if (!bodyHasAtmosphere) return Vector3d.zero;

            Vector3d airVel = SurfaceVelocity(pos, vel);

            Vector3d localVel = attitude * Vector3d.up * airVel.magnitude;

            Vector3d localLift = vessel.Lift(localVel, dynamicPressurekPa, mach);

            QuaternionD vesselToWorld = Quaternion.FromToRotation(localVel, airVel); // QuaternionD.FromToRotation is not working in Unity 4.3

            return vesselToWorld * localLift;
        }

        double Pressure(Vector3d pos)
        {
            double altitude = pos.magnitude - bodyRadius;
            return StaticPressure(altitude);
        }

        Vector3d SurfaceVelocity(Vector3d pos, Vector3d vel)
        {
            //if we're low enough, calculate the airspeed properly:
            return vel - Vector3d.Cross(bodyAngularVelocity, pos);
        }

        double AirDensity(Vector3d pos, double altitude)
        {
            double pressure = StaticPressure(altitude);
            double temp = GetTemperature(pos, altitude);

            return FlightGlobals.getAtmDensity(pressure, temp, mainBody);
        }

        double StaticPressure(double altitude)
        {
            if (!mainBody.atmosphere)
            {
                return 0;
            }

            if (altitude >= mainBody.atmosphereDepth)
            {
                return 0;
            }
            if (!mainBody.atmosphereUsePressureCurve)
            {
                return mainBody.atmospherePressureSeaLevel * Math.Pow(1 - mainBody.atmosphereTemperatureLapseRate * altitude / mainBody.atmosphereTemperatureSeaLevel, mainBody.atmosphereGasMassLapseRate);
            }
            if (!mainBody.atmospherePressureCurveIsNormalized)
            {
                return simCurves.AtmospherePressureCurve.Evaluate((float)altitude);
            }
            return Mathf.Lerp(0f, (float)mainBody.atmospherePressureSeaLevel, simCurves.AtmospherePressureCurve.Evaluate((float)(altitude / mainBody.atmosphereDepth)));
        }

        // Lifted from the Trajectories mod.
        private double GetTemperature(Vector3d position, double altitude)
        {
            if (!mainBody.atmosphere)
                return PhysicsGlobals.SpaceTemperature;

            if (altitude > mainBody.atmosphereDepth)
                return PhysicsGlobals.SpaceTemperature;

            Vector3 up = position.normalized;
            float polarAngle = Mathf.Acos(Vector3.Dot(mainBody.bodyTransform.up, up));
            if (polarAngle > Mathf.PI * 0.5f)
            {
                polarAngle = Mathf.PI - polarAngle;
            }
            float time = (Mathf.PI * 0.5f - polarAngle) * Mathf.Rad2Deg;

            Vector3 sunVector = (FlightGlobals.Bodies[0].position - position + mainBody.position).normalized;
            float sunAxialDot = Vector3.Dot(sunVector, mainBody.bodyTransform.up);
            float bodyPolarAngle = Mathf.Acos(Vector3.Dot(mainBody.bodyTransform.up, up));
            float sunPolarAngle = Mathf.Acos(sunAxialDot);
            float sunBodyMaxDot = (1.0f + Mathf.Cos(sunPolarAngle - bodyPolarAngle)) * 0.5f;
            float sunBodyMinDot = (1.0f + Mathf.Cos(sunPolarAngle + bodyPolarAngle)) * 0.5f;
            float sunDotCorrected = (1.0f + Vector3.Dot(sunVector, Quaternion.AngleAxis(45f * Mathf.Sign((float)mainBody.rotationPeriod), mainBody.bodyTransform.up) * up)) * 0.5f;
            float sunDotNormalized = (sunDotCorrected - sunBodyMinDot) / (sunBodyMaxDot - sunBodyMinDot);
            double atmosphereTemperatureOffset = (double)simCurves.LatitudeTemperatureBiasCurve.Evaluate(time) + (double)simCurves.LatitudeTemperatureSunMultCurve.Evaluate(time) * sunDotNormalized + (double)simCurves.AxialTemperatureSunMultCurve.Evaluate(sunAxialDot);

            double temperature;
            if (!mainBody.atmosphereUseTemperatureCurve)
            {
                temperature = mainBody.atmosphereTemperatureSeaLevel - mainBody.atmosphereTemperatureLapseRate * altitude;
            }
            else
            {
                temperature = !mainBody.atmosphereTemperatureCurveIsNormalized ?
                    simCurves.AtmosphereTemperatureCurve.Evaluate((float)altitude) :
                    UtilMath.Lerp(simCurves.SpaceTemperature, mainBody.atmosphereTemperatureSeaLevel, simCurves.AtmosphereTemperatureCurve.Evaluate((float)(altitude / mainBody.atmosphereDepth)));
            }

            temperature += (double)simCurves.AtmosphereTemperatureSunMultCurve.Evaluate((float)altitude) * atmosphereTemperatureOffset;

            return temperature;
        }

        private double ShockTemperature(double velocity, double mach)
        {
            double newtonianTemperatureFactor = velocity * PhysicsGlobals.NewtonianTemperatureFactor;
            double convectiveMachLerp = Math.Pow(UtilMath.Clamp01((mach - PhysicsGlobals.NewtonianMachTempLerpStartMach) / (PhysicsGlobals.NewtonianMachTempLerpEndMach - PhysicsGlobals.NewtonianMachTempLerpStartMach)), PhysicsGlobals.NewtonianMachTempLerpExponent);
            if (convectiveMachLerp > 0)
            {
                double machTemperatureScalar = PhysicsGlobals.MachTemperatureScalar * Math.Pow(velocity, PhysicsGlobals.MachTemperatureVelocityExponent);
                newtonianTemperatureFactor = UtilMath.LerpUnclamped(newtonianTemperatureFactor, machTemperatureScalar, convectiveMachLerp);
            }
            return newtonianTemperatureFactor * HighLogic.CurrentGame.Parameters.Difficulty.ReentryHeatScale * mainBody.shockTemperatureMultiplier;
        }

        private double nextLog = 0;

        public void Log(Vector3d pos, Vector3d vel)
        {
            if (t > nextLog)
            {
                nextLog = t + 10;

                double altitude = pos.magnitude - bodyRadius;
                Vector3d airVel = SurfaceVelocity(pos, vel);

                double atmosphericTemperature = GetTemperature(pos, altitude);

                double speedOfSound = mainBody.GetSpeedOfSound(Pressure(pos), AirDensity(pos, altitude));
                float mach = Mathf.Min((float)(airVel.magnitude / speedOfSound), 50f);

                float dynamicPressurekPa = (float)(0.0005 * AirDensity(pos, altitude) * airVel.sqrMagnitude);



                result.debugLog += "\n "
                    + (t - startUT).ToString("F2").PadLeft(8)
                    + " Alt:" + altitude.ToString("F0").PadLeft(6)
                    + " Vel:" + vel.magnitude.ToString("F2").PadLeft(8)
                    + " AirVel:" + airVel.magnitude.ToString("F2").PadLeft(8)
                    + " SoS:" + speedOfSound.ToString("F2").PadLeft(6)
                    + " mach:" + mach.ToString("F2").PadLeft(6)
                    + " dynP:" + dynamicPressurekPa.ToString("F5").PadLeft(9)
                    + " Temp:" + atmosphericTemperature.ToString("F2").PadLeft(8)
                    + " Lat:" + referenceFrame.Latitude(pos).ToString("F2").PadLeft(6);
            }
        }

        // FloatCurve (Unity Animation curve) are not thread safe so we need a local copy of the curves for the thread
        public class SimCurves
        {
            private static readonly Pool<SimCurves> pool = new Pool<SimCurves>(Create, Reset);

            private SimCurves()
            {
            }

            public static int PoolSize
            {
                get { return pool.Size; }
            }

            private static SimCurves Create()
            {
                return new SimCurves();
            }

            public virtual void Release()
            {
                pool.Release(this);
            }

            private static void Reset(SimCurves obj)
            {
            }

            public static SimCurves Borrow(CelestialBody newBody)
            {
                SimCurves curve = pool.Borrow();
                curve.Setup(newBody);
                return curve;
            }

            private void Setup(CelestialBody newBody)
            {
                // No point in copying those again if we already have them loaded
                if (!loaded)
                {
                    dragCurveCd         = new FloatCurve(PhysicsGlobals.DragCurveCd.Curve.keys);
                    dragCurveCdPower    = new FloatCurve(PhysicsGlobals.DragCurveCdPower.Curve.keys);
                    dragCurveMultiplier = new FloatCurve(PhysicsGlobals.DragCurveMultiplier.Curve.keys);

                    dragCurveSurface    = new FloatCurve(PhysicsGlobals.SurfaceCurves.dragCurveSurface .Curve.keys);
                    dragCurveTail       = new FloatCurve(PhysicsGlobals.SurfaceCurves.dragCurveTail.Curve.keys);
                    dragCurveTip        = new FloatCurve(PhysicsGlobals.SurfaceCurves.dragCurveTip.Curve.keys);

                    liftCurve           = new FloatCurve(PhysicsGlobals.BodyLiftCurve.liftCurve.Curve.keys);
                    liftMachCurve       = new FloatCurve(PhysicsGlobals.BodyLiftCurve.liftMachCurve.Curve.keys);
                    dragCurve           = new FloatCurve(PhysicsGlobals.BodyLiftCurve.dragCurve.Curve.keys);
                    dragMachCurve       = new FloatCurve(PhysicsGlobals.BodyLiftCurve.dragMachCurve.Curve.keys);

                    dragCurvePseudoReynolds = new FloatCurve(PhysicsGlobals.DragCurvePseudoReynolds.Curve.keys);

                    spaceTemperature = PhysicsGlobals.SpaceTemperature;
                    loaded = true;
                }

                if (newBody != body)
                {
                    body = newBody;
                    atmospherePressureCurve = new FloatCurve(newBody.atmospherePressureCurve.Curve.keys);
                    atmosphereTemperatureSunMultCurve = new FloatCurve(newBody.atmosphereTemperatureSunMultCurve.Curve.keys);
                    latitudeTemperatureBiasCurve = new FloatCurve(newBody.latitudeTemperatureBiasCurve.Curve.keys);
                    latitudeTemperatureSunMultCurve = new FloatCurve(newBody.latitudeTemperatureSunMultCurve.Curve.keys);
                    atmosphereTemperatureCurve = new FloatCurve(newBody.atmosphereTemperatureCurve.Curve.keys);
                    axialTemperatureSunMultCurve = new FloatCurve(newBody.axialTemperatureSunMultCurve.Curve.keys);
                }
            }

            private bool loaded = false;

            private CelestialBody body;

            private FloatCurve liftCurve;
            private FloatCurve liftMachCurve;
            private FloatCurve dragCurve;
            private FloatCurve dragMachCurve;

            private FloatCurve dragCurveTail;
            private FloatCurve dragCurveSurface;
            private FloatCurve dragCurveTip;

            private FloatCurve dragCurveCd;
            private FloatCurve dragCurveCdPower;
            private FloatCurve dragCurveMultiplier;

            private FloatCurve atmospherePressureCurve;

            private FloatCurve atmosphereTemperatureSunMultCurve;

            private FloatCurve latitudeTemperatureBiasCurve;

            private FloatCurve latitudeTemperatureSunMultCurve;

            private FloatCurve axialTemperatureSunMultCurve;

            private FloatCurve atmosphereTemperatureCurve;

            private FloatCurve dragCurvePseudoReynolds;

            private double spaceTemperature;

            public FloatCurve LiftCurve
            {
                get { return liftCurve; }
            }

            public FloatCurve LiftMachCurve
            {
                get { return liftMachCurve; }
            }

            public FloatCurve DragCurve
            {
                get { return dragCurve; }
            }

            public FloatCurve DragMachCurve
            {
                get { return dragMachCurve; }
            }

            public FloatCurve DragCurveTail
            {
                get { return dragCurveTail; }
            }

            public FloatCurve DragCurveSurface
            {
                get { return dragCurveSurface; }
            }

            public FloatCurve DragCurveTip
            {
                get { return dragCurveTip; }
            }

            public FloatCurve DragCurveCd
            {
                get { return dragCurveCd; }
            }

            public FloatCurve DragCurveCdPower
            {
                get { return dragCurveCdPower; }
            }

            public FloatCurve DragCurveMultiplier
            {
                get { return dragCurveMultiplier; }
            }

            public FloatCurve AtmospherePressureCurve
            {
                get { return atmospherePressureCurve; }
            }

            public FloatCurve AtmosphereTemperatureSunMultCurve
            {
                get { return atmosphereTemperatureSunMultCurve; }
            }

            public FloatCurve LatitudeTemperatureBiasCurve
            {
                get { return latitudeTemperatureBiasCurve; }
            }

            public FloatCurve LatitudeTemperatureSunMultCurve
            {
                get { return latitudeTemperatureSunMultCurve; }
            }

            public FloatCurve AxialTemperatureSunMultCurve
            {
                get { return axialTemperatureSunMultCurve; }
            }

            public FloatCurve AtmosphereTemperatureCurve
            {
                get { return atmosphereTemperatureCurve; }
            }

            public FloatCurve DragCurvePseudoReynolds
            {
                get { return dragCurvePseudoReynolds; }
            }

            public double SpaceTemperature
            {
                get { return spaceTemperature; }
            }
        }


        public enum Outcome { LANDED, AEROBRAKED, TIMED_OUT, NO_REENTRY, ERROR }

        public struct prediction
        {
            // debuging
            public double firstDrag;
            public double firstLift;
            public double speedOfSound;
            public double mach;
            public double dynamicPressurekPa;
        }

        public class Result
        {
            public double maxdt;
            public int steps;

            public double timeToComplete;

            public ulong id; // give each set of results a new id so we can check to see if the result has changed.
            public Outcome outcome;
            public Exception exception;

            public CelestialBody body;
            public ReferenceFrame referenceFrame;
            public double endUT;

            public AbsoluteVector startPosition;
            public AbsoluteVector endPosition;
            public AbsoluteVector endVelocity;

            public bool aeroBrake;
            public double aeroBrakeUT;
            public AbsoluteVector aeroBrakePosition;
            public AbsoluteVector aeroBrakeVelocity;

            public double endASL;
            public List<AbsoluteVector> trajectory;

            public double maxDragGees;
            public double deltaVExpended;

            public bool multiplierHasError;
            public double parachuteMultiplier;

            // Provide all the input paramaters to the simulation in the result to aid debugging
            public Orbit input_initialOrbit;
            public double input_UT;
            //public double input_dragMassExcludingUsedParachutes;
            public List<SimulatedParachute> input_parachuteList;
            public double input_mass;
            public IDescentSpeedPolicy input_descentSpeedPolicy;
            public double input_decelEndAltitudeASL;
            public double input_maxThrustAccel;
            public double input_parachuteSemiDeployMultiplier;
            public double input_probableLandingSiteASL;
            public bool input_multiplierHasError;
            public double input_dt;

            public string debugLog;

            private static readonly Pool<Result> pool = new Pool<Result>(Create, Reset);

            public static int PoolSize
            {
                get { return pool.Size; }
            }

            private static Result Create()
            {
                return new Result();
            }

            public void Release()
            {
                if (trajectory != null)
                    ListPool<AbsoluteVector>.Instance.Release(trajectory);
                exception = null;
                pool.Release(this);
            }

            private static void Reset(Result obj)
            {
                obj.aeroBrake = false;
            }

            public static Result Borrow()
            {
                Result result = pool.Borrow();
                return result;
            }

            // debuging
            public prediction prediction;

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

            public Vector3d WorldAeroBrakePosition()
            {
                return referenceFrame.WorldPositionAtCurrentTime(aeroBrakePosition);
            }

            public Vector3d WorldAeroBrakeVelocity()
            {
                return referenceFrame.WorldVelocityAtCurrentTime(aeroBrakeVelocity);
            }

            public Orbit AeroBrakeOrbit()
            {
                return MuUtils.OrbitFromStateVectors(WorldAeroBrakePosition(), WorldAeroBrakeVelocity(), body, endUT);
            }

            public Disposable<List<Vector3d>> WorldTrajectory(double timeStep, bool world=true)
            {
                Disposable<List<Vector3d>> ret = ListPool<Vector3d>.Instance.BorrowDisposable();

                if (trajectory.Count == 0) return ret;

                if (world)
                    ret.value.Add(referenceFrame.WorldPositionAtCurrentTime(trajectory[0]));
                else
                    ret.value.Add(referenceFrame.BodyPositionAtCurrentTime(trajectory[0]));
                double lastTime = trajectory[0].UT;
                for (int i = 0; i < trajectory.Count; i++)
                {
                    AbsoluteVector absolute = trajectory[i];
                    if (absolute.UT > lastTime + timeStep)
                    {
                        if (world)
                            ret.value.Add(referenceFrame.WorldPositionAtCurrentTime(absolute));
                        else
                            ret.value.Add(referenceFrame.BodyPositionAtCurrentTime(absolute));
                        lastTime = absolute.UT;
                    }
                }
                return ret;
            }

            // A method to calculate the overshoot (length of the component of the vector from the target to the actual landing position that is parallel to the vector from the start position to the target site.)
            public double GetOvershoot(EditableAngle targetLatitude,EditableAngle targetLongitude)
            {
                // Get the start, end and target positions as a set of 3d vectors that we can work with
                Vector3d end = this.body.GetWorldSurfacePosition(endPosition.latitude, endPosition.longitude, 0) - body.position;
                Vector3d target = this.body.GetWorldSurfacePosition(targetLatitude, targetLongitude, 0) - body.position;
                Vector3d start = this.body.GetWorldSurfacePosition(startPosition.latitude, startPosition.longitude, 0) - body.position;

                // First we need to get two vectors that are non orthogonal to each other and to the vector from the start to the target. TODO can we simplify this code by using Vector3d.Exclude?
                Vector3d start2Target = target - start;
                Vector3d orthog1 = Vector3d.Cross(start2Target,Vector3d.up);
                // check for the spaecial case where start2target is parrallel to up. If it is then the result will be zero,and we need to try again
                if(orthog1 == Vector3d.up)
                {
                    orthog1 = Vector3d.Cross(start2Target,Vector3d.forward);
                }
                Vector3d orthog2 = Vector3d.Cross(start2Target,orthog1);

                // Now that we have those two orthogonal vectors, we can project any vector onto the two planes defined by them to give us the vector component that is parallel to start2Target.
                Vector3d target2end = end - target;

                Vector3d overshoot = target2end.ProjectOnPlane(orthog1).ProjectOnPlane(orthog2);

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
                //resultText += "\n input_dragMassExcludingUsedParachutes: " + input_dragMassExcludingUsedParachutes;
                resultText += "\n input_mass: " + input_mass;
                if (null != input_descentSpeedPolicy) { resultText += "\n input_descentSpeedPolicy: " + input_descentSpeedPolicy.ToString(); }
                resultText += "\n input_decelEndAltitudeASL: " + input_decelEndAltitudeASL;
                resultText += "\n input_maxThrustAccel: " + input_maxThrustAccel;
                resultText += "\n input_parachuteSemiDeployMultiplier: " + input_parachuteSemiDeployMultiplier;
                resultText += "\n input_probableLandingSiteASL: " + input_probableLandingSiteASL;
                resultText += "\n input_multiplierHasError: " + input_multiplierHasError;
                resultText += "\n input_dt: " + input_dt;
                resultText += "\n}";
                resultText += "\nid: " + id;
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

        public void UpdateAtCurrentTime(CelestialBody body)
        {
            lat0lon0AtStart = body.GetSurfaceNVector(0, 0);
            lat0lon90AtStart = body.GetSurfaceNVector(0, 90);
            lat90AtStart = body.GetSurfaceNVector(90, 0);
            epoch = Planetarium.GetUniversalTime();
            referenceBody = body;
        }

        //Vector3d must be either a position RELATIVE to referenceBody, or a velocity
        public AbsoluteVector ToAbsolute(Vector3d vector3d, double UT)
        {
            AbsoluteVector absolute = new AbsoluteVector();

            absolute.latitude = Latitude(vector3d);

            double longitude = UtilMath.Rad2Deg * Math.Atan2(Vector3d.Dot(vector3d.normalized, lat0lon90AtStart), Vector3d.Dot(vector3d.normalized, lat0lon0AtStart));
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


        public Vector3d BodyPositionAtCurrentTime(AbsoluteVector absolute)
        {
            return referenceBody.position + absolute.radius * referenceBody.GetSurfaceNVector(absolute.latitude, absolute.longitude);
        }


        //Interprets a given AbsoluteVector as a velocity, and returns the corresponding Vector3d velocity
        //in world coordinates.
        public Vector3d WorldVelocityAtCurrentTime(AbsoluteVector absolute)
        {
            double now = Planetarium.GetUniversalTime();
            double unrotatedLongitude = MuUtils.ClampDegrees360(absolute.longitude - 360 * (now - absolute.UT) / referenceBody.rotationPeriod);
            return absolute.radius * referenceBody.GetSurfaceNVector(absolute.latitude, unrotatedLongitude);
        }

        public double Latitude(Vector3d vector3d)
        {
            return UtilMath.Rad2Deg * Math.Asin(Vector3d.Dot(vector3d.normalized, lat90AtStart));
        }

    }


}

using System;
using System.Collections.Generic;
using Smooth.Dispose;
using Smooth.Pools;
using UnityEngine;

namespace MuMech
{
    public partial class ReentrySimulation
    {
        // Input values
        private Orbit _inputInitialOrbit;

        private double _inputUT;

        //double input_mass;
        private IDescentSpeedPolicy _inputDescentSpeedPolicy;
        private double              _inputDecelEndAltitudeASL;
        private double              _inputMaxThrustAccel;
        private double              _inputParachuteSemiDeployMultiplier;
        private double              _inputProbableLandingSiteASL;
        private bool                _inputMultiplierHasError;
        private double              _inputDT;

        //parameters of the problem:
        private readonly Orbit _initialOrbit = new Orbit();

        private bool _bodyHasAtmosphere;

        //double seaLevelAtmospheres;
        //double scaleHeight;
        private double _bodyRadius;
        private double _gravParameter;

        //double mass;
        private SimulatedVessel     _vessel;
        private Vector3d            _bodyAngularVelocity;
        private IDescentSpeedPolicy _descentSpeedPolicy;
        private double              _decelRadius;
        private double              _aerobrakedRadius;
        private double              _startUT;

        // we're not actually allowed to call any functions on this from our separate thread, we just keep it as reference
        // FIXME: that's a lie, its used all over the place.
        private CelestialBody _mainBody;

        private double _maxThrustAccel;

        // This is the height of the ground at the point we think we will land. It is infact calculated by getting the height of the previous prediction. It is used to decide when the parachutes will be deployed.
        private double _probableLandingSiteASL;

        // This is the height of the ground from the centre of the body at the point we think we will land. It is infact calculated by getting the height of the previous prediction. It is used to decide when the parachutes will be deployed, and when we have landed.
        private double _probableLandingSiteRadius;

        private QuaternionD _attitude;

        private          bool           _orbitReenters;
        private readonly ReferenceFrame _referenceFrame = new ReferenceFrame();

        private double _dt;

        private double _maxDT;

        //private const double min_dt = 0.01; //in seconds
        public double MinDT; //in seconds

        private double _maxSimulatedTime; //in seconds

        // Maximum numbers of orbits we want to predict
        private double _maxOrbits;

        private bool _noSKiptoFreefall;

        private double _parachuteSemiDeployMultiplier;
        private bool   _multiplierHasError;

        //Dynamical variables
        private Vector3d _x;      //coordinate system used is centered on main body
        private Vector3d _startX; //start position
        private Vector3d _v;
        private double   _t;

        private Vector3 _lastRecordedDrag;

        //Accumulated results
        private double               _maxDragGees;
        private double               _deltaVExpended;
        private List<AbsoluteVector> _trajectory;

        private int _steps;

        public static int    ActiveStep;
        public static double ActiveDt;

        // FloatCurve (Unity Animation curve) are not thread safe so we need a local copy of the curves for the thread
        private SimCurves _simCurves;

        // debug
        private static bool _once = true;

        private static readonly Pool<ReentrySimulation> _pool = new Pool<ReentrySimulation>(Create, Reset);

        public static int PoolSize => _pool.Size;

        private static ReentrySimulation Create()
        {
            return new ReentrySimulation();
        }

        public void Release()
        {
            _pool.Release(this);
        }

        private static void Reset(ReentrySimulation obj)
        {
        }

        public static ReentrySimulation Borrow(Orbit initialOrbit, double ut, SimulatedVessel vessel, SimCurves simcurves,
            IDescentSpeedPolicy descentSpeedPolicy, double decelEndAltitudeASL, double maxThrustAccel, double parachuteSemiDeployMultiplier,
            double probableLandingSiteASL, bool multiplierHasError, double dt, double minDT, double maxOrbits, bool noSKiptoFreefall)
        {
            ReentrySimulation sim = _pool.Borrow();
            sim.Init(initialOrbit, ut, vessel, simcurves, descentSpeedPolicy, decelEndAltitudeASL, maxThrustAccel,
                parachuteSemiDeployMultiplier, probableLandingSiteASL, multiplierHasError, dt, minDT, maxOrbits, noSKiptoFreefall);
            return sim;
        }

        private void Init(Orbit initialOrbit, double ut, SimulatedVessel vessel, SimCurves simcurves, IDescentSpeedPolicy descentSpeedPolicy,
            double decelEndAltitudeASL, double maxThrustAccel, double parachuteSemiDeployMultiplier, double probableLandingSiteASL,
            bool multiplierHasError, double dt, double minDT, double maxOrbits, bool noSKiptoFreefall)
        {
            // Store all the input values as they were given
            _inputInitialOrbit = initialOrbit;
            _inputUT           = ut;

            _vessel                             = vessel;
            _inputDescentSpeedPolicy            = descentSpeedPolicy;
            _inputDecelEndAltitudeASL           = decelEndAltitudeASL;
            _inputMaxThrustAccel                = maxThrustAccel;
            _inputParachuteSemiDeployMultiplier = parachuteSemiDeployMultiplier;
            _inputProbableLandingSiteASL        = probableLandingSiteASL;
            _inputMultiplierHasError            = multiplierHasError;
            _inputDT                            = dt;
            // the vessel attitude relative to the surface vel. Fixed for now
            _attitude = Quaternion.Euler(180, 0, 0);

            MinDT  = minDT;
            _maxDT = dt;
            _dt    = _maxDT;
            _steps = 0;

            _maxOrbits = maxOrbits;

            _noSKiptoFreefall = noSKiptoFreefall;

            // Get a copy of the original orbit, to be more thread safe
            //initialOrbit = new Orbit();
            _initialOrbit.UpdateFromOrbitAtUT(initialOrbit, ut, initialOrbit.referenceBody);

            CelestialBody body = initialOrbit.referenceBody;
            _bodyHasAtmosphere = body.atmosphere;
            _bodyRadius        = body.Radius;
            _gravParameter     = body.gravParameter;

            _parachuteSemiDeployMultiplier = parachuteSemiDeployMultiplier;
            _multiplierHasError            = multiplierHasError;

            _bodyAngularVelocity       = body.angularVelocity;
            _descentSpeedPolicy        = descentSpeedPolicy;
            _decelRadius               = _bodyRadius + decelEndAltitudeASL;
            _aerobrakedRadius          = _bodyRadius + body.RealMaxAtmosphereAltitude();
            _mainBody                  = body;
            _maxThrustAccel            = maxThrustAccel;
            _probableLandingSiteASL    = probableLandingSiteASL;
            _probableLandingSiteRadius = probableLandingSiteASL + _bodyRadius;
            _referenceFrame.UpdateAtCurrentTime(initialOrbit.referenceBody);
            _orbitReenters = OrbitReenters(initialOrbit);

            _startX = _initialOrbit.WorldBCIPositionAtUT(_startUT);
            // This calls some Unity function so it should be done outside the thread
            if (_orbitReenters)
            {
                _startUT = ut;
                _t       = _startUT;
                AdvanceToFreefallEnd(_initialOrbit);
            }

            _maxDragGees    = 0;
            _deltaVExpended = 0;
            _trajectory     = ListPool<AbsoluteVector>.Instance.Borrow();

            _simCurves = simcurves;

            _once = true;
        }

        private        Result _result;
        private static ulong  _resultId;

        public Result RunSimulation()
        {
            _result = Result.Borrow();
            try
            {
                // First put all the problem parameters into the result, to aid debugging.
                _result.InputInitialOrbit                  = _inputInitialOrbit;
                _result.InputUT                            = _inputUT;
                _result.InputDescentSpeedPolicy            = _inputDescentSpeedPolicy;
                _result.InputDecelEndAltitudeASL           = _inputDecelEndAltitudeASL;
                _result.InputMaxThrustAccel                = _inputMaxThrustAccel;
                _result.InputParachuteSemiDeployMultiplier = _inputParachuteSemiDeployMultiplier;
                _result.InputProbableLandingSiteASL        = _inputProbableLandingSiteASL;
                _result.InputMultiplierHasError            = _inputMultiplierHasError;
                _result.InputDT                            = _inputDT;

                //MechJebCore.print("Sim Start");

                if (!_orbitReenters)
                {
                    _result.Outcome = Outcome.NO_REENTRY;
                    return _result;
                }

                _result.StartPosition = _referenceFrame.ToAbsolute(_x, _t);

                // Simulate a maximum of maxOrbits periods of a circular orbit at the entry altitude
                _maxSimulatedTime = _maxOrbits * 2.0 * Math.PI * Math.Sqrt(Math.Pow(Math.Abs(_x.magnitude), 3.0) / _gravParameter);

                RecordTrajectory();

                double maxT = _t + _maxSimulatedTime;
                while (true)
                {
                    if (Landed())
                    {
                        _result.Outcome = Outcome.LANDED;
                        break;
                    }

                    if (!_result.AeroBrake && Aerobraked())
                    {
                        _result.AeroBrake         = true;
                        _result.AeroBrakeUT       = _t;
                        _result.AeroBrakePosition = _referenceFrame.ToAbsolute(_x, _t);
                        _result.AeroBrakeVelocity = _referenceFrame.ToAbsolute(_v, _t);
                        //break;
                    }

                    if (_t > maxT || Escaping() || _steps > 50000)
                    {
                        _result.Outcome = _result.AeroBrake ? Outcome.AEROBRAKED : Outcome.TIMED_OUT;
                        break;
                    }

                    //RK4Step();
                    BS34Step();
                    LimitSpeed();
                    RecordTrajectory();
                }

                //MechJebCore.print("Sim ready " + result.outcome + " " + (t - startUT).ToString("F2"));
                _result.ID                  = _resultId++;
                _result.Body                = _mainBody;
                _result.ReferenceFrame      = _referenceFrame;
                _result.EndUT               = _t;
                _result.TimeToComplete      = _t - _inputUT;
                _result.MaxDragGees         = _maxDragGees;
                _result.DeltaVExpended      = _deltaVExpended;
                _result.EndPosition         = _referenceFrame.ToAbsolute(_x, _t);
                _result.EndVelocity         = _referenceFrame.ToAbsolute(_v, _t);
                _result.Trajectory          = _trajectory;
                _result.ParachuteMultiplier = _parachuteSemiDeployMultiplier;
                _result.MultiplierHasError  = _multiplierHasError;
                _result.Maxdt               = _maxDT;
                _result.Steps               = _steps;
            }
            catch (Exception ex)
            {
                //Debug.LogError("Exception thrown during Reentry Simulation : " + ex.GetType() + ":" + ex.Message + "\n"+ ex.StackTrace);
                _result.Exception = ex;
                _result.Outcome   = Outcome.ERROR;
            }
            finally
            {
                if (_trajectory != _result.Trajectory)
                    ListPool<AbsoluteVector>.Instance.Release(_trajectory);
                _vessel.Release();
                _simCurves.Release();
            }

            return _result;
        }

        private bool OrbitReenters(Orbit initialOrbit)
        {
            return initialOrbit.PeR < _decelRadius || initialOrbit.PeR < _aerobrakedRadius;
        }

        private bool Landed()
        {
            return _x.magnitude < _probableLandingSiteRadius;
        }

        private bool Aerobraked()
        {
            return _bodyHasAtmosphere && _x.magnitude > _aerobrakedRadius && Vector3d.Dot(_x, _v) > 0;
        }

        private bool Escaping()
        {
            double escapeVel = Math.Sqrt(2 * _gravParameter / _x.magnitude);
            return _bodyHasAtmosphere && _v.magnitude > escapeVel && Vector3d.Dot(_x, _v) > 0;
        }

        private void AdvanceToFreefallEnd(Orbit initialOrbit)
        {
            _t = FindFreefallEndTime(initialOrbit);

            _x = initialOrbit.WorldBCIPositionAtUT(_t);
            _v = initialOrbit.WorldOrbitalVelocityAtUT(_t);

            if (double.IsNaN(_v.magnitude))
            {
                //For eccentricities close to 1, the Orbit class functions are unreliable and
                //v may come out as NaN. If that happens we estimate v from conservation
                //of energy and the assumption that v is vertical (since ecc. is approximately 1).

                //0.5 * v^2 - GM / r = E   =>    v = sqrt(2 * (E + GM / r))
                double mu = initialOrbit.referenceBody.gravParameter;
                double e = -mu / (2 * initialOrbit.semiMajorAxis);
                _v = Math.Sqrt(Math.Abs(2 * (e + mu / _x.magnitude))) * _x.normalized;
                if (initialOrbit.MeanAnomalyAtUT(_t) > Math.PI) _v *= -1;
            }
        }

        //This is a convenience function used by the reentry simulation. It does a binary search for the first UT
        //in the interval (lowerUT, upperUT) for which condition(UT, relative position, orbital velocity) is true
        private double FindFreefallEndTime(Orbit initialOrbit)
        {
            if (_noSKiptoFreefall || FreefallEnded(initialOrbit, _t))
            {
                return _t;
            }

            double lowerUT = _t;
            double upperUT = initialOrbit.NextPeriapsisTime(_t);

            const double PRECISION = 1.0;
            while (upperUT - lowerUT > PRECISION)
            {
                double testUT = (upperUT + lowerUT) / 2;
                if (FreefallEnded(initialOrbit, testUT)) upperUT = testUT;
                else lowerUT                                     = testUT;
            }

            return (upperUT + lowerUT) / 2;
        }

        //Freefall orbit ends when either
        // - we enter the atmosphere, or
        // - our vertical velocity is negative and either
        //    - we've landed or
        //    - the descent speed policy says to start braking
        private bool FreefallEnded(Orbit initialOrbit, double ut)
        {
            Vector3d pos = initialOrbit.WorldBCIPositionAtUT(ut);
            Vector3d surfaceVelocity = SurfaceVelocity(pos, initialOrbit.WorldOrbitalVelocityAtUT(ut));

            if (pos.magnitude < _aerobrakedRadius) return true;
            if (Vector3d.Dot(surfaceVelocity, initialOrbit.Up(ut)) > 0) return false;
            if (pos.magnitude < _decelRadius) return true;
            if (_descentSpeedPolicy != null && surfaceVelocity.magnitude > _descentSpeedPolicy.MaxAllowedSpeed(pos, surfaceVelocity)) return true;
            return false;
        }

        // Bogacki–Shampine method
        private void BS34Step()
        {
            bool repeatWithSmallerStep;

            //Log(x, v);
            const double TOL = 0.01;
            const double D9 = 1d / 9d;
            const double D24 = 1d / 24d;

            do
            {
                _steps++;
                ActiveStep            = _steps;
                ActiveDt              = _dt;
                repeatWithSmallerStep = false;
                Vector3d errorv;
                // Perform the calculation
                Vector3d dx;
                Vector3d dv;
                {
                    Vector3d dv1 = _dt * TotalAccel(_x, _v, true);
                    Vector3d dx1 = _dt * _v;

                    Vector3d dv2 = _dt * TotalAccel(_x + 0.5 * dx1, _v + 0.5 * dv1);
                    Vector3d dx2 = _dt * (_v + 0.5 * dv1);

                    Vector3d dv3 = _dt * TotalAccel(_x + 0.75 * dx2, _v + 0.75 * dv2);
                    Vector3d dx3 = _dt * (_v + 0.75 * dv2);

                    Vector3d dv4 = _dt * TotalAccel(_x + 2 * D9 * dx1 + 3 * D9 * dx2 + 4 * D9 * dx3, _v + 2 * D9 * dv1 + 3 * D9 * dv2 + 4 * D9 * dv3);
                    //Vector3d dx4 = dt * (v + 2d / 9 * dv1 + 1d / 3 * dv2 + 4d / 9 * dv3);

                    dx = (2 * dx1 + 3 * dx2 + 4 * dx3) * D9;
                    dv = (2 * dv1 + 3 * dv2 + 4 * dv3) * D9;

                    //Vector3d zx = (7 * dx1 + 6 * dx2 + 8 * dx3 + 3 * dx4) * d24;
                    Vector3d zv = (7 * dv1 + 6 * dv2 + 8 * dv3 + 3 * dv4) * D24;
                    errorv = zv - dv;
                }


                // Consider opening the parachutes. If we do open them, and the dt is not as small as it could be, make it smaller and repeat,
                Vector3 xForChuteSim = _x + dx;
                double altASL = xForChuteSim.magnitude - _bodyRadius;
                double altAGL = altASL - _probableLandingSiteASL;
                double pressure = Pressure(xForChuteSim);

                Vector3d airVel = SurfaceVelocity(xForChuteSim, _v + dv);
                double airDensity = AirDensity(xForChuteSim, altASL);
                double speedOfSound = _mainBody.GetSpeedOfSound(Pressure(xForChuteSim), airDensity);
                double velocity = airVel.magnitude;
                double mach = Math.Min(velocity / speedOfSound, 50f);
                double shockTemp = ShockTemperature(velocity, mach);

                // check if the parachute will open in the next frame or if that frame will be underground
                bool willChutesOpen = altASL < _probableLandingSiteASL || _vessel.WillChutesDeploy(altAGL, altASL, _probableLandingSiteASL, pressure,
                    shockTemp, _t, _parachuteSemiDeployMultiplier);

                double nextDT;
                double errorMagnitude = Math.Max(errorv.magnitude, 10e-6);
                if (!willChutesOpen)
                {
                    nextDT = _dt * 0.9 * Math.Pow(TOL / errorMagnitude, 1 / 3d);
                }
                else
                {
                    // The chute will open at the next dt so we want to make it shorter so they don't
                    // until we have dt = min_dt and we can safely open them
                    // The same system avoids skiping the ground with a large dt
                    nextDT = _dt * 0.5;
                }

                // I don't see how it could append but it has shown up...
                if (double.IsNaN(nextDT))
                    nextDT = MinDT;

                nextDT = Math.Max(nextDT, MinDT);
                nextDT = Math.Min(nextDT, 10);

                double sqrStartDist = (_x - _startX).sqrMagnitude;
                // The first 1km is always high precision
                if (sqrStartDist < 1000 * 1000)
                    nextDT = Math.Min(nextDT, 0.02);
                else if (sqrStartDist < 5000 * 5000)
                    nextDT = Math.Min(nextDT, 0.5);
                else if (sqrStartDist < 10000 * 10000)
                    nextDT = Math.Min(nextDT, 1);

                if ((errorMagnitude > TOL || willChutesOpen) && _dt > MinDT)
                {
                    _dt                   = nextDT;
                    repeatWithSmallerStep = true;
                }
                else
                {
                    _maxDragGees = Math.Max(_maxDragGees, _lastRecordedDrag.magnitude / 9.81f);
                    bool parachutesDeploying = _vessel.Simulate(altAGL, altASL, _probableLandingSiteASL, pressure, shockTemp, _t,
                        _parachuteSemiDeployMultiplier);
                    _x += dx;
                    _v += dv;
                    _t += _dt;
                    // If a parachute is opening we need to lower dt to make sure we capture the opening sequence properly
                    _dt = parachutesDeploying ? MinDT : nextDT;
                }
            } while (repeatWithSmallerStep);
        }

        //enforce the descent speed policy
        private void LimitSpeed()
        {
            if (_descentSpeedPolicy == null) return;

            Vector3d surfaceVel = SurfaceVelocity(_x, _v);
            double maxAllowedSpeed = _descentSpeedPolicy.MaxAllowedSpeed(_x, surfaceVel);
            if (surfaceVel.magnitude > maxAllowedSpeed)
            {
                double dV = Math.Min(surfaceVel.magnitude - maxAllowedSpeed, _dt * _maxThrustAccel);
                surfaceVel      -= dV * surfaceVel.normalized;
                _deltaVExpended += dV;
                _v              =  surfaceVel + Vector3d.Cross(_bodyAngularVelocity, _x);
            }
        }

        private void RecordTrajectory()
        {
            _trajectory.Add(_referenceFrame.ToAbsolute(_x, _t));
        }

        private Vector3d TotalAccel(Vector3d pos, Vector3d vel, bool record = false)
        {
            Vector3d airVel = SurfaceVelocity(pos, vel);
            double altitude = pos.magnitude - _bodyRadius;

            double airDensity = AirDensity(pos, altitude);
            double pressure = Pressure(pos);
            double speedOfSound = _mainBody.GetSpeedOfSound(pressure, airDensity);
            float mach = Mathf.Min((float)(airVel.magnitude / speedOfSound), 50f);

            float dynamicPressurekPa = (float)(0.0005 * airDensity * airVel.sqrMagnitude);

            double pseudoReynolds = airDensity * airVel.magnitude;
            double pseudoReDragMult = _simCurves.DragCurvePseudoReynolds.Evaluate((float)pseudoReynolds);

            if (_once)
            {
                _result.Prediction.FirstDrag          = DragForce(pos, vel, dynamicPressurekPa, mach).magnitude / 9.81;
                _result.Prediction.FirstLift          = LiftForce(pos, vel, dynamicPressurekPa, mach).magnitude / 9.81;
                _result.Prediction.Mach               = mach;
                _result.Prediction.SpeedOfSound       = speedOfSound;
                _result.Prediction.DynamicPressurekPa = dynamicPressurekPa;
            }

            Vector3d dragAccel = DragForce(pos, vel, dynamicPressurekPa, mach) * pseudoReDragMult / _vessel.totalMass;

            if (record)
                _lastRecordedDrag = dragAccel;

            Vector3d gravAccel = GravAccel(pos);
            Vector3d liftAccel = LiftForce(pos, vel, dynamicPressurekPa, mach) / _vessel.totalMass;

            Vector3d totalAccel = gravAccel + dragAccel + liftAccel;

            if (_once)
                _once = false;

            return totalAccel;
        }

        private Vector3d GravAccel(Vector3d pos)
        {
            return -(_gravParameter / pos.sqrMagnitude) * pos.normalized;
        }

        private Vector3d DragForce(Vector3d pos, Vector3d vel, float dynamicPressurekPa, float mach)
        {
            if (!_bodyHasAtmosphere) return Vector3d.zero;

            Vector3d airVel = SurfaceVelocity(pos, vel);

            Vector3d localVel = _attitude * Vector3d.up * airVel.magnitude;

            // TODO : check if it is forward, back, up or down...
            // Lift works with a velocity in SHIP coordinate and return a vector in ship coordinate
            Vector3d shipDrag = _vessel.Drag(localVel, dynamicPressurekPa, mach);

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

            return -airVel.normalized * shipDrag.magnitude;
        }

        private Vector3d LiftForce(Vector3d pos, Vector3d vel, float dynamicPressurekPa, float mach)
        {
            if (!_bodyHasAtmosphere) return Vector3d.zero;

            Vector3d airVel = SurfaceVelocity(pos, vel);

            Vector3d localVel = _attitude * Vector3d.up * airVel.magnitude;

            Vector3d localLift = _vessel.Lift(localVel, dynamicPressurekPa, mach);

            QuaternionD vesselToWorld = Quaternion.FromToRotation(localVel, airVel); // QuaternionD.FromToRotation is not working in Unity 4.3

            return vesselToWorld * localLift;
        }

        private double Pressure(Vector3d pos)
        {
            double altitude = pos.magnitude - _bodyRadius;
            return StaticPressure(altitude);
        }

        private Vector3d SurfaceVelocity(Vector3d pos, Vector3d vel)
        {
            //if we're low enough, calculate the airspeed properly:
            return vel - Vector3d.Cross(_bodyAngularVelocity, pos);
        }

        private double AirDensity(Vector3d pos, double altitude)
        {
            double pressure = StaticPressure(altitude);
            double temp = GetTemperature(pos, altitude);

            return FlightGlobals.getAtmDensity(pressure, temp, _mainBody);
        }

        private double StaticPressure(double altitude)
        {
            if (!_mainBody.atmosphere)
                return 0;

            if (altitude >= _mainBody.atmosphereDepth)
                return 0;

            if (_mainBody.atmosphereUsePressureCurve)
            {
                if (_mainBody.atmospherePressureCurveIsNormalized)
                    return Mathf.Lerp(0f, (float)_mainBody.atmospherePressureSeaLevel,
                        _simCurves.AtmospherePressureCurve.Evaluate((float)(altitude / _mainBody.atmosphereDepth)));

                return _simCurves.AtmospherePressureCurve.Evaluate((float)altitude);
            }

            return _mainBody.atmospherePressureSeaLevel *
                   Math.Pow(1 - _mainBody.atmosphereTemperatureLapseRate * altitude / _mainBody.atmosphereTemperatureSeaLevel,
                       _mainBody.atmosphereGasMassLapseRate);
        }

        // Lifted from the Trajectories mod.
        private double GetTemperature(Vector3d position, double altitude)
        {
            if (!_mainBody.atmosphere)
                return PhysicsGlobals.SpaceTemperature;

            if (altitude > _mainBody.atmosphereDepth)
                return PhysicsGlobals.SpaceTemperature;

            Vector3 up = position.normalized;
            float polarAngle = Mathf.Acos(Vector3.Dot(_mainBody.bodyTransform.up, up));
            if (polarAngle > Mathf.PI * 0.5f)
            {
                polarAngle = Mathf.PI - polarAngle;
            }

            float time = (Mathf.PI * 0.5f - polarAngle) * Mathf.Rad2Deg;

            Vector3 sunVector = (FlightGlobals.Bodies[0].position - position + _mainBody.position).normalized;
            Vector3 bodyUp = _mainBody.bodyTransform.up;
            float sunAxialDot = Vector3.Dot(sunVector, bodyUp);
            float bodyPolarAngle = Mathf.Acos(Vector3.Dot(bodyUp, up));
            float sunPolarAngle = Mathf.Acos(sunAxialDot);
            float sunBodyMaxDot = (1.0f + Mathf.Cos(sunPolarAngle - bodyPolarAngle)) * 0.5f;
            float sunBodyMinDot = (1.0f + Mathf.Cos(sunPolarAngle + bodyPolarAngle)) * 0.5f;
            float sunDotCorrected = (1.0f + Vector3.Dot(sunVector,
                Quaternion.AngleAxis(45f * Mathf.Sign((float)_mainBody.rotationPeriod), bodyUp) * up)) * 0.5f;
            float sunDotNormalized = (sunDotCorrected - sunBodyMinDot) / (sunBodyMaxDot - sunBodyMinDot);
            double atmosphereTemperatureOffset = _simCurves.LatitudeTemperatureBiasCurve.Evaluate(time) +
                                                 (double)_simCurves.LatitudeTemperatureSunMultCurve.Evaluate(time) * sunDotNormalized +
                                                 _simCurves.AxialTemperatureSunMultCurve.Evaluate(sunAxialDot);

            double temperature;
            if (!_mainBody.atmosphereUseTemperatureCurve)
            {
                temperature = _mainBody.atmosphereTemperatureSeaLevel - _mainBody.atmosphereTemperatureLapseRate * altitude;
            }
            else
            {
                temperature = !_mainBody.atmosphereTemperatureCurveIsNormalized
                    ? _simCurves.AtmosphereTemperatureCurve.Evaluate((float)altitude)
                    : UtilMath.Lerp(_simCurves.SpaceTemperature, _mainBody.atmosphereTemperatureSeaLevel,
                        _simCurves.AtmosphereTemperatureCurve.Evaluate((float)(altitude / _mainBody.atmosphereDepth)));
            }

            temperature += _simCurves.AtmosphereTemperatureSunMultCurve.Evaluate((float)altitude) * atmosphereTemperatureOffset;

            return temperature;
        }

        private double ShockTemperature(double velocity, double mach)
        {
            double newtonianTemperatureFactor = velocity * PhysicsGlobals.NewtonianTemperatureFactor;
            double convectiveMachLerp =
                Math.Pow(
                    UtilMath.Clamp01((mach - PhysicsGlobals.NewtonianMachTempLerpStartMach) /
                                     (PhysicsGlobals.NewtonianMachTempLerpEndMach - PhysicsGlobals.NewtonianMachTempLerpStartMach)),
                    PhysicsGlobals.NewtonianMachTempLerpExponent);
            if (convectiveMachLerp > 0)
            {
                double machTemperatureScalar =
                    PhysicsGlobals.MachTemperatureScalar * Math.Pow(velocity, PhysicsGlobals.MachTemperatureVelocityExponent);
                newtonianTemperatureFactor = UtilMath.LerpUnclamped(newtonianTemperatureFactor, machTemperatureScalar, convectiveMachLerp);
            }

            return newtonianTemperatureFactor * HighLogic.CurrentGame.Parameters.Difficulty.ReentryHeatScale * _mainBody.shockTemperatureMultiplier;
        }

        private double _nextLog;

        public void Log(Vector3d pos, Vector3d vel)
        {
            if (_t > _nextLog)
            {
                _nextLog = _t + 10;

                double altitude = pos.magnitude - _bodyRadius;
                Vector3d airVel = SurfaceVelocity(pos, vel);

                double atmosphericTemperature = GetTemperature(pos, altitude);

                double speedOfSound = _mainBody.GetSpeedOfSound(Pressure(pos), AirDensity(pos, altitude));
                float mach = Mathf.Min((float)(airVel.magnitude / speedOfSound), 50f);

                float dynamicPressurekPa = (float)(0.0005 * AirDensity(pos, altitude) * airVel.sqrMagnitude);


                _result.DebugLog += "\n "
                                    + (_t - _startUT).ToString("F2").PadLeft(8)
                                    + " Alt:" + altitude.ToString("F0").PadLeft(6)
                                    + " Vel:" + vel.magnitude.ToString("F2").PadLeft(8)
                                    + " AirVel:" + airVel.magnitude.ToString("F2").PadLeft(8)
                                    + " SoS:" + speedOfSound.ToString("F2").PadLeft(6)
                                    + " mach:" + mach.ToString("F2").PadLeft(6)
                                    + " dynP:" + dynamicPressurekPa.ToString("F5").PadLeft(9)
                                    + " Temp:" + atmosphericTemperature.ToString("F2").PadLeft(8)
                                    + " Lat:" + _referenceFrame.Latitude(pos).ToString("F2").PadLeft(6);
            }
        }

        public enum Outcome { LANDED, AEROBRAKED, TIMED_OUT, NO_REENTRY, ERROR }

        public struct Prediction
        {
            // debuging
            public double FirstDrag;
            public double FirstLift;
            public double SpeedOfSound;
            public double Mach;
            public double DynamicPressurekPa;
        }

        public class Result
        {
            public double Maxdt;
            public int    Steps;

            public double TimeToComplete;

            public ulong     ID; // give each set of results a new id so we can check to see if the result has changed.
            public Outcome   Outcome;
            public Exception Exception;

            public CelestialBody  Body;
            public ReferenceFrame ReferenceFrame;
            public double         EndUT;

            public AbsoluteVector StartPosition;
            public AbsoluteVector EndPosition;
            public AbsoluteVector EndVelocity;

            public bool           AeroBrake;
            public double         AeroBrakeUT;
            public AbsoluteVector AeroBrakePosition;
            public AbsoluteVector AeroBrakeVelocity;

            public double               EndASL;
            public List<AbsoluteVector> Trajectory;

            public double MaxDragGees;
            public double DeltaVExpended;

            public bool   MultiplierHasError;
            public double ParachuteMultiplier;

            // Provide all the input paramaters to the simulation in the result to aid debugging
            public Orbit InputInitialOrbit;

            public double InputUT;

            //public double input_dragMassExcludingUsedParachutes;
            public List<SimulatedParachute> InputParachuteList;
            public IDescentSpeedPolicy      InputDescentSpeedPolicy;
            public double                   InputDecelEndAltitudeASL;
            public double                   InputMaxThrustAccel;
            public double                   InputParachuteSemiDeployMultiplier;
            public double                   InputProbableLandingSiteASL;
            public bool                     InputMultiplierHasError;
            public double                   InputDT;

            public string DebugLog;

            private static readonly Pool<Result> _pool = new Pool<Result>(Create, Reset);

            private static Result Create()
            {
                return new Result();
            }

            public void Release()
            {
                if (Trajectory != null)
                    ListPool<AbsoluteVector>.Instance.Release(Trajectory);
                Exception = null;
                _pool.Release(this);
            }

            private static void Reset(Result obj)
            {
                obj.AeroBrake = false;
            }

            public static Result Borrow()
            {
                Result result = _pool.Borrow();
                return result;
            }

            // debuging
            public Prediction Prediction;

            public Vector3d RelativeEndPosition()
            {
                return WorldEndPosition() - Body.position;
            }

            public Vector3d WorldEndPosition()
            {
                return ReferenceFrame.WorldPositionAtCurrentTime(EndPosition);
            }

            private Vector3d WorldEndVelocity()
            {
                return ReferenceFrame.WorldVelocityAtCurrentTime(EndVelocity);
            }

            public Orbit EndOrbit()
            {
                return MuUtils.OrbitFromStateVectors(WorldEndPosition(), WorldEndVelocity(), Body, EndUT);
            }

            public Vector3d WorldAeroBrakePosition()
            {
                return ReferenceFrame.WorldPositionAtCurrentTime(AeroBrakePosition);
            }

            public Vector3d WorldAeroBrakeVelocity()
            {
                return ReferenceFrame.WorldVelocityAtCurrentTime(AeroBrakeVelocity);
            }

            public Orbit AeroBrakeOrbit()
            {
                return MuUtils.OrbitFromStateVectors(WorldAeroBrakePosition(), WorldAeroBrakeVelocity(), Body, EndUT);
            }

            public Disposable<List<Vector3d>> WorldTrajectory(double timeStep, bool world = true)
            {
                Disposable<List<Vector3d>> ret = ListPool<Vector3d>.Instance.BorrowDisposable();

                if (Trajectory.Count == 0) return ret;

                ret.value.Add(world
                    ? ReferenceFrame.WorldPositionAtCurrentTime(Trajectory[0])
                    : ReferenceFrame.BodyPositionAtCurrentTime(Trajectory[0]));
                double lastTime = Trajectory[0].UT;
                for (int i = 0; i < Trajectory.Count; i++)
                {
                    AbsoluteVector absolute = Trajectory[i];
                    if (absolute.UT > lastTime + timeStep)
                    {
                        ret.value.Add(
                            world ? ReferenceFrame.WorldPositionAtCurrentTime(absolute) : ReferenceFrame.BodyPositionAtCurrentTime(absolute));
                        lastTime = absolute.UT;
                    }
                }

                return ret;
            }

            // A method to calculate the overshoot (length of the component of the vector from the target to the actual landing position that is parallel to the vector from the start position to the target site.)
            public double GetOvershoot(EditableAngle targetLatitude, EditableAngle targetLongitude)
            {
                // Get the start, end and target positions as a set of 3d vectors that we can work with
                Vector3d end = Body.GetWorldSurfacePosition(EndPosition.Latitude, EndPosition.Longitude, 0) - Body.position;
                Vector3d target = Body.GetWorldSurfacePosition(targetLatitude, targetLongitude, 0) - Body.position;
                Vector3d start = Body.GetWorldSurfacePosition(StartPosition.Latitude, StartPosition.Longitude, 0) - Body.position;

                // First we need to get two vectors that are non orthogonal to each other and to the vector from the start to the target. TODO can we simplify this code by using Vector3d.Exclude?
                Vector3d start2Target = target - start;
                var orthog1 = Vector3d.Cross(start2Target, Vector3d.up);
                // check for the spaecial case where start2target is parrallel to up. If it is then the result will be zero,and we need to try again
                if (orthog1 == Vector3d.up)
                {
                    orthog1 = Vector3d.Cross(start2Target, Vector3d.forward);
                }

                var orthog2 = Vector3d.Cross(start2Target, orthog1);

                // Now that we have those two orthogonal vectors, we can project any vector onto the two planes defined by them to give us the vector component that is parallel to start2Target.
                Vector3d target2End = end - target;

                Vector3d overshoot = target2End.ProjectOnPlane(orthog1).ProjectOnPlane(orthog2);

                // finally how long is it? We know it is parrallel to start2target, so if we add it to start2target, and then get the difference of the lengths, that should give us a positive or negative
                double overshootLength = (start2Target + overshoot).magnitude - start2Target.magnitude;

                return overshootLength;
            }

            public override string ToString()
            {
                string resultText = "Simulation result\n{";

                resultText += "Inputs:\n{";
                if (null != InputInitialOrbit) { resultText += "\n input_initialOrbit: " + InputInitialOrbit; }

                resultText += "\n input_UT: " + InputUT;
                //resultText += "\n input_dragMassExcludingUsedParachutes: " + input_dragMassExcludingUsedParachutes;
                //resultText += "\n input_mass: " + _inputMass;
                if (null != InputDescentSpeedPolicy) { resultText += "\n input_descentSpeedPolicy: " + InputDescentSpeedPolicy; }

                resultText += "\n input_decelEndAltitudeASL: " + InputDecelEndAltitudeASL;
                resultText += "\n input_maxThrustAccel: " + InputMaxThrustAccel;
                resultText += "\n input_parachuteSemiDeployMultiplier: " + InputParachuteSemiDeployMultiplier;
                resultText += "\n input_probableLandingSiteASL: " + InputProbableLandingSiteASL;
                resultText += "\n input_multiplierHasError: " + InputMultiplierHasError;
                resultText += "\n input_dt: " + InputDT;
                resultText += "\n}";
                resultText += "\nid: " + ID;
                resultText += "\noutcome: " + Outcome;
                resultText += "\nmaxdt: " + Maxdt;
                resultText += "\ntimeToComplete: " + TimeToComplete;
                resultText += "\nendUT: " + EndUT;
                if (null != ReferenceFrame) { resultText += "\nstartPosition: " + ReferenceFrame.WorldPositionAtCurrentTime(StartPosition); }

                if (null != ReferenceFrame) { resultText += "\nendPosition: " + ReferenceFrame.WorldPositionAtCurrentTime(EndPosition); }

                resultText += "\nendASL: " + EndASL;
                resultText += "\nendVelocity: " + EndVelocity.Longitude + "," + EndVelocity.Latitude + "," + EndVelocity.Radius;
                resultText += "\nmaxDragGees: " + MaxDragGees;
                resultText += "\ndeltaVExpended: " + DeltaVExpended;
                resultText += "\nmultiplierHasError: " + MultiplierHasError;
                resultText += "\nparachuteMultiplier: " + ParachuteMultiplier;
                resultText += "\n}";

                return resultText;
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
        public double Latitude;
        public double Longitude;
        public double Radius;
        public double UT;
    }

    //A ReferenceFrame is a scheme for converting Vector3d positions and velocities into AbsoluteVectors, and vice versa
    public class ReferenceFrame
    {
        private double        _epoch;
        private Vector3d      _lat0Lon0AtStart;
        private Vector3d      _lat0Lon90AtStart;
        private Vector3d      _lat90AtStart;
        private CelestialBody _referenceBody;

        public void UpdateAtCurrentTime(CelestialBody body)
        {
            _lat0Lon0AtStart  = body.GetSurfaceNVector(0, 0);
            _lat0Lon90AtStart = body.GetSurfaceNVector(0, 90);
            _lat90AtStart     = body.GetSurfaceNVector(90, 0);
            _epoch            = Planetarium.GetUniversalTime();
            _referenceBody    = body;
        }

        //Vector3d must be either a position RELATIVE to referenceBody, or a velocity
        public AbsoluteVector ToAbsolute(Vector3d vector3d, double ut)
        {
            var absolute = new AbsoluteVector { Latitude = Latitude(vector3d) };

            double longitude = UtilMath.Rad2Deg * Math.Atan2(Vector3d.Dot(vector3d.normalized, _lat0Lon90AtStart),
                Vector3d.Dot(vector3d.normalized, _lat0Lon0AtStart));
            longitude          -= 360 * (ut - _epoch) / _referenceBody.rotationPeriod;
            absolute.Longitude =  MuUtils.ClampDegrees180(longitude);

            absolute.Radius = vector3d.magnitude;

            absolute.UT = ut;

            return absolute;
        }

        //Interprets a given AbsoluteVector as a position, and returns the corresponding Vector3d position
        //in world coordinates.
        public Vector3d WorldPositionAtCurrentTime(AbsoluteVector absolute)
        {
            return _referenceBody.position + WorldVelocityAtCurrentTime(absolute);
        }

        public Vector3d BodyPositionAtCurrentTime(AbsoluteVector absolute)
        {
            return _referenceBody.position + absolute.Radius * _referenceBody.GetSurfaceNVector(absolute.Latitude, absolute.Longitude);
        }

        //Interprets a given AbsoluteVector as a velocity, and returns the corresponding Vector3d velocity
        //in world coordinates.
        public Vector3d WorldVelocityAtCurrentTime(AbsoluteVector absolute)
        {
            double now = Planetarium.GetUniversalTime();
            double unrotatedLongitude = MuUtils.ClampDegrees360(absolute.Longitude - 360 * (now - absolute.UT) / _referenceBody.rotationPeriod);
            return absolute.Radius * _referenceBody.GetSurfaceNVector(absolute.Latitude, unrotatedLongitude);
        }

        public double Latitude(Vector3d vector3d)
        {
            return UtilMath.Rad2Deg * Math.Asin(Vector3d.Dot(vector3d.normalized, _lat90AtStart));
        }
    }
}

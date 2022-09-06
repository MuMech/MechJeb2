// #define DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using MechJebLib.Maths;
using UnityEngine;

namespace MuMech
{
    public class TransferCalculator
    {
        public int  BestDate;
        public int  BestDuration;
        public bool Stop = false;

        private int _pendingJobs;

        // Original parameters, only used to check if parameters have changed
        public readonly Orbit OriginOrbit;
        public readonly Orbit DestinationOrbit;

        private readonly Orbit _origin;
        private readonly Orbit _destination;

        protected          int    NextDateIndex;
        protected readonly int    DateSamples;
        public readonly    double MinDepartureTime;
        public readonly    double MaxDepartureTime;
        public readonly    double MinTransferTime;
        public readonly    double MaxTransferTime;
        protected readonly int    MaxDurationSamples;

        public readonly double[,] Computed;
#if DEBUG
        private readonly string[,] _log;
#endif

        public           double ArrivalDate = -1;
        private readonly bool   _includeCaptureBurn;

        public TransferCalculator(
            Orbit o, Orbit target,
            double minDepartureTime,
            double maxTransferTime,
            double minSamplingStep, bool includeCaptureBurn) :
            this(o, target, minDepartureTime, minDepartureTime + maxTransferTime, 3600, maxTransferTime,
                Math.Min(1000, Math.Max(200, (int)(maxTransferTime / Math.Max(minSamplingStep, 60.0)))),
                Math.Min(1000, Math.Max(200, (int)(maxTransferTime / Math.Max(minSamplingStep, 60.0)))), includeCaptureBurn)
        {
            StartThreads();
        }

        protected TransferCalculator(
            Orbit o, Orbit target,
            double minDepartureTime,
            double maxDepartureTime,
            double minTransferTime,
            double maxTransferTime,
            int width,
            int height,
            bool includeCaptureBurn)
        {
            OriginOrbit      = o;
            DestinationOrbit = target;

            _origin = new Orbit();
            _origin.UpdateFromOrbitAtUT(o, minDepartureTime, o.referenceBody);
            _destination = new Orbit();
            _destination.UpdateFromOrbitAtUT(target, minDepartureTime, target.referenceBody);
            MaxDurationSamples  = height;
            DateSamples         = width;
            NextDateIndex       = DateSamples;
            MinDepartureTime    = minDepartureTime;
            MaxDepartureTime    = maxDepartureTime;
            MinTransferTime     = minTransferTime;
            MaxTransferTime     = maxTransferTime;
            _includeCaptureBurn = includeCaptureBurn;
            Computed            = new double[DateSamples, MaxDurationSamples];
            _pendingJobs        = 0;

#if DEBUG
            _log = new string[DateSamples, MaxDurationSamples];
#endif
        }

        protected void StartThreads()
        {
            if (_pendingJobs != 0)
                throw new Exception("Computation threads have already been started");

            _pendingJobs = Math.Max(1, Environment.ProcessorCount - 1);
            for (int job = 0; job < _pendingJobs; job++)
                ThreadPool.QueueUserWorkItem(ComputeDeltaV);

            //pending_jobs = 1;
            //ComputeDeltaV(this);
        }

        private bool IsBetter(int dateIndex1, int durationIndex1, int dateIndex2, int durationIndex2)
        {
            return Computed[dateIndex1, durationIndex1] > Computed[dateIndex2, durationIndex2];
        }

        private void CalcLambertDVs(double t0, double dt, out Vector3d exitDV, out Vector3d captureDV)
        {
            double t1 = t0 + dt;
            CelestialBody originPlanet = _origin.referenceBody;

            Vector3d v1_0 = originPlanet.orbit.getOrbitalVelocityAtUT(t0);
            Vector3d r1 = originPlanet.orbit.getRelativePositionAtUT(t0);

            Vector3d r2 = _destination.getRelativePositionAtUT(t1);
            Vector3d v2_1 = _destination.getOrbitalVelocityAtUT(t1);

            Vector3d v1;
            Vector3d v2;
            try
            {

                Gooding.Solve(originPlanet.referenceBody.gravParameter, r1, v1_0, r2, dt, 0, out v1, out v2);
            }
            catch
            {
                v1 = v1_0;
                v2 = v2_1;
                // ignored
            }

            exitDV    = v1 - v1_0;
            captureDV = v2_1 - v2;
        }

        private void ComputeDeltaV(object args)
        {
            for (int dateIndex = TakeDateIndex();
                dateIndex >= 0;
                dateIndex = TakeDateIndex())
            {
                double t0 = DateFromIndex(dateIndex);

                if (double.IsInfinity(t0)) continue;

                int durationSamples = DurationSamplesForDate(dateIndex);
                for (int durationIndex = 0; durationIndex < durationSamples; durationIndex++)
                {
                    if (Stop)
                        break;

                    double dt = DurationFromIndex(durationIndex);

                    CalcLambertDVs(t0, dt, out Vector3d exitDV, out Vector3d captureDV);
                    ManeuverParameters maneuver = ComputeEjectionManeuver(exitDV, _origin, t0);

                    Computed[dateIndex, durationIndex] = maneuver.dV.magnitude;
                    if (_includeCaptureBurn)
                        Computed[dateIndex, durationIndex] += captureDV.magnitude;
#if DEBUG
                    _log[dateIndex, durationIndex] += "," + Computed[dateIndex, durationIndex];
#endif
                }
            }

            JobFinished();
        }

        private void JobFinished()
        {
            int remaining = Interlocked.Decrement(ref _pendingJobs);
            if (remaining == 0)
            {
                for (int dateIndex = 0; dateIndex < DateSamples; dateIndex++)
                {
                    int n = DurationSamplesForDate(dateIndex);
                    for (int durationIndex = 0; durationIndex < n; durationIndex++)
                        if (IsBetter(BestDate, BestDuration, dateIndex, durationIndex))
                        {
                            BestDate     = dateIndex;
                            BestDuration = durationIndex;
                        }
                }

                ArrivalDate = DateFromIndex(BestDate) + DurationFromIndex(BestDuration);

                _pendingJobs = -1;

#if DEBUG
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                StreamWriter f = File.CreateText(dir + "/DeltaVWorking.csv");
                f.WriteLine(OriginOrbit.referenceBody.referenceBody.gravParameter);
                for (int dateIndex = 0; dateIndex < DateSamples; dateIndex++)
                {
                    int n = DurationSamplesForDate(dateIndex);
                    for (int durationIndex = 0; durationIndex < n; durationIndex++) f.WriteLine(_log[dateIndex, durationIndex]);
                }
#endif
            }
        }

        public bool Finished => _pendingJobs == -1;

        public virtual int Progress => (int)(100 * (1 - Math.Sqrt((double)Math.Max(0, NextDateIndex) / DateSamples)));

        private int TakeDateIndex()
        {
            return Interlocked.Decrement(ref NextDateIndex);
        }

        protected virtual int DurationSamplesForDate(int dateIndex)
        {
            return (int)(MaxDurationSamples * (MaxDepartureTime - DateFromIndex(dateIndex)) / MaxTransferTime);
        }

        public double DurationFromIndex(int index)
        {
            return MinTransferTime + index * (MaxTransferTime - MinTransferTime) / MaxDurationSamples;
        }

        public double DateFromIndex(int index)
        {
            return MinDepartureTime + index * (MaxDepartureTime - MinDepartureTime) / DateSamples;
        }

        private static ManeuverParameters ComputeEjectionManeuver(Vector3d exitVelocity, Orbit initialOrbit, double ut0, bool debug = false)
        {
            // get our reference position on the orbit
            Vector3d r0 = initialOrbit.getRelativePositionAtUT(ut0);
            Vector3d v0 = initialOrbit.getOrbitalVelocityAtUT(ut0);

            // analytic solution for paring orbit ejection to hyperbolic v-infinity
            SpaceMath.singleImpulseHyperbolicBurn(initialOrbit.referenceBody.gravParameter, r0, v0, exitVelocity, out Vector3d vneg,
                out Vector3d vpos, out Vector3d r, out double dt, debug);

            if (!dt.IsFinite() || !r.magnitude.IsFinite() || !vpos.magnitude.IsFinite() || !vneg.magnitude.IsFinite())
                Debug.Log("[MechJeb TransferCalculator] BUG mu = " + initialOrbit.referenceBody.gravParameter + " r0 = " + r0 + " v0 = " + v0 +
                          " vinf = " + exitVelocity);

            return new ManeuverParameters((vpos - vneg).xzy, ut0 + dt);
        }

        private double        _impulseScale;
        private double        _timeScale;
        private double        _initialTime;
        private double        _arrivalTime;
        private double        _targetPeR;
        private Orbit         _initialOrbit;
        private CelestialBody _targetBody;

        private void FindSOIObjective(double[] x, double[] fi, object obj)
        {
            Vector3d dv = new Vector3d(x[0], x[1], x[2]) * _impulseScale;

            double burnUT = _initialTime + x[3] * _timeScale;
            double arrivalUT = _arrivalTime + x[4] * _timeScale;

            OrbitalManeuverCalculator.PatchedConicInterceptBody(_initialOrbit, _targetBody, dv, burnUT, arrivalUT, out Orbit orbit);

            Vector3d err = orbit.getTruePositionAtUT(arrivalUT) - _targetBody.orbit.getTruePositionAtUT(arrivalUT);

            fi[0] = dv.sqrMagnitude / _impulseScale / _impulseScale;

            fi[1] = err.x * err.x / 1e+6;
            fi[2] = err.y * err.y / 1e+6;
            fi[3] = err.z * err.z / 1e+6;

            OrbitalManeuverCalculator.OrbitPool.Release(orbit);
        }

        private void FindSOI(ManeuverParameters maneuver, ref double utArrival)
        {
            const int VARS = 5;
            const double DIFFSTEP = 1e-10;
            const double EPSX = 1e-4;
            const int MAXITS = 1000;
            const int EQUALITYCONSTRAINTS = 3;
            const int INEQUALITYCONSTRAINTS = 0;

            double[] x = new double[VARS];

            _impulseScale = maneuver.dV.magnitude;
            _timeScale    = _initialOrbit.period;
            _initialTime  = maneuver.UT;
            _arrivalTime  = utArrival;

            x[0] = maneuver.dV.x / _impulseScale;
            x[1] = maneuver.dV.y / _impulseScale;
            x[2] = maneuver.dV.z / _impulseScale;
            x[3] = 0;
            x[4] = 0;

            alglib.minnlccreatef(VARS, x, DIFFSTEP, out alglib.minnlcstate state);
            alglib.minnlcsetstpmax(state, 1e-3);
            //double rho = 250.0;
            //int outerits = 5;
            //alglib.minnlcsetalgoaul(state, rho, outerits);
            //alglib.minnlcsetalgoslp(state);
            alglib.minnlcsetalgosqp(state);
            alglib.minnlcsetcond(state, EPSX, MAXITS);

            alglib.minnlcsetnlc(state, EQUALITYCONSTRAINTS, INEQUALITYCONSTRAINTS);

            alglib.minnlcoptimize(state, FindSOIObjective, null, null);
            alglib.minnlcresults(state, out x, out alglib.minnlcreport rep);

            Debug.Log("Transfer calculator: termination type=" + rep.terminationtype);
            Debug.Log("Transfer calculator: iteration count=" + rep.iterationscount);

            maneuver.dV = new Vector3d(x[0], x[1], x[2]) * _impulseScale;
            maneuver.UT = _initialTime + x[3] * _timeScale;
            utArrival   = _arrivalTime + x[4] * _timeScale;
        }

        private void PeriapsisObjective(double[] x, double[] fi, object obj)
        {
            Vector3d dv = new Vector3d(x[0], x[1], x[2]) * _impulseScale;

            double burnUT = _initialTime;
            double arrivalUT = _arrivalTime;

            OrbitalManeuverCalculator.PatchedConicInterceptBody(_initialOrbit, _targetBody, dv, burnUT, arrivalUT, out Orbit orbit);

            if (orbit.referenceBody == _targetBody)
            {
                double err = (orbit.PeR - _targetPeR) / 1e6;
                fi[0] = dv.sqrMagnitude / _impulseScale / _impulseScale;
                fi[1] = err * err;
            }
            else
            {
                fi[1] = fi[0] = 1e300;
            }

            OrbitalManeuverCalculator.OrbitPool.Release(orbit);
        }

        private void AdjustPeriapsis(ManeuverParameters maneuver, ref double utArrival)
        {
            const int VARS = 3;
            const double DIFFSTEP = 1e-10;
            const double EPSX = 1e-4;
            const int MAXITS = 1000;
            const int EQUALITYCONSTRAINTS = 1;
            const int INEQUALITYCONSTRAINTS = 0;

            double[] x = new double[VARS];

            Debug.Log("epoch: " + Planetarium.GetUniversalTime());
            Debug.Log("initial orbit around source: "+ _initialOrbit.MuString());
            Debug.Log("source: " + _initialOrbit.referenceBody.orbit.MuString());
            Debug.Log("target: " + _targetBody.orbit.MuString());
            Debug.Log("source mu: " + _initialOrbit.referenceBody.gravParameter);
            Debug.Log("target mu: " + _targetBody.gravParameter);
            Debug.Log("sun mu: " + _initialOrbit.referenceBody.referenceBody.gravParameter);
            Debug.Log("maneuver guess dV: " + maneuver.dV);
            Debug.Log("maneuver guess UT: " + maneuver.UT);
            Debug.Log("arrival guess UT: " + utArrival);
            _initialOrbit.GetOrbitalStateVectorsAtUT(maneuver.UT,out Vector3d r1,out Vector3d v1);
            Debug.Log($"initial orbit at {maneuver.UT} x = {r1}; v = {v1}");
            _initialOrbit.referenceBody.orbit.GetOrbitalStateVectorsAtUT(maneuver.UT,out Vector3d r2,out Vector3d v2);
            Debug.Log($"source at {maneuver.UT} x = {r2}; v = {v2}");
            _targetBody.orbit.GetOrbitalStateVectorsAtUT(utArrival,out Vector3d r3,out Vector3d v3);
            Debug.Log($"source at {utArrival} x = {r3}; v = {v3}");

            _impulseScale = maneuver.dV.magnitude;
            _timeScale    = _initialOrbit.period;
            _initialTime  = maneuver.UT;
            _arrivalTime  = utArrival;

            x[0] = maneuver.dV.x / _impulseScale;
            x[1] = maneuver.dV.y / _impulseScale;
            x[2] = maneuver.dV.z / _impulseScale;

            //
            // run the NLP
            //
            alglib.minnlccreatef(VARS, x, DIFFSTEP, out alglib.minnlcstate state);
            alglib.minnlcsetstpmax(state, 1e-3);
            double rho = 250.0;
            int outerits = 5;
            alglib.minnlcsetalgoaul(state, rho, outerits);
            //alglib.minnlcsetalgoslp(state);
            //alglib.minnlcsetalgosqp(state);
            alglib.minnlcsetcond(state, EPSX, MAXITS);

            alglib.minnlcsetnlc(state, EQUALITYCONSTRAINTS, INEQUALITYCONSTRAINTS);

            alglib.minnlcsetprecexactrobust(state, 0);

            alglib.minnlcoptimize(state, PeriapsisObjective, null, null);
            alglib.minnlcresults(state, out x, out alglib.minnlcreport rep);

            Debug.Log("Transfer calculator: termination type=" + rep.terminationtype);
            Debug.Log("Transfer calculator: iteration count=" + rep.iterationscount);

            maneuver.dV = new Vector3d(x[0], x[1], x[2]) * _impulseScale;
            maneuver.UT = _initialTime;
        }

        public List<ManeuverParameters> OptimizeEjection(double utTransfer, Orbit initialOrbit, CelestialBody targetBody,
            double utArrival, double earliestUT, double targetPeR, bool includeCaptureBurn)
        {
            int n = 0;

            _initialOrbit = initialOrbit;
            _targetBody   = targetBody;
            _targetPeR    = targetPeR;

            var nodeList = new List<ManeuverParameters>();

            while (true)
            {
                bool failed = false;

                CalcLambertDVs(utTransfer, utArrival - utTransfer, out Vector3d exitDV, out Vector3d _);

                Orbit source = initialOrbit.referenceBody.orbit; // helicentric orbit of the source planet

                // helicentric transfer orbit
                var transferOrbit = new Orbit();
                transferOrbit.UpdateFromStateVectors(source.getRelativePositionAtUT(utTransfer),
                    source.getOrbitalVelocityAtUT(utTransfer) + exitDV, source.referenceBody, utTransfer);

                OrbitalManeuverCalculator.SOI_intercept(transferOrbit, initialOrbit.referenceBody, utTransfer, utArrival, out double utSoiExit);

                // convert from heliocentric to body centered velocity
                Vector3d vsoi = transferOrbit.getOrbitalVelocityAtUT(utSoiExit) -
                                initialOrbit.referenceBody.orbit.getOrbitalVelocityAtUT(utSoiExit);

                // find the magnitude of Vinf from energy
                double vsoiMag = vsoi.magnitude;
                double eh = vsoiMag * vsoiMag / 2 - initialOrbit.referenceBody.gravParameter / initialOrbit.referenceBody.sphereOfInfluence;
                double vinfMag = Math.Sqrt(2 * eh);

                // scale Vsoi by the Vinf magnitude (this is now the Vinf target that will yield Vsoi at the SOI interface, but in the Vsoi direction)
                Vector3d vinf = vsoi / vsoi.magnitude * vinfMag;

                // using Vsoi seems to work slightly better here than the Vinf from the heliocentric computation at UT_Transfer
                //ManeuverParameters maneuver = ComputeEjectionManeuver(Vsoi, initial_orbit, UT_transfer, true);
                ManeuverParameters maneuver = ComputeEjectionManeuver(vinf, initialOrbit, utTransfer);

                // the arrival time plus a bit extra
                double extraArrival = maneuver.UT + (utArrival - maneuver.UT) * 1.1;

                // check to see if we're in the SOI
                OrbitalManeuverCalculator.PatchedConicInterceptBody(_initialOrbit, _targetBody, maneuver.dV, maneuver.UT, extraArrival,
                    out Orbit orbit2);

                if (orbit2.referenceBody != _targetBody)
                {
                    Debug.Log("Transfer calculator:  analytic solution does not intersect SOI, doing some expensive thinking to move it closer...");
                    // update the maneuver and arrival times to move into the SOI
                    FindSOI(maneuver, ref utArrival);
                }

                extraArrival = maneuver.UT + (utArrival - maneuver.UT) * 1.1;

                OrbitalManeuverCalculator.PatchedConicInterceptBody(_initialOrbit, _targetBody, maneuver.dV, maneuver.UT, extraArrival,
                    out Orbit orbit3);

                if (orbit3.referenceBody == _targetBody)
                {
                    Debug.Log("Transfer calculator: adjusting periapsis target");
                    AdjustPeriapsis(maneuver, ref extraArrival);
                }
                else
                {
                    failed = true;
                    Debug.Log("Transfer calculator: failed to find the SOI");
                }

                // try again in one orbit if the maneuver node is in the past
                if (maneuver.UT < earliestUT || failed)
                {
                    Debug.Log("Transfer calculator: maneuver is " + (earliestUT - maneuver.UT) + " s too early, trying again in " +
                              initialOrbit.period + " s");
                    utTransfer += initialOrbit.period;
                }
                else
                {
                    Debug.Log("from optimizer DV = " + maneuver.dV + " t = " + maneuver.UT + " original arrival = " + utArrival);
                    nodeList.Add(maneuver);
                    break;
                }

                if (n++ > 10) throw new OperationException("Ejection Optimization failed; try manual selection");
            }

            if (nodeList.Count <= 0 || !(targetPeR > 0) || !includeCaptureBurn)
                return nodeList;

            // calculate the incoming orbit
            OrbitalManeuverCalculator.PatchedConicInterceptBody(initialOrbit, targetBody, nodeList[0].dV, nodeList[0].UT, utArrival,
                out Orbit incomingOrbit);
            double burnUT = incomingOrbit.NextPeriapsisTime(incomingOrbit.StartUT);
            nodeList.Add(new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToCircularize(incomingOrbit, burnUT), burnUT));

            return nodeList;
        }
    }

    public class AllGraphTransferCalculator : TransferCalculator
    {
        public AllGraphTransferCalculator(
            Orbit o, Orbit target,
            double minDepartureTime,
            double maxDepartureTime,
            double minTransferTime,
            double maxTransferTime,
            int width,
            int height,
            bool includeCaptureBurn) : base(o, target, minDepartureTime, maxDepartureTime, minTransferTime, maxTransferTime, width, height,
            includeCaptureBurn)
        {
            StartThreads();
        }

        protected override int DurationSamplesForDate(int dateIndex)
        {
            return MaxDurationSamples;
        }

        public override int Progress => Math.Min(100, (int)(100 * (1 - (double)NextDateIndex / DateSamples)));
    }
}

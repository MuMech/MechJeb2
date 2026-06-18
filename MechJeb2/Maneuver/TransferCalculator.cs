// #define DEBUG

extern alias JetBrainsAnnotations;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using MechJebLib.Functions;
using MechJebLib.Lambert;
using MechJebLib.Primitives;
using MechJebLibBindings;
using UnityEngine;
using UnityToolbag;

namespace MuMech
{
    public class TransferCalculator
    {
        public int BestDate;
        public int BestDuration;
        public bool Stop = false;

        private int _pendingJobs;

        // Original parameters, only used to check if parameters have changed
        public readonly Orbit OriginOrbit;
        public readonly Orbit DestinationOrbit;

        private readonly Orbit _origin;
        private readonly Orbit _destination;

        protected int NextDateIndex;
        public readonly int DateSamples;
        public readonly double MinDepartureTime;
        public readonly double MaxDepartureTime;
        public readonly double MinTransferTime;
        public readonly double MaxTransferTime;
        protected readonly int MaxDurationSamples;

        public readonly double[,] Computed;
#if DEBUG
        private readonly string[,] _log;
#endif

        public double ArrivalDate = -1;
        private readonly bool _includeCaptureBurn;

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
            OriginOrbit = o;
            DestinationOrbit = target;

            _origin = new Orbit();
            _origin.UpdateFromOrbitAtUT(o, minDepartureTime, o.referenceBody);
            _destination = new Orbit();
            _destination.UpdateFromOrbitAtUT(target, minDepartureTime, target.referenceBody);
            MaxDurationSamples = height;
            DateSamples = width;
            NextDateIndex = DateSamples;
            MinDepartureTime = minDepartureTime;
            MaxDepartureTime = maxDepartureTime;
            MinTransferTime = minTransferTime;
            MaxTransferTime = maxTransferTime;
            _includeCaptureBurn = includeCaptureBurn;
            Computed = new double[DateSamples, MaxDurationSamples];
            _pendingJobs = 0;

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

        private bool IsBetter(int dateIndex1, int durationIndex1, int dateIndex2, int durationIndex2) =>
            Computed[dateIndex1, durationIndex1] > Computed[dateIndex2, durationIndex2];

        private void CalcLambertDVs(double t0, double dt, out Vector3d exitDV, out Vector3d captureDV)
        {
            double t1 = t0 + dt;
            CelestialBody originPlanet = _origin.referenceBody;

            var v10 = originPlanet.orbit.getOrbitalVelocityAtUT(t0).ToV3();
            var r1 = originPlanet.orbit.getRelativePositionAtUT(t0).ToV3();

            var r2 = _destination.getRelativePositionAtUT(t1).ToV3();
            var v21 = _destination.getOrbitalVelocityAtUT(t1).ToV3();

            V3 v1;
            V3 v2;
            try
            {
                (v1, v2) = Gooding.Solve(originPlanet.referenceBody.gravParameter, r1, r2, dt, TransferGeometry.Prograde, 0, V3.Cross(r1, v10));
            }
            catch
            {
                v1 = v10;
                v2 = v21;
                // ignored
            }

            exitDV = (v1 - v10).ToVector3d();
            captureDV = (v21 - v2).ToVector3d();
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
                            BestDate = dateIndex;
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

        private int TakeDateIndex() => Interlocked.Decrement(ref NextDateIndex);

        protected virtual int DurationSamplesForDate(int dateIndex) =>
            (int)(MaxDurationSamples * (MaxDepartureTime - DateFromIndex(dateIndex)) / MaxTransferTime);

        public double DurationFromIndex(int index) => MinTransferTime + index * (MaxTransferTime - MinTransferTime) / MaxDurationSamples;

        public double DateFromIndex(int index) => MinDepartureTime + index * (MaxDepartureTime - MinDepartureTime) / DateSamples;

        private static ManeuverParameters ComputeEjectionManeuver(Vector3d exitVelocity, Orbit initialOrbit, double ut0, bool debug = false)
        {
            // get our reference position on the orbit
            Vector3d r0 = initialOrbit.getRelativePositionAtUT(ut0);
            Vector3d v0 = initialOrbit.getOrbitalVelocityAtUT(ut0);

            // analytic solution for parking orbit ejection to hyperbolic v-infinity
            (V3 vneg, V3 vpos, V3 r, double dt) = Astro.SingleImpulseHyperbolicBurn(initialOrbit.referenceBody.gravParameter, r0.ToV3(), v0.ToV3(),
                exitVelocity.ToV3(), debug);

            if (!dt.IsFinite() || !r.magnitude.IsFinite() || !vpos.magnitude.IsFinite() || !vneg.magnitude.IsFinite())
            {
                Dispatcher.InvokeAsync(() =>
                {
                    Debug.Log(
                        $"[MechJeb TransferCalculator] BUG mu = {initialOrbit.referenceBody.gravParameter} r0 = {r0} v0 = {v0} vinf = {exitVelocity}");
                });
            }

            return new ManeuverParameters((vpos - vneg).V3ToWorld(), ut0 + dt);
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

        protected override int DurationSamplesForDate(int dateIndex) => MaxDurationSamples;

        public override int Progress => Math.Min(100, (int)(100 * (1 - (double)NextDateIndex / DateSamples)));
    }
}

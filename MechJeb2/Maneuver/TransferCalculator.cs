// #define DEBUG

using System;
using System.Threading;
using UnityEngine;

namespace MuMech
{
    public class TransferCalculator
    {
        public int bestDate = 0;
        public int bestDuration = 0;
        public bool stop = false;

        int pendingJobs;

        // Original parameters, only used to check if parameters have changed
        public readonly Orbit originOrbit;
        public readonly Orbit destinationOrbit;

        protected readonly Orbit origin;
        protected readonly Orbit destination;

        protected int nextDateIndex;
        protected readonly int dateSamples;
        public readonly double minDepartureTime;
        public readonly double maxDepartureTime;
        public readonly double minTransferTime;
        public readonly double maxTransferTime;
        protected readonly int maxDurationSamples;

        public double[,] computed;
#if DEBUG
        string[,] log;
#endif

        public double arrivalDate = -1;
        bool includeCaptureBurn;

        public double minDV;

        public TransferCalculator(
                Orbit o, Orbit target,
                double min_departure_time,
                double max_transfer_time,
                double min_sampling_step, bool includeCaptureBurn):
            this(o, target, min_departure_time, min_departure_time + max_transfer_time, 3600, max_transfer_time,
                    Math.Min(1000, Math.Max(200, (int) (max_transfer_time / Math.Max(min_sampling_step, 60.0)))),
                    Math.Min(1000, Math.Max(200, (int) (max_transfer_time / Math.Max(min_sampling_step, 60.0)))), includeCaptureBurn)
        {
            StartThreads();
        }

        protected TransferCalculator(
                Orbit o, Orbit target,
                double min_departure_time,
                double max_departure_time,
                double min_transfer_time,
                double max_transfer_time,
                int width,
                int height,
                bool includeCaptureBurn)
        {
            originOrbit = o;
            destinationOrbit = target;

            origin = new Orbit();
            origin.UpdateFromOrbitAtUT(o, min_departure_time, o.referenceBody);
            destination = new Orbit();
            destination.UpdateFromOrbitAtUT(target, min_departure_time, target.referenceBody);
            maxDurationSamples = height;
            dateSamples = width;
            nextDateIndex = dateSamples;
            this.minDepartureTime = min_departure_time;
            this.maxDepartureTime = max_departure_time;
            this.minTransferTime = min_transfer_time;
            this.maxTransferTime = max_transfer_time;
            this.includeCaptureBurn = includeCaptureBurn;
            computed = new double[dateSamples, maxDurationSamples];
            pendingJobs = 0;

#if DEBUG
            log = new string[dateSamples, maxDurationSamples];
#endif
        }

        protected void StartThreads()
        {

            if (pendingJobs != 0)
                throw new Exception("Computation threads have already been started");

            pendingJobs = Math.Max(1, Environment.ProcessorCount - 1);
            for (int job = 0; job < pendingJobs; job++)
                ThreadPool.QueueUserWorkItem(ComputeDeltaV);

            //			pending_jobs = 1;
            //			ComputeDeltaV(this);
        }

        private bool IsBetter(int date_index1, int duration_index1, int date_index2, int duration_index2)
        {
            return computed[date_index1, duration_index1] > computed[date_index2, duration_index2];
        }

        void CalcLambertDVs(double t0, double dt, out Vector3d exitDV, out Vector3d captureDV)
        {
            double t1 = t0 + dt;
            CelestialBody origin_planet = origin.referenceBody;

            Vector3d V1_0 = origin_planet.orbit.getOrbitalVelocityAtUT(t0);
            Vector3d R1 = origin_planet.orbit.getRelativePositionAtUT(t0);

            Vector3d R2 = destination.getRelativePositionAtUT(t1);
            Vector3d V2_1 = destination.getOrbitalVelocityAtUT(t1);

            Vector3d V1 = V1_0;
            Vector3d V2 = V2_1;
            try {
                GoodingSolver.Solve(origin_planet.referenceBody.gravParameter, R1, V1_0, R2, V2_1, dt, 0, out V1, out V2);
            }
            catch (Exception) {}

            exitDV = V1 - V1_0;
            captureDV = V2_1 - V2;
        }

        void ComputeDeltaV(object args)
        {
            for (int date_index = TakeDateIndex();
                    date_index >= 0;
                    date_index = TakeDateIndex())
            {
                double t0 = DateFromIndex(date_index);

                if (double.IsInfinity(t0))
                {
                    continue;
                }

                int duration_samples = DurationSamplesForDate(date_index);
                for (int duration_index = 0; duration_index < duration_samples; duration_index++)
                {
                    if (stop)
                        break;

                    double dt = DurationFromIndex(duration_index);

                    Vector3d exitDV, captureDV;
                    CalcLambertDVs(t0, dt, out exitDV, out captureDV);
                    var maneuver = ComputeEjectionManeuver(exitDV, origin, t0);

                    computed[date_index, duration_index] = maneuver.dV.magnitude;
                    if (includeCaptureBurn)
                        computed[date_index, duration_index] += captureDV.magnitude;
#if DEBUG
                    log[date_index, duration_index] += "," + computed[date_index, duration_index];
#endif
                }
            }

            JobFinished();
        }

        private void JobFinished()
        {
            int remaining = Interlocked.Decrement(ref pendingJobs);
            if (remaining == 0)
            {
                for (int date_index = 0; date_index < dateSamples; date_index++)
                {
                    int n = DurationSamplesForDate(date_index);
                    for (int duration_index = 0; duration_index < n; duration_index++)
                    {
                        if (IsBetter(bestDate, bestDuration, date_index, duration_index))
                        {
                            bestDate = date_index;
                            bestDuration = duration_index;
                        }
                    }
                }

                arrivalDate = DateFromIndex(bestDate) + DurationFromIndex(bestDuration);
                minDV = computed[bestDate, bestDuration];

                pendingJobs = -1;

#if DEBUG
                string dir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var f = System.IO.File.CreateText(dir + "/DeltaVWorking.csv");
                f.WriteLine(originOrbit.referenceBody.referenceBody.gravParameter);
                for (int date_index = 0; date_index < dateSamples; date_index++)
                {
                    int n = DurationSamplesForDate(date_index);
                    for (int duration_index = 0; duration_index < n; duration_index++)
                    {
                        f.WriteLine(log[date_index, duration_index]);
                    }
                }
#endif
            }
        }

        public bool Finished { get { return pendingJobs == -1; } }

        public virtual int Progress { get
            { return (int)(100 * (1 - Math.Sqrt((double)Math.Max(0, nextDateIndex) / dateSamples))); } }

        protected int TakeDateIndex() { return Interlocked.Decrement(ref nextDateIndex); }

        public virtual int DurationSamplesForDate(int date_index)
        {
            return (int)(maxDurationSamples * (maxDepartureTime - DateFromIndex(date_index)) / maxTransferTime);
        }
        public double DurationFromIndex(int index)
        {
            return minTransferTime + index * (maxTransferTime - minTransferTime) / maxDurationSamples;
        }
        public double DateFromIndex(int index)
        {
            return minDepartureTime + index * (maxDepartureTime - minDepartureTime) / dateSamples;
        }

        // Compute the two intersecting lines of a cone with a corner at (0,0,0) with a plane passing by (0,0,0)
        // If there is no intersection, return the line on the plane closest to the cone (assumes cone_angle > pi/2)
        void IntersectConePlane(Vector3d cone_direction, double cone_angle, Vector3d plane_normal, out Vector3d intersect_1, out Vector3d intersect_2)
        {
            intersect_1 = Vector3d.zero;
            intersect_2 = Vector3d.zero;

            cone_direction.Normalize();
            plane_normal.Normalize();

            // Change the frame of reference:
            // x = cone_direction
            // y = plane_normal projected on the plane normal to cone_direction
            // z = x * y
            Vector3d x = cone_direction;
            Vector3d z = Vector3d.Cross(x, plane_normal).normalized;
            Vector3d y = Vector3d.Cross(z, x);

            // transition matrix from the temporary frame to the global frame
            Matrix3x3f M_i_tmp = new Matrix3x3f();
            for (int i = 0; i < 3; i++)
            {
                M_i_tmp[i, 0] = (float)x[i];
                M_i_tmp[i, 1] = (float)y[i];
                M_i_tmp[i, 2] = (float)z[i];
            }

            Vector3d n = M_i_tmp.transpose() * plane_normal;
            double cos_phi = -n.x / (n.y * Math.Tan(cone_angle));

            if (Math.Abs(cos_phi) <= 1.0f)
            {
                intersect_1.x = Math.Cos(cone_angle);
                intersect_1.y = Math.Sin(cone_angle) * cos_phi;
                intersect_1.z = Math.Sin(cone_angle) * Math.Sqrt(1 - cos_phi * cos_phi);
            }
            else
            {
                intersect_1.x = -n.y;
                intersect_1.y = n.x;
                intersect_1.z = 0.0;
            }

            intersect_2.x = intersect_1.x;
            intersect_2.y = intersect_1.y;
            intersect_2.z = -intersect_1.z;

            intersect_1 = M_i_tmp * intersect_1;
            intersect_2 = M_i_tmp * intersect_2;
        }

        double AngleAboutAxis(Vector3d v1, Vector3d v2, Vector3d axis)
                    {
            axis.Normalize();

            v1 = (v1 - Vector3d.Dot(axis, v1) * axis).normalized;
            v2 = (v2 - Vector3d.Dot(axis, v2) * axis).normalized;

            return Math.Atan2(Vector3d.Dot(axis, Vector3d.Cross(v1, v2)), Vector3d.Dot(v1, v2));
        }

        public ManeuverParameters ComputeEjectionManeuver(Vector3d exit_velocity, Orbit initial_orbit, double UT_0)
        {
                Vector3d r0;
                Vector3d v0;
                Vector3d vneg = new Vector3d();
                Vector3d vpos = new Vector3d();
                Vector3d r = new Vector3d();
                double dt = 0;

                // get our reference position on the orbit
                initial_orbit.GetOrbitalStateVectorsAtUT(UT_0, out r0, out v0);

                // analytic solution for paring orbit ejection to hyperbolic v-infinity
                SpaceMath.singleImpulseHyperbolicBurn(initial_orbit.referenceBody.gravParameter, r0, v0, exit_velocity, ref vneg, ref vpos, ref r, ref dt);

                return new ManeuverParameters((vpos - vneg).xzy, UT_0 + dt);
        }

        void zeroMissObjectiveFunction(double[] x, double[] fi, object obj, Orbit initial_orbit, CelestialBody target, double UT_arrival)
        {
            double t = x[3];
            Vector3d DV = new Vector3d(x[0], x[1], x[2]);

            Orbit orbit;
            OrbitalManeuverCalculator.PatchedConicInterceptBody(initial_orbit, target, DV, t, UT_arrival, out orbit);

            Vector3d err = orbit.getTruePositionAtUT(UT_arrival) - target.orbit.getTruePositionAtUT(UT_arrival);

            fi[0] = err.x;
            fi[1] = err.y;
            fi[2] = err.z;
        }

        void periapsisObjectiveFunction(double[] x, double[] fi, object obj, Orbit initial_orbit, CelestialBody target, double UT_arrival, double target_PeR, ref bool failed)
        {
            double t = x[3];
            Vector3d DV = new Vector3d(x[0], x[1], x[2]);

            Orbit orbit;
            OrbitalManeuverCalculator.PatchedConicInterceptBody(initial_orbit, target, DV, t, UT_arrival, out orbit);

            if (orbit.referenceBody == target)
            {
                fi[0]  = ( orbit.PeR - target_PeR );
                failed = false;  // we intersected at some point with the target body SOI
            }
            else
            {
                fi[0] = ( orbit.getTruePositionAtUT(UT_arrival) - target.orbit.getTruePositionAtUT(UT_arrival) ).magnitude;
                failed = true;  // we did not intersect the target body SOI
            }
        }

        public ManeuverParameters OptimizeEjection(double UT_transfer, Orbit initial_orbit, Orbit target, CelestialBody target_body, double UT_arrival, double earliest_UT)
        {
            int N = 0;

            while(true)
            {
                const double DIFFSTEP = 1e-6;
                const double EPSX = 1e-9;
                const int MAXITS = 100;

                alglib.minlmstate state;
                alglib.minlmreport rep;

                double[] x = new double[4];
                double[] scale = new double[4];

                Vector3d exitDV, captureDV;
                CalcLambertDVs(UT_transfer, UT_arrival - UT_transfer, out exitDV, out captureDV);

                Orbit source = initial_orbit.referenceBody.orbit; // helicentric orbit of the source planet

                // helicentric transfer orbit
                Orbit transfer_orbit = new Orbit();
                transfer_orbit.UpdateFromStateVectors(source.getRelativePositionAtUT(UT_transfer), source.getOrbitalVelocityAtUT(UT_transfer) + exitDV, source.referenceBody, UT_transfer);

                double UT_SOI_exit;
                OrbitalManeuverCalculator.SOI_intercept(transfer_orbit, initial_orbit.referenceBody, UT_transfer, UT_arrival, out UT_SOI_exit);

                // convert from heliocentric to body centered velocity
                Vector3d Vsoi = transfer_orbit.getOrbitalVelocityAtUT(UT_SOI_exit) - initial_orbit.referenceBody.orbit.getOrbitalVelocityAtUT(UT_SOI_exit);

                // using Vsoi works a better here than the Vinf from the heliocentric computation at UT_Transfer
                ManeuverParameters maneuver = ComputeEjectionManeuver(Vsoi, initial_orbit, UT_transfer);

                //
                // common setup for the optimization problems
                //

                x[0] = maneuver.dV.x;
                x[1] = maneuver.dV.y;
                x[2] = maneuver.dV.z;
                x[3] = maneuver.UT;

                scale[0] = scale[1] = scale[2] = 1000.0f;
                scale[3] = initial_orbit.period;

                //
                // initial patched conic shooting to precisely hit the target
                //

                const int ZEROMISSCONS = 3;
                double[] fi = new double[ZEROMISSCONS];

                zeroMissObjectiveFunction(x, fi, null, initial_orbit, target_body, UT_arrival);
                Debug.Log("zero miss phase before optimization = " + new Vector3d(fi[0], fi[1], fi[2]).magnitude + " m");

                alglib.minlmcreatev(4, ZEROMISSCONS, x, DIFFSTEP, out state);
                alglib.minlmsetcond(state, EPSX, MAXITS);
                alglib.minlmsetscale(state, scale);
                alglib.minlmoptimize(state, (x, fi, obj) => zeroMissObjectiveFunction(x, fi, obj, initial_orbit, target_body, UT_arrival), null, null);
                alglib.minlmresults(state, out x, out rep);

                zeroMissObjectiveFunction(x, fi, null, initial_orbit, target_body, UT_arrival);
                Debug.Log("zero miss phase after optimization = " + new Vector3d(fi[0], fi[1], fi[2]).magnitude + " m");

                Debug.Log("Transfer calculator: termination type=" + rep.terminationtype);
                Debug.Log("Transfer calculator: iteration count=" + rep.iterationscount);

                //
                // Fine tuning of the periapsis
                //

                bool failed = false;
                double target_PeR = 350000;

                const int PERIAPSISCONS = 1;
                double[] fi2 = new double[PERIAPSISCONS];

                periapsisObjectiveFunction(x, fi2, null, initial_orbit, target_body, UT_arrival, target_PeR, ref failed);
                Debug.Log("periapsis phase before optimization = " + x[0] + " m");

                alglib.minlmcreatev(4, PERIAPSISCONS, x, DIFFSTEP, out state);
                alglib.minlmsetcond(state, EPSX, MAXITS);
                alglib.minlmsetscale(state, scale);
                alglib.minlmoptimize(state, (x, fi, obj) => periapsisObjectiveFunction(x, fi, obj, initial_orbit, target_body, UT_arrival, target_PeR, ref failed), null, null);
                alglib.minlmresults(state, out x, out rep);

                periapsisObjectiveFunction(x, fi2, null, initial_orbit, target_body, UT_arrival, target_PeR, ref failed);
                Debug.Log("periapsis phase after optimization = " + x[0] + " m");

                Debug.Log("Transfer calculator: termination type=" + rep.terminationtype);
                Debug.Log("Transfer calculator: iteration count=" + rep.iterationscount);

                //
                // exit conditions and error handling
                //

                // try again if we failed to intersect the target orbit
                if ( failed )
                {
                    Debug.Log("Failed to intersect target orbit");
                    N++;
                }
                // try again in one orbit if the maneuver node is in the past
                else if (x[3] < earliest_UT || failed)
                {
                    Debug.Log("Transfer calculator: maneuver is " + (earliest_UT - x[3]) + " s too early, trying again in " + initial_orbit.period + " s");
                    N++;
                }
                else {
                    Debug.Log("from optimizer DV = " + new Vector3d(x[0], x[1], x[2]) + " t = " + x[3] + " original arrival = " + UT_arrival);
                    return new ManeuverParameters(new Vector3d(x[0], x[1], x[2]), x[3]);
                }
                if (N > 10)
                {
                    throw new OperationException("Ejection Optimization failed; try manual selection");
                }
                UT_transfer += initial_orbit.period;
            }
        }
    }

    public class AllGraphTransferCalculator : TransferCalculator
    {
        public AllGraphTransferCalculator(
                Orbit o, Orbit target,
                double min_departure_time,
                double max_departure_time,
                double min_transfer_time,
                double max_transfer_time,
                int width,
                int height,
                bool includeCaptureBurn) : base(o, target, min_departure_time, max_departure_time, min_transfer_time, max_transfer_time, width, height, includeCaptureBurn)
        {
            StartThreads();
        }

        public override int DurationSamplesForDate(int date_index)
        {
            return maxDurationSamples;
        }

        public override int Progress { get { return Math.Min(100, (int)(100 * (1 - (double)nextDateIndex / dateSamples))); } }
    }
}

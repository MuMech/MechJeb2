using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace MuMech
{ 
    // This is where all the number-crunching occurs to output a set of throttles
    // given a set of thrusters and the desired translation and rotation vectors.
    public class RCSSolver
    {
        public class Thruster
        {
            public readonly Part part;
            public readonly ModuleRCS partModule;
            public readonly float originalForce;

            private readonly Vector3 pos;
            private readonly Vector3[] thrustDirections;

            public Thruster(Vector3 pos, Vector3[] thrustDirections, Part p, ModuleRCS pm)
            {
                this.pos = pos;
                this.thrustDirections = thrustDirections;
                this.originalForce = pm.thrusterPower;
                this.part = p;
                this.partModule = pm;
            }

            public void RestoreOriginalForce()
            {
                partModule.thrusterPower = originalForce;
            }

            public Vector3 GetThrust(Vector3 direction, Vector3 rotation)
            {
                Vector3 force = Vector3.zero;
                // The game appears to throttle a thruster based on the dot
                // product of its thrust vector (normalized) and the
                // direction vector (not normalized!).
                foreach (var thrustDir in thrustDirections)
                {
                    Vector3 torque = -Vector3.Cross(pos, thrustDir);
                    float translateThrottle = Vector3.Dot(direction, thrustDir);
                    float rotateThrottle = Vector3.Dot(rotation, torque);
                    float throttle = Mathf.Clamp01(translateThrottle + rotateThrottle);

                    force += thrustDir * throttle;
                }
                return force;
            }

            public Vector3 GetTorque(Vector3 thrust)
            {
                return -Vector3.Cross(pos, thrust);
            }
        }

        protected double[,] A;
        protected double[] B;

        private double factorTorque = 1;
        private double factorTranslate = 0.005;
        private double factorWaste = 1;
        private double wasteThreshold = 0.25;

        private enum PARAMS { TORQUE_X, TORQUE_Y, TORQUE_Z, TRANS_X, TRANS_Y, TRANS_Z, WASTE, FUDGE };

        private int PARAMLength = Enum.GetValues(typeof (PARAMS)).Length;

        public void UpdateTuningParameters(RCSSolverTuningParams tuningParams)
        {
            factorTorque    = tuningParams.factorTorque;
            factorTranslate = tuningParams.factorTranslate;
            factorWaste     = tuningParams.factorWaste;
            wasteThreshold  = tuningParams.wasteThreshold;
        }

        protected void cost_func(double[] x, ref double func, object obj) {
            func = 0;
            for (int attr = 0; attr < B.Length; attr++) {
                double tmp = 0;
                for (int row = 0; row < x.Length; row++) {
                    tmp += x[row] * A[attr, row];
                }
                tmp -= B[attr];
                func += tmp * tmp;
            }
        }

        public double[] run(List<Thruster> fullThrusters, Vector3 direction, Vector3 rotation)
        {
            direction = direction.normalized;

            int fullCount = fullThrusters.Count;
            List<Thruster> thrusters = new List<Thruster>();
            Vector3[] thrustForces = new Vector3[fullCount];

            // Initialize the matrix based on thruster directions.
            for (int i = 0; i < fullCount; i++)
            {
                Thruster thruster = fullThrusters[i];
                thrustForces[i] = thruster.GetThrust(direction, rotation);
                if (thrustForces[i].magnitude > 0)
                {
                    thrusters.Add(thruster);
                }
            }

            int count = thrusters.Count;

            if (count == 0) return null;

            // We want to minimize torque (3 values), translation error (3 values),
            // and any thrust that's wasted due to not being toward 'direction' (1
            // value). We also have a value to make sure there's always -some-
            // thrust.
            A = new double[PARAMLength, count];
            B = new double[PARAMLength];

            for (int i = 0; i < B.Length; i++)
            {
                B[i] = 0;
            }
            B[B.Length - 1] = 0.001 * count;

            double[] x = new double[count];
            double[] bndl = new double[count];
            double[] bndu = new double[count];

            // Initialize the matrix based on thruster directions.
            int tIdx = -1;
            for (int i = 0; i < fullCount; i++)
            {
                Vector3 thrust = thrustForces[i];
                if (thrust.magnitude == 0)
                {
                    continue;
                }
                Thruster thruster = thrusters[++tIdx];

                Vector3 torque = thruster.GetTorque(thrust);
                Vector3 thrustNorm = thrust.normalized;

                Vector3 torqueErr = torque - rotation;
                Vector3 transErr = thrustNorm - direction;

                // Waste is a value from [0..2] indicating how much thrust is being
                // wasted due to not being toward 'direction':
                //     0: perfectly aligned with direction
                //     1: perpendicular to direction
                //     2: perfectly opposite direction
                float waste = 1 - Vector3.Dot(thrustNorm, direction);

                if (waste < wasteThreshold) waste = 0;

                A[0, tIdx] = torqueErr.x * factorTorque;
                A[1, tIdx] = torqueErr.y * factorTorque;
                A[2, tIdx] = torqueErr.z * factorTorque;
                A[3, tIdx] = transErr.x * factorTranslate;
                A[4, tIdx] = transErr.y * factorTranslate;
                A[5, tIdx] = transErr.z * factorTranslate;
                A[6, tIdx] = waste * factorWaste;
                A[7, tIdx] = 0.001;
                x[tIdx] = 1;
                bndl[tIdx] = 0;
                bndu[tIdx] = 1;
            }

            alglib.minbleicstate state;
            alglib.minbleicreport rep;

            const double epsg = 0.01;
            const double epsf = 0;
            const double epsx = 0;
            const double diffstep = 1.0e-6;
            const int maxits = 0;

            double[] throttles;

            alglib.minbleiccreatef(x, diffstep, out state);
            alglib.minbleicsetbc(state, bndl, bndu);
            alglib.minbleicsetcond(state, epsg, epsf, epsx, maxits);
            alglib.minbleicoptimize(state, cost_func, null, null);
            alglib.minbleicresults(state, out throttles, out rep);

            double m = throttles.Max();

            if (m > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    throttles[i] /= m;
                }
            }

            double[] fullThrottles = new double[fullCount];

            int j = 0;
            for (int i = 0; i < fullCount; i++)
            {
                fullThrottles[i] = (thrustForces[i].magnitude == 0) ? 0 : throttles[j++];
            }

            return fullThrottles;
        }
    }

// Recalculating throttles is expensive. This class is used as a key into a
// dictionary of previously-calculated throttles.
    public class RCSSolverKey
    {
        // x, y, and z values will each be mapped to the integer range
        // [-precision..precision]. The results cache will have
        //      6 * (p + 1)^2 + 2
        // entries, assuming p >= 1. Setting precision to 0 is a bad idea.
        // (p: cache_size) 1: 26, 2: 56, 3: 98, 4: 152, 5: 218
        private static int precision = 2;

        private readonly int hash;

        public static void SetPrecision(int precision)
        {
            RCSSolverKey.precision = precision;
        }

        private float Bucketize(double d, int precision)
        {
            return (float)Mathf.RoundToInt((float)d * precision) / precision;
        }

        public RCSSolverKey(ref Vector3 d, Vector3 rot)
        {
            if (d == Vector3.zero)
            {
                // We shouldn't be solving for this, but just in case...
                hash = 0;
                return;
            }

            // Extend the vector so that it's on the unit square and so that each of
            // its components is on one of 'precision' fixed intervals
            d.Normalize();
            float maxabs = Mathf.Max(Mathf.Abs(d.x), Mathf.Max(Mathf.Abs(d.y), Mathf.Abs(d.z)));

            // Note that we're modifying the ref vector we were given. This is
            // because the caller needs to calculate a result with a vector as
            // representative of this key as possible. (As a one-dimensional
            // example, let's say we accept inputs from 0 to 10 and round to the
            // nearest integer. If we're given 1.6, we'll round to 2. If we're then
            // given 2.4, we'll return whatever we calculated for 1.6. To reduce
            // average error, it's best to calculate for the bucket's exact value.)
            d.x = Bucketize(d.x / maxabs, precision);
            d.y = Bucketize(d.y / maxabs, precision);
            d.z = Bucketize(d.z / maxabs, precision);
            int x = (int)(d.x * 127);
            int y = (int)(d.y * 127);
            int z = (int)(d.z * 127);
            hash = (int)(((x & 0xFF) << 16) + ((y & 0xFF) << 8) + (z & 0xFF));
        }

        public override bool Equals(object other)
        {
            var oth = other as RCSSolverKey;
            return (oth != null) && hash == oth.hash;
        }

        public override int GetHashCode()
        {
            return hash;
        }

        public override string ToString()
        {
            return hash.ToString("x6");
        }
    }

    public class RCSSolverTuningParams
    {
        public double wasteThreshold = 0;
        public double factorTorque = 0;
        public double factorTranslate = 0;
        public double factorWaste = 0;
    }

    public class RCSSolverThread
    {
        public double calculationTime { get; private set; }
        public double comError { get { return _comError.value; } }
        public double comErrorThreshold { get; private set; }
        public double maxComError { get; private set; }
        public string statusString { get; private set; }
        public string errorString { get; private set; }
        public int taskCount { get { return tasks.Count + resultsQueue.Count + (isWorking ? 1 : 0); } }
        public int cacheHits { get; private set; }
        public int cacheMisses { get; private set; }
        public int cacheSize { get { return results.Count; } }

        private RCSSolver solver = new RCSSolver();
        private MovingAverage _calculationTime = new MovingAverage(10);

        // A moving average reduces measurement error due to ship flexing.
        private MovingAverage _comError = new MovingAverage(10);

        private Queue tasks = Queue.Synchronized(new Queue());
        private AutoResetEvent workEvent = new AutoResetEvent(false);
        private bool stopRunning = false;
        private Thread t = null;
        private bool isWorking = false;

        private int lastPartCount = 0;
        private List<ModuleRCS> lastDisabled = new List<ModuleRCS>();
        private Vector3 lastCoM = Vector3.zero;

        // Entries in the results queue have been calculated by the solver thread
        // but not yet added to the results dictionary. GetThrottles() will check
        // the results dictionary first, and then, if no result was found, empty the
        // results queue into the results dictionary.
        private Queue resultsQueue = Queue.Synchronized(new Queue());
        private Dictionary<RCSSolverKey, double[]> results = new Dictionary<RCSSolverKey, double[]>();
        public HashSet<RCSSolverKey> pending = new HashSet<RCSSolverKey>();
        private List<RCSSolver.Thruster> thrusters = new List<RCSSolver.Thruster>();
        private double[] originalThrottles;
        private double[] zeroThrottles;
        private double[] double0 = new double[0];

        // Make a separate list of thrusters to give to clients, just to be sure
        // they don't mess up our internal one.
        private List<RCSSolver.Thruster> callerThrusters = new List<RCSSolver.Thruster>();

        public void UpdateTuningParameters(RCSSolverTuningParams tuningParams)
        {
            solver.UpdateTuningParameters(tuningParams);
            ClearResults();
        }

        public void start()
        {
            lock (solver)
            {
                if (t == null)
                {
                    ClearResults();
                    cacheHits = cacheMisses = 0;
                    isWorking = false;

                    // Make sure CheckVessel() doesn't try to reuse throttle info
                    // from when this thread was enabled previously, even if the
                    // vessel hasn't changed. Invalidating vessel information on
                    // thread start lets the UI toggle act as a reset button.
                    lastPartCount = 0;
                    maxComError = 0;

                    stopRunning = false;
                    t = new Thread(run);
                    t.Start();
                }
            }
        }

        public void stop()
        {
            lock (solver)
            {
                if (t != null)
                {
                    //ResetThrusterForces();
                    stopRunning = true;
                    workEvent.Set();
                    t.Abort();
                    t = null;
                }
            }
        }

        private class SolverTask
        {
            public readonly RCSSolverKey key;
            public readonly Vector3 direction;
            public readonly Vector3 rotation;
            public readonly DateTime timeSubmitted;

            public SolverTask(RCSSolverKey key, Vector3 direction, Vector3 rotation)
            {
                this.key = key;
                this.direction = direction;
                this.rotation = rotation;
                this.timeSubmitted = DateTime.Now;
            }
        }

        private class SolverResult
        {
            public readonly RCSSolverKey key;
            public readonly double[] throttles;

            public SolverResult(RCSSolverKey key, double[] throttles)
            {
                this.key = key;
                this.throttles = throttles;
            }
        }

        private void ClearResults()
        {
            // Note that a task being worked on right now will have already been
            // removed from 'tasks' and will add its result to the results queue.
            // TODO: Fix this so that any such stale results are never used.
            tasks.Clear();
            results.Clear();
            resultsQueue.Clear();
            pending.Clear();

            // Note that we do NOT clear _comError. It's a moving average, and if
            // exceeds the CoM shift threshold on successive CheckVessel() calls,
            // that tells us the threshold needs to be higher for this ship.
        }

        public void ResetThrusterForces()
        {
            for (int i = 0; i < thrusters.Count; i++)
            {
                var t = thrusters[i];
                t.RestoreOriginalForce();
            }
        }

        private static Vector3 WorldToVessel(Vessel vessel, Vector3 pos)
        {
            // Translate to the vessel's reference frame.
            return Quaternion.Inverse(vessel.GetTransform().rotation) * pos;
        }

        private static Vector3 VesselRelativePos(Vector3 com, Vessel vessel, Part p)
        {
            if (p.Rigidbody == null) return Vector3.zero;

            // Find our distance from the vessel's center of mass, in world
            // coordinates.
            return WorldToVessel(vessel, p.Rigidbody.worldCenterOfMass - com);
        }

        private void CheckVessel(Vessel vessel, VesselState state)
        {
            bool changed = false;

            var rcsBalancer = VesselExtensions.GetMasterMechJeb(vessel).rcsbal;

            if (vessel.parts.Count != lastPartCount)
            {
                lastPartCount = vessel.parts.Count;
                changed = true;
            }

            // Make sure all thrusters are still enabled, because if they're not,
            // our calculations will be wrong.
            for (int i = 0; i < thrusters.Count; i++)
            {
                if (!thrusters[i].partModule.isEnabled)
                {
                    changed = true;
                    break;
                }
            }

            // Likewise, make sure any previously-disabled RCS modules are still
            // disabled.
            for (int i = 0; i < lastDisabled.Count; i++)
            {
                var pm = lastDisabled[i];
                if (pm.isEnabled)
                {
                    changed = true;
                    break;
                }
            }

            // See if the CoM has moved too much.
            Rigidbody rootPartBody = vessel.rootPart.rigidbody;
            if (rootPartBody != null)
            {
                // But how much is "too much"? Well, it probably has something to do
                // with the ship's moment of inertia (MoI). Let's say the distance
                // 'd' that the CoM is allowed to shift without a reset is:
                //
                //      d = moi * x + c
                //
                // where 'moi' is the magnitude of the ship's moment of inertia and
                // 'x' and 'c' are tuning parameters to be determined.
                //
                // Using a few actual KSP ships, I burned RCS fuel (or moved fuel
                // from one tank to another) to see how far the CoM could shift
                // before the the rotation error on translation became annoying.
                // I came up with roughly:
                //
                //      d         moi
                //      0.005    2.34
                //      0.04    11.90
                //      0.07    19.96
                //
                // I then halved each 'd' value, because we'd like to address this
                // problem -before- it becomes annoying. Least-squares linear
                // regression on the (moi, d/2) pairs gives the following (with
                // adjusted R^2 = 0.999966):
                //
                //      moi = 542.268 d + 1.00654
                //      d = (moi - 1) / 542
                //
                // So the numbers below have some basis in reality. =)

                // Assume MoI magnitude is always >=2.34, since that's all I tested.
                comErrorThreshold = (Math.Max(state.MoI.magnitude, 2.34) - 1) / 542;

                Vector3 comState = state.CoM;
                Vector3 rootPos = state.rootPartPos;
                Vector3 com = WorldToVessel(vessel, comState - rootPos);
                double thisComErr = (lastCoM - com).magnitude;
                maxComError = Math.Max(maxComError, thisComErr);
                _comError.value = thisComErr;
                if (_comError > comErrorThreshold)
                {
                    lastCoM = com;
                    changed = true;
                }
            }

            if (!changed) return;

            // Something about the vessel has changed. We need to reset everything.

            lastDisabled.Clear();

            // ModuleRCS has no originalThrusterPower attribute, so we have
            // to explicitly reset it.
            ResetThrusterForces();

            // Rebuild the list of thrusters.
            var ts = new List<RCSSolver.Thruster>();
            for (int index = 0; index < vessel.parts.Count; index++)
            {
                Part p = vessel.parts[index];
                foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                {
                    if (!pm.isEnabled)
                    {
                        // Keep track of this module so we'll know if it's enabled.
                        lastDisabled.Add(pm);
                    }
                    else if (p.Rigidbody != null && !pm.isJustForShow)
                    {
                        Vector3 pos = VesselRelativePos(state.CoM, vessel, p);

                        // Create a single RCSSolver.Thruster for this part. This
                        // requires some assumptions about how the game's RCS code will
                        // drive the individual thrusters (which we can't control).

                        Vector3[] thrustDirs = new Vector3[pm.thrusterTransforms.Count];
                        Quaternion rotationQuat = Quaternion.Inverse(vessel.GetTransform().rotation);
                        for (int i = 0; i < pm.thrusterTransforms.Count; i++)
                        {
                            thrustDirs[i] = (rotationQuat * -pm.thrusterTransforms[i].up).normalized;
                        }

                        ts.Add(new RCSSolver.Thruster(pos, thrustDirs, p, pm));
                    }
                }
            }
            callerThrusters.Clear();
            originalThrottles = new double[ts.Count];
            zeroThrottles = new double[ts.Count];
            for (int i = 0; i < ts.Count; i++)
            {
                originalThrottles[i] = ts[i].originalForce;
                zeroThrottles[i] = 0;
                callerThrusters.Add(ts[i]);
            }

            thrusters = ts;
            ClearResults();
        }

        // The list of throttles is ordered under the assumption that you iterate
        // over the vessel as follows:
        //      foreach part in vessel.parts:
        //          foreach rcsModule in part.Modules.OfType<ModuleRCS>:
        //              ...
        // Note that rotation balancing is not supported at the moment.
        public void GetThrottles(Vessel vessel, VesselState state, Vector3 direction,
            out double[] throttles, out List<RCSSolver.Thruster> thrustersOut)
        {
            thrustersOut = callerThrusters;

            Vector3 rotation = Vector3.zero;

            var rcsBalancer = VesselExtensions.GetMasterMechJeb(vessel).rcsbal;

            // Update vessel info if needed.
            CheckVessel(vessel, state);

            Vector3 dir = direction.normalized;
            RCSSolverKey key = new RCSSolverKey(ref dir, rotation);

            if (thrusters.Count == 0)
            {
                throttles = double0;
            }
            else if (direction == Vector3.zero)
            {
                throttles = originalThrottles;
            }
            else if (results.TryGetValue(key, out throttles))
            {
                cacheHits++;
            }
            else
            {
                // This task hasn't been calculated. We'll handle that here.
                // Meanwhile, TryGetValue() will have set 'throttles' to null, but
                // we'll make it a 0-element array instead to avoid null checks.
                cacheMisses++;
                throttles = double0;

                if (pending.Contains(key))
                {
                    // We've submitted this key before, so we need to check the
                    // results queue.
                    while (resultsQueue.Count > 0)
                    {
                        SolverResult sr = (SolverResult)resultsQueue.Dequeue();
                        results[sr.key] = sr.throttles;
                        pending.Remove(sr.key);
                        if (sr.key == key)
                        {
                            throttles = sr.throttles;
                        }
                    }
                }
                else
                {
                    // This task was neither calculated nor pending, so we've never
                    // submitted it. Do so!
                    pending.Add(key);
                    tasks.Enqueue(new SolverTask(key, dir, rotation));
                    workEvent.Set();
                }
            }

            // Return a copy of the array to make sure ours isn't modified.
            throttles = (double[])throttles.Clone();
        }

        private void run()
        {
            // Each iteration of this loop will consume all items in 'tasks'.
            while (workEvent.WaitOne() && !stopRunning)
            {
                try
                {
                    statusString = "working";
                    while (tasks.Count > 0 && !stopRunning)
                    {
                        SolverTask task = (SolverTask)tasks.Dequeue();
                        DateTime start = DateTime.Now;
                        isWorking = true;

                        double[] throttles = solver.run(thrusters, task.direction, task.rotation);

                        isWorking = false;
                        resultsQueue.Enqueue(new SolverResult(task.key, throttles));

                        _calculationTime.value = (DateTime.Now - start).TotalSeconds;
                        calculationTime = _calculationTime;
                    }
                    statusString = "idle";
                }
                catch (InvalidOperationException)
                {
                    // Dequeue() failed due to the queue being cleared.
                }
                catch (Exception e)
                {
                    errorString = e.Message + " ..[" + e.Source + "].. " + e.StackTrace;
                }
            }
        }
    }
}
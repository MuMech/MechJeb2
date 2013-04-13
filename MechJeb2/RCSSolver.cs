using MuMech;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

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

    public double[] run(List<Thruster> thrusters, Vector3 direction, Vector3 rotation)
    {
        direction = direction.normalized;

        int count = thrusters.Count;

        if (count == 0) return null;

        // We want to minimize torque (3 values), translation error (3 values),
        // and any thrust that's wasted due to not being toward 'direction' (1
        // value). We also have a value to make sure there's always -some-
        // thrust.
        A = new double[Enum.GetValues(typeof(PARAMS)).Length, count];
        B = new double[Enum.GetValues(typeof(PARAMS)).Length];

        for (int i = 0; i < B.Length; i++)
        {
            B[i] = 0;
        }
        B[B.Length - 1] = 0.001 * count;

        double[] x = new double[count];
        double[] bndl = new double[count];
        double[] bndu = new double[count];

        // Initialize the matrix based on thruster directions.
        for (int i = 0; i < thrusters.Count; i++)
        {
            Thruster thruster = thrusters[i];
            Vector3 thrust = thruster.GetThrust(direction, rotation);
            Vector3 torque = thruster.GetTorque(thrust);
            Vector3 thrustNorm = thrust.normalized;

            Vector3 torqueErr, transErr;
            float waste;
            if (torque.sqrMagnitude > 0)
            {
                torqueErr = torque - rotation;
                transErr = thrustNorm - direction;

                // Waste is a value from [0..2] indicating how much thrust is being
                // wasted due to not being toward 'direction':
                //     0: perfectly aligned with direction
                //     1: perpendicular to direction
                //     2: perfectly opposite direction
                waste = 1 - Vector3.Dot(thrustNorm, direction);

                if (waste < wasteThreshold) waste = 0;
            }
            else
            {
                torqueErr = transErr = Vector3.zero;
                waste = 0;
            }

            A[0, i] = torqueErr.x * factorTorque;
            A[1, i] = torqueErr.y * factorTorque;
            A[2, i] = torqueErr.z * factorTorque;
            A[3, i] = transErr.x * factorTranslate;
            A[4, i] = transErr.y * factorTranslate;
            A[5, i] = transErr.z * factorTranslate;
            A[6, i] = waste * factorWaste;
            A[7, i] = 0.001;
            x[i]    = 1;
            bndl[i] = 0;
            bndu[i] = 1;

            if (torque.sqrMagnitude == 0)
            {
                B[7] -= A[7, i];
                A[7, i] = 0;
                x[i]    = 0;
                bndu[i] = 0;
            }
        }

        alglib.minbleicstate state;
        alglib.minbleicreport rep;

        double epsg = 0.000001;
        double epsf = 0;
        double epsx = 0;
        double diffstep = 1.0e-6;
        int maxits = 0;

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

        return throttles;
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
        int x = (int)((d.x + 1) * 0.5 * 255);
        int y = (int)((d.y + 1) * 0.5 * 255);
        int z = (int)((d.z + 1) * 0.5 * 255);
        hash = (x * precision) << 16 + (y * precision) << 8 + (z * precision);
    }

    public override bool Equals(object other)
    {
        var oth = other as RCSSolverKey;
        return (oth == null) ? false : hash == oth.hash;
    }

    public override int GetHashCode()
    {
        return hash;
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
    public string statusString { get; private set; }

    private RCSSolver solver = new RCSSolver();
    private MovingAverage _calculationTime = new MovingAverage(10);

    private Queue tasks = Queue.Synchronized(new Queue());
    private AutoResetEvent workEvent = new AutoResetEvent(false);
    private bool stopRunning = false;
    private Thread t = null;

    private int lastPartCount = 0;
    private Vector3d lastCoM = Vector3d.zero;
    private int changeFlag = 0;
    private Vessel lastVessel;
    private List<ModuleRCS> lastDisabled = new List<ModuleRCS>();

    private Dictionary<RCSSolverKey, double[]> results = new Dictionary<RCSSolverKey, double[]>();
    private List<RCSSolver.Thruster> thrusters = new List<RCSSolver.Thruster>();

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
                tasks.Clear();
                results.Clear();
                changeFlag = 0;
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
                stopRunning = true;
                workEvent.Set();
                t.Join();
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

    private void ClearResults()
    {
        changeFlag++;
        lock (results)
        {
            results.Clear();
        }
    }

    public void ResetThrusterForces()
    {
        foreach (var t in thrusters)
        {
            t.RestoreOriginalForce();
        }
    }

    private static Vector3 VesselRelativePos(Vector3 com, Vessel vessel, Part p)
    {
        if (p.Rigidbody == null) return Vector3.zero;

        // Find our distance from the vessel's center of
        // mass, in world coordinates.
        Vector3 pos = p.Rigidbody.worldCenterOfMass - com;

        // Translate to the vessel's reference frame.
        pos = Quaternion.Inverse(vessel.GetTransform().rotation) * pos;
        return pos;
    }

    private void CheckVessel(Vessel vessel, VesselState state, Vector3 direction, Vector3 rotation)
    {
        bool changed = (vessel != lastVessel || vessel.parts.Count != lastPartCount);

        // Make sure the center of mass hasn't shifted too much.
        if (!changed)
        {
            var rcsBalancer = VesselExtensions.GetMasterMechJeb(vessel).rcsbal;
            double comShiftSquared = (state.CoM - lastCoM).sqrMagnitude;
            changed |= (comShiftSquared >= rcsBalancer.comShiftTrigger);
        }

        // Make sure all thrusters are still enabled, because if they're not,
        // our calculations will be wrong.
        for (int i = 0; i < thrusters.Count && !changed; i++)
        {
            if (!thrusters[i].partModule.isEnabled)
            {
                changed = true;
            }
        }

        // Likewise, make sure any disabled RCS modules we found last time
        // around are still disabled.
        foreach (var pm in lastDisabled)
        {
            if (pm.isEnabled)
            {
                changed = true;
                break;
            }
        }

        if (!changed) return;

        // Something about the vessel has changed. We need to reset everything.

        lastVessel = vessel;
        lastCoM = state.CoM;
        lastPartCount = vessel.parts.Count;
        lastDisabled.Clear();

        // ModuleRCS has no originalThrusterPower attribute, so we have
        // to explicitly reset it.
        ResetThrusterForces();

        // Rebuild the list of thrusters.
        var ts = new List<RCSSolver.Thruster>();
        foreach (Part p in vessel.parts)
        {
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
                    Vector3 partForce = Vector3.zero;

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
        foreach (var t in ts)
        {
            callerThrusters.Add(t);
        }

        lock (results)
        {
            thrusters = ts;
        }
        ClearResults();
    }

    // The list of throttles is ordered under the assumption that you iterate
    // over the vessel as follows:
    //      foreach part in vessel.parts:
    //          foreach rcsModule in part.Modules.OfType<ModuleRCS>:
    //              ...
    public void GetThrottles(Vessel vessel, VesselState state, Vector3 direction, Vector3 rotation,
        out double[] throttles, out List<RCSSolver.Thruster> thrusters)
    {
        // Update vessel info if needed.
        CheckVessel(vessel, state, direction, rotation);

        Vector3 dir = direction.normalized;
        RCSSolverKey key = new RCSSolverKey(ref dir, rotation);
        double[] result = null;
        bool found = false;

        lock (results)
        {
            if (results.ContainsKey(key))
            {
                result = results[key];
                found = true;
            }
            else
            {
                // Store a null value to indicate that this result is being
                // calculated (or will be in a few lines, anyway).
                results[key] = null;
            }
        }

        if (!found)
        {
            // Calculate this task so the caller will eventually get an answer.
            tasks.Enqueue(new SolverTask(key, dir, rotation));
            workEvent.Set();
        }

        if (result != null)
        {
            // Return a copy of the array to make sure ours isn't modified.
            result = (double[])result.Clone();
        }

        throttles = result;
        thrusters = callerThrusters;
    }

    private void run()
    {
        // Each iteration of this loop will consume all items in 'tasks' and
        // work on the newest one.
        while (workEvent.WaitOne() && !stopRunning)
        {
            // It's possible that we were signalled but that our task was
            // consumed on the previous iteration of this loop (since the
            // producer adds the task and -then- signals the event).
            // TODO: This should no longer be the case. Remove this.
            if (tasks.Count == 0)
            {
                if (statusString == null || statusString == "")
                {
                    statusString = "no work found!";
                }
                continue;
            }

            SolverTask task = (SolverTask)tasks.Dequeue();

            try
            {
                int myChangeFlag = changeFlag;

                double[] throttles = solver.run(thrusters, task.direction, task.rotation);

                lock (results)
                {
                    if (myChangeFlag == changeFlag)
                    {
                        results[task.key] = throttles;
                    }
                }

                _calculationTime.value = DateTime.Now.Subtract(task.timeSubmitted).TotalSeconds;
                calculationTime = _calculationTime;
            }
            catch (Exception e)
            {
                statusString = e.Message + " ..[" + e.Source + "].. " + e.StackTrace;
            }
        }
    }
}

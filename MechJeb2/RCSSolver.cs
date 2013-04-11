using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

public class RCSSolver
{
    protected double[,] A;
    protected double[] B;

    public double factorTorque = 1;
    public double factorTranslate = 0.005;
    public double factorWaste = 1;
    public double wasteThreshold = 0.25;

    private enum PARAMS { TORQUE_X, TORQUE_Y, TORQUE_Z, TRANS_X, TRANS_Y, TRANS_Z, WASTE, FUDGE };

    public class Thruster
    {
        public readonly Vector3 pos;
        public readonly Vector3 direction;

        public readonly Part part;
        public readonly ModuleRCS partModule;
        public readonly float originalForce;

        public Thruster(Vector3 pos, Vector3 direction, Part p, ModuleRCS pm)
        {
            this.pos = pos;
            this.direction = direction;
            this.originalForce = pm.thrusterPower;
            this.part = p;
            this.partModule = pm;
        }

        public void RestoreOriginalForce()
        {
            partModule.thrusterPower = originalForce;
        }
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

    private Vector3 get_torque(Thruster thruster, Vector3 CoM, float throttle = 1)
    {
        Vector3 toCoM = CoM - thruster.pos;
        Vector3 thrust = thruster.direction * throttle;
        return Vector3.Cross(toCoM, thrust);
    }

    public double[] run(List<Thruster> thrusters, Vector3 direction, Vector3 rotation)
    {
        double[] throttles = new double[0];
        direction = direction.normalized;

        Vector3 CoM = Vector3.zero;

        int count = thrusters.Count;

        if (count == 0) return throttles;

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
            Vector3 thrust = thruster.direction;
            Vector3 torque = get_torque(thruster, CoM);
            Vector3 torqueErr = torque - rotation;
            Vector3 transErr = thrust - direction;

            // Waste is a value from [0..2] indicating how much thrust is being
            // wasted due to not being toward 'direction':
            //     0: perfectly aligned with direction
            //     1: perpendicular to direction
            //     2: perfectly opposite direction
            float waste = 1 - Vector3.Dot(thrust, direction);

            if (waste < wasteThreshold) waste = 0;

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
        }

        alglib.minbleicstate state;
        alglib.minbleicreport rep;

        double epsg = 0.000001;
        double epsf = 0;
        double epsx = 0;
        double diffstep = 1.0e-6;
        int maxits = 0;

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

public class RCSSolverThread
{
    public RCSSolver solver = new RCSSolver();
    public double timeSeconds = 0;
    public string statusString = null;

    private double[] throttles = null;

    private Queue tasks = Queue.Synchronized(new Queue());
    private AutoResetEvent workEvent = new AutoResetEvent(false);
    private bool stopRunning = false;
    private Thread t = null;

    public void start()
    {
        lock (solver)
        {
            if (t == null)
            {
                tasks.Clear();
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
        public readonly List<RCSSolver.Thruster> thrusters;
        public readonly Vector3 direction;
        public readonly Vector3 rotation;

        public SolverTask(List<RCSSolver.Thruster> thrusters, Vector3 direction, Vector3 rotation)
        {
            this.thrusters = new List<RCSSolver.Thruster>();
            this.direction = direction;
            this.rotation = rotation;
            foreach (var t in thrusters)
            {
                this.thrusters.Add(t);
            }
        }
    }

    public void post_task(List<RCSSolver.Thruster> thrusters, Vector3 direction, Vector3 rotation)
    {
        tasks.Enqueue(new SolverTask(thrusters, direction, rotation));
        workEvent.Set();
    }

    public double[] get_throttles()
    {
        // If there are outstanding tasks, return null instead of stale throttle
        // values.
        return (tasks.Count > 0) ? null : throttles;
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
            if (tasks.Count == 0) continue;

            DateTime start = DateTime.Now;

            // Ignore all but the latest task.
            while (tasks.Count > 1) tasks.Dequeue();
            SolverTask task = (SolverTask)tasks.Dequeue();

            try
            {
                throttles = solver.run(task.thrusters, task.direction, task.rotation);
            }
            catch (Exception e)
            {
                statusString = e.Message + " ..[" + e.Source + "].. " + e.StackTrace;
                throttles = null;
            }
            DateTime stop = DateTime.Now;
            timeSeconds = stop.Subtract(start).TotalSeconds;
        }
    }
}

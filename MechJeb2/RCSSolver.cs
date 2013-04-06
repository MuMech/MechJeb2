using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

public class RCSSolver
{
    protected double[,] A;
    protected double[] B;

    public double efficiency;
    public Vector3 extraTorque;

    public class Thruster
    {
        public Vector3 pos;
        public Vector3 direction;

        public Part part;
        public ModuleRCS partModule;

        public Thruster(Vector3 pos, Vector3 direction, Part p = null, ModuleRCS pm = null)
        {
            this.pos = pos;
            this.direction = direction;
            this.part = p;
            this.partModule = pm;
        }
    }

    protected void cost_func(double[] x, ref double func, object obj) {
        func = 0;
        for (int i = 0; i < B.Length; i++) {
            double tmp = 0;
            for (int j = 0; j < x.Length; j++) {
                tmp += x[j] * A[i, j];
            }
            tmp -= B[i];
            func += tmp * tmp;
        }
    }

    private Vector3 get_torque(Thruster thruster, Vector3 CoM, float throttle = 1)
    {
        Vector3 toCoM = CoM - thruster.pos;
        Vector3 thrust = thruster.direction.normalized * throttle;
        return Vector3.Cross(toCoM, thrust);
    }

    public void run(List<Thruster> thrusters, Vector3 direction, out double[] proportion)
    {
        proportion = new double[0];

        Vector3 CoM = Vector3.zero;

        int count = thrusters.Count;

        // We want to minimize torque (3 values) and any thrust that's not
        // toward 'direction' (1 value). We also have a value to make sure
        // there's always -some- thrust.
        A = new double[5, count];
        B = new double[] { 0, 0, 0, 0, 0.001 * count };
        double[] x = new double[count];
        if (x.Length == proportion.Length) {
            x = proportion;
        }
        double[] bndl = new double[count];
        double[] bndu = new double[count];

        // Initialize the matrix based on thruster directions.
        for (int i = 0; i < thrusters.Count; i++)
        {
            Thruster thruster = thrusters[i];
            Vector3 thrust = thruster.direction.normalized;
            Vector3 torque = get_torque(thruster, CoM);

            // Waste is a value from [0..2] indicating how much thrust is being
            // wasted due to not being toward 'direction'.
            float waste = 1 - Vector3.Dot(thrust, direction);

            A[0, i] = torque.x;
            A[1, i] = torque.y;
            A[2, i] = torque.z;
            A[3, i] = waste;
            A[4, i] = 0.001;
            x[i]    = 1;
            bndl[i] = 0;
            bndu[i] = 1;
        }

        alglib.minbleicstate state;
        alglib.minbleicreport rep;

        double epsg = 0.000001;
        double epsf = 0;
        double epsx = 0;
        double epso = 0.00001;
        double epsi = 0.00001;
        double diffstep = 1.0e-6;

        alglib.minbleiccreatef(x, diffstep, out state);
        alglib.minbleicsetbc(state, bndl, bndu);
        alglib.minbleicsetinnercond(state, epsg, epsf, epsx);
        alglib.minbleicsetoutercond(state, epso, epsi);
        alglib.minbleicoptimize(state, cost_func, null, null);
        alglib.minbleicresults(state, out proportion, out rep);

        double m = proportion.Max();

        for (int i = 0; i < count; i++)
        {
            proportion[i] /= m;
        }

        // All done! But before we return, let's calculate some interesting
        // statistics.
        
        double totalThrust = 0;
        double effectiveThrust = 0;
        for (int i = 0; i < count; i++)
        {
            totalThrust += proportion[i];
            effectiveThrust += proportion[i] * (1 - A[3, i]);
        }
        efficiency = effectiveThrust / totalThrust;

        Vector3 extraTorque = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            extraTorque += get_torque(thrusters[i], CoM, (float) proportion[i]);
        }
    }
}

public class RCSSolverThread
{
    public RCSSolver solver = new RCSSolver();
    public double timeSeconds = 0;
    public string statusString;

    private double[] throttles;
    private List<RCSSolver.Thruster> thrusters;
    private Vector3 direction;
    private AutoResetEvent workEvent = new AutoResetEvent(false);
    private Boolean stopRunning = false;
    private Thread t = null;

    public void start()
    {
        lock (solver)
        {
            if (t == null)
            {
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

    public void post_task(List<RCSSolver.Thruster> thrusters, Vector3 direction)
    {
        // Use a copy of this list in case the caller wants to modify theirs
        // later.
        var newThrusters = new List<RCSSolver.Thruster>();
        newThrusters.InsertRange(0, thrusters);

        this.thrusters = newThrusters;
        this.direction = direction;
        workEvent.Set();
    }

    public double[] get_throttles()
    {
        return (double[])throttles.Clone();
    }

    private void run()
    {
        while (workEvent.WaitOne())
        {
            if (stopRunning) return;
            DateTime start = new DateTime();
            try
            {
                solver.run(thrusters, direction, out throttles);
            }
            catch (Exception e)
            {
                statusString = e.Message + " ..[" + e.Source + "].. " + e.StackTrace;
                Debug.Log(statusString);
            }
            DateTime stop = new DateTime();
            timeSeconds = stop.Subtract(start).TotalMilliseconds;
        }
    }
}

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

    public double totalThrust;
    public double efficiency;
    public Vector3 extraTorque;

    public double factorTorque = 1;
    public double factorTranslate = 0.005;
    public double factorWaste = 1;
    public double wasteThreshold = 0.25;

    private enum PARAMS { TORQUE_X, TORQUE_Y, TORQUE_Z, TRANS_X, TRANS_Y, TRANS_Z, WASTE, FUDGE };

    public class Thruster
    {
        public Vector3 pos;
        public Vector3 direction;

        public Part part;
        public ModuleRCS partModule;

        public Thruster(Vector3 pos, Vector3 direction, Part p = null, ModuleRCS pm = null)
        {
            this.pos = pos;
            this.direction = direction.normalized;
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
        Vector3 thrust = thruster.direction * throttle;
        return Vector3.Cross(toCoM, thrust);
    }

    public double[] run(List<Thruster> thrusters, Vector3 direction)
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
        if (x.Length == throttles.Length) {
            x = throttles;
        }
        double[] bndl = new double[count];
        double[] bndu = new double[count];

        // Initialize the matrix based on thruster directions.
        for (int i = 0; i < thrusters.Count; i++)
        {
            Thruster thruster = thrusters[i];
            Vector3 thrust = thruster.direction;
            Vector3 torque = get_torque(thruster, CoM);
            Vector3 transErr = direction - thrust;

            // Waste is a value from [0..2] indicating how much thrust is being
            // wasted due to not being toward 'direction'.
            float waste = 1 - Vector3.Dot(thrust, direction);

            if (waste < wasteThreshold) waste = 0;

            A[0, i] = torque.x * factorTorque;
            A[1, i] = torque.y * factorTorque;
            A[2, i] = torque.z * factorTorque;
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
        double epso = 0.00001;
        double epsi = 0.00001;
        double diffstep = 1.0e-6;

        alglib.minbleiccreatef(x, diffstep, out state);
        alglib.minbleicsetbc(state, bndl, bndu);
        alglib.minbleicsetinnercond(state, epsg, epsf, epsx);
        alglib.minbleicsetoutercond(state, epso, epsi);
        alglib.minbleicoptimize(state, cost_func, null, null);
        alglib.minbleicresults(state, out throttles, out rep);

        double m = throttles.Max();

        for (int i = 0; i < count; i++)
        {
            throttles[i] /= m;
        }

        // All done! But before we return, let's calculate some interesting
        // statistics.
        
        double thrustSum = 0;
        double effectiveThrust = 0;
        for (int i = 0; i < count; i++)
        {
            thrustSum += throttles[i];
            effectiveThrust += throttles[i] * Vector3.Dot(thrusters[i].direction, direction);
        }
        totalThrust = thrustSum;
        efficiency = effectiveThrust / totalThrust;

        Vector3 extraTorque = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            extraTorque += get_torque(thrusters[i], CoM, (float) throttles[i]);
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

        // Delete our previously calculated throttles so that get_throttles()
        // doesn't return bad values.
        throttles = null;

        workEvent.Set();
    }

    public double[] get_throttles()
    {
        return (throttles == null) ? null : (double[])throttles.Clone();
    }

    private void run()
    {
        while (workEvent.WaitOne())
        {
            if (stopRunning) return;
            DateTime start = DateTime.Now;
            try
            {
                throttles = solver.run(thrusters, direction);
            }
            catch (Exception e)
            {
                statusString = e.Message + " ..[" + e.Source + "].. " + e.StackTrace;
                Debug.Log(statusString);
            }
            DateTime stop = DateTime.Now;
            timeSeconds = stop.Subtract(start).TotalSeconds;
        }
    }
}

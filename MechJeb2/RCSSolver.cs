using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            public readonly Part      Part;
            public readonly ModuleRCS PartModule;
            public readonly float     OriginalForce;

            private readonly Vector3   _pos;
            private readonly Vector3[] _thrustDirections;

            public Thruster(Vector3 pos, Vector3[] thrustDirections, Part p, ModuleRCS pm)
            {
                _pos              = pos;
                _thrustDirections = thrustDirections;
                OriginalForce     = pm.thrusterPower;
                Part              = p;
                PartModule        = pm;
            }

            public void RestoreOriginalForce() => PartModule.thrusterPower = OriginalForce;

            public Vector3 GetThrust(Vector3 direction, Vector3 rotation)
            {
                Vector3 force = Vector3.zero;
                // The game appears to throttle a thruster based on the dot
                // product of its thrust vector (normalized) and the
                // direction vector (not normalized!).
                foreach (Vector3 thrustDir in _thrustDirections)
                {
                    Vector3 torque = -Vector3.Cross(_pos, thrustDir);
                    float translateThrottle = Vector3.Dot(direction, thrustDir);
                    float rotateThrottle = Vector3.Dot(rotation, torque);
                    float throttle = Mathf.Clamp01(translateThrottle + rotateThrottle);

                    force += thrustDir * throttle;
                }

                return force;
            }

            public Vector3 GetTorque(Vector3 thrust) => -Vector3.Cross(_pos, thrust);
        }

        private double[,] _a;
        private double[]  _b;

        private double _factorTorque    = 1;
        private double _factorTranslate = 0.005;
        private double _factorWaste     = 1;
        private double _wasteThreshold  = 0.25;

        private enum Params { TORQUE_X, TORQUE_Y, TORQUE_Z, TRANS_X, TRANS_Y, TRANS_Z, WASTE, FUDGE }

        private readonly int _paramLength = Enum.GetValues(typeof(Params)).Length;

        public void UpdateTuningParameters(RCSSolverTuningParams tuningParams)
        {
            _factorTorque    = tuningParams.FactorTorque;
            _factorTranslate = tuningParams.FactorTranslate;
            _factorWaste     = tuningParams.FactorWaste;
            _wasteThreshold  = tuningParams.WasteThreshold;
        }

        private void cost_func(double[] x, ref double func, object obj)
        {
            func = 0;
            for (int attr = 0; attr < _b.Length; attr++)
            {
                double tmp = 0;
                for (int row = 0; row < x.Length; row++)
                {
                    tmp += x[row] * _a[attr, row];
                }

                tmp  -= _b[attr];
                func += tmp * tmp;
            }
        }

        public double[] Run(List<Thruster> fullThrusters, Vector3 direction, Vector3 rotation)
        {
            direction = direction.normalized;

            int fullCount = fullThrusters.Count;
            var thrusters = new List<Thruster>();
            var thrustForces = new Vector3[fullCount];

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
            _a = new double[_paramLength, count];
            _b = new double[_paramLength];

            for (int i = 0; i < _b.Length; i++)
            {
                _b[i] = 0;
            }

            _b[_b.Length - 1] = 0.001 * count;

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

                if (waste < _wasteThreshold) waste = 0;

                _a[0, tIdx] = torqueErr.x * _factorTorque;
                _a[1, tIdx] = torqueErr.y * _factorTorque;
                _a[2, tIdx] = torqueErr.z * _factorTorque;
                _a[3, tIdx] = transErr.x * _factorTranslate;
                _a[4, tIdx] = transErr.y * _factorTranslate;
                _a[5, tIdx] = transErr.z * _factorTranslate;
                _a[6, tIdx] = waste * _factorWaste;
                _a[7, tIdx] = 0.001;
                x[tIdx]     = 1;
                bndl[tIdx]  = 0;
                bndu[tIdx]  = 1;
            }

            alglib.minbleicreport rep;

            const double EPSG = 0.01;
            const double EPSF = 0;
            const double EPSX = 0;
            const double DIFFSTEP = 1.0e-6;
            const int MAXITS = 0;

            alglib.minbleiccreatef(x, DIFFSTEP, out alglib.minbleicstate state);
            alglib.minbleicsetbc(state, bndl, bndu);
            alglib.minbleicsetcond(state, EPSG, EPSF, EPSX, MAXITS);
            alglib.minbleicoptimize(state, cost_func, null, null);
            alglib.minbleicresults(state, out double[] throttles, out rep);

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
                fullThrottles[i] = thrustForces[i].magnitude == 0 ? 0 : throttles[j++];
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
        private static int _precision = 2;

        private readonly int _hash;

        public static void SetPrecision(int precision) => _precision = precision;

        private float Bucketize(double d, int precision) => (float)Mathf.RoundToInt((float)d * precision) / precision;

        public RCSSolverKey(ref Vector3 d, Vector3 rot)
        {
            if (d == Vector3.zero)
            {
                // We shouldn't be solving for this, but just in case...
                _hash = 0;
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
            d.x = Bucketize(d.x / maxabs, _precision);
            d.y = Bucketize(d.y / maxabs, _precision);
            d.z = Bucketize(d.z / maxabs, _precision);
            int x = (int)(d.x * 127);
            int y = (int)(d.y * 127);
            int z = (int)(d.z * 127);
            _hash = ((x & 0xFF) << 16) + ((y & 0xFF) << 8) + (z & 0xFF);
        }

        public override bool Equals(object other)
        {
            var oth = other as RCSSolverKey;
            return oth != null && _hash == oth._hash;
        }

        public override int GetHashCode() => _hash;

        public override string ToString() => _hash.ToString("x6");
    }

    public class RCSSolverTuningParams
    {
        public double WasteThreshold  = 0;
        public double FactorTorque    = 0;
        public double FactorTranslate = 0;
        public double FactorWaste     = 0;
    }

    public class RCSSolverThread
    {
        public double CalculationTime   { get; private set; }
        public double ComError          => _comError.Value;
        public double ComErrorThreshold { get; private set; }
        public double MaxComError       { get; private set; }
        public string StatusString      { get; private set; }
        public string ErrorString       { get; private set; }
        public int    TaskCount         => _tasks.Count + _resultsQueue.Count + (_isWorking ? 1 : 0);
        public int    CacheHits         { get; private set; }
        public int    CacheMisses       { get; private set; }
        public int    CacheSize         => _results.Count;

        private readonly RCSSolver     _solver          = new RCSSolver();
        private readonly MovingAverage _calculationTime = new MovingAverage();

        // A moving average reduces measurement error due to ship flexing.
        private readonly MovingAverage _comError = new MovingAverage();

        private readonly Queue          _tasks     = Queue.Synchronized(new Queue());
        private readonly AutoResetEvent _workEvent = new AutoResetEvent(false);
        private          bool           _stopRunning;
        private          Thread         _t;
        private          bool           _isWorking;

        private          int             _lastPartCount;
        private readonly List<ModuleRCS> _lastDisabled = new List<ModuleRCS>();
        private          Vector3         _lastCoM      = Vector3.zero;

        // Entries in the results queue have been calculated by the solver thread
        // but not yet added to the results dictionary. GetThrottles() will check
        // the results dictionary first, and then, if no result was found, empty the
        // results queue into the results dictionary.
        private readonly Queue                              _resultsQueue = Queue.Synchronized(new Queue());
        private readonly Dictionary<RCSSolverKey, double[]> _results      = new Dictionary<RCSSolverKey, double[]>();
        private readonly HashSet<RCSSolverKey>              _pending      = new HashSet<RCSSolverKey>();
        private          List<RCSSolver.Thruster>           _thrusters    = new List<RCSSolver.Thruster>();
        private          double[]                           _originalThrottles;
        private          double[]                           _zeroThrottles;
        private readonly double[]                           _double0 = Array.Empty<double>();

        // Make a separate list of thrusters to give to clients, just to be sure
        // they don't mess up our internal one.
        private readonly List<RCSSolver.Thruster> _callerThrusters = new List<RCSSolver.Thruster>();

        public void UpdateTuningParameters(RCSSolverTuningParams tuningParams)
        {
            _solver.UpdateTuningParameters(tuningParams);
            ClearResults();
        }

        public void Start()
        {
            lock (_solver)
            {
                if (_t == null)
                {
                    ClearResults();
                    CacheHits  = CacheMisses = 0;
                    _isWorking = false;

                    // Make sure CheckVessel() doesn't try to reuse throttle info
                    // from when this thread was enabled previously, even if the
                    // vessel hasn't changed. Invalidating vessel information on
                    // thread start lets the UI toggle act as a reset button.
                    _lastPartCount = 0;
                    MaxComError    = 0;

                    _stopRunning = false;
                    _t           = new Thread(Run);
                    _t.Start();
                }
            }
        }

        public void Stop()
        {
            lock (_solver)
            {
                if (_t != null)
                {
                    //ResetThrusterForces();
                    _stopRunning = true;
                    _workEvent.Set();
                    _t.Abort();
                    _t = null;
                }
            }
        }

        private class SolverTask
        {
            public readonly RCSSolverKey Key;
            public readonly Vector3      Direction;
            public readonly Vector3      Rotation;

            public SolverTask(RCSSolverKey key, Vector3 direction, Vector3 rotation)
            {
                Key       = key;
                Direction = direction;
                Rotation  = rotation;
            }
        }

        private class SolverResult
        {
            public readonly RCSSolverKey Key;
            public readonly double[]     Throttles;

            public SolverResult(RCSSolverKey key, double[] throttles)
            {
                Key       = key;
                Throttles = throttles;
            }
        }

        private void ClearResults()
        {
            // Note that a task being worked on right now will have already been
            // removed from 'tasks' and will add its result to the results queue.
            // TODO: Fix this so that any such stale results are never used.
            _tasks.Clear();
            _results.Clear();
            _resultsQueue.Clear();
            _pending.Clear();

            // Note that we do NOT clear _comError. It's a moving average, and if
            // exceeds the CoM shift threshold on successive CheckVessel() calls,
            // that tells us the threshold needs to be higher for this ship.
        }

        public void ResetThrusterForces()
        {
            for (int i = 0; i < _thrusters.Count; i++)
            {
                RCSSolver.Thruster t = _thrusters[i];
                t.RestoreOriginalForce();
            }
        }

        private static Vector3 WorldToVessel(Vessel vessel, Vector3 pos) =>
            // Translate to the vessel's reference frame.
            Quaternion.Inverse(vessel.GetTransform().rotation) * pos;

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

            if (vessel.parts.Count != _lastPartCount)
            {
                _lastPartCount = vessel.parts.Count;
                changed        = true;
            }

            // Make sure all thrusters are still enabled, because if they're not,
            // our calculations will be wrong.
            for (int i = 0; i < _thrusters.Count; i++)
            {
                if (!_thrusters[i].PartModule.isEnabled)
                {
                    changed = true;
                    break;
                }
            }

            // Likewise, make sure any previously-disabled RCS modules are still
            // disabled.
            for (int i = 0; i < _lastDisabled.Count; i++)
            {
                ModuleRCS pm = _lastDisabled[i];
                if (pm.isEnabled)
                {
                    changed = true;
                    break;
                }
            }

            // See if the CoM has moved too much.
            Rigidbody rootPartBody = vessel.rootPart.rb;
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
                ComErrorThreshold = (Math.Max(state.MoI.magnitude, 2.34) - 1) / 542;

                Vector3 comState = state.CoM;
                Vector3 rootPos = state.rootPartPos;
                Vector3 com = WorldToVessel(vessel, comState - rootPos);
                double thisComErr = (_lastCoM - com).magnitude;
                MaxComError     = Math.Max(MaxComError, thisComErr);
                _comError.Value = thisComErr;
                if (_comError > ComErrorThreshold)
                {
                    _lastCoM = com;
                    changed  = true;
                }
            }

            if (!changed) return;

            // Something about the vessel has changed. We need to reset everything.

            _lastDisabled.Clear();

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
                        _lastDisabled.Add(pm);
                    }
                    else if (p.Rigidbody != null && !pm.isJustForShow)
                    {
                        Vector3 pos = VesselRelativePos(state.CoM, vessel, p);

                        // Create a single RCSSolver.Thruster for this part. This
                        // requires some assumptions about how the game's RCS code will
                        // drive the individual thrusters (which we can't control).

                        var thrustDirs = new Vector3[pm.thrusterTransforms.Count];
                        var rotationQuat = Quaternion.Inverse(vessel.GetTransform().rotation);
                        for (int i = 0; i < pm.thrusterTransforms.Count; i++)
                        {
                            thrustDirs[i] = (rotationQuat * -pm.thrusterTransforms[i].up).normalized;
                        }

                        ts.Add(new RCSSolver.Thruster(pos, thrustDirs, p, pm));
                    }
                }
            }

            _callerThrusters.Clear();
            _originalThrottles = new double[ts.Count];
            _zeroThrottles     = new double[ts.Count];
            for (int i = 0; i < ts.Count; i++)
            {
                _originalThrottles[i] = ts[i].OriginalForce;
                _zeroThrottles[i]     = 0;
                _callerThrusters.Add(ts[i]);
            }

            _thrusters = ts;
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
            thrustersOut = _callerThrusters;

            Vector3 rotation = Vector3.zero;

            // Update vessel info if needed.
            CheckVessel(vessel, state);

            Vector3 dir = direction.normalized;
            var key = new RCSSolverKey(ref dir, rotation);

            if (_thrusters.Count == 0)
            {
                throttles = _double0;
            }
            else if (direction == Vector3.zero)
            {
                throttles = _originalThrottles;
            }
            else if (_results.TryGetValue(key, out throttles))
            {
                CacheHits++;
            }
            else
            {
                // This task hasn't been calculated. We'll handle that here.
                // Meanwhile, TryGetValue() will have set 'throttles' to null, but
                // we'll make it a 0-element array instead to avoid null checks.
                CacheMisses++;
                throttles = _double0;

                if (_pending.Contains(key))
                {
                    // We've submitted this key before, so we need to check the
                    // results queue.
                    while (_resultsQueue.Count > 0)
                    {
                        var sr = (SolverResult)_resultsQueue.Dequeue();
                        _results[sr.Key] = sr.Throttles;
                        _pending.Remove(sr.Key);
                        if (sr.Key == key)
                        {
                            throttles = sr.Throttles;
                        }
                    }
                }
                else
                {
                    // This task was neither calculated nor pending, so we've never
                    // submitted it. Do so!
                    _pending.Add(key);
                    _tasks.Enqueue(new SolverTask(key, dir, rotation));
                    _workEvent.Set();
                }
            }

            // Return a copy of the array to make sure ours isn't modified.
            throttles = (double[])throttles.Clone();
        }

        private void Run()
        {
            // Each iteration of this loop will consume all items in 'tasks'.
            while (_workEvent.WaitOne() && !_stopRunning)
            {
                try
                {
                    StatusString = "working";
                    while (_tasks.Count > 0 && !_stopRunning)
                    {
                        var task = (SolverTask)_tasks.Dequeue();
                        DateTime start = DateTime.Now;
                        _isWorking = true;

                        double[] throttles = _solver.Run(_thrusters, task.Direction, task.Rotation);

                        _isWorking = false;
                        _resultsQueue.Enqueue(new SolverResult(task.Key, throttles));

                        _calculationTime.Value = (DateTime.Now - start).TotalSeconds;
                        CalculationTime        = _calculationTime;
                    }

                    StatusString = "idle";
                }
                catch (InvalidOperationException)
                {
                    // Dequeue() failed due to the queue being cleared.
                }
                catch (Exception e)
                {
                    ErrorString = e.Message + " ..[" + e.Source + "].. " + e.StackTrace;
                }
            }
        }
    }
}

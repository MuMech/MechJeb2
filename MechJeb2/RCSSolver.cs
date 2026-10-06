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
            private readonly Vector3 _pos;
            private readonly Vector3[] _thrustDirections;

            public Thruster(Vector3 pos, Vector3[] thrustDirections)
            {
                _pos = pos;
                _thrustDirections = thrustDirections;
            }

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
        private double[] _b;

        private enum Params { TORQUE_X, TORQUE_Y, TORQUE_Z, TRANS_X, TRANS_Y, TRANS_Z, WASTE, FUDGE }

        private readonly int _paramLength = Enum.GetValues(typeof(Params)).Length;

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

                tmp -= _b[attr];
                func += tmp * tmp;
            }
        }

        public double[] Run(IReadOnlyList<Thruster> fullThrusters, Vector3 direction, Vector3 rotation, RCSSolverTuningParams tuning)
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

            if (count == 0) return new double[fullCount]; // array of zeros

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

                if (waste < tuning.WasteThreshold) waste = 0;

                _a[(int)Params.TORQUE_X, tIdx] = torqueErr.x * tuning.FactorTorque;
                _a[(int)Params.TORQUE_Y, tIdx] = torqueErr.y * tuning.FactorTorque;
                _a[(int)Params.TORQUE_Z, tIdx] = torqueErr.z * tuning.FactorTorque;
                _a[(int)Params.TRANS_X, tIdx] = transErr.x * tuning.FactorTranslate;
                _a[(int)Params.TRANS_Y, tIdx] = transErr.y * tuning.FactorTranslate;
                _a[(int)Params.TRANS_Z, tIdx] = transErr.z * tuning.FactorTranslate;
                _a[(int)Params.WASTE, tIdx] = waste * tuning.FactorWaste;
                _a[(int)Params.FUDGE, tIdx] = 0.001;
                x[tIdx] = 1;
                bndl[tIdx] = 0;
                bndu[tIdx] = 1;
            }

            const double EPSG = 0.01;
            const double EPSF = 0;
            const double EPSX = 0;
            const double DIFFSTEP = 1.0e-6;
            const int MAXITS = 0;

            alglib.minbleiccreatef(x, DIFFSTEP, out alglib.minbleicstate state);
            alglib.minbleicsetbc(state, bndl, bndu);
            alglib.minbleicsetcond(state, EPSG, EPSF, EPSX, MAXITS);
            alglib.minbleicoptimize(state, cost_func, null, null);
            alglib.minbleicresults(state, out double[] throttles, out alglib.minbleicreport _);

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

    // Immutable so that a task on the solver thread can safely keep using the
    // parameters it was submitted with after they've been replaced.
    // Note that default(RCSSolverTuningParams) is all zeros; use Default instead.
    public readonly struct RCSSolverTuningParams
    {
        public static readonly RCSSolverTuningParams Default = new RCSSolverTuningParams(0.25, 1, 0.005, 1);

        public readonly double WasteThreshold;
        public readonly double FactorTorque;
        public readonly double FactorTranslate;
        public readonly double FactorWaste;

        public RCSSolverTuningParams(double wasteThreshold, double factorTorque, double factorTranslate, double factorWaste)
        {
            WasteThreshold = wasteThreshold;
            FactorTorque = factorTorque;
            FactorTranslate = factorTranslate;
            FactorWaste = factorWaste;
        }
    }

    public class RCSSolverThread
    {
        public double CalculationTime { get; private set; }
        public string StatusString    { get; private set; }
        public string ErrorString     { get; private set; }
        public int    TaskCount       => _tasks.Count + _resultsQueue.Count + (_isWorking ? 1 : 0);
        public int    CacheHits       { get; private set; }
        public int    CacheMisses     { get; private set; }
        public int    CacheSize       => _results.Count;

        private readonly RCSSolver _solver = new RCSSolver();
        private readonly MovingAverage _calculationTime = new MovingAverage();

        private readonly Queue _tasks = Queue.Synchronized(new Queue());
        private readonly AutoResetEvent _workEvent = new AutoResetEvent(false);
        private bool _stopRunning;
        private Thread _t;
        private bool _isWorking;

        // Entries in the results queue have been calculated by the solver thread
        // but not yet added to the results dictionary. GetThrottles() will check
        // the results dictionary first, and then, if no result was found, empty the
        // results queue into the results dictionary.
        private readonly Queue _resultsQueue = Queue.Synchronized(new Queue());
        private readonly Dictionary<RCSSolverKey, double[]> _results = new Dictionary<RCSSolverKey, double[]>();
        private readonly HashSet<RCSSolverKey> _pending = new HashSet<RCSSolverKey>();
        private readonly double[] _double0 = Array.Empty<double>();

        // Only read and replaced on the caller's thread. Each task carries its
        // own reference to the snapshot it was submitted with, so the solver
        // thread never reads this field.
        private RCSSolver.Thruster[] _thrusters = Array.Empty<RCSSolver.Thruster>();
        private RCSSolverTuningParams _tuningParams = RCSSolverTuningParams.Default;

        // Incremented whenever cached results are invalidated. Tasks and results
        // are tagged with the generation they were submitted in, so a result that
        // was still being calculated when the cache was cleared is discarded.
        private int _generation;

        public void UpdateTuningParameters(RCSSolverTuningParams tuningParams)
        {
            _tuningParams = tuningParams;
            ClearResults();
        }

        // Replaces the set of thrusters to solve for. The throttles returned by
        // GetThrottles() are indexed the same way as this array.
        public void SetThrusters(RCSSolver.Thruster[] thrusters)
        {
            _thrusters = thrusters;
            ClearResults();
        }

        public void Start()
        {
            lock (_solver)
            {
                if (_t == null)
                {
                    ClearResults();
                    CacheHits = CacheMisses = 0;
                    _isWorking = false;

                    _stopRunning = false;
                    _t = new Thread(Run);
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
            public readonly Vector3 Direction;
            public readonly Vector3 Rotation;
            public readonly RCSSolver.Thruster[] Thrusters;
            public readonly RCSSolverTuningParams TuningParams;
            public readonly int Generation;

            public SolverTask(RCSSolverKey key, Vector3 direction, Vector3 rotation, RCSSolver.Thruster[] thrusters,
                RCSSolverTuningParams tuningParams, int generation)
            {
                Key = key;
                Direction = direction;
                Rotation = rotation;
                Thrusters = thrusters;
                TuningParams = tuningParams;
                Generation = generation;
            }
        }

        private class SolverResult
        {
            public readonly RCSSolverKey Key;
            public readonly double[] Throttles;
            public readonly int Generation;

            public SolverResult(RCSSolverKey key, double[] throttles, int generation)
            {
                Key = key;
                Throttles = throttles;
                Generation = generation;
            }
        }

        private void ClearResults()
        {
            // A task being worked on right now will have already been removed
            // from 'tasks' and will add its result to the results queue. Bumping
            // the generation makes GetThrottles() discard that stale result.
            _generation++;
            _tasks.Clear();
            _results.Clear();
            _resultsQueue.Clear();
            _pending.Clear();
        }

        // Returns the throttles for 'direction', indexed the same way as the
        // array last passed to SetThrusters(). Returns an empty array if there
        // are no thrusters, the direction is zero, or the result has not been
        // calculated yet.
        // Note that rotation balancing is not supported at the moment.
        public double[] GetThrottles(Vector3 direction)
        {
            Vector3 rotation = Vector3.zero;

            Vector3 dir = direction.normalized;
            var key = new RCSSolverKey(ref dir, rotation);

            double[] throttles;

            if (_thrusters.Length == 0 || direction == Vector3.zero)
            {
                throttles = _double0;
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

                        // Calculated for a thruster set or tuning parameters
                        // that have since been replaced.
                        if (sr.Generation != _generation)
                            continue;

                        _results[sr.Key] = sr.Throttles;
                        _pending.Remove(sr.Key);
                        if (sr.Key.Equals(key))
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
                    _tasks.Enqueue(new SolverTask(key, dir, rotation, _thrusters, _tuningParams, _generation));
                    _workEvent.Set();
                }
            }

            // Return a copy of the array to make sure ours isn't modified.
            return (double[])throttles.Clone();
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

                        double[] throttles = _solver.Run(task.Thrusters, task.Direction, task.Rotation, task.TuningParams);

                        _isWorking = false;
                        _resultsQueue.Enqueue(new SolverResult(task.Key, throttles, task.Generation));

                        _calculationTime.Value = (DateTime.Now - start).TotalSeconds;
                        CalculationTime = _calculationTime;
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

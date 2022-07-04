/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System;
using System.Collections.Generic;

namespace MechJebLib.Primitives
{
    public abstract class HBase<T> : IDisposable
    {
        public double MinTime = double.MaxValue;
        public double MaxTime = double.MinValue;

        private int _lastLo = -1;

        protected readonly SortedList<double, HFrame<T>> _list = new SortedList<double, HFrame<T>>();

        public void Add(double time, T value)
        {
            _list[time] = new HFrame<T>(time, Allocate(value), Allocate(), Allocate(), true);
            MinTime     = Math.Min(MinTime, time);
            MaxTime     = Math.Max(MaxTime, time);
            RecomputeTangents(_list.IndexOfKey(time));
            _lastLo = -1;
        }

        public void Add(double time, T value, T inTangent, T outTangent)
        {
            _list[time] = new HFrame<T>(time, Allocate(value), Allocate(inTangent), Allocate(outTangent));
            MinTime     = Math.Min(MinTime, time);
            MaxTime     = Math.Max(MaxTime, time);
            RecomputeTangents(_list.IndexOfKey(time));
            _lastLo = -1;
        }

        public void Add(double time, T value, T tangent)
        {
            if (_list.ContainsKey(time))
            {
                HFrame<T> temp  = _list.Values[_list.IndexOfKey(time)];
                temp.Value      = Allocate(value);
                temp.OutTangent = tangent;
                _list[time]     = temp;
            }
            else
            {
                Add(time, value, tangent, tangent);
            }
        }

        // checks the most recent bracket first, then the next bracket, then does binary search
        private int FindIndex(double value)
        {
            if (_list.Count == 0)
                return -1;

            if (value <= MinTime)
                return 0;

            if (value >= MaxTime)
                return _list.Count - 1;

            // acceleration for sequential access
            if (_lastLo > 0 && value > _list.Keys[_lastLo])
            {
                if (value < _list.Keys[_lastLo + 1])
                    return ~(_lastLo + 1); // return hi value for a range

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (value == _list.Keys[_lastLo + 1])
                {
                    _lastLo += 1;
                    return _lastLo;
                }

                if (value > _list.Keys[_lastLo + 1] && value < _list.Keys[_lastLo + 2])
                {
                    _lastLo += 1;
                    return ~(_lastLo + 1); // return hi value for a range
                }
            }

            // fall back to binary search
            int lo = 0;
            int hi = _list.Count - 1;

            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                int order = value.CompareTo(_list.Keys[i]);

                if (order == 0)
                {
                    _lastLo = i;
                    return i;
                }

                if (order > 0)
                    lo = i + 1;
                else
                    hi = i - 1;
            }

            _lastLo = lo - 1; // Confusingly: this is the high value now

            return ~lo;
        }

        protected abstract T    Allocate();
        protected abstract T    Allocate(T value);
        protected abstract void Subtract(T a, T b, ref T result);
        protected abstract void Divide(T a, double b, ref T result);
        protected abstract void Multiply(T a, double b, ref T result);
        protected abstract void Addition(T a, T b, ref T result);

        // FIXME: we need to average the tangents on either side
        private void RecomputeTangents(int i)
        {
            // if there is only one
            if (_list.Count == 1)
            {
                HFrame<T> temp = _list.Values[0];
                if (temp.AutoTangent)
                {
                    temp.InTangent   = temp.OutTangent = Allocate();
                    _list[temp.Time] = temp;
                }

                return;
            }

            // fix the current one
            FixTangent(i);

            // fix left side
            if (i != 0)
                FixTangent(i - 1);

            // fix right side
            if (i != _list.Count - 1)
                FixTangent(i+1);
        }

        private void FixTangent(int i)
        {
            HFrame<T> current = _list.Values[i];

            if (!current.AutoTangent)
                return;
            
            T slope1 = Allocate();

            if (i < _list.Count - 1)
            {
                // there is a right side
                HFrame<T> right = _list.Values[i + 1];
                Subtract(right.Value, current.Value, ref slope1);
                Divide(slope1, right.Time - current.Time, ref slope1);

                if (i == 0)
                {
                    // there is no left
                    current.InTangent = current.OutTangent = slope1;
                    _list[current.Time] = current;
                    return;
                }
            }
            
            T slope2 = Allocate();

            if (i > 0)
            {
                // there is a left side
                HFrame<T> left = _list.Values[i-1];

                Subtract(current.Value, left.Value, ref slope2);
                Divide(slope2, current.Time - left.Time, ref slope2);

                if (i == _list.Count - 1)
                {
                    // there is no right
                    current.InTangent   = current.OutTangent = slope2;
                    _list[current.Time] = current;
                    return;
                }
            }
            
            // there is a left and a right, so average them
            Addition(slope1, slope2, ref slope1);
            Divide(slope1, 2.0, ref slope1);
            current.InTangent   = current.OutTangent = slope1;
            _list[current.Time] = current;
        }

        protected abstract T Interpolant(double x1, T y1, T yp1, double x2, T y2, T yp2, double x);

        public T Evaluate(double t)
        {
            if (_list.Count == 0)
                return Allocate();

            T ret = Allocate();
            
            if (t < MinTime)
            {
                
                Multiply(_list.Values[0].InTangent, MinTime - t, ref ret);
                Subtract(_list.Values[0].Value, ret, ref ret);
                return ret;
            }

            if (t > MaxTime)
            {
                Multiply(_list.Values[_list.Count - 1].OutTangent, t - MaxTime, ref ret);
                Addition(_list.Values[_list.Count - 1].Value, ret, ref ret);
                return ret;
            }

            int hi = FindIndex(t);

            if (hi >= 0)
                return Allocate(_list.Values[hi].Value);

            hi = ~hi;

            HFrame<T> testKeyframe = _list.Values[hi - 1];
            HFrame<T> testKeyframe2 = _list.Values[hi];

            return Interpolant(testKeyframe.Time, testKeyframe.Value, testKeyframe.OutTangent,
                testKeyframe2.Time, testKeyframe2.Value, testKeyframe2.InTangent, t);
        }

        public virtual void Clear()
        {
            _list.Clear();
        }

        public virtual void Dispose()
        {
            Clear();
        }
    }

    public struct HFrame<T>
    {
        public T InTangent;
        public T OutTangent;

        public readonly double Time;

        // FIXME: we don't need to support InValue/OutValue
        public  T    Value;
        public readonly bool AutoTangent;

        public HFrame(double time, T value, T inTangent, T outTangent, bool autoTangent = false)
        {
            Time        = time;
            Value       = value;
            InTangent   = inTangent;
            OutTangent  = outTangent;
            AutoTangent = autoTangent;
        }
    }
}

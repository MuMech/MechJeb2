using System;
using System.Collections;
using System.Collections.Generic;

namespace MuMech.MathJ
{
    public interface ICurve<out T>
    {
        double MinTime { get; }
        double MaxTime { get; }
        int    Count   { get; }

        T Evaluate(double time);
        // IEnumerator<Curve<T>.Keyframe<T>> GetEnumerator();
    }

    public abstract class Curve<T> : ICurve<T>, IEnumerable<Curve<T>.Keyframe<T>>
    {
        protected readonly AVL<double, Keyframe<T>> Keyframes = new AVL<double, Keyframe<T>>();

        public double MinTime => Keyframes.FirstData.Time;
        public double MaxTime => Keyframes.LastData.Time;

        public int Count => Keyframes.Count;

        public virtual void Add(double time, T value, T tangent)
        {
            if (Keyframes.TryGetValue(time, out Keyframe<T> old))
            {
                old.OutTangent = tangent;
                old.OutValue   = value;
                Keyframes.Replace(time, old);
            }
            else
            {
                Keyframe<T> frame = Keyframe<T>.Get(time, value, tangent, tangent);

                Keyframes.Add(frame);
            }
        }

        public void Add(Keyframe<T> frame)
        {
            Keyframes.Add(frame);
        }

        protected abstract T EvaluationFunction(double x1, T y1, T yp1, double wt1, double x2, T y2, T yp2, double wt2, double x);

        public virtual T Evaluate(double time)
        {
            (Keyframe<T> min, Keyframe<T> max) = Keyframes.FindRange(time);

            return EvaluationFunction(min.Time, min.OutValue, min.OutTangent, min.OutWeight, max.Time, max.InValue, max.InTangent, max.InWeight,
                time);
        }

        public virtual void Clear()
        {
            foreach (Keyframe<T> keyframe in Keyframes)
                keyframe.Dispose();

            Keyframes.Clear();
        }

        public class Keyframe<T2> : IExtendedComparable<Keyframe<T2>, double>, IDisposable
        {
            private static readonly ObjectPool<Keyframe<T2>> KeyframePool = new ObjectPool<Keyframe<T2>>();

            public double Time;
            public T2     InValue    = default!;
            public T2     OutValue   = default!;
            public T2     InTangent  = default!;
            public T2     OutTangent = default!;
            public double InWeight;
            public double OutWeight;

            private void Update(double time, T2 value, T2 inTangent, T2 outTangent, double inWeight = 1 / 3d, double outWeight = 1 / 3d)
            {
                Update(time, value, value, inTangent, outTangent, inWeight, outWeight);
            }

            private void Update(double time, T2 inValue, T2 outValue, T2 inTangent, T2 outTangent, double inWeight = 1 / 3d,
                double outWeight = 1 / 3d)
            {
                Time       = time;
                InValue    = inValue;
                OutValue   = outValue;
                InTangent  = inTangent;
                OutTangent = outTangent;
                InWeight   = inWeight;
                OutWeight  = outWeight;
            }

            public int CompareTo(Keyframe<T2> other)
            {
                return Time.CompareTo(other.Time);
            }

            public int CompareTo(double other)
            {
                return Time.CompareTo(other);
            }

            public void Dispose()
            {
                if (InValue is double[] invalue)
                    Utils.DoublePool.Return(invalue);
                if (OutValue is double[] outvalue)
                    Utils.DoublePool.Return(outvalue);
                if (InTangent is double[] intangent)
                    Utils.DoublePool.Return(intangent);
                if (OutTangent is double[] outtangent)
                    Utils.DoublePool.Return(outtangent);
                KeyframePool.Return(this);
            }

            public static Keyframe<T2> Get(double time, T2 inValue, T2 outValue, T2 inTangent, T2 outTangent, double inWeight = 1 / 3d,
                double outWeight = 1 / 3d)
            {
                Keyframe<T2> keyframe = KeyframePool.Get();
                keyframe.Update(time, inValue, outValue, inTangent, outTangent, inWeight, outWeight);
                return keyframe;
            }

            public static Keyframe<T2> Get(double time, T2 value, T2 inTangent, T2 outTangent)
            {
                Keyframe<T2> keyframe = KeyframePool.Get();
                keyframe.Update(time, value, inTangent, outTangent);
                return keyframe;
            }
        }

        public IEnumerator<Keyframe<T>> GetEnumerator()
        {
            return Keyframes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

namespace MuMech.MathJ
{
    public class CN : Curve<double[]>
    {
        public  int  AutotangentOffset = -1;
        private bool _appliedAutoTangent;

        public CN(int n)
        {
            N = n;
        }

        public int N { get; }

        public override void Add(double time, double[] value, double[] tangent)
        {
            if (Keyframes.TryGetValue(time, out Keyframe<double[]> old))
            {
                old.OutTangent = tangent;
                old.OutValue   = value;
                Keyframes.Replace(time, old);
            }
            else
            {
                double[] invalue = Utils.DoublePool.Rent(N);
                double[] intangent = Utils.DoublePool.Rent(N);
                double[] outvalue = Utils.DoublePool.Rent(N);
                double[] outtangent = Utils.DoublePool.Rent(N);

                for (int i = 0; i < N; i++)
                {
                    invalue[i]   = outvalue[i]   = value[i];
                    intangent[i] = outtangent[i] = tangent[i];
                }

                Keyframe<double[]> frame = Keyframe<double[]>.Get(time, invalue, outvalue, intangent, outtangent);

                Keyframes.Add(frame);
            }

            _appliedAutoTangent = false;
        }

        private void EvaluationFunction(double x1, double[] y1, double[] yp1, double wt1, double x2, double[] y2, double[] yp2, double wt2, double x,
            double[] y)
        {
            Functions.AnimationCurveInterpolant(N, x1, y1, yp1, wt1, x2, y2, yp2, wt2, x, y);
        }

        /// <summary>
        ///     Applies the autotangent generation to all keyframes for values >= the autotangent offset.
        /// </summary>
        private void ApplyAutoTangent()
        {
            Keyframe<double[]> lastKeyframe = Keyframes.FirstData;

            // FIXME: for now all we do is LERP, fitting a cubic spline to possibly irregular data is a bit of a PITA
            foreach (Keyframe<double[]> keyframe in Keyframes)
            {
                if (keyframe.CompareTo(Keyframes.FirstData) == 0)
                    for (int i = AutotangentOffset; i < N; i++)
                    {
                        double slope = (keyframe.InValue[i] - lastKeyframe.OutValue[i]) / (keyframe.Time - lastKeyframe.Time);
                        keyframe.InTangent[i]      = slope;
                        lastKeyframe.OutTangent[i] = slope;

                        if (lastKeyframe == Keyframes.FirstData)
                            lastKeyframe.InTangent[i] = slope;
                        if (keyframe == Keyframes.LastData)
                            keyframe.OutTangent[i] = slope;
                    }

                lastKeyframe = keyframe;
            }

            _appliedAutoTangent = true;
        }

        public override double[] Evaluate(double time)
        {
            if (AutotangentOffset > -1 && !_appliedAutoTangent)
                ApplyAutoTangent();

            (Keyframe<double[]> min, Keyframe<double[]> max) = Keyframes.FindRange(time);

            double[] y = Utils.DoublePool.Rent(N);

            EvaluationFunction(min.Time, min.OutValue, min.OutTangent, min.OutWeight, max.Time, max.InValue, max.InTangent, max.InWeight, time, y);

            return y;
        }

        protected override double[] EvaluationFunction(double x1, double[] y1, double[] yp1, double wt1, double x2, double[] y2, double[] yp2,
            double wt2, double x)
        {
            return Functions.AnimationCurveInterpolant(N, x1, y1, yp1, wt1, x2, y2, yp2, wt2, x);
        }

        public static Curve<Vector3d>.Keyframe<Vector3d> ExtractVectorKeyFrame(Keyframe<double[]> old, int startOffset)
        {
            Curve<Vector3d>.Keyframe<Vector3d> frame = Curve<Vector3d>.Keyframe<Vector3d>.Get(
                old.Time,
                new Vector3d(old.InValue[startOffset], old.InValue[startOffset + 1], old.InValue[startOffset + 2]),
                new Vector3d(old.OutValue[startOffset], old.OutValue[startOffset + 1], old.OutValue[startOffset + 2]),
                new Vector3d(old.InTangent[startOffset], old.InTangent[startOffset + 1], old.InTangent[startOffset + 2]),
                new Vector3d(old.OutTangent[startOffset], old.OutTangent[startOffset + 1], old.OutTangent[startOffset + 2]),
                old.InWeight,
                old.OutWeight
            );

            return frame;
        }

        public static Curve<double>.Keyframe<double> ExtractScalarKeyFrame(Keyframe<double[]> old, int startOffset)
        {
            Curve<double>.Keyframe<double> frame = Curve<double>.Keyframe<double>.Get(
                old.Time,
                old.InValue[startOffset],
                old.OutValue[startOffset],
                old.InTangent[startOffset],
                old.OutTangent[startOffset],
                old.InWeight,
                old.OutWeight
            );

            return frame;
        }

        public override void Clear()
        {
            base.Clear();
            _appliedAutoTangent = false;
            AutotangentOffset   = -1;
        }
    }
}

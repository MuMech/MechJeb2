/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Interpolants;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MechJebLibBindings
{
    public static class FloatCurveExtensions
    {
        public static void LoadFromFloatCurve(this DoubleInterpolant h, FloatCurve? f)
        {
            h.Clear();

            if (f == null) return;

            double lastValue = double.NaN;
            double lastTime = double.NaN;
            double lastOutTangent = double.NaN;

            // Unity out-of-bounds behavior is to reproduce the constant value, which we do here by using initial and
            // final constant nodes.  This also correctly reproduces a FloatCurve with a single frame that has no
            // interpolation.
            foreach (Keyframe frame in f.Curve.keys)
            {
                if (IsFinite(lastValue))
                    h.Append(CubicHermiteDoubleNode.Rent(lastTime, frame.time - lastTime, lastValue, lastOutTangent, frame.value, frame.inTangent), lastTime, frame.time);
                else
                    h.Append(ConstantDoubleNode.Rent(frame.value), frame.time, frame.time);
                lastValue = frame.value;
                lastTime = frame.time;
                lastOutTangent = frame.outTangent;
            }
            if (IsFinite(lastValue))
                h.Append(ConstantDoubleNode.Rent(lastValue), lastTime, lastTime);
        }
    }
}

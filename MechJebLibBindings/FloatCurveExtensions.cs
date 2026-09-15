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

            foreach (Keyframe frame in f.Curve.keys)
            {
                // convert from frames to nodes.
                // XXX: if the first or last frame in the FloatCurve has intangent != outtangent we don't reproduce correct
                // out-of-bounds behavior (Q: do FloatCurves support out of bounds behavior?)
                if (IsFinite(lastValue))
                    h.Append(CubicHermiteDoubleNode.Rent(frame.time, lastTime - frame.time, lastValue, lastOutTangent, frame.value, frame.inTangent), frame.time);
                lastValue = frame.value;
                lastTime = frame.time;
                lastOutTangent = frame.outTangent;
            }
        }
    }
}

/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */
﻿using MechJebLib.Primitives;
using UnityEngine;

namespace MechJebLibBindings
{
    public static class H1Extensions
    {
        public static void LoadH1(this H1 h, FloatCurve f)
        {
            h.Clear();

            if (f == null) return;

            foreach (Keyframe frame in f.Curve.keys)
                h.Add(frame.time, frame.value, frame.inTangent, frame.outTangent);
        }
    }
}

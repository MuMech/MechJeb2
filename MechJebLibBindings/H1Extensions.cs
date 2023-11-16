using MechJebLib.Primitives;
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

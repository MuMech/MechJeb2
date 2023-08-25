using MechJebLib.Core;
using MechJebLib.Core.TwoBody;
using MechJebLib.Primitives;
using MechJebLib.Utils;

namespace MechJebLib.Maneuvers
{
    public class Simple
    {
        public static V3 DeltaVRelativeToCircularVelocity(double mu, V3 r, V3 v, double n = 1.0)
        {
            Check.Finite(mu);
            Check.Finite(r);
            Check.Finite(v);
            Check.Positive(mu);
            Check.NonZero(r);

            var h = V3.Cross(r, v);
            return n * Maths.CircularVelocityFromHvec(mu, r, h) - v;
        }

        public static V3 DeltaVToCircularize(double mu, V3 r, V3 v)
        {
            Check.Finite(mu);
            Check.Finite(r);
            Check.Finite(v);
            Check.Positive(mu);
            Check.NonZero(r);

            var h = V3.Cross(r, v);
            return Maths.CircularVelocityFromHvec(mu, r, h) - v;
        }

        public static V3 DeltaVToCircularizeAfterTime(double mu, V3 r, V3 v, double dt)
        {
            Check.Finite(mu);
            Check.Finite(r);
            Check.Finite(v);
            Check.Finite(dt);
            Check.Positive(mu);
            Check.NonZero(r);

            (V3 r1, V3 v1) = Shepperd.Solve(mu, dt, r, v);
            var h = V3.Cross(r1, v1);
            return Maths.CircularVelocityFromHvec(mu, r, h) - v1;
        }

        public static (V3 dv, double dt) ManeuverToCircularizeAtPeriapsis(double mu, V3 r, V3 v)
        {
            Check.Finite(mu);
            Check.Finite(r);
            Check.Finite(v);
            Check.Positive(mu);
            Check.NonZero(r);

            double dt = Maths.TimeToNextPeriapsis(mu, r, v);

            Check.Finite(dt);

            return (DeltaVToCircularizeAfterTime(mu, r, v, dt), dt);
        }

        public static (V3 dv, double dt) ManeuverToCircularizeAtApoapsis(double mu, V3 r, V3 v)
        {
            Check.Finite(mu);
            Check.Finite(r);
            Check.Finite(v);
            Check.Positive(mu);
            Check.NonZero(r);

            double dt = Maths.TimeToNextApoapsis(mu, r, v);

            Check.Finite(dt);

            return (DeltaVToCircularizeAfterTime(mu, r, v, dt), dt);
        }

        public static V3 DeltaVToChangeInclination(V3 r, V3 v, double newInc) => Maths.VelocityForInclination(r, v, newInc) - v;

        public static V3 DeltaVToChangeFPA(V3 r, V3 v, double newFPA) => Maths.VelocityForFPA(r, v, newFPA) - v;
    }
}

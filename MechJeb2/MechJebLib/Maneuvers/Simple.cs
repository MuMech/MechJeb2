using MechJebLib.Core;
using MechJebLib.Core.TwoBody;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static System.Math;
using static MechJebLib.Utils.Statics;

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

        public static V3 DeltaVToEllipticize(double mu, V3 r, V3 v, double newPeR, double newApR)
        {
            double rm = r.magnitude;
            // orbital energy
            double e = -mu / (newPeR + newApR);
            // orbital angular momentum
            double l = Sqrt(Abs((Powi(e * (newApR - newPeR), 2) - mu * mu) / (2 * e)));
            // orbital kinetic energy
            double ke = e + mu / rm;

            // radial/transverse as used in RSW, see Vallado
            double vtransverse = l / rm;
            double vradial = Sqrt(Abs(2 * ke - vtransverse * vtransverse));

            V3 radialhat = r.normalized;
            V3 transversehat = V3.Cross(V3.Cross(radialhat, v), radialhat).normalized;

            V3 one = vtransverse * transversehat + vradial * radialhat - v;
            V3 two = vtransverse * transversehat - vradial * radialhat - v;

            return one.magnitude < two.magnitude ? one : two;
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

using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using MechJebLib.Utils;
using static System.Math;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.Maneuvers
{
    public class Simple
    {
        public static V3 DeltaVRelativeToCircularVelocity(double mu, V3 r, V3 v, double n = 1.0)
        {
            Check.PositiveFinite(mu);
            Check.Finite(v);
            Check.NonZeroFinite(r);

            var h = V3.Cross(r, v);
            return n * Astro.CircularVelocityFromHvec(mu, r, h) - v;
        }

        public static V3 DeltaVToCircularize(double mu, V3 r, V3 v)
        {
            Check.PositiveFinite(mu);
            Check.Finite(v);
            Check.NonZeroFinite(r);

            var h = V3.Cross(r, v);
            return Astro.CircularVelocityFromHvec(mu, r, h) - v;
        }

        public static V3 DeltaVToEllipticize(double mu, V3 r, V3 v, double newPeR, double newApR)
        {
            Check.PositiveFinite(mu);
            Check.NonZeroFinite(r);
            Check.Finite(v);
            Check.PositiveFinite(newPeR);
            Check.Finite(newApR);

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

            Check.Finite(one);
            Check.Finite(two);

            return one.magnitude < two.magnitude ? one : two;
        }

        public static V3 DeltaVToCircularizeAfterTime(double mu, V3 r, V3 v, double dt)
        {
            Check.PositiveFinite(mu);
            Check.NonZeroFinite(r);
            Check.Finite(v);
            Check.Finite(dt);

            (V3 r1, V3 v1) = Shepperd.Solve(mu, dt, r, v);
            var h = V3.Cross(r1, v1);
            return Astro.CircularVelocityFromHvec(mu, r, h) - v1;
        }

        public static (V3 dv, double dt) ManeuverToCircularizeAtPeriapsis(double mu, V3 r, V3 v)
        {
            Check.PositiveFinite(mu);
            Check.NonZeroFinite(r);
            Check.Finite(v);

            double dt = Astro.TimeToNextPeriapsis(mu, r, v);

            Check.Finite(dt);

            return (DeltaVToCircularizeAfterTime(mu, r, v, dt), dt);
        }

        public static (V3 dv, double dt) ManeuverToCircularizeAtApoapsis(double mu, V3 r, V3 v)
        {
            Check.PositiveFinite(mu);
            Check.NonZeroFinite(r);
            Check.Finite(v);

            double dt = Astro.TimeToNextApoapsis(mu, r, v);
            V3 dv = DeltaVToCircularizeAfterTime(mu, r, v, dt);

            Check.Finite(dv);
            Check.Finite(dt);

            return (dv, dt);
        }

        // simple proportional yaw guidance for non-hyperbolic target orbits
        public static V3 VelocityForLaunchInclination(double mu, V3 r, V3 v, double newInc, double rotFreq)
        {
            Check.PositiveFinite(mu);
            Check.NonZeroFinite(r);
            Check.Finite(v);
            Check.Finite(newInc);

            // as long as we're not launching to hyperbolic orbits, this vgo will always point
            // in 'front' of us.
            V3 v1 = Astro.EscapeVelocityForInclination(mu, r, newInc);
            V3 v2 = Astro.EscapeVelocityForInclination(mu, r, -newInc);
            V3 dv1 = v1 - v;
            V3 dv2 = v2 - v;

            V3 dv = dv1.magnitude < dv2.magnitude ? dv1 : dv2;

            // if the surface horizontal velocity is very small and the dv magnitudes are nearly equal, then
            // assume we are doing vertical rise from launch and the sign of newInc determines if we take the
            // northward or southward going track.
            V3 rhat = r.normalized;
            V3 vsurf = v - V3.Cross(rotFreq * V3.northpole, r);
            V3 vhoriz = vsurf - V3.Dot(vsurf, rhat) * rhat;
            if (vhoriz.magnitude / v1.magnitude < 0.05 && Abs(dv1.magnitude - dv2.magnitude) / dv1.magnitude < 0.05)
                dv = dv1;

            Check.Finite(dv);

            return dv;
        }

        public static double HeadingForLaunchInclination(double mu, V3 r, V3 v, double newInc, double rotFreq) =>
            Astro.HeadingForVelocity(r, VelocityForLaunchInclination(mu, r, v, newInc, rotFreq));

        public static V3 DeltaVToChangeInclination(V3 r, V3 v, double newInc)
        {
            Check.NonZeroFinite(r);
            Check.Finite(v);
            Check.Finite(newInc);

            V3 dv = Astro.VelocityForInclination(r, v, newInc) - v;

            Check.Finite(dv);

            return dv;
        }

        public static V3 DeltaVToChangeFPA(V3 r, V3 v, double newFPA)
        {
            Check.NonZeroFinite(r);
            Check.Finite(v);
            Check.Finite(newFPA);

            V3 dv = Astro.VelocityForFPA(r, v, newFPA) - v;

            Check.Finite(dv);

            return dv;
        }
    }
}

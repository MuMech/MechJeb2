using System;
using UnityEngine;

/*
 * This is the Space-Shuttle Conic State Extrpolator
 *
 * I belive it is Goodyear's universal variables with Battin's normalization.
 *
 * The main benefit over KSP's Orbit class is its a bit faster, and there's no gawdawful
 * Inverse Rotation issues in the atmosphere.
 */

namespace MuMech
{
    public static class ConicStateUtils
    {
        public struct CSER
        {
            public double dtcp;
            public double xcp;
        }

        public class ConicStateException : Exception {}

        public class ConicStateBadArguments : ConicStateException {}

        /*
         * mu - gravitational parameter
         * r0 - starting radius vector
         * v0 - starting velocity vector
         * dt - time of extrapolation
         * last - prior state for sequential calls, zero'd to initialize
         * r - predicted radius vector
         * v - predicted velocity vector
         */

        public static void CSE(double mu, Vector3d r0, Vector3d v0, double dt, out Vector3d r, out Vector3d v)
        {
            CSER last = new CSER();
            CSE(mu, r0, v0, dt, ref last, out r, out v);
        }

        public static void CSE(double mu, Vector3d r0, Vector3d v0, double dt, ref CSER last, out Vector3d r, out Vector3d v)
        {
            if (!r0.magnitude.IsFinite() || !v0.magnitude.IsFinite() || !dt.IsFinite() || !mu.IsFinite())
                throw new ConicStateBadArguments();

            double dtcp = last.dtcp;

            if ( last.dtcp == 0.0D )
                dtcp = dt;

            double xcp = last.xcp;
            double x = xcp;
            double A, D, E;

            int kmax = 10;
            int imax = 10;

            double f0;

            if ( dt >= 0 )
                f0 = 1;
            else
                f0 = -1;

            int n = 0;
            double r0m = r0.magnitude;

            double f1 = f0 * Math.Sqrt(r0m/mu);
            double f2 = 1/f1;
            double f3 = f2/r0m;
            double f4 = f1*r0m;
            double f5 = f0/Math.Sqrt(r0m);
            double f6 = f0*Math.Sqrt(r0m);

            Vector3d ir0 = r0/r0m;
            Vector3d v0s = f1*v0;
            double sigma0s = Vector3d.Dot(ir0,v0s);
            double b0 = Vector3d.Dot(v0s,v0s)-1;
            double alphas = 1-b0;  // reciprocal of the noramlized sma = r0m / sma = 2 - r0m * v0m * v0m / mu

            double xguess = f5*x;
            double xlast = f5*xcp;
            double xmin = 0;
            double dts = f3*dt;
            double dtlast = f3*dtcp;
            double dtmin = 0;

            /* Math.Sqrt(Math.Abs(alphas)) < (small number) check is missing for parabolic orbits */

            double xmax = 2*Math.PI/Math.Sqrt(Math.Abs(alphas));

            double dtmax;
            double xP = 0.0;
            double Ps = 0.0;

            if ( alphas>0 )
            {
                // circular/elliptical orbit
                dtmax = xmax/alphas;
                xP = xmax;
                Ps = dtmax;
                while ( dts>=Ps )
                {
                    // 1 or more orbits into the future
                    n = n+1;
                    dts = dts-Ps;
                    dtlast = dtlast-Ps;
                    xguess = xguess-xP;
                    xlast = xlast-xP;
                }
            }
            else
            {
                // hyperbolic orbit
                KTTI(xmax, sigma0s, alphas, kmax, out dtmax, out A, out D, out E);
                while ( dtmax < dts )
                {
                    dtmin = dtmax;
                    xmin = xmax;
                    xmax = 2 * xmax;
                    KTTI(xmax, sigma0s, alphas, kmax, out dtmax, out A, out D, out E);
                }
            }

            if ( (xguess<=xmin) || (xguess>=xmax) )
                xguess = ( xmin + xmax ) / 2.0;

            double dtguess;

            KTTI(xguess, sigma0s, alphas, kmax, out dtguess, out A, out D, out E);

            if ( dts<dtguess )
            {
                if ( (xlast>xguess) && (xlast<xmax) && (dtlast>dtguess) && (dtlast<dtmax) )
                {
                    xmax = xlast;
                    dtmax = dtlast;
                }
            }
            else
            {
                if ( (xlast>xmin) && (xlast<xguess) && (dtlast>dtmin) && (dtlast<dtguess) )
                {
                    xmin = xlast;
                    dtmin = dtlast;
                }
            }

            Kepler(imax, dts, ref xguess, ref dtguess, xmin, dtmin, xmax, dtmax, sigma0s, alphas, kmax, out A, out D, out E);

            double rs = 1 + 2*(b0*A + sigma0s*D*E);
            double b4 = 1/rs;

            double xc = f6*(xguess+n*xP);
            double dtc = f4*(dtguess+n*Ps);

            last.dtcp = dtc;
            last.xcp = xc;
            /* last.A = A;
            last.D = D;
            last.E = E; */

            double F = 1 - 2*A;
            double Gs = 2*(D*E + sigma0s*A);
            double Fts = -2*b4*D*E;
            double Gt = 1 - 2*b4*A;

            r = r0m*(F*ir0 + Gs*v0s);
            v = f2*(Fts*ir0 + Gt*v0s);
        }

        static void KTTI(double xarg, double s0s, double a, int kmax, out double t, out double A, out double D, out double E)
        {
            double u1 = USS(xarg, a, kmax);

            double zs = 2*u1;
            E = 1 - 0.5*a*zs*zs;
            if ( ((1.0+E)/2.0) < 0 )
            {
                Debug.Log("uh oh!");
            }
            double w = Math.Sqrt( (1.0+E)/2.0 );
            D = w*zs;
            A = D*D;
            double B = 2*(E+s0s*D);

            double Q = QCF(w);

            t = D*(B+A*Q);
        }

        static double USS(double xarg, double a, int kmax)
        {
            double du1 = xarg/4.0;
            double u1 = du1;
            double f7 = -a * du1*du1;
            double k=3;

            double u1old = u1;
            du1 = f7*du1 / (k*(k-1));
            u1 = u1+du1;

            while ( (u1 != u1old) && (k < kmax) )
            {
                k=k+2;
                du1 = f7*du1 / (k*(k-1));
                u1old = u1;
                u1 = u1+du1;
            }
            return u1;
        }

        static double QCF(double w)
        {
            double xq;

            /* FIXME: check for w < epsilon is missing */

            if ( w < 1 )
                xq=21.04-13.04*w;
            else if ( w < 4.625 )
                xq = (5/3)*(2*w+5);
            else if ( w < 13.846 )
                xq = (10/7)*(w+12);
            else if ( w < 44 )
                xq = 0.5*(w+60);
            else if ( w < 100 )
                xq = 0.25*(w+164);
            else
                xq = 70;

            double b=0;
            double y=(w-1)/(w+1);
            int j= (int)Math.Floor(xq);

            b=y/(1+(j-1)/(j+2)*(1-b));
            while ( j > 2 )
            {
                j=j-1;
                b=y/(1+(j-1)/(j+2)*(1-b));
            }

            return 1.0/(w*w) * ( 1.0 + ( 2.0 - b/2.0 ) / ( 3*w*(w+1.0) ) );
        }

        static void Kepler(int imax, double dts, ref double xguess, ref double dtguess, double xmin, double dtmin, double xmax,
                double dtmax, double s0s, double a, int kmax, out double A, out double D, out double E)
        {
            A = D = E = 0.0;

            for(int i = 1; i < imax; i++)
            {
                double dterror = dts - dtguess;

                if ( Math.Abs(dterror) < 1E-6 )
                    break;

                double dxs;

                Secant(dterror, xguess, dtguess, ref xmin, ref dtmin, ref xmax, ref dtmax, out dxs);
                double xold = xguess;
                xguess = xguess + dxs;

                if ( xguess == xold )
                    break;

                double dtold = dtguess;

                KTTI(xguess, s0s, a, kmax, out dtguess, out A, out D, out E);

                if ( dtguess == dtold )
                    break;
            }
        }

        static void Secant(double dterror, double xguess, double dtguess, ref double xmin, ref double dtmin, ref double xmax,
                ref double dtmax, out double dxs)
        {
            double eps = 1E-6;
            double dtminp = dtguess-dtmin;
            double dtmaxp = dtguess-dtmax;

            if ( Math.Abs(dtminp) < eps || Math.Abs(dtmaxp) < eps )
            {
                dxs = 0;
            }
            else
            {
                if ( dterror < 0 )
                {
                    dxs = ( xguess-xmax ) * ( dterror/dtmaxp );
                    if ( ( xguess + dxs ) <= xmin )
                        dxs = ( xguess-xmin ) * ( dterror/dtminp );
                    xmax = xguess;
                    dtmax = dtguess;
                }
                else
                {
                    dxs = ( xguess-xmin ) * ( dterror/dtminp );
                    if ( ( xguess+dxs ) >= xmax )
                       dxs = ( xguess-xmax ) * ( dterror/dtmaxp );
                    xmin = xguess;
                    dtmin = dtguess;
                }
            }
        }
    }
}

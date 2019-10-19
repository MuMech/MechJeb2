using System;
using UnityEngine;
using System.Collections.Generic;

namespace MuMech {
    public class Shepperd {
        public static void Solve(double mu, double tau, Vector3d ri, Vector3d vi, out Vector3d rf, out Vector3d vf)
        {
            double tolerance = 1.0e-12;
            double u = 0;
            int  imax = 50;
            double umax = Double.MaxValue;
            double umin = Double.MinValue;
            double orbits = 0;
            double tdesired = tau;
            double threshold = tolerance * Math.Abs(tdesired);
            double r0 = ri.magnitude;
            double n0 = Vector3d.Dot(ri, vi);

            double beta = 2.0 * (mu / r0) - vi.sqrMagnitude;

            if (beta != 0.0)
            {
                umax = 1.0 / Math.Sqrt(Math.Abs(beta));
                umin = -1.0 / Math.Sqrt(Math.Abs(beta));
            }

            if (beta > 0.0)
            {
                orbits = beta * tau - 2 * n0;
                orbits = 1 + (orbits * Math.Sqrt(beta)) / (Math.PI * mu);
                orbits = Math.Floor(orbits / 2);
            }

            double uold = Double.MinValue;
            double dtold = Double.MinValue;
            double u0;
            double u1 = 0.0;
            double u2 = 0.0;
            double u3;
            double r1 = 0.0;

            double q, n, r, l, s, d, gcf, k, gold, dt, slope, terror, ustep, h0, h1;

            for(int i = 1; i < imax; i++ )
            {
                q = beta * u * u;

                q = q / (1.0 + q);

                n = 0;
                r = 1;
                l = 1;
                s = 1;
                d = 3;
                gcf = 1;
                k = -5;

                gold = 0;

                while (gcf != gold)
                {
                    k = -k;
                    l = l + 2;
                    d = d + 4 * l;
                    n = n + (1 + k) * l;
                    r = d / (d - n * r * q);
                    s = (r - 1) * s;
                    gold = gcf;
                    gcf  = gold + s;
                }

                h0 = 1 - 2 * q;
                h1 = 2 * u * (1 - q);
                u0 = 2 * h0 * h0 - 1;
                u1 = 2 * h0 * h1;
                u2 = 2 * h1 * h1;
                u3 = 2 * h1 * u2 * gcf / 3;

                if (orbits != 0)
                {
                    u3 = u3 + 2 * Math.PI * orbits / (beta * Math.Sqrt(beta));
                }

                r1 = r0 * u0 + n0 * u1 + mu * u2;
                dt = r0 * u1 + n0 * u2 + mu * u3;
                slope = 4 * r1 / (1 + beta * u * u);

                terror = tdesired - dt;

                if (Math.Abs(terror) < threshold)
                    break;

                if ((i > 1) && (u == uold) )
                    break;

                if ((i > 1) && (dt == dtold) )
                    break;

                uold  = u;
                dtold = dt;

                ustep = terror / slope;

                if (ustep > 0)
                {
                    umin = u;
                    u = u + ustep;

                    if (u > umax)
                    {
                        u = (umin + umax) / 2;
                    }
                } else {
                    umax = u;
                    u = u + ustep;

                    if (u < umin)
                    {
                        u = (umin + umax) / 2;
                    }
                }
                if (i == imax)
                {
                    // FIXME: throw
                }
            }

            double f = 1.0 - (mu / r0) * u2;
            double gg = 1.0 - (mu / r1) * u2;
            double g  =  r0 * u1 + n0 * u2;
            double ff = -mu * u1 / (r0 * r1);

            rf = new Vector3d();
            vf = new Vector3d();

            for(int i = 0; i < 3; i++)
            {
                rf[i] = f  * ri[i] + g  * vi[i];
                vf[i] = ff * ri[i] + gg * vi[i];
            }
        }
    }
}

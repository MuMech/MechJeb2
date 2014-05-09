using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    ///////////////////////////////////////////////////////////
    // LambertSolver kindly provided by Arrowstar - Thanks!  //
    ///////////////////////////////////////////////////////////

    //Solves Lambert's problem, namely:
    //  "What orbit around primary takes you from position R1 to position R2 in a time interval dt?" 
    //Output is in the form of two velocity vectors V1 and V2, which respectively give the initial and final velocity
    //of the required orbit segment
    //There are always two solutions to Lambert's problem, a "short way," which traverses < 180 degrees, 
    //and a "long way," which traverses > 180 degrees. The shortway input says which of these solutions to find.
    public class LambertSolver
    {
        public static void Solve(Vector3d R1, Vector3d R2, double dt, CelestialBody primary, bool shortway, out Vector3d V1, out Vector3d V2)
        {
            double muCB = primary.gravParameter;

            double R1mag = R1.magnitude;
            double R2mag = R2.magnitude;

            double tm;
            if (shortway)
            {
                tm = 1.0;
            }
            else
            {
                tm = -1.0;
            }

            double cosDeltaTA = (Vector3d.Dot(R1, R2)) / (R1mag * R2mag);
            double sinDeltaTA = tm * Math.Sqrt(1 - Math.Pow(cosDeltaTA, 2));
            double deltaTA = Math.Atan2(sinDeltaTA, cosDeltaTA);
            if (deltaTA < 0)
            {
                deltaTA = deltaTA + 2 * Math.PI;
            }

            double c = Math.Sqrt(Math.Pow(R1mag, 2) + Math.Pow(R2mag, 2) - 2 * R1mag * R2mag * cosDeltaTA);

            double s = (R1mag + R2mag + c) / 2;

            double epsilon = (R2mag - R1mag) / R1mag;

            double intemed1 = R2mag / R1mag;
            double TanSqr2w = (Math.Pow(epsilon, 2) / 4) / (Math.Sqrt(intemed1) + intemed1 * (2 + Math.Sqrt(intemed1)));

            double sinSqrDeltaTAOver4 = Math.Pow(Math.Sin(deltaTA / 4), 2);
            double cosSqrDeltaTAOver4 = Math.Pow(Math.Cos(deltaTA / 4), 2);
            double rop = Math.Sqrt(R1mag * R2mag) * (cosSqrDeltaTAOver4 + TanSqr2w);

            double l;
            if (shortway)
            {
                l = (sinSqrDeltaTAOver4 + TanSqr2w) / (sinSqrDeltaTAOver4 + TanSqr2w + Math.Cos(deltaTA / 2));
            }
            else
            {
                l = (cosSqrDeltaTAOver4 + TanSqr2w - Math.Cos(deltaTA / 2)) / (cosSqrDeltaTAOver4 + TanSqr2w);
            }

            double m = (muCB * Math.Pow(dt, 2)) / (8 * Math.Pow(rop, 3));

            double x = l;
            double x_change = 1;

            double y = 0;
            int loops = 0;
            do
            {
                double ksi = ComputeKsi(x, 8);

                double h1 = (Math.Pow((l + x), 2) * (1 + 3 * x + ksi)) / ((1 + 2 * x + l) * (4 * x + ksi * (3 + x)));
                double h2 = (m * (x - l + ksi)) / ((1 + 2 * x + l) * (4 * x + ksi * (3 + x)));

                double[] polyConsts = { -h2, 0, -(1 + h1), 1 };
                PolynomialFunction yEqnPoly = new PolynomialFunction(polyConsts);

                //const double relativeAccuracy = 1.0e-12;
                //const double absoluteAccuracy = 1.0e-12;
                //BracketingNthOrderBrentSolver solver = new BracketingNthOrderBrentSolver(relativeAccuracy, absoluteAccuracy, 10);
                //y = solver.solve(10000, yEqnPoly, -10000, 10000, 0.0, AllowedSolution.ANY_SIDE);

                //replaced Arrowstar's above four commented lines with this solver:  -The_Duck 
                //Use an initial guess of 10; NewtonSolver will get stuck with an initial guess of zero.
                y = NewtonSolver.Solve(yEqnPoly, 10, 1.0e-12, 50);

                double x_new = Math.Sqrt(Math.Pow((1 - l) / 2, 2) + m / Math.Pow(y, 2)) - (1 + l) / 2;
                x_change = Math.Abs(x - x_new);
                x = x_new;
                loops++;

            } while (x_change > Math.Pow(10, -6) && loops < 30);

            double a = (muCB * Math.Pow(dt, 2)) / (16 * Math.Pow(rop, 2) * x * Math.Pow(y, 2));

            double f = 0;
            double g = 0;
            double g_dot = 0;
            const double small = 1e-5;
            if (a > small)
            {

                double sinBetaEOver2 = Math.Sqrt((s - c) / (2 * a));
                double betaE = 2 * Math.Asin(sinBetaEOver2);
                if (deltaTA > Math.PI)
                {
                    betaE = -betaE;
                }

                double amin = s / 2;
                double tmin = Math.Sqrt(Math.Pow(amin, 3) / muCB) * (Math.PI - betaE + Math.Sin(betaE));

                double alphaE = 2 * Math.Asin(Math.Sqrt(s / (2 * a)));
                if (dt > tmin)
                {
                    alphaE = 2 * Math.PI - alphaE;
                }

                double deltaE = alphaE - betaE;

                f = 1 - (a / R1mag) * (1 - Math.Cos(deltaE));
                g = dt - Math.Sqrt(Math.Pow(a, 3) / muCB) * (deltaE - Math.Sin(deltaE));
                g_dot = 1 - (a / R2mag) * (1 - Math.Cos(deltaE));

            }
            else if (a < -small)
            {
                //Asinh asinh=new Asinh();
                //double alphaH = 2*asinh.value(Math.Sqrt(s/(-2*a)));
                //double betaH = 2*asinh.value(Math.Sqrt((s-c)/(-2*a)));
                //Porting Arrowstar's above three lines to: -The_Duck
                double alphaH = 2 * MuUtils.Asinh(Math.Sqrt(s / (-2 * a)));
                double betaH = 2 * MuUtils.Asinh(Math.Sqrt((s - c) / (-2 * a)));

                double deltaH = alphaH - betaH;

                f = 1 - (a / R1mag) * (1 - Math.Cosh(deltaH));
                g = dt - Math.Sqrt(-Math.Pow(a, 3) / muCB) * (Math.Sinh(deltaH) - deltaH);
                g_dot = 1 - (a / R2mag) * (1 - Math.Cosh(deltaH));
            }
            else
            {
                //List<Vector3d> VArr = null;
                //commented out the above line from Arrowstar's original, since it seems to do nothing and throws a compiler error -The_Duck
            }

            V1 = (R2 - f * R1) / g;
            V2 = (g_dot * R2 - R1) / g;
        }

        private static double ComputeKsi(double x, int numLevels)
        {
            double ksi = 0;
            double eta = x / Math.Pow((Math.Sqrt(1 + x) + 1), 2);
            double num = 8 * (Math.Sqrt(1 + x) + 1);
            double denom = 1;

            if (numLevels > 0)
            {
                denom = 3 + 1 / (eta + ComputeKsi(eta, numLevels - 1));
            }
            ksi = num / denom;

            return ksi;
        }
    }

    //A simple root-finder. We really ought to use some existing root-finder that someone else has put a 
    //lot of time and effort into, but this one seems to work
    public static class NewtonSolver
    {
        public static double Solve(PolynomialFunction p, double guess, double tolerance, int maxIterations)
        {
            double x = guess;
            double iter;
            for (iter = 0; iter < maxIterations; iter++)
            {
                double delta = p.Evaluate(x) / p.Derivative(x);
                if (Math.Abs(delta) < tolerance) break;
                x = x - delta;
            }
            if (iter == maxIterations)
            {
                throw new Exception("NewtonSolver reached iteration limit of " + maxIterations + " on polynomial " + p.ToString());
            }
            return x;
        }
    }

    //Represents a polynomial function of one variable, and can evaluate its own
    //derivative exactly. 
    public class PolynomialFunction
    {
        double[] coeffs; //coeffs[i] is the coefficient of x^i

        public PolynomialFunction(double[] coeffs)
        {
            this.coeffs = coeffs;
        }

        //Evaluate the polynomial at x
        public double Evaluate(double x)
        {
            double ret = 0;
            double pow = 1;
            for (int i = 0; i < coeffs.Length; i++)
            {
                ret += coeffs[i] * pow;
                pow *= x;
            }
            return ret;
        }

        //Evaluate the derivative of the polynomial at x
        public double Derivative(double x)
        {
            double ret = 0;
            double pow = 1;
            for (int i = 1; i < coeffs.Length; i++)
            {
                ret += i * coeffs[i] * pow;
                pow *= x;
            }
            return ret;
        }

        public override string ToString()
        {
            string ret = "";
            for (int i = 0; i < coeffs.Length; i++)
            {
                ret += coeffs[i].ToString() + " * x^" + i + " + ";
            }
            return ret;
        }
    }
}

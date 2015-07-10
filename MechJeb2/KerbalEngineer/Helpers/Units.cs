// 
//     Kerbal Engineer Redux
// 
//     Copyright (C) 2014 CYBUTEK
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

namespace KerbalEngineer.Helpers
{
    using System;

    public static class Units
    {
        public const double GRAVITY = 9.80665;

        public static string Concat(int value1, int value2)
        {
            return value1 + " / " + value2;
        }

        public static string ConcatF(double value1, double value2, int decimals = 1)
        {
            return value1.ToString("F" + decimals) + " / " + value2.ToString("F" + decimals);
        }

        public static string ConcatF(double value1, double value2, double value3, int decimals = 1)
        {
            return value1.ToString("F" + decimals) + " / " + value2.ToString("F" + decimals) + " / " + value3.ToString("F" + decimals);
        }

        public static string ConcatN(double value1, double value2, int decimals = 1)
        {
            return value1.ToString("N" + decimals) + " / " + value2.ToString("N" + decimals);
        }

        public static string ConcatN(double value1, double value2, double value3, int decimals = 1)
        {
            return value1.ToString("N" + decimals) + " / " + value2.ToString("N" + decimals) + " / " + value3.ToString("N" + decimals);
        }

        public static string Cost(double value, int decimals = 1)
        {
            if (value >= 1000000.0)
            {
                return (value / 1000.0).ToString("N" + decimals) + "K";
            }
            return value.ToString("N" + decimals);
        }

        public static string Cost(double value1, double value2, int decimals = 1)
        {
            if (value1 >= 1000000.0 || value2 >= 1000000.0)
            {
                return (value1 / 1000.0).ToString("N" + decimals) + " / " + (value2 / 1000.0).ToString("N" + decimals) + "K";
            }
            return value1.ToString("N" + decimals) + " / " + value2.ToString("N" + decimals);
        }

        public static string ToAcceleration(double value, int decimals = 2)
        {
            return value.ToString("N" + decimals) + "m/s²";
        }

        public static string ToAcceleration(double value1, double value2, int decimals = 2)
        {
            return value1.ToString("N" + decimals) + " / " + value2.ToString("N" + decimals) + "m/s²";
        }

        public static string ToAngle(double value, int decimals = 5)
        {
            return value.ToString("F" + decimals) + "°";
        }

        public static string ToAngleDMS(double value)
        {
            double absAngle = Math.Abs(value);
            int deg = (int)Math.Floor(absAngle);
            double rem = absAngle - deg;
            int min = (int)Math.Floor(rem * 60);
            rem -= ((double)min / 60);
            int sec = (int)Math.Floor(rem * 3600);
            return String.Format("{0:0}° {1:00}' {2:00}\"", deg, min, sec);
        }

        public static string ToDistance(double value, int decimals = 1)
        {
            if (Math.Abs(value) < 1000000.0)
            {
                if (Math.Abs(value) >= 10.0)
                {
                    return value.ToString("N" + decimals) + "m";
                }

                value *= 100.0;
                if (Math.Abs(value) >= 100.0)
                {
                    return value.ToString("N" + decimals) + "cm";
                }

                value *= 10.0;
                return value.ToString("N" + decimals) + "mm";
            }

            value /= 1000.0;
            if (Math.Abs(value) < 1000000.0)
            {
                return value.ToString("N" + decimals) + "km";
            }

            value /= 1000.0;
            return value.ToString("N" + decimals) + "Mm";
        }

        public static string ToFlux(double value)
        {
            return value.ToString("#,0.00") + "W";
        }

        public static string ToForce(double value)
        {
            return value.ToString((value < 100000.0) ? (value < 10000.0) ? (value < 100.0) ? (Math.Abs(value) < Double.Epsilon) ? "N0" : "N3" : "N2" : "N1" : "N0") + "kN";
        }

        public static string ToForce(double value1, double value2)
        {
            string format1 = (value1 < 100000.0) ? (value1 < 10000.0) ? (value1 < 100.0) ? (Math.Abs(value1) < Double.Epsilon) ? "N0" : "N3" : "N2" : "N1" : "N0";
            string format2 = (value2 < 100000.0) ? (value2 < 10000.0) ? (value2 < 100.0) ? (Math.Abs(value2) < Double.Epsilon) ? "N0" : "N3" : "N2" : "N1" : "N0";
            return value1.ToString(format1) + " / " + value2.ToString(format2) + "kN";
        }

        public static string ToMach(double value)
        {
            return value.ToString("0.00") + "Ma";
        }

        public static string ToMass(double value, int decimals = 0)
        {
            if (value >= 1000.0)
            {
                return value.ToString("N" + decimals + 2) + "t";
            }

            value *= 1000.0;
            return value.ToString("N" + decimals) + "kg";
        }

        public static string ToMass(double value1, double value2, int decimals = 0)
        {
            if (value1 >= 1000.0f || value2 >= 1000.0f)
            {
                return value1.ToString("N" + decimals + 2) + " / " + value2.ToString("N" + decimals + 2) + "t";
            }

            value1 *= 1000.0;
            value2 *= 1000.0;
            return value1.ToString("N" + decimals) + " / " + value2.ToString("N" + decimals) + "kg";
        }

        public static string ToPercent(double value, int decimals = 2)
        {
            value *= 100.0;
            return value.ToString("F" + decimals) + "%";
        }

        public static string ToRate(double value, int decimals = 1)
        {
            return value < 1.0 ? (value * 60.0).ToString("F" + decimals) + "/min" : value.ToString("F" + decimals) + "/sec";
        }

        public static string ToSpeed(double value, int decimals = 2)
        {
            if (Math.Abs(value) < 1.0)
            {
                return (value * 1000.0).ToString("N" + decimals) + "mm/s";
            }
            return value.ToString("N" + decimals) + "m/s";
        }

        public static string ToTemperature(double value)
        {
            return value.ToString("#,0") + "K";
        }

        public static string ToTemperature(double value1, double value2)
        {
            return value1.ToString("#,0") + " / " + value2.ToString("#,0") + "K";
        }
        //public static string ToTime(double value)
        //{
        //    return TimeFormatter.ConvertToString(value);
        //}

        public static string ToTorque(double value)
        {
            return value.ToString((value < 100.0) ? (Math.Abs(value) < Double.Epsilon) ? "N0" : "N1" : "N0") + "kNm";
        }
    }
}
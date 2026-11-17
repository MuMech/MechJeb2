using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

#nullable enable

namespace MuMech
{
    /// <summary>
    ///     What the vessel flew under PSG guidance, a sample every INTERVAL seconds of MET, in the units of SolutionTrace's columns
    ///     so the PSG Solution window can lay it over the plan: degrees, the fraction of full thrust, and the Qα and Q fractions of
    ///     the limits PSG plans with (zero without a limit).  The angle of attack is the angle between the vessel's nose and its
    ///     surface velocity, as AeroModel measures it on the plan, and q is KSP's, not PSG's exponential atmosphere.
    /// </summary>
    public sealed class PSGFlownTrack
    {
        public enum Column
        {
            MET,
            ALTITUDE,
            SPEED,
            SURFACE_SPEED,
            PITCH,
            HEADING,
            THROTTLE,
            AOA,
            Q_ALPHA_FRACTION,
            Q_FRACTION
        }

        public static readonly int ColumnCount = Enum.GetValues(typeof(Column)).Length;

        public const double INTERVAL = 0.5;

        // three hours of MET, about 1 MB
        public const int MAX_SAMPLES = 21600;

        private readonly List<double>[] _columns = new List<double>[ColumnCount];

        public PSGFlownTrack()
        {
            for (int i = 0; i < ColumnCount; i++)
                _columns[i] = new List<double>(256);
        }

        public int Count => _columns[0].Count;

        public double this[Column c, int row] => _columns[(int)c][row];

        public double LastMET => Count > 0 ? _columns[(int)Column.MET][Count - 1] : double.NegativeInfinity;

        public void Clear()
        {
            foreach (List<double> c in _columns)
                c.Clear();
        }

        // a sample, unless the last one is less than INTERVAL old or the track is full; a MET that went back (a revert) starts again
        public void Record(MechJebCore core)
        {
            VesselState vs = core.VesselState;
            double met = vs.MET;

            if (met < LastMET)
                Clear();
            if (met < LastMET + INTERVAL || Count >= MAX_SAMPLES)
                return;

            double aoa = vs.SpeedSurface > 0.01 ? Vector3d.Angle(vs.Forward, vs.SurfaceVelocity) : 0;
            double q = vs.DynamicPressure;
            double qAlphaLimit = core.PSGSimulation.QAlphaLimit;
            double qLimit = core.AscentSettings.ThrustLimits.LimitDynamicPressure ? core.AscentSettings.ThrustLimits.MaxDynamicPressure.Val : 0;

            Add(Column.MET, met);
            Add(Column.ALTITUDE, vs.AltitudeASL);
            Add(Column.SPEED, vs.SpeedOrbital);
            Add(Column.SURFACE_SPEED, vs.SpeedSurface);
            Add(Column.PITCH, vs.Pitch);
            Add(Column.HEADING, vs.Heading);
            Add(Column.THROTTLE, vs.ThrustAvailable > 0 ? vs.ThrustCurrent / vs.ThrustAvailable : 0);
            Add(Column.AOA, aoa);
            Add(Column.Q_ALPHA_FRACTION, qAlphaLimit > 0 ? q * UtilMath.Deg2Rad * aoa / qAlphaLimit : 0);
            Add(Column.Q_FRACTION, qLimit > 0 ? q / qLimit : 0);
        }

        private void Add(Column c, double v) => _columns[(int)c].Add(v);

        /// <summary>Writes the samples as CSV, a header of column names first, numbers in the invariant culture.</summary>
        public void WriteCsv(TextWriter writer)
        {
            writer.WriteLine(string.Join(",", Enum.GetNames(typeof(Column))));
            for (int row = 0; row < Count; row++)
            {
                for (int c = 0; c < ColumnCount; c++)
                {
                    if (c > 0)
                        writer.Write(',');
                    writer.Write(_columns[c][row].ToString("R", CultureInfo.InvariantCulture));
                }

                writer.WriteLine();
            }
        }
    }
}

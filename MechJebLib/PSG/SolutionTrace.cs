/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PSG
{
    /// <summary>
    ///     A SI Solution sampled for plotting and export: a row per sample, phase by phase, with SamplesPerNode samples across
    ///     each interpolant node so that what the cubic pieces do between their collocation points shows.  Each phase is sampled
    ///     on its own interpolant from its start to its end, so a jump at a phase boundary shows as two rows at the same MET.
    ///     It also keeps the phase boundaries, the node boundaries and the collocation points.  Sampling evaluates the solution,
    ///     which updates its lookup caches: sample on the thread that owns it.
    /// </summary>
    public sealed class SolutionTrace
    {
        public enum Column
        {
            MET,
            PHASE,
            KSP_STAGE,
            MJ_PHASE,
            COAST,
            UNGUIDED,
            ALTITUDE,
            SPEED,
            SURFACE_SPEED,
            PITCH,
            HEADING,
            THROTTLE,
            MASS,
            DELTA_V,
            DELTA_V_LEFT,
            AOA,
            Q_ALPHA_FRACTION,
            Q_FRACTION
        }

        public static readonly int ColumnCount = Enum.GetValues(typeof(Column)).Length;

        public const int DEFAULT_SAMPLES_PER_NODE = 8;

        private readonly List<double>[] _columns = new List<double>[ColumnCount];

        // the MET where each phase starts, and the end of the last one
        public readonly List<double> PhaseBoundaries = new List<double>();

        // the bounds between interpolant nodes, of every phase
        public readonly List<double> NodeBoundaries = new List<double>();

        // the times the interpolants pass through (the collocation points of a SolutionBuilder solution)
        public readonly List<double> Knots = new List<double>();

        public AeroModel Aero { get; private set; }

        public int Count => _columns[0].Count;

        public double T0 { get; private set; }
        public double Tf { get; private set; }

        public SolutionTrace()
        {
            for (int i = 0; i < ColumnCount; i++)
                _columns[i] = new List<double>();
        }

        public double this[Column c, int row] => _columns[(int)c][row];

        public void Clear()
        {
            foreach (List<double> c in _columns)
                c.Clear();
            PhaseBoundaries.Clear();
            NodeBoundaries.Clear();
            Knots.Clear();
            T0 = Tf = 0;
        }

        /// <summary>
        ///     Replaces the trace with solution's.  Pitch and heading are those of the thrust direction at the solution's own
        ///     position, the angles are in degrees, the throttle is the fraction of full thrust, and the Qα and Q columns are the
        ///     fractions of their limits (AeroModel), zero without one.  DELTA_V is the delta-v used since the solution's start,
        ///     DELTA_V_LEFT what is left to its end, which compares across solutions computed at different times.
        /// </summary>
        public void Sample(Solution solution, int samplesPerNode = DEFAULT_SAMPLES_PER_NODE)
        {
            Check.False(solution.Normalized, "SolutionTrace samples SI units, use DeNormalized()");
            Check.True(samplesPerNode >= 1);

            Clear();
            Aero = solution.Aero;
            T0 = solution.T0;
            Tf = solution.Tf;

            var bounds = new List<double>();

            for (int p = 0; p < solution.Segments; p++)
            {
                double start = solution.StartTime(p);
                double end = solution.EndTime(p);
                PhaseBoundaries.Add(start);

                bounds.Clear();
                solution.NodeBoundaries(p, bounds);
                NodeBoundaries.AddRange(bounds);
                solution.Knots(p, Knots);

                if (bounds.Count < 2)
                {
                    AddRow(solution, p, start);
                    AddRow(solution, p, end);
                    continue;
                }

                for (int n = 0; n + 1 < bounds.Count; n++)
                {
                    double left = bounds[n], right = bounds[n + 1];
                    for (int k = 0; k < samplesPerNode; k++)
                        AddRow(solution, p, left + (right - left) * k / samplesPerNode);
                }

                AddRow(solution, p, bounds[bounds.Count - 1]);
            }

            if (solution.Segments > 0)
                PhaseBoundaries.Add(solution.Tf);
        }

        private void AddRow(Solution solution, int p, double t)
        {
            InterpolantLayout x = solution.State(p, t);
            Phase phase = solution.Phases[p];
            AeroModel aero = solution.Aero;
            (double pitch, double heading) = Astro.ECIToPitchHeading(x.R, x.U);

            Add(Column.MET, t);
            Add(Column.PHASE, p);
            Add(Column.KSP_STAGE, phase.KSPStage);
            Add(Column.MJ_PHASE, phase.MJPhase);
            Add(Column.COAST, phase.Coast ? 1 : 0);
            Add(Column.UNGUIDED, phase.Unguided ? 1 : 0);
            Add(Column.ALTITUDE, x.R.magnitude - aero.RBody);
            Add(Column.SPEED, x.V.magnitude);
            Add(Column.SURFACE_SPEED, aero.SurfaceVelocity(x.R, x.V).magnitude);
            Add(Column.PITCH, Rad2Deg(pitch));
            Add(Column.HEADING, Rad2Deg(heading));
            Add(Column.THROTTLE, phase.Coast ? 0 : x.T);
            Add(Column.MASS, x.M);
            double dv = solution.DV(Math.Min(Math.Max(t, solution.StartTime(p)), solution.EndTime(p)));
            Add(Column.DELTA_V, dv);
            Add(Column.DELTA_V_LEFT, solution.DV(solution.Tf) - dv);
            Add(Column.AOA, Rad2Deg(aero.AngleOfAttack(x.R, x.V, x.U)));
            Add(Column.Q_ALPHA_FRACTION, aero.QAlphaFraction(x.R, x.V, x.U));
            Add(Column.Q_FRACTION, aero.QFraction(x.R, x.V));
        }

        private void Add(Column c, double v) => _columns[(int)c].Add(v);

        /// <summary>The smallest and largest finite values of a column, (0, 0) when it has none.</summary>
        public (double min, double max) Range(Column c)
        {
            List<double> col = _columns[(int)c];
            double min = double.PositiveInfinity, max = double.NegativeInfinity;
            foreach (double v in col)
            {
                if (!IsFinite(v))
                    continue;
                if (v < min) min = v;
                if (v > max) max = v;
            }

            return min <= max ? (min, max) : (0, 0);
        }

        /// <summary>Writes the rows as CSV, a header of column names first, numbers in the invariant culture.</summary>
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

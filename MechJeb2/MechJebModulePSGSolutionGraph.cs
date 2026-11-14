extern alias JetBrainsAnnotations;
using System;
using System.IO;
using JetBrainsAnnotations::JetBrains.Annotations;
using MechJebLib.PSG;
using UnityEngine;
using Column = MechJebLib.PSG.SolutionTrace.Column;
using FlownColumn = MuMech.PSGFlownTrack.Column;

#nullable enable

namespace MuMech
{
    /// <summary>
    ///     The PSG Solution window: plots the Solution the guidance controller flies, sampled across each interpolant node
    ///     (SolutionTrace), with what the vessel flew (PSGFlownTrack) over it, and exports both as CSV.  The window resamples
    ///     each new Solution unless Hold is on.
    /// </summary>
    public class MechJebModulePSGSolutionGraph : DisplayModule
    {
        public MechJebModulePSGSolutionGraph(MechJebCore core) : base(core) { }

        // one plottable quantity: the plan's column, the flown one (none for some), how to show it, and its Y range
        internal readonly struct SeriesInfo
        {
            public readonly string Label;
            public readonly Column Column;
            public readonly FlownColumn? Flown;
            public readonly Color Color;

            // the plotted value is the column's times Factor (fractions shown as percents)
            public readonly double Factor;

            // a range the Y labels keep, widened by the data when it goes outside; NaN for one from the data alone
            public readonly double Min, Max;

            public SeriesInfo(string label, Column column, FlownColumn? flown, Color color, double factor = 1, double min = double.NaN,
                double max = double.NaN)
            {
                Label = label;
                Column = column;
                Flown = flown;
                Color = color;
                Factor = factor;
                Min = min;
                Max = max;
            }
        }

        internal static readonly SeriesInfo[] Series =
        {
            new SeriesInfo("Pitch °", Column.PITCH, FlownColumn.PITCH, XKCDColors.Mint, 1, 0, 90),
            new SeriesInfo("Heading °", Column.HEADING, FlownColumn.HEADING, XKCDColors.Lime),
            new SeriesInfo("Throttle %", Column.THROTTLE, FlownColumn.THROTTLE, XKCDColors.LightRed, 100, 0, 100),
            new SeriesInfo("AoA °", Column.AOA, FlownColumn.AOA, XKCDColors.Lavender, 1, 0),
            new SeriesInfo("Qα % of limit", Column.Q_ALPHA_FRACTION, FlownColumn.Q_ALPHA_FRACTION, XKCDColors.Cyan, 100, 0, 100),
            new SeriesInfo("Q % of limit", Column.Q_FRACTION, FlownColumn.Q_FRACTION, XKCDColors.Orange, 100, 0, 100),
            new SeriesInfo("Altitude m", Column.ALTITUDE, FlownColumn.ALTITUDE, XKCDColors.White, 1, 0),
            new SeriesInfo("Speed m/s", Column.SPEED, FlownColumn.SPEED, XKCDColors.Apricot, 1, 0),
            new SeriesInfo("Surface speed m/s", Column.SURFACE_SPEED, FlownColumn.SURFACE_SPEED, XKCDColors.Yellow, 1, 0),
            new SeriesInfo("Mass kg", Column.MASS, null, XKCDColors.Pink, 1, 0),
            new SeriesInfo("Δv m/s", Column.DELTA_V, null, XKCDColors.Beige, 1, 0)
        };

        public static int SeriesCount => Series.Length;

        // which series are shown, a bit per entry of Series: pitch, throttle and Qα
        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public int DisplayedMask = (1 << 0) | (1 << 2) | (1 << 4);

        // the series the Y labels belong to
        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public int ScaleIdx;

        // the plot's height in 128 px steps
        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public int VSize = 2;

        // the visible MET span is the whole trajectory over 2^Zoom
        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public int Zoom;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool ShowFlown = true;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool ShowPhases = true;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool ShowNodes;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool ShowKnots;

        // keep the sampled Solution instead of following each new one
        public bool Hold;

        internal const int MAX_ZOOM = 8;
        internal const int MAX_VSIZE = 6;

        public readonly SolutionTrace Plan = new SolutionTrace();

        // how many Solutions were sampled since the window opened, the last one included
        public int Sampled { get; private set; }

        private Solution? _sampled;

        public PSGFlownTrack Flown => Core.Guidance.FlownTrack;

        internal bool Displayed(int i) => (DisplayedMask & (1 << i)) != 0;

        internal void SetDisplayed(int i, bool shown) => DisplayedMask = shown ? DisplayedMask | (1 << i) : DisplayedMask & ~(1 << i);

        internal void ClampSettings()
        {
            Zoom = Mathf.Clamp(Zoom, 0, MAX_ZOOM);
            VSize = Mathf.Clamp(VSize, 1, MAX_VSIZE);
            if (ScaleIdx < 0 || ScaleIdx >= Series.Length) ScaleIdx = 0;
        }

        /// <summary>Samples the guidance's Solution when it is a new one and Hold is off. True when the plan changed.</summary>
        public bool Refresh()
        {
            Solution? solution = Core.Guidance.Solution;
            if (solution == null || ReferenceEquals(solution, _sampled) || (Hold && Plan.Count > 0))
                return false;

            Plan.Sample(solution);
            _sampled = solution;
            Sampled++;
            return true;
        }

        // the plan is the Solution guidance flies now (it may be held)
        public bool PlanIsCurrent => ReferenceEquals(Core.Guidance.Solution, _sampled);

        protected override void OnModuleEnabled() => Sampled = 0;

        protected override void OnModuleDisabled()
        {
            // a closed window drops its reference to the Solution
            _sampled = null;
            Plan.Clear();
            Hold = false;
        }

        /// <summary>
        ///     Writes the plan, and the flown track when it has samples, to GameData/MechJeb2/Export/ as
        ///     &lt;vessel&gt;_PSG_&lt;time&gt;_plan.csv and _flown.csv.  Returns the plan's path, or null with no plan.
        /// </summary>
        public string? ExportCsv()
        {
            if (Plan.Count == 0)
                return null;

            string exportPath = KSPUtil.ApplicationRootPath + "/GameData/MechJeb2/Export/";
            Directory.CreateDirectory(exportPath);

            string vesselName = Vessel != null ? MuUtils.SanitizeFileName(Vessel.vesselName) : "";
            string stem = exportPath + vesselName + "_PSG_" + DateTime.Now.ToString("yyyyMMddHHmmss");

            string planPath = stem + "_plan.csv";
            using (var writer = new StreamWriter(planPath))
                Plan.WriteCsv(writer);

            string message = "Exported as\n" + planPath;
            if (Flown.Count > 0)
            {
                string flownPath = stem + "_flown.csv";
                using (var writer = new StreamWriter(flownPath))
                    Flown.WriteCsv(writer);
                message += "\n" + flownPath;
            }

            ScreenMessages.PostScreenMessage(message, 5);
            return planPath;
        }

        public override string GetName() => "PSG Solution";

        public override string IconName() => "PSGSolution";
    }
}

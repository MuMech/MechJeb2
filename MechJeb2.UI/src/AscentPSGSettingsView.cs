using System;
using KodeKit;
using KodeUI;
using MechJebLib.PSG;
using UnityEngine;
using Tone = KodeKit.KodeWindow.Tone;

namespace MuMech.UI
{
    /// <summary>
    /// PSG settings that need the player's attention: their rows are marked in the PSG Settings window, and the Ascent window's PSG
    /// Settings button carries a badge while any of them is.
    /// </summary>
    static class PsgAttention
    {
        /// <summary>Aref 0 (the default) on a body with an atmosphere: PSG then plans the ascent with no drag at all.</summary>
        public static bool ArefUnset(MechJebCore core) => core.PSGSimulation.Aref <= 0 && Atmosphere(core);

        /// <summary>A minimum coast longer than the maximum, which PSG refuses to plan with.</summary>
        public static bool CoastTimesInverted(MechJebCore core) => core.PSGSimulation.MinCoast > core.PSGSimulation.MaxCoast;

        /// <summary>A Qα limit PSG clamps to between 0 and 10000 Pa-rad.</summary>
        public static bool QaClamped(MechJebCore core) => core.PSGSimulation.LimitQa < 0 || core.PSGSimulation.LimitQa > 10000;

        public static bool Any(MechJebCore core) => ArefUnset(core) || CoastTimesInverted(core) || QaClamped(core);

        // the vessel's body in flight, Kerbin (or the home body) in the editor
        static bool Atmosphere(MechJebCore core)
        {
            CelestialBody body = core.vessel != null ? core.vessel.mainBody : FlightGlobals.GetHomeBody();
            return body != null && body.atmosphere;
        }
    }

    /// <summary>
    /// MechJebModuleAscentPSGSettingsMenu ("PSG Settings") in KodeKit.
    /// The per-stage table (FlightPlan.DrawStageGUI) is native: pooled retained rows with garbage-free text.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleAscentPSGSettingsMenu))]
    public class AscentPSGSettingsView : KodeView
    {
        readonly MechJebModuleAscentPSGSettingsMenu menu;

        public AscentPSGSettingsView(MechJebModuleAscentPSGSettingsMenu menu) : base(menu) => this.menu = menu;

        protected override float MinWidth => 300;

        MechJebCore Core => menu.Core;
        MechJebModuleAscentSettings Settings => Core.AscentSettings;
        MechJebModulePSGSimulation Psgs => Core.PSGSimulation;

        bool hasStages;

        // the flight plan is kept current by MechJebModuleAscentMenu.OnFixedUpdate; this window only shows it
        protected override void BeforeSync()
        {
            hasStages = menu.HasStages;
            stageCount = StageList?.Count ?? 0;
            UpdateSkipped();
        }

        // the burns Min ΔV leaves out of the plan: some Δv, but under Min ΔV. Rebuilt only when that set changes, as the
        // label reads it every frame.
        string skipped;
        double skippedKey = double.NaN;

        void UpdateSkipped()
        {
            System.Collections.Generic.List<MechJebLib.FuelFlowSimulation.FuelStats> vac = Core.StageStats.VacStats;
            double min = Psgs.MinDeltaV;
            int top = Settings.LastStage;
            double key = min * 7919 + top;
            int count = 0;
            for (int i = 0; i < vac.Count; i++)
                if (vac[i].KSPStage >= top && vac[i].DeltaV >= 0.5 && vac[i].DeltaV < min)
                {
                    count++;
                    key = key * 31 + vac[i].KSPStage * 100003 + Math.Round(vac[i].DeltaV);
                }

            if (key == skippedKey) return;
            skippedKey = key;
            if (count == 0)
            {
                skipped = null;
                return;
            }

            var sb = new System.Text.StringBuilder(count == 1 ? "Stage left out of the plan, under Min ΔV: " : "Stages left out of the plan, under Min ΔV: ");
            bool first = true;
            for (int i = 0; i < vac.Count; i++)
                if (vac[i].KSPStage >= top && vac[i].DeltaV >= 0.5 && vac[i].DeltaV < min)
                {
                    if (!first) sb.Append(", ");
                    first = false;
                    sb.Append(vac[i].KSPStage).Append(" (").Append(Math.Round(vac[i].DeltaV).ToString("F0")).Append(" m/s)");
                }

            skipped = sb.ToString();
        }

        // under the stage table: which burns the plan leaves out, in the caution colour
        void BuildSkipped(KodeWindow w, UIObject parent)
        {
            UIText note = w.Wrap(w.BoundLabel(parent, () => skipped ?? " ", true));
            KodeWindow.ToneText(note, Tone.Caution);
            w.Visible(note, () => skipped != null);
        }

        int stageCount;

        // grows (and rebuilds the view) when a plan has more stages than rows; kept across rebuilds
        static int poolSize = 12;

        System.Collections.Generic.List<FlightPlanStage> StageList => Psgs.FlightPlan?.Stages;

        sealed class StageRow
        {
            public GameObject Root, Unguided, Fixed;
            public CharText Text;
            public int LastFlags = -1;
        }

        void BuildStageTable(KodeWindow w, UIObject parent)
        {
            LayoutPanel box = KodeWindow.Box(parent);
            w.Visible(box.gameObject, () => stageCount > 0);

            var rows = new StageRow[poolSize];
            for (int i = 0; i < rows.Length; i++)
            {
                Layout row = KodeWindow.Row(box);
                UIText text = KodeWindow.LiveLabel(row, "999 99,999 m/s", false);
                UIText unguided = KodeWindow.Label(row, " (unguided)");
                UIText fixedLabel = KodeWindow.Label(row, " (fixed)");
                rows[i] = new StageRow
                {
                    Root = row.gameObject, Unguided = unguided.gameObject, Fixed = fixedLabel.gameObject, Text = new CharText(text.tmpText)
                };
                row.gameObject.SetActive(false);
            }

            w.AddSync(() =>
            {
                int count = stageCount;
                if (count > rows.Length)
                {
                    poolSize = count + 4;
                    RebuildRequested = true;
                    count = rows.Length;
                }

                System.Collections.Generic.List<FlightPlanStage> stages = StageList;
                if (stages == null) count = 0;
                for (int i = 0; i < rows.Length; i++)
                {
                    StageRow row = rows[i];
                    FlightPlanStage stage = i < count && i < stages.Count ? stages[i] : null;
                    if (stage == null)
                    {
                        if (row.Root.activeSelf) row.Root.SetActive(false);
                        continue;
                    }

                    if (!row.Root.activeSelf) row.Root.SetActive(true);
                    // "{KSPStage,3} {DeltaV:##,###0} m/s"
                    CharText t = row.Text.Clear();
                    int stageNumber = stage.KSPStage;
                    int digits = stageNumber < 0 ? 2 : stageNumber < 10 ? 1 : stageNumber < 100 ? 2 : 3;
                    for (int pad = digits; pad < 3; pad++) t.Append(' ');
                    t.AppendInt(stageNumber).Append(' ');
                    AppendGrouped(t, stage.DeltaV);
                    t.Append(" m/s").Commit();

                    int flags = (stage.Unguided ? 1 : 0) | (stage.AllowShutdown ? 0 : 2);
                    if (flags != row.LastFlags)
                    {
                        row.LastFlags = flags;
                        row.Unguided.SetActive(stage.Unguided);
                        row.Fixed.SetActive(!stage.AllowShutdown);
                    }
                }
            });
        }

        /// <summary>A value as "##,###0" does: rounded to an integer with thousands separators.</summary>
        static void AppendGrouped(CharText t, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) > 9e15) value = 0;
            long v = (long)Math.Round(value, MidpointRounding.AwayFromZero);
            if (v < 0)
            {
                t.Append('-');
                v = -v;
            }

            AppendGrouped(t, v);
        }

        static void AppendGrouped(CharText t, long v)
        {
            if (v >= 1000)
            {
                AppendGrouped(t, v / 1000);
                t.Append(',');
                long rest = v % 1000;
                if (rest < 100) t.Append('0');
                if (rest < 10) t.Append('0');
                t.AppendInt(rest);
            }
            else
                t.AppendInt(v);
        }

        void CoastToggle(KodeWindow w, UIObject row, string text, int location) =>
            w.Toggle(row, text, () => Settings.CoastLocation == location, v =>
            {
                if (v) Settings.CoastLocation = location;
            });

        protected override void BuildContents(KodeWindow w)
        {
            if (KodeWindow.ModernLayout)
            {
                BuildModern(w);
                return;
            }

            UIObject r = w.Root;

            Layout stages = KodeWindow.Panel(r);
            BuildStageTable(w, stages);
            BuildSkipped(w, stages);
            w.Visible(stages.gameObject, () => hasStages);

            LayoutPanel box = KodeWindow.Box(r);
            AscentUi.SimpleBox(w, box, "Min ΔV: ", Psgs.MinDeltaV, "m/s", 30);
            AscentUi.SimpleBox(w, box, "Last Stage: ", Settings.LastStage);
            AscentUi.ToggledBox(w, box, "Fixed Burn Stages: ", () => Settings.FixedStagesFlag, v => Settings.FixedStagesFlag = v,
                Settings.FixedStagesInternal, null, 100);
            AscentUi.ToggledBox(w, box, "Unguided Stages: ", () => Settings.UnguidedStagesFlag, v => Settings.UnguidedStagesFlag = v,
                Settings.UnguidedStagesInternal, null, 100);

            box = KodeWindow.Box(r);
            AscentUi.ToggledBox(w, box, "Coast Stage: ", () => Settings.CoastStageFlag, v => Settings.CoastStageFlag = v,
                Settings.CoastStageInternal, null, 100);
            Layout coast = KodeWindow.Row(box);
            CoastToggle(w, coast, "Coast Before", -1);
            CoastToggle(w, coast, "Coast During", 0);
            CoastToggle(w, coast, "Coast After", 1);
            Layout coastTimes = KodeWindow.Row(box);
            w.Attention(AscentUi.SimpleBox(w, coastTimes, "Min Coast:", Psgs.MinCoast, "s", 40), () => PsgAttention.CoastTimesInverted(Core));
            w.Attention(AscentUi.SimpleBox(w, coastTimes, "Max Coast:", Psgs.MaxCoast, "s", 40), () => PsgAttention.CoastTimesInverted(Core));
            AscentUi.SimpleBox(w, box, "Ullage lead time: ", Core.Guidance.UllageLeadTime, "s", 60);

            box = KodeWindow.Box(r);
            Layout spinup = KodeWindow.Row(box);
            AscentUi.ToggledBox(w, spinup, "Spinup stage: ", () => Settings.SpinupStageFlag, v => Settings.SpinupStageFlag = v,
                Settings.SpinupStageInternal, null, 30);
            AscentUi.SimpleBox(w, spinup, "ω: ", Settings.SpinupAngularVelocity, "rpm", 30);
            AscentUi.SimpleBox(w, box, "Spinup lead time: ", Settings.SpinupLeadTime, "s", 60);

            box = KodeWindow.Box(r);
            Layout drag = KodeWindow.Row(box);
            AscentUi.SimpleBox(w, drag, "Cd:", Psgs.Cd, "", 40);
            w.Attention(AscentUi.SimpleBox(w, drag, "Aref:", Psgs.Aref, "m²", 40), () => PsgAttention.ArefUnset(Core));
            UIText arefNote = AscentUi.ToneText(box, ArefNote, Tone.Caution, true);
            KodeWindow.WrapToWidth(arefNote);
            w.Visible(arefNote.gameObject, () => PsgAttention.ArefUnset(Core));
            AscentUi.SimpleBox(w, box, "#MechJeb_Ascent_label13", Settings.PitchStartHeight, "m", 40); //Booster Pitch start:
            AscentUi.SimpleBox(w, box, "#MechJeb_Ascent_label14", Settings.PitchRate, "°/s", 40); //Booster Pitch rate:

            box = KodeWindow.Box(r);
            w.Attention(AscentUi.SimpleBox(w, box, "#MechJeb_Ascent_label17", Psgs.LimitQa, "Pa-rad"), () => PsgAttention.QaClamped(Core)); //Qα limit
            UIText clamped = AscentUi.ToneText(box, "Qα limit has been clamped to between 0 and 10000 Pa-rad", Tone.Bad, true);
            KodeWindow.WrapToWidth(clamped);
            w.Visible(clamped.gameObject, () => OutsideRecommended && (Psgs.LimitQa < 0 || Psgs.LimitQa > 10000));
            UIText recommended = AscentUi.ToneText(box, "#MechJeb_Ascent_label20", Tone.Warn, true); //Qα limit is recommended to be 1000 to 4000 Pa-rad
            KodeWindow.WrapToWidth(recommended);
            w.Visible(recommended.gameObject, () => OutsideRecommended && !(Psgs.LimitQa < 0 || Psgs.LimitQa > 10000));

            w.Toggle(box, "Draw Trajectory on Map", () => Core.Guidance.ShouldDrawTrajectory, v => Core.Guidance.ShouldDrawTrajectory = v);
        }

        bool OutsideRecommended => Psgs.LimitQa < 1000 || Psgs.LimitQa > 4000;

        const string ArefNote = "Aref is 0: PSG plans the ascent with no drag. Set the rocket's cross-section area.";

        static readonly string[] coastLocations = { "Before", "During", "After" };

        // modern layout: titled groups of aligned fields, the coast location as one choice, the Qα warnings in a status bar
        void BuildModern(KodeWindow w)
        {
            UIObject r = w.Root;

            Layout stages = KodeWindow.Panel(r);
            KodeWindow.Header(stages, "#MechJeb_NewUI_Stages"); //Stages
            BuildStageTable(w, stages);
            BuildSkipped(w, stages);
            w.Visible(stages.gameObject, () => hasStages);

            KodeWindow.PropertyGrid plan = w.Properties(r);
            ModernRows.Field(plan, "Min ΔV", Psgs.MinDeltaV, "m/s", 50);
            ModernRows.Field(plan, "Last Stage", Settings.LastStage, "", 50);
            ModernRows.Switch(plan, "Fixed Burn Stages", () => Settings.FixedStagesFlag, v => Settings.FixedStagesFlag = v, Settings.FixedStagesInternal, "", 100);
            ModernRows.Switch(plan, "Unguided Stages", () => Settings.UnguidedStagesFlag, v => Settings.UnguidedStagesFlag = v,
                Settings.UnguidedStagesInternal, "", 100);

            UIObject g = w.Group(r, "#MechJeb_NewUI_Coast"); //Coast
            KodeWindow.PropertyGrid coast = w.Properties(g);
            ModernRows.Switch(coast, "Coast Stage", () => Settings.CoastStageFlag, v => Settings.CoastStageFlag = v, Settings.CoastStageInternal, "", 100);
            Layout where = KodeWindow.Row(g);
            w.Segmented(where, coastLocations, () => Settings.CoastLocation + 1, i => Settings.CoastLocation = i - 1);
            KodeWindow.PropertyGrid coastTimes = w.Properties(g);
            w.Attention(ModernRows.Field(coastTimes, "Min Coast", Psgs.MinCoast, "s", 50), () => PsgAttention.CoastTimesInverted(Core));
            w.Attention(ModernRows.Field(coastTimes, "Max Coast", Psgs.MaxCoast, "s", 50), () => PsgAttention.CoastTimesInverted(Core));
            ModernRows.Field(coastTimes, "Ullage lead time", Core.Guidance.UllageLeadTime, "s", 50);

            g = w.Group(r, "#MechJeb_NewUI_Spinup"); //Spinup
            KodeWindow.PropertyGrid spin = w.Properties(g);
            ModernRows.Switch(spin, "Spinup stage", () => Settings.SpinupStageFlag, v => Settings.SpinupStageFlag = v, Settings.SpinupStageInternal, "", 50);
            ModernRows.Field(spin, "ω", Settings.SpinupAngularVelocity, "rpm", 50);
            ModernRows.Field(spin, "Spinup lead time", Settings.SpinupLeadTime, "s", 50);

            g = w.Group(r, "#MechJeb_NewUI_BoosterDrag"); //Booster and drag
            KodeWindow.PropertyGrid aero = w.Properties(g);
            ModernRows.Field(aero, "#MechJeb_Ascent_label13", Settings.PitchStartHeight, "m", 50); //Booster Pitch start:
            ModernRows.Field(aero, "#MechJeb_Ascent_label14", Settings.PitchRate, "°/s", 50); //Booster Pitch rate:
            ModernRows.Field(aero, "Cd", Psgs.Cd, "", 50);
            w.Attention(ModernRows.Field(aero, "Aref", Psgs.Aref, "m²", 50), () => PsgAttention.ArefUnset(Core));
            UIText arefNote = aero.Note(ArefNote);
            KodeWindow.ToneText(arefNote, Tone.Caution);
            KodeWindow.WrapToWidth(arefNote);
            w.Visible(arefNote.transform.parent.gameObject, () => PsgAttention.ArefUnset(Core));

            g = w.Group(r, "#MechJeb_NewUI_Limits"); //Limits
            KodeWindow.PropertyGrid limits = w.Properties(g);
            w.Attention(ModernRows.Field(limits, "#MechJeb_Ascent_label17", Psgs.LimitQa, "Pa-rad", 60), () => PsgAttention.QaClamped(Core)); //Qα limit
            bool Clamped() => Psgs.LimitQa < 0 || Psgs.LimitQa > 10000;
            Layout qa = w.StatusBar(g, () => Clamped() ? Tone.Bad : Tone.Warn);
            UIText clamped = GuidanceUi.Wrapped(qa, "Qα limit has been clamped to between 0 and 10000 Pa-rad");
            w.Visible(clamped.gameObject, Clamped);
            UIText recommended = GuidanceUi.Wrapped(qa, "#MechJeb_Ascent_label20"); //Qα limit is recommended to be 1000 to 4000 Pa-rad
            w.Visible(recommended.gameObject, () => !Clamped());
            w.Visible(qa.gameObject, () => OutsideRecommended);

            w.SwitchRow(r, "Draw Trajectory on Map", () => Core.Guidance.ShouldDrawTrajectory, v => Core.Guidance.ShouldDrawTrajectory = v);
        }
    }
}

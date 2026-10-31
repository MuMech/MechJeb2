using System;
using KodeKit;
using KodeUI;
using MechJebLib.PSG;
using UnityEngine;
using Tone = KodeKit.KodeWindow.Tone;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleAscentPSGSettingsMenu ("PSG Settings") in KodeKit, row for row with its WindowGUI.
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

        // the logical plan WindowGUI rebuilt before drawing
        protected override void BeforeSync()
        {
            hasStages = menu.UpdateLogicalPlan();
            stageCount = StageList?.Count ?? 0;
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
            AscentUi.SimpleBox(w, coastTimes, "Min Coast:", Psgs.MinCoast, "s", 40);
            AscentUi.SimpleBox(w, coastTimes, "Max Coast:", Psgs.MaxCoast, "s", 40);
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
            AscentUi.SimpleBox(w, drag, "Aref:", Psgs.Aref, "m²", 40);
            AscentUi.SimpleBox(w, box, "#MechJeb_Ascent_label13", Settings.PitchStartHeight, "m", 40); //Booster Pitch start:
            AscentUi.SimpleBox(w, box, "#MechJeb_Ascent_label14", Settings.PitchRate, "°/s", 40); //Booster Pitch rate:

            box = KodeWindow.Box(r);
            AscentUi.SimpleBox(w, box, "#MechJeb_Ascent_label17", Psgs.LimitQa, "Pa-rad"); //Qα limit
            UIText clamped = AscentUi.ToneText(box, "Qα limit has been clamped to between 0 and 10000 Pa-rad", Tone.Bad, true);
            clamped.tmpText.enableWordWrapping = true;
            w.Visible(clamped.gameObject, () => OutsideRecommended && (Psgs.LimitQa < 0 || Psgs.LimitQa > 10000));
            UIText recommended = AscentUi.ToneText(box, "#MechJeb_Ascent_label20", Tone.Warn, true); //Qα limit is recommended to be 1000 to 4000 Pa-rad
            recommended.tmpText.enableWordWrapping = true;
            w.Visible(recommended.gameObject, () => OutsideRecommended && !(Psgs.LimitQa < 0 || Psgs.LimitQa > 10000));

            w.Toggle(box, "Draw Trajectory on Map", () => Core.Guidance.ShouldDrawTrajectory, v => Core.Guidance.ShouldDrawTrajectory = v);
        }

        bool OutsideRecommended => Psgs.LimitQa < 1000 || Psgs.LimitQa > 4000;

        static readonly string[] coastLocations = { "Before", "During", "After" };

        // modern layout: titled groups of aligned fields, the coast location as one choice, the Qα warnings in a status bar
        void BuildModern(KodeWindow w)
        {
            UIObject r = w.Root;

            Layout stages = KodeWindow.Panel(r);
            KodeWindow.Header(stages, "#MechJeb_NewUI_Stages"); //Stages
            BuildStageTable(w, stages);
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
            ModernRows.Field(coastTimes, "Min Coast", Psgs.MinCoast, "s", 50);
            ModernRows.Field(coastTimes, "Max Coast", Psgs.MaxCoast, "s", 50);
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
            ModernRows.Field(aero, "Aref", Psgs.Aref, "m²", 50);

            g = w.Group(r, "#MechJeb_NewUI_Limits"); //Limits
            KodeWindow.PropertyGrid limits = w.Properties(g);
            ModernRows.Field(limits, "#MechJeb_Ascent_label17", Psgs.LimitQa, "Pa-rad", 60); //Qα limit
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

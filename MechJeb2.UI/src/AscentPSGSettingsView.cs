using System;
using KodeKit;
using KodeUI;
using MechJebLib.PSG;
using UnityEngine;
using Tone = KodeKit.KodeWindow.Tone;

namespace MuMech.UI
{
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
            UpdateAero();
        }

        // what AutoAero filled Cd and Aref with, and from what. Rebuilt only when the estimate changes.
        string aeroCd = "", aeroAref = "", aeroSource = "";
        double aeroKey = double.NaN;

        void UpdateAero()
        {
            if (!Psgs.AutoAero)
                return;

            if (HighLogic.LoadedSceneIsFlight)
                Psgs.UpdateAutoAero();

            PSGAeroEstimate e = Psgs.AeroEstimate;
            double key = ((Psgs.Cd * 7919 + Psgs.Aref) * 31 + e.CdA) * 31 + (int)e.Source * 2 + (e.FromPlan ? 1 : 0) +
                (double.IsNaN(e.MachAtMaxQ) ? 0 : Math.Round(e.MachAtMaxQ, 2) * 1e-3);
            if (key == aeroKey) return;
            aeroKey = key;

            aeroCd = Psgs.Cd.Val.ToString("0.###");
            aeroAref = Psgs.Aref.Val.ToString("0.###");
            aeroSource = AeroSource(e);
        }

        static string AeroSource(PSGAeroEstimate e)
        {
            if (e.Source == PSGAeroEstimate.Model.NONE)
                return "Worked out from the vessel in flight, before each PSG solve.";

            string model = e.Source == PSGAeroEstimate.Model.FAR ? "FAR" : "the parts' drag cubes";
            string where = e.FromPlan
                ? $"averaged along PSG's ascent (maximum Q at Mach {e.MachAtMaxQ:0.0})"
                : "averaged over Mach 0.5 to 2 until PSG has a plan";
            return $"Drag area {e.CdA:0.###} m² from {model}, nose first, {where}.";
        }

        // the burns Min ΔV leaves out of the plan: some Δv, but under Min ΔV. Rebuilt only when that set changes, as the
        // label reads it every frame.
        string skipped;
        double skippedKey = double.NaN;

        void UpdateSkipped()
        {
            System.Collections.Generic.List<MechJebLib.FuelFlowSimulation.FuelStats> vac = Core.StageStats.VacStats;
            double min = Psgs.MinDeltaV;
            int top = Psgs.PlannedLastKSPStage();
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
        // the PSG Stages window, where the plan is edited (which stages burn, their flags, the coast), and its reset
        void BuildEditorRow(KodeWindow w, UIObject parent)
        {
            MechJebModuleAscentPSGStagesMenu editor = Core.GetComputerModule<MechJebModuleAscentPSGStagesMenu>();
            Layout row = KodeWindow.Row(parent);
            KodeWindow.FlexibleSpace(row);
            UIButton reset = KodeWindow.Button(row, "Reset plan", editor.ResetPlan);
            Tooltips.Add(reset.gameObject, ResetTooltip);
            w.Visible(reset.gameObject, () => Psgs.PlanEdited && !Psgs.PlanLocked);
            KodeWindow.Button(row, "Stage editor", () => editor.Enabled = !editor.Enabled);
        }

        public const string ResetTooltip = "Forget the edits: the default plan, every burn guided, with no coast.";

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

        bool OutsideRecommended => Psgs.LimitQa < 1000 || Psgs.LimitQa > 4000;

        const string RelightNote = "An engine of the coast stage can't light again after the coast (no restart, or no ignitions left).";

        const string ArefNote = "Aref is 0: PSG plans the ascent with no drag. Set the rocket's cross-section area.";

        // Titled groups of aligned fields, the Qα warnings in a status bar
        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            Layout stages = KodeWindow.Panel(r);
            KodeWindow.Header(stages, "#MechJeb_NewUI_Stages"); //Stages
            BuildStageTable(w, stages);
            BuildSkipped(w, stages);
            BuildEditorRow(w, stages);
            w.Visible(stages.gameObject, () => hasStages);

            KodeWindow.PropertyGrid plan = w.Properties(r);
            OptionRows.Field(plan, "Min ΔV", Psgs.MinDeltaV, "m/s", 50);

            // which stages burn, their flags and where the coast goes are set in the Stage editor
            UIObject g = w.Group(r, "#MechJeb_NewUI_Coast"); //Coast
            KodeWindow.PropertyGrid coast = w.Properties(g);
            UIText relight = coast.Note(RelightNote);
            KodeWindow.ToneText(relight, Tone.Caution);
            KodeWindow.WrapToWidth(relight);
            w.Visible(relight.transform.parent.gameObject, () => PsgAttention.CoastNoRelight(Core));
            KodeWindow.PropertyGrid coastTimes = w.Properties(g);
            w.Attention(OptionRows.Field(coastTimes, "Min Coast", Psgs.MinCoast, "s", 50), () => PsgAttention.CoastTimesInverted(Core));
            w.Attention(OptionRows.Field(coastTimes, "Max Coast", Psgs.MaxCoast, "s", 50), () => PsgAttention.CoastTimesInverted(Core));
            OptionRows.Field(coastTimes, "Ullage lead time", Core.Guidance.UllageLeadTime, "s", 50);

            g = w.Group(r, "#MechJeb_NewUI_Spinup"); //Spinup
            KodeWindow.PropertyGrid spin = w.Properties(g);
            OptionRows.Field(spin, "ω", Settings.SpinupAngularVelocity, "rpm", 50);
            OptionRows.Field(spin, "Spinup lead time", Settings.SpinupLeadTime, "s", 50);

            g = w.Group(r, "#MechJeb_NewUI_BoosterDrag"); //Booster and drag
            KodeWindow.PropertyGrid aero = w.Properties(g);
            OptionRows.Field(aero, "#MechJeb_Ascent_label13", Settings.PitchStartHeight, "m", 50); //Booster Pitch start:
            OptionRows.Field(aero, "#MechJeb_Ascent_label14", Settings.PitchRate, "°/s", 50); //Booster Pitch rate:
            OptionRows.Switch(aero, "Cd and Aref from the vessel", () => Psgs.AutoAero, v => Psgs.AutoAero = v);
            Layout cdField = OptionRows.Field(aero, "Cd", Psgs.Cd, "", 50);
            Layout arefField = OptionRows.Field(aero, "Aref", Psgs.Aref, "m²", 50);
            w.Attention(arefField, () => PsgAttention.ArefUnset(Core));
            w.Visible(cdField.gameObject, () => !Psgs.AutoAero);
            w.Visible(arefField.gameObject, () => !Psgs.AutoAero);
            Layout cdValue = aero.Value("Cd", () => aeroCd);
            Layout arefValue = aero.Value("Aref", () => aeroAref, "m²");
            w.Visible(cdValue.gameObject, () => Psgs.AutoAero);
            w.Visible(arefValue.gameObject, () => Psgs.AutoAero);
            UIText aeroNote = aero.Note("");
            KodeWindow.WrapToWidth(aeroNote);
            string shownSource = null;
            w.AddSync(() =>
            {
                if (ReferenceEquals(shownSource, aeroSource)) return;
                shownSource = aeroSource;
                aeroNote.Text(aeroSource);
            });
            w.Visible(aeroNote.transform.parent.gameObject, () => Psgs.AutoAero);
            UIText arefNote = aero.Note(ArefNote);
            KodeWindow.ToneText(arefNote, Tone.Caution);
            KodeWindow.WrapToWidth(arefNote);
            w.Visible(arefNote.transform.parent.gameObject, () => PsgAttention.ArefUnset(Core));

            g = w.Group(r, "#MechJeb_NewUI_Limits"); //Limits
            KodeWindow.PropertyGrid limits = w.Properties(g);
            w.Attention(OptionRows.Field(limits, "#MechJeb_Ascent_label17", Psgs.LimitQa, "Pa-rad", 60), () => PsgAttention.QaClamped(Core)); //Qα limit
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

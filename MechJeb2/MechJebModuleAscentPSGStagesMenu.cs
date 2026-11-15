extern alias JetBrainsAnnotations;
using System.Collections.Generic;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.PSG;

#nullable enable

namespace MuMech
{
    /// <summary>
    ///     The PSG Stages window: the vessel's stages as the PSG plan uses them, edited directly (PlanEditor) instead of with
    ///     KSP stage numbers. An edit keeps the plan as the vessel's own (PSGSimulation.PlanEdited); Reset to settings goes back
    ///     to generating it from the stage-number fields. The plan is frozen once the ascent is engaged, so the window only
    ///     shows it then.
    /// </summary>
    public class MechJebModuleAscentPSGStagesMenu : DisplayModule
    {
        public MechJebModuleAscentPSGStagesMenu(MechJebCore core) : base(core) { }

        public override bool ShowInMenu => false;

        private MechJebModulePSGSimulation Psgs => Core.PSGSimulation;

        public List<FuelStats> VacStats => Core.StageStats.VacStats;

        // the plan can only be edited before the ascent is engaged: PSGSimulation freezes the flight plan for the flight
        public bool CanEdit => !Psgs.Enabled && VacStats.Count > 0;

        public bool PlanEdited => Psgs.PlanEdited;

        // what the window has selected, kept here because a rebuild makes a new view: a burn row's index in flight order, or the
        // coast
        public int SelectedBurn;
        public bool CoastSelected;

        /// <summary>The current plan as an editor, read from the stage stats and the saved or generated plan.</summary>
        public PlanEditor Read() => new PlanEditor(Psgs.LogicalPlan?.Plan ?? new LogicalPlan(), VacStats, Psgs.MinDeltaV);

        /// <summary>Keeps editor's plan as the vessel's own, edited plan.</summary>
        public void Apply(PlanEditor editor)
        {
            if (!CanEdit) return;
            Psgs.SetEditedPlan(editor.Plan());
            Dirty = true;
        }

        /// <summary>
        ///     Forgets the edits and the stage-number settings the window replaced (Last Stage, Fixed, Unguided, Coast Stage, which
        ///     no window shows any more): the plan is the default one again, every burn guided with no coast. kOS still sets them.
        /// </summary>
        public void ResetToSettings()
        {
            if (!CanEdit) return;
            MechJebModuleAscentSettings s = Core.AscentSettings;
            s.LastStage.Val = -1;
            s.FixedStagesFlag = s.UnguidedStagesFlag = s.CoastStageFlag = false;
            s.CoastLocation = -1;
            s.Dirty = true;
            Psgs.ResetPlanToSettings(s);
            Dirty = true;
        }

        // The options of a stage the vessel stages into (PlanEditor.Row.Staged), by its KSP stage. They are flight settings, not
        // part of the plan: they apply whether or not the plan is edited, and stay editable in flight.

        /// <summary>Spin up in the stage before kspStage, holding the staging until spun (AscentSettings' one spin-up stage).</summary>
        public bool SpinBefore(int kspStage) => Core.AscentSettings.SpinupStage == kspStage + 1;

        public void SetSpinBefore(int kspStage, bool on)
        {
            MechJebModuleAscentSettings s = Core.AscentSettings;
            if (on)
            {
                s.SpinupStageFlag = true;
                s.SpinupStageInternal.Val = kspStage + 1;
            }
            else if (SpinBefore(kspStage))
                s.SpinupStageFlag = false;

            s.Dirty = true;
        }

        /// <summary>Hot staging is on for every stage in the staging settings: the rows show it and can't turn it off.</summary>
        public bool HotStagingEverywhere => Core.Staging.HotStaging;

        public double HotStagingLeadTime => Core.Staging.HotStagingLeadTime;

        public bool HotStageInto(int kspStage) => Core.Staging.HotStagesInto(kspStage);

        public void SetHotStageInto(int kspStage, bool on)
        {
            Core.Staging.HotStagingStages.Set(kspStage, on);
            Core.Staging.Dirty = true;
        }

        /// <summary>"Use RCS to ullage" is off in the thrust settings: no stage ullages with RCS before it lights.</summary>
        public bool RcsUllageOff => !Core.Thrust.AutoRCSUllaging;

        public bool RcsUllage(int kspStage) => Core.Thrust.RcsUllageFor(kspStage);

        public void SetRcsUllage(int kspStage, bool on)
        {
            Core.Thrust.NoRcsUllageStages.Set(kspStage, !on);
            Core.Thrust.Dirty = true;
        }

        // the stage stats follow the vessel while the window is open, as the Ascent window does
        public override void OnFixedUpdate()
        {
            if (Vessel == null || !Vessel.isActiveVessel) return;
            Core.StageStats.RequestUpdate();
            if (!Psgs.Enabled) Psgs.UpdateLogicalPlan(Core.AscentSettings);
        }

        public override string GetName() => "PSG Stages";

        public override string IconName() => "PSGStages";
    }
}

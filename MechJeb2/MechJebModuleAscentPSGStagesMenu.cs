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

        public void ResetToSettings()
        {
            if (!CanEdit) return;
            Psgs.ResetPlanToSettings(Core.AscentSettings);
            Dirty = true;
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

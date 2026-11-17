extern alias JetBrainsAnnotations;
using System.Collections.Generic;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.PSG;

#nullable enable

namespace MuMech
{
    /// <summary>
    ///     The PSG Stages window: the vessel's stages as the PSG plan uses them, and the plan edited directly (PlanEditor). An
    ///     edit keeps the plan as the vessel's own (PSGSimulation.PlanEdited); Reset plan goes back to the default plan. The
    ///     plan is locked once the ascent is engaged or the vessel leaves the ground (PSGSimulation.PlanLocked), so the window
    ///     only shows it then.
    /// </summary>
    public class MechJebModuleAscentPSGStagesMenu : DisplayModule
    {
        public MechJebModuleAscentPSGStagesMenu(MechJebCore core) : base(core) { }

        public override bool ShowInMenu => false;

        private MechJebModulePSGSimulation Psgs => Core.PSGSimulation;

        public List<FuelStats> VacStats => Core.StageStats.VacStats;

        public bool CanEdit => !Psgs.PlanLocked && VacStats.Count > 0;

        public bool PlanEdited => Psgs.PlanEdited;

        // what the window has selected, kept here because a rebuild makes a new view: a burn row's index in flight order, or the
        // coast
        public int SelectedBurn;
        public bool CoastSelected;

        /// <summary>The current plan as an editor, read from the stage stats and the saved or default plan.</summary>
        public PlanEditor Read() => new PlanEditor(Psgs.LogicalPlan?.Plan ?? new LogicalPlan(), VacStats, Psgs.MinDeltaV);

        /// <summary>Keeps editor's plan as the vessel's own, edited plan.</summary>
        public void Apply(PlanEditor editor)
        {
            if (!CanEdit) return;
            if (Psgs.SetEditedPlan(editor.Plan()))
                Dirty = true;
        }

        /// <summary>Forgets the edits: the plan is the default one again, every burn guided with no coast.</summary>
        public void ResetPlan()
        {
            if (!CanEdit) return;
            if (Psgs.ResetPlan())
                Dirty = true;
        }

        // the stage stats follow the vessel while the window is open, as the Ascent window does
        public override void OnFixedUpdate()
        {
            if (Vessel == null || !Vessel.isActiveVessel) return;
            Core.StageStats.RequestUpdate();
            Psgs.UpdateLogicalPlan();
        }

        public override string GetName() => "PSG Stages";

        public override string IconName() => "PSGStages";
    }
}

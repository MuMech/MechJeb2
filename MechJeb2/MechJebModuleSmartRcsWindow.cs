using KSP.Localization;

namespace MuMech
{
    // The SmartRcs window: its open state and position. MechJebModuleSmartRcs does the work, with the window open or closed.
    public class MechJebModuleSmartRcsWindow : DisplayModule
    {
        private MechJebModuleSmartRcs _smartRcs;

        public MechJebModuleSmartRcsWindow(MechJebCore core) : base(core) { }

        internal MechJebModuleSmartRcs SmartRcs => _smartRcs ??= Core.GetComputerModule<MechJebModuleSmartRcs>();

        public override string LogicModuleName => nameof(MechJebModuleSmartRcs);

        protected override ComputerModule ActiveUser => SmartRcs;

        public override string GetName() => Localizer.Format("#MechJeb_SmartRcs_title"); //"SmartRcs"

        public override string IconName() => "SmartRcs";
    }
}

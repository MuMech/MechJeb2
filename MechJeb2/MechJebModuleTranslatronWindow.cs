using KSP.Localization;

namespace MuMech
{
    // The Translatron window: its open state and position. MechJebModuleTranslatron does the work, with the window open or closed.
    public class MechJebModuleTranslatronWindow : DisplayModule
    {
        private MechJebModuleTranslatron _translatron;

        public MechJebModuleTranslatronWindow(MechJebCore core) : base(core) { }

        public MechJebModuleTranslatron Translatron => _translatron ??= Core.GetComputerModule<MechJebModuleTranslatron>();

        public override string LogicModuleName => nameof(MechJebModuleTranslatron);

        protected override ComputerModule ActiveUser => Translatron;

        public override string GetName() => Localizer.Format("#MechJeb_Translatron_title");

        public override string IconName() => "Translatron";
    }
}

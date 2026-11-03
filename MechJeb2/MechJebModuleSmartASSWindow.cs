using KSP.Localization;

namespace MuMech
{
    // The SmartASS window: its open state and position. MechJebModuleSmartASS does the work, with the window open or closed.
    public class MechJebModuleSmartASSWindow : DisplayModule
    {
        public MechJebModuleSmartASSWindow(MechJebCore core) : base(core) { }

        public override string LogicModuleName => nameof(MechJebModuleSmartASS);

        protected override ComputerModule ActiveUser => Core.SmartASS;

        public override string GetName() =>
            Core.eduMode
                ? Localizer.Format("#MechJeb_SmartACS_title")
                : Localizer.Format("#MechJeb_SmartASS_title"); //"Smart A.C.S.":"Smart A.S.S."

        public override string IconName() => Core.eduMode ? "Smart A.C.S." : "Smart A.S.S.";
    }
}

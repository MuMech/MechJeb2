using KSP.Localization;

namespace MuMech
{
    // The Warp Helper window: its open state and position. MechJebModuleWarpHelper does the work, with the window open or closed.
    public class MechJebModuleWarpHelperWindow : DisplayModule
    {
        private MechJebModuleWarpHelper _helper;

        public MechJebModuleWarpHelperWindow(MechJebCore core) : base(core) { }

        public MechJebModuleWarpHelper Helper => _helper ??= Core.GetComputerModule<MechJebModuleWarpHelper>();

        public override string LogicModuleName => nameof(MechJebModuleWarpHelper);

        public override bool IsActive() => Helper.warping;

        public override string GetName() => Localizer.Format("#MechJeb_WarpHelper_title"); //"Warp Helper"

        public override string IconName() => "Warp Helper";
    }
}

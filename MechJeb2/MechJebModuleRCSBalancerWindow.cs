extern alias JetBrainsAnnotations;
using KSP.Localization;

namespace MuMech
{
    // The RCS Balancer window: its open state and position. MechJebModuleRCSBalancer runs on its smart translation setting, with
    // the window open or closed.
    public class MechJebModuleRCSBalancerWindow : DisplayModule
    {
        private MechJebModuleRCSBalancer _balancer;

        public MechJebModuleRCSBalancerWindow(MechJebCore core) : base(core) { }

        public MechJebModuleRCSBalancer Balancer => _balancer ??= Core.GetComputerModule<MechJebModuleRCSBalancer>();

        // the balancer is its own user, so the menu entry shows active while it balances
        protected override ComputerModule ActiveUser => Balancer;

        public override string GetName() => Localizer.Format("#MechJeb_RCSBalancer_title"); //"RCS Balancer"

        public override string IconName() => "RCS Balancer";
    }
}

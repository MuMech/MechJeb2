extern alias JetBrainsAnnotations;
using KSP.Localization;

namespace MuMech
{
    // The SmartRcs logic: it runs all flight, with the window (MechJebModuleSmartRcsWindow) open or closed
    internal class MechJebModuleSmartRcs : ComputerModule
    {
        public enum Target
        {
            OFF,
            ZERO_RVEL
        }

        public static readonly string[]
            TargetTexts = { Localizer.Format("#MechJeb_SmartRcs_button1"), Localizer.Format("#MechJeb_SmartRcs_button2") }; //"OFF", "ZERO RVEL"

        public Target target;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool autoDisableSmartRCS = true;

        [GeneralInfoItem("#MechJeb_DisableSmartRcsAutomatically", InfoItem.Category.Misc)] //Disable SmartRcs automatically
        public void AutoDisableSmartRCS() { } //"Disable SmartRcs automatically"

        public MechJebModuleSmartRcs(MechJebCore core) : base(core) => Enabled = true;

        /// <summary>
        /// True while another module uses the RCS controller: SmartRcs then shows only "AUTO". Set every tick, with the window
        /// open or closed.
        /// </summary>
        internal bool RcsUsedByOtherModule { get; private set; }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            // a lost target, or another module, cleared the RCS controller's users: SmartRcs no longer holds anything, so it shows OFF
            if (target != Target.OFF && !Core.RCS.Users.Contains(this))
                target = Target.OFF;

            RcsUsedByOtherModule = CheckOtherUsers();
        }

        // with autoDisableSmartRCS, SmartRcs steps aside as soon as another module takes the RCS controller, so it doesn't
        // suddenly turn on when that autopilot finishes
        private bool CheckOtherUsers()
        {
            if (!Core.RCS.Enabled)
                return false;

            bool other = false;
            for (int i = 0; i < Core.RCS.Users.Count; i++)
            {
                if (!Equals(Core.RCS.Users[i]))
                {
                    other = true;
                    break;
                }
            }

            if (!other)
                return false;

            if (autoDisableSmartRCS)
            {
                target = Target.OFF;
                if (Core.RCS.Users.Contains(this))
                    Core.RCS.Users.Remove(this);
            }

            return true;
        }

        public void Engage()
        {
            switch (target)
            {
                case Target.OFF:
                    Core.RCS.Users.Remove(this);
                    return;
                case Target.ZERO_RVEL:
                    Core.RCS.Users.Add(this);
                    Core.RCS.SetTargetRelative(Vector3d.zero);
                    break;
            }
        }
    }
}

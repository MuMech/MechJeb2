extern alias JetBrainsAnnotations;
using UnityEngine;
using UnityEngine.Profiling;

namespace MuMech
{
    public class MechJebModuleAscentSettingsMenu : DisplayModule
    {
        public MechJebModuleAscentSettingsMenu(MechJebCore core) : base(core) { }

        public override bool ShowInMenu => false;

        private  MechJebModuleAscentSettings      _ascentSettings => Core.AscentSettings;
        internal MechJebModuleAscentBaseAutopilot _autopilot      => Core.Ascent;

        internal readonly string _climbString = $"{CachedLocalizer.Instance.MechJebAscentLabel22}: ";
        internal readonly string _turnString = $"{CachedLocalizer.Instance.MechJebAscentLabel23}: ";

        // PSG manages the throttle itself (shared with the new UI, which calls it every frame the window is shown)
        internal void ForcePsgThrustLimits()
        {
            if (_ascentSettings.AscentType == AscentType.PSG)
            {
                Core.Thrust.LimitThrottle = false;
                Core.Thrust.ElectricThrottle = false;
            }
        }

        public override string GetName() => "Ascent Settings";

        public override string IconName() => "Ascent Settings";
    }
}

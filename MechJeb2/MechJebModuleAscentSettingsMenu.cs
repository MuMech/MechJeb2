extern alias JetBrainsAnnotations;
using UnityEngine;
using UnityEngine.Profiling;

namespace MuMech
{
    public class MechJebModuleAscentSettingsMenu : DisplayModule
    {
        public MechJebModuleAscentSettingsMenu(MechJebCore core) : base(core) { }

        public override bool ShowInMenu => false;

        private MechJebModuleAscentSettings _ascentSettings => Core.AscentSettings;

        internal readonly string _climbString = $"{CachedLocalizer.Instance.MechJebAscentLabel22}: ";
        internal readonly string _turnString = $"{CachedLocalizer.Instance.MechJebAscentLabel23}: ";

        // the new UI calls it every frame the window is shown, so the window shows the limits PSG flies with
        internal void ForcePsgThrustLimits()
        {
            if (_ascentSettings.AscentType == AscentType.PSG)
                _ascentSettings.ForcePsgThrustLimits();
        }

        public override string GetName() => "Ascent Settings";

        public override string IconName() => "Ascent Settings";
    }
}

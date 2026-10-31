extern alias JetBrainsAnnotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleAttitudeAdjustment : DisplayModule
    {
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool showInfos;

        public MechJebModuleAttitudeAdjustment(MechJebCore core) : base(core) { }

        public override string GetName() => Localizer.Format("#MechJeb_AttitudeAdjust_title"); //Attitude Adjustment

        public override string IconName() => "Attitude Adjustment";
    }
}

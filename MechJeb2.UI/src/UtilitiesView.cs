using KodeKit;
using KodeUI;
using MechJebLibBindings;
using UnityEngine;
using static MuMech.MechJebModuleThrustController;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleThrustWindow ("Utilities") in KodeKit, row for row with its WindowGUI. The limiter and autostage rows
    /// are the general info items' native builders (GeneralItemBuilders), shared with the custom windows.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleThrustWindow))]
    public class UtilitiesView : KodeView
    {
        readonly MechJebModuleThrustWindow window;

        public UtilitiesView(MechJebModuleThrustWindow window) : base(window) => this.window = window;

        protected override float MinWidth => 250;

        MechJebCore Core => window.Core;
        MechJebModuleThrustController Thrust => window.Core.Thrust;

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            GeneralItemBuilders.Build(w, r, "General:ThrustController.LimitToMaxDynamicPressureInfoItem", Thrust);
            GeneralItemBuilders.Build(w, r, "General:ThrustController.LimitToPreventOverheatsInfoItem", Thrust);
            GeneralItemBuilders.Build(w, r, "General:ThrustController.LimitAccelerationInfoItem", Thrust);
            GeneralItemBuilders.Build(w, r, "General:ThrustController.LimitThrottleInfoItem", Thrust);
            GeneralItemBuilders.Build(w, r, "General:ThrustController.LimiterMinThrottleInfoItem", Thrust);
            GeneralItemBuilders.Build(w, r, "General:ThrustController.LimitElectricInfoItem", Thrust);
            GeneralItemBuilders.Build(w, r, "General:ThrustController.LimitToPreventFlameoutInfoItem", Thrust);
            if (ReflectionUtils.IsLoadedRealFuels)
            {
                // does nothing in stock, so it is not shown if RF is not loaded
                GeneralItemBuilders.Build(w, r, "General:ThrustController.LimitToPreventUnstableIgnitionInfoItem", Thrust);
                GeneralItemBuilders.Build(w, r, "General:ThrustController.AutoRCsUllageInfoItem", Thrust);
            }

            w.Toggle(r, "#MechJeb_Utilities_checkbox3", () => Thrust.ManageIntakes, v => Thrust.ManageIntakes = v);
            Layout jet = KodeWindow.Row(r);
            KodeWindow.Label(jet, "#MechJeb_Utilities_label2", true);
            EditFields.Field(w, jet, () => Thrust.FlameoutSafetyPct.Text, s => Thrust.FlameoutSafetyPct.Text = s, 40, () => Thrust.FlameoutSafetyPct.IsParsed);
            KodeWindow.Label(jet, "%");

            // DifferentialThrottleMenu + the failure messages under it
            GeneralItemBuilders.Build(w, r, "General:ThrustController.DifferentialThrottleMenu", Thrust);
            UIText diffStatus = w.BoundLabel(r, DifferentialStatusText, true);
            diffStatus.tmpText.enableWordWrapping = true;
            Color Warn() => Theme.Current.Warn;
            diffStatus.tmpText.color = Warn();
            w.Behaviour.AddRestyle(() => diffStatus.tmpText.color = Warn());
            w.Visible(diffStatus.transform.parent.gameObject,
                () => Thrust.DifferentialThrottle && window.Vessel.LiftedOff() && Thrust.DifferentialThrottleSuccess != DifferentialThrottleStatus.SUCCESS);

            GeneralItemBuilders.Build(w, r, "General:SolarPanelController.SolarPanelDeployButton", Core.Solarpanel);
            GeneralItemBuilders.Build(w, r, "General:DeployableAntennaController.AntennaDeployButton", Core.AntennaControl);

            GeneralItemBuilders.Build(w, r, "General:ThrustWindow.Autostage", window);
            UIButton once = KodeWindow.Button(r, "#MechJeb_Utilities_button1", () => Core.Staging.AutostageOnce(window), true);
            w.Visible(once.gameObject, () => !Core.Staging.Enabled);

            Layout autostageSettings = r.Add<Layout>("AutostageSettings");
            autostageSettings.Vertical().ControlChildSize(true, true).ChildForceExpand(true, false);
            autostageSettings.FlexibleLayout(true, false);
            autostageSettings.Finish();
            GeneralItemBuilders.Build(w, autostageSettings, "General:StagingController.AutostageSettingsInfoItem", Core.Staging);
            w.Visible(autostageSettings.gameObject, () => Core.Staging.Enabled);

            UIText status = w.BoundLabel(r, () => Core.Staging.AutostageStatus(), true);
            w.Visible(status.transform.parent.gameObject, () => Core.Staging.Enabled);
        }

        string DifferentialStatusText()
        {
            switch (Thrust.DifferentialThrottleSuccess)
            {
                case DifferentialThrottleStatus.MORE_ENGINES_REQUIRED: return KSP.Localization.Localizer.Format("#MechJeb_Utilities_label3");
                case DifferentialThrottleStatus.ALL_ENGINES_OFF: return KSP.Localization.Localizer.Format("#MechJeb_Utilities_label4");
                case DifferentialThrottleStatus.SOLVER_FAILED: return KSP.Localization.Localizer.Format("#MechJeb_Utilities_label5");
                default: return "";
            }
        }
    }
}

using System;
using KodeKit;
using KodeUI;
using MechJebLibBindings;
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
            if (KodeWindow.ModernLayout)
            {
                BuildModern(w);
                return;
            }

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
            EditUi.Field(w, jet, () => Thrust.FlameoutSafetyPct, 40);
            KodeWindow.Label(jet, "%");

            // DifferentialThrottleMenu + the failure messages under it
            GeneralItemBuilders.Build(w, r, "General:ThrustController.DifferentialThrottleMenu", Thrust);
            UIText diffStatus = OpUi.DynamicParagraph(w, r, DifferentialStatus(), true);
            w.Visible(diffStatus.gameObject, DifferentialFailed);

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

            UIText status = w.BoundLabel(r, AutostageStatus(), true);
            w.Visible(status, () => Core.Staging.Enabled);
        }

        // modern layout: the same options in property grids (switches on the left, fields dimmed when off), under headers
        void BuildModern(KodeWindow w)
        {
            UIObject r = w.Root;

            UIObject g = w.Group(r, "#MechJeb_NewUI_ThrottleLimits"); //Throttle limits
            ModernRows.ThrustLimits(w, g, Thrust, null, false);

            g = w.Group(r, "#MechJeb_NewUI_Jets"); //Jet engines
            KodeWindow.PropertyGrid jets = w.Properties(g);
            ModernRows.Switch(jets, "#MechJeb_Ascent_checkbox13", () => Thrust.LimitToPreventFlameout, v => Thrust.LimitToPreventFlameout = v,
                tone: LimiterTones.For(Thrust, LimitMode.FLAMEOUT)); //"Prevent jet flameout"
            ModernRows.Switch(jets, "#MechJeb_Utilities_checkbox3", () => Thrust.ManageIntakes, v => Thrust.ManageIntakes = v);
            ModernRows.Field(jets, "#MechJeb_Utilities_label2", Thrust.FlameoutSafetyPct, "%", 50);

            g = w.Group(r, "#MechJeb_NewUI_Other"); //Other
            KodeWindow.PropertyGrid other = w.Properties(g);
            ModernRows.Switch(other, "#MechJeb_Ascent_checkbox19", () => Thrust.DifferentialThrottle, LimiterTones.SetDifferential(Thrust),
                tone: () => LimiterTones.Differential(Thrust)); //"Differential throttle"
            Layout diffBar = w.StatusBar(g, () => KodeWindow.Tone.Warn);
            OpUi.DynamicParagraph(w, diffBar, DifferentialStatus());
            w.Visible(diffBar.gameObject, DifferentialFailed);

            g = w.Group(r, "#MechJeb_NewUI_Deployment"); //Deployment
            DeployRow(w, g, Core.Solarpanel, "#MechJeb_SolarPanelDeployButton"); //"Auto-deploy solar panels"
            DeployRow(w, g, Core.AntennaControl, "#MechJeb_Autodeployantennas"); //"Auto-deploy antennas"

            g = w.Group(r, "#MechJeb_NewUI_Autostage"); //Autostage
            Layout auto = KodeWindow.Row(g);
            UnityEngine.UI.Toggle autostage = w.SwitchRow(auto, "#MechJeb_Utilities_checkbox1", () => Core.Staging.Users.Contains(window), v =>
            {
                if (v) Core.Staging.Users.Add(window);
                else Core.Staging.Users.Remove(window);
                window.autostageSavedState = v;
            }); //"Autostage"
            // the IMGUI item stored the state on every draw
            w.AddSync(() => window.autostageSavedState = Core.Staging.Users.Contains(window));
            UIButton once = KodeWindow.Button(auto, "#MechJeb_Utilities_button1", () => Core.Staging.AutostageOnce(window)); //"Autostage once"
            w.Visible(once.gameObject, () => !Core.Staging.Enabled);

            Layout settings = KodeWindow.Panel(g);
            ModernRows.AutostageSettings(w, settings, Core.Staging);
            w.Visible(settings.gameObject, () => Core.Staging.Enabled);

            Layout statusBar = w.StatusBar(g, () => KodeWindow.Tone.Normal);
            w.BoundLabel(statusBar, AutostageStatus(), true);
            w.Visible(statusBar.gameObject, () => Core.Staging.Enabled);
        }

        /// <summary>[switch] Auto-deploy ..., and the "Toggle ... (currently ...)" button under it (beside it, the window would double in width).</summary>
        static void DeployRow(KodeWindow w, UIObject parent, MechJebModuleDeployableController c, string autoKey)
        {
            w.SwitchRow(parent, autoKey, () => c.AutoDeploy, v => c.AutoDeploy = v);
            UIButton b = KodeWindow.Button(parent, c.CurrentButtonText ?? " ", c.ToggleDeploy, true);
            string shown = c.CurrentButtonText;
            w.AddSync(() =>
            {
                string s = c.CurrentButtonText;
                if ((object)s == shown) return; // the controller swaps between cached strings
                shown = s;
                b.Text(s ?? " ");
            });
        }

        bool DifferentialFailed() =>
            Thrust.DifferentialThrottle && window.Vessel.LiftedOff() && Thrust.DifferentialThrottleSuccess != DifferentialThrottleStatus.SUCCESS;

        // the texts, made when what they show changes (AutostageStatus concatenates a new string on every call)
        Func<string> DifferentialStatus() => Memo.Of(() => (int)Thrust.DifferentialThrottleSuccess, _ => DifferentialStatusText());

        Func<string> AutostageStatus() =>
            Memo.Of(() => (Core.Staging.Enabled, Core.Staging.AutostagingOnce, (int)Core.Staging.AutostageLimit), _ => Core.Staging.AutostageStatus());

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

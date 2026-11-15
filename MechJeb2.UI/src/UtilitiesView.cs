using System;
using KodeKit;
using KodeUI;
using MechJebLibBindings;
using static MuMech.MechJebModuleThrustController;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleThrustWindow ("Utilities") in KodeKit. The limiter and autostage rows
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

        // The same options in property grids (switches on the left, fields dimmed when off), under headers
        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            UIObject g = w.Group(r, "#MechJeb_NewUI_ThrottleLimits"); //Throttle limits
            KodeWindow.PropertyGrid limits = OptionRows.ThrustLimits(w, g, Thrust, Thrust, OptionRows.GeneralLimitsApply(Thrust), null, false);
            // greyed while an autopilot flies with its own throttle limits: these apply again when it stops
            limits.Dim(limits.Table.gameObject, null, () => Thrust.LimitsOverrideUser != null);

            g = w.Group(r, "#MechJeb_NewUI_Jets"); //Jet engines
            KodeWindow.PropertyGrid jets = w.Properties(g);
            OptionRows.Switch(jets, "#MechJeb_Ascent_checkbox13", () => Thrust.LimitToPreventFlameout, v => Thrust.LimitToPreventFlameout = v,
                tone: LimiterTones.For(Thrust, LimitMode.FLAMEOUT)); //"Prevent jet flameout"
            OptionRows.Switch(jets, "#MechJeb_Utilities_checkbox3", () => Thrust.ManageIntakes, v => Thrust.ManageIntakes = v);
            OptionRows.Field(jets, "#MechJeb_Utilities_label2", Thrust.FlameoutSafetyPct, "%", 50);

            g = w.Group(r, "#MechJeb_NewUI_Other"); //Other
            KodeWindow.PropertyGrid other = w.Properties(g);
            OptionRows.Switch(other, "#MechJeb_Ascent_checkbox19", () => Thrust.DifferentialThrottle, LimiterTones.SetDifferential(Thrust),
                tone: () => LimiterTones.Differential(Thrust)); //"Differential throttle"
            Layout diffBar = w.StatusBar(g, () => KodeWindow.Tone.Warn);
            OpUi.DynamicParagraph(w, diffBar, DifferentialStatus());
            w.Visible(diffBar.gameObject, DifferentialFailed);

            if (Core.Solarpanel != null || Core.AntennaControl != null)
            {
                g = w.Group(r, "#MechJeb_NewUI_Deployment"); //Deployment
                DeployRow(w, g, Core.Solarpanel, "#MechJeb_SolarPanelDeployButton"); //"Auto-deploy solar panels"
                DeployRow(w, g, Core.AntennaControl, "#MechJeb_Autodeployantennas"); //"Auto-deploy antennas"
            }

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
            KodeWindow.PropertyGrid rules = OptionRows.AutostageSettings(w, settings, Core.Staging, Core.Staging);
            // greyed while an autopilot stages by its own rules
            rules.Dim(rules.Table.gameObject, null, () => Core.Staging.RulesOverrideUser != null);
            w.Visible(settings.gameObject, () => Core.Staging.Enabled);

            Layout statusBar = w.StatusBar(g, () => KodeWindow.Tone.Normal);
            w.BoundLabel(statusBar, AutostageStatus(), true);
            w.Visible(statusBar.gameObject, () => Core.Staging.Enabled);
        }

        /// <summary>[switch] Auto-deploy ..., and the "Toggle ... (currently ...)" button under it (beside it, the window would double in width).</summary>
        static void DeployRow(KodeWindow w, UIObject parent, MechJebModuleDeployableController c, string autoKey)
        {
            // a part's blacklist can leave the core without the controller
            if (c == null) return;
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
            Memo.Of(() => (Core.Staging.Enabled, Core.Staging.AutostagingOnce, (int)Core.Staging.Rules.AutostageLimit), _ => Core.Staging.AutostageStatus());

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

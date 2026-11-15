using System;
using KodeKit;
using KodeUI;
using Tone = KodeKit.KodeWindow.Tone;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleAscentSettingsMenu ("Ascent Settings") in KodeKit.
    /// The thrust limiter rows are the ones of the Utilities window; the autostage settings are
    /// OptionRows.AutostageSettings.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleAscentSettingsMenu))]
    public class AscentSettingsView : KodeView
    {
        readonly MechJebModuleAscentSettingsMenu menu;
        MechJebModuleAscentClassicAutopilot classic;

        public AscentSettingsView(MechJebModuleAscentSettingsMenu menu) : base(menu) => this.menu = menu;

        protected override float MinWidth => 275;

        MechJebCore Core => menu.Core;
        MechJebModuleAscentSettings Settings => Core.AscentSettings;
        MechJebModuleAscentClassicAutopilot Classic => classic ??= Core.GetComputerModule<MechJebModuleAscentClassicAutopilot>();
        MechJebModuleThrustController Thrust => Core.Thrust;

        bool psg;

        protected override void BeforeSync()
        {
            psg = Settings.AscentType == AscentType.PSG;
        }

        // "° (x.x°)" after the AoA field, formatted when the tenth changes
        long aoaKey = long.MinValue;
        string aoaText;

        string CurrentMaxAoA()
        {
            double v = Settings.CurrentMaxAoA;
            long key = (long)Math.Round(v * 10);
            if (key != aoaKey || aoaText == null)
            {
                aoaKey = key;
                aoaText = $"° ({v:F1}°)";
            }

            return aoaText;
        }

        // About 25 options in five folding groups, each a property grid with the switches on the left
        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            UIObject g = w.Group(r, "#MechJeb_NewUI_ThrottleLimits"); //Throttle limits
            OptionRows.ThrustLimits(w, g, Thrust, () => psg, false);

            g = w.Group(r, "#MechJeb_NewUI_Steering"); //Steering
            KodeWindow.PropertyGrid steer = w.Properties(g);
            OptionRows.Switch(steer, "#MechJeb_Ascent_checkbox2", () => Settings.ForceRoll, v => Settings.ForceRoll = v); //Force Roll
            Layout climb = OptionRows.Field(steer, menu._climbString, Settings.VerticalRoll, "°", 50); //climb
            Layout turn = OptionRows.Field(steer, menu._turnString, Settings.TurnRoll, "°", 50); //turn
            Layout alt = OptionRows.Field(steer, "Alt: ", Settings.RollAltitude, "m", 50);
            foreach (Layout row in new[] { climb, turn, alt })
                w.Visible(row.gameObject, () => Settings.ForceRoll);

            // [switch] Limit AoA to [max] ° (x.x°): the unit cell shows the current limit
            Layout aoa = OptionRows.Switch(steer, "#MechJeb_Ascent_checkbox3", () => Classic.LimitAoA, v => Classic.LimitAoA = v,
                tone: () => Settings.LimitingAoA ? Tone.Good : Tone.Normal); //Limit AoA to
            TMPro.TMP_InputField maxAoA = EditFields.Field(w, aoa, () => Classic.MaxAoA.Text, s => Classic.MaxAoA.Text = s, 50,
                () => Classic.MaxAoA.IsParsed);
            UIText current = w.BoundLabel(aoa, "° (99.9°)", CurrentMaxAoA);
            steer.Dim(maxAoA.gameObject, KodeWindow.CellOf(current), () => !Classic.LimitAoA);
            Layout fadeout = OptionRows.Field(steer, "#MechJeb_Ascent_label24", Classic.AOALimitFadeoutPressure, "Pa", 60); //Dynamic Pressure Fadeout
            w.Visible(aoa.gameObject, () => !psg);
            w.Visible(fadeout.gameObject, () => !psg && Classic.LimitAoA);

            Layout corrective = OptionRows.Switch(steer, "#MechJeb_Ascent_checkbox4", () => Classic.CorrectiveSteering,
                v => Classic.CorrectiveSteering = v); //Corrective steering
            Layout gain = OptionRows.Field(steer, "#MechJeb_Ascent_label25", Classic.CorrectiveSteeringGain, "", 50); // Gain
            w.Visible(corrective.gameObject, () => Settings.AscentType == AscentType.CLASSIC);
            w.Visible(gain.gameObject, () => Settings.AscentType == AscentType.CLASSIC && Classic.CorrectiveSteering);

            g = w.Group(r, "#MechJeb_NewUI_Autostage"); //Autostage
            w.SwitchRow(g, "#MechJeb_Ascent_checkbox5", () => Settings.Autostage, v => Settings.Autostage = v); //Autostage
            Layout autostage = KodeWindow.Panel(g);
            OptionRows.AutostageSettings(w, autostage, Core.Staging);
            w.Visible(autostage.gameObject, () => Settings.Autostage);

            g = w.Group(r, "#MechJeb_NewUI_Deployment"); //Deployment
            w.SwitchRow(g, "#MechJeb_Ascent_checkbox6", () => Settings.AutoDeploySolarPanels, v => Settings.AutoDeploySolarPanels = v); //Auto-deploy solar panels
            w.SwitchRow(g, "#MechJeb_Ascent_checkbox7", () => Settings.AutoDeployAntennas, v => Settings.AutoDeployAntennas = v); //Auto-deploy antennas

            g = w.Group(r, "#MechJeb_NewUI_Other"); //Other
            NodeUi.AutowarpToggle(w, g, Core, "#MechJeb_Ascent_checkbox8"); //Auto-warp
            UnityEngine.UI.Toggle skip = w.SwitchRow(g, "#MechJeb_Ascent_checkbox9", () => Classic.SkipCircularization,
                v => Classic.SkipCircularization = v); //Skip Circularization
            w.Visible(skip, () => !psg);
            UnityEngine.UI.Toggle rss = w.SwitchRow(g, "Module disabling does not kill throttle", () => Core.Settings.RssMode, v => Core.Settings.RssMode = v);
            w.Visible(rss, () => psg);
        }
    }
}

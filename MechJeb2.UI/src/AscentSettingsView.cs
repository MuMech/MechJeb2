using System;
using KodeKit;
using KodeUI;
using Tone = KodeKit.KodeWindow.Tone;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleAscentSettingsMenu ("Ascent Settings") in KodeKit, row for row with its WindowGUI.
    /// The thrust limiter rows are the ones of the Utilities window; the autostage settings still use their IMGUI method through ImguiBlock.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleAscentSettingsMenu))]
    public class AscentSettingsView : KodeView
    {
        readonly MechJebModuleAscentSettingsMenu menu;

        public AscentSettingsView(MechJebModuleAscentSettingsMenu menu) : base(menu) => this.menu = menu;

        protected override float MinWidth => 275;

        MechJebCore Core => menu.Core;
        MechJebModuleAscentSettings Settings => Core.AscentSettings;
        MechJebModuleThrustController Thrust => Core.Thrust;

        bool psg;

        protected override void BeforeSync()
        {
            psg = Settings.AscentType == AscentType.PSG;
            menu.ForcePsgThrustLimits();
        }

        // "º (x.x°)" after the AoA field, formatted when the tenth changes
        long aoaKey = long.MinValue;
        string aoaText;

        string CurrentMaxAoA()
        {
            double v = menu._autopilot != null ? menu._autopilot.CurrentMaxAoA : 0;
            long key = (long)Math.Round(v * 10);
            if (key != aoaKey || aoaText == null)
            {
                aoaKey = key;
                aoaText = $"º ({v:F1}°)";
            }

            return aoaText;
        }

        protected override void BuildContents(KodeWindow w)
        {
            if (KodeWindow.ModernLayout)
            {
                BuildModern(w);
                return;
            }

            LayoutPanel box = KodeWindow.Box(w.Root);

            // the thrust limiter items, the Utilities window's builders (the throttle and electric limits hide in PSG ascents)
            GeneralItemBuilders.Build(w, box, "General:ThrustController.LimitToPreventOverheatsInfoItem", Thrust);
            GeneralItemBuilders.Build(w, box, "General:ThrustController.LimitToMaxDynamicPressureInfoItem", Thrust);
            GeneralItemBuilders.Build(w, box, "General:ThrustController.LimitAccelerationInfoItem", Thrust);
            Layout throttle = KodeWindow.Panel(box);
            w.Visible(throttle.gameObject, () => !psg);
            GeneralItemBuilders.Build(w, throttle, "General:ThrustController.LimitThrottleInfoItem", Thrust);
            GeneralItemBuilders.Build(w, box, "General:ThrustController.LimiterMinThrottleInfoItem", Thrust);
            Layout electric = KodeWindow.Panel(box);
            w.Visible(electric.gameObject, () => !psg);
            GeneralItemBuilders.Build(w, electric, "General:ThrustController.LimitElectricInfoItem", Thrust);

            // force roll
            w.Toggle(box, "#MechJeb_Ascent_checkbox2", () => Settings.ForceRoll, v => Settings.ForceRoll = v); //Force Roll
            Layout roll = KodeWindow.Row(box);
            KodeWindow.Space(roll, 15);
            AscentUi.SimpleBox(w, roll, menu._climbString, Settings.VerticalRoll, "º", 30, false); //climb
            AscentUi.SimpleBox(w, roll, menu._turnString, Settings.TurnRoll, "º", 30, false); //turn
            AscentUi.SimpleBox(w, roll, "Alt: ", Settings.RollAltitude, "m", 30, false);
            w.Visible(roll.gameObject, () => Settings.ForceRoll);

            // AoA limit (not PSG)
            Layout aoa = KodeWindow.Panel(box);
            w.Visible(aoa.gameObject, () => !psg);
            Layout limit = KodeWindow.Row(aoa);
            w.Toggle(limit, "#MechJeb_Ascent_checkbox3", () => Settings.LimitAoA, v => Settings.LimitAoA = v,
                () => Settings.LimitingAoA ? Tone.Good : Tone.Normal); //Limit AoA to
            EditUi.Field(w, limit, Settings.MaxAoA, 30);
            w.BoundLabel(limit, "º (99.9°)", CurrentMaxAoA);

            Layout fadeout = KodeWindow.Row(aoa);
            KodeWindow.Space(fadeout, 25);
            AscentUi.SimpleBox(w, fadeout, "#MechJeb_Ascent_label24", Settings.AOALimitFadeoutPressure, "Pa", 50, false, //Dynamic Pressure Fadeout
                () => Settings.LimitingAoA && Core.VesselState != null && Core.VesselState.DynamicPressure < Settings.AOALimitFadeoutPressure
                    ? Tone.Good
                    : Tone.Normal);
            w.Visible(fadeout.gameObject, () => Settings.LimitAoA);

            // corrective steering (Classic only)
            Layout steering = KodeWindow.Row(box);
            w.Visible(steering.gameObject, () => Settings.AscentType == AscentType.CLASSIC);
            w.Toggle(steering, "#MechJeb_Ascent_checkbox4", () => Settings.CorrectiveSteering, v => Settings.CorrectiveSteering = v); //Corrective steering
            Layout gain = KodeWindow.Row(steering);
            AscentUi.SimpleBox(w, gain, "#MechJeb_Ascent_label25", Settings.CorrectiveSteeringGain, null, 40, false); // Gain
            w.Visible(gain.gameObject, () => Settings.CorrectiveSteering);

            // autostage
            w.Toggle(box, "#MechJeb_Ascent_checkbox5", () => Settings.Autostage, v => Settings.Autostage = v); //Autostage
            Layout autostage = KodeWindow.Panel(box);
            GeneralItemBuilders.Build(w, autostage, "General:StagingController.AutostageSettingsInfoItem", Core.Staging);
            w.Visible(autostage.gameObject, () => Settings.Autostage);

            w.Toggle(box, "#MechJeb_Ascent_checkbox6", () => Settings.AutoDeploySolarPanels, v => Settings.AutoDeploySolarPanels = v); //Auto-deploy solar panels
            w.Toggle(box, "#MechJeb_Ascent_checkbox7", () => Settings.AutoDeployAntennas, v => Settings.AutoDeployAntennas = v); //Auto-deploy antennas

            Layout warp = KodeWindow.Row(box);
            NodeUi.AutowarpToggle(w, warp, Core, "#MechJeb_Ascent_checkbox8"); //Auto-warp
            UnityEngine.UI.Toggle skip = w.Toggle(warp, "#MechJeb_Ascent_checkbox9", () => Settings.SkipCircularization,
                v => Settings.SkipCircularization = v); //Skip Circularization
            w.Visible(KodeWindow.RowOf(skip), () => !psg);

            UnityEngine.UI.Toggle rss = w.Toggle(box, "Module disabling does not kill throttle", () => Core.Settings.RssMode, v => Core.Settings.RssMode = v);
            w.Visible(KodeWindow.RowOf(rss), () => psg);
        }

        // modern layout: about 25 options in five folding groups, each a property grid with the switches on the left
        void BuildModern(KodeWindow w)
        {
            UIObject r = w.Root;

            UIObject g = w.Group(r, "#MechJeb_NewUI_ThrottleLimits"); //Throttle limits
            ModernRows.ThrustLimits(w, g, Thrust, () => psg, false);

            g = w.Group(r, "#MechJeb_NewUI_Steering"); //Steering
            KodeWindow.PropertyGrid steer = w.Properties(g);
            ModernRows.Switch(steer, "#MechJeb_Ascent_checkbox2", () => Settings.ForceRoll, v => Settings.ForceRoll = v); //Force Roll
            Layout climb = ModernRows.Field(steer, menu._climbString, Settings.VerticalRoll, "º", 50); //climb
            Layout turn = ModernRows.Field(steer, menu._turnString, Settings.TurnRoll, "º", 50); //turn
            Layout alt = ModernRows.Field(steer, "Alt: ", Settings.RollAltitude, "m", 50);
            foreach (Layout row in new[] { climb, turn, alt })
                w.Visible(row.gameObject, () => Settings.ForceRoll);

            // [switch] Limit AoA to [max] º (x.x°): the unit cell shows the current limit
            Layout aoa = ModernRows.Switch(steer, "#MechJeb_Ascent_checkbox3", () => Settings.LimitAoA, v => Settings.LimitAoA = v,
                tone: () => Settings.LimitingAoA ? Tone.Good : Tone.Normal); //Limit AoA to
            TMPro.TMP_InputField maxAoA = EditFields.Field(w, aoa, () => Settings.MaxAoA.Text, s => Settings.MaxAoA.Text = s, 50,
                () => Settings.MaxAoA.IsParsed);
            UIText current = w.BoundLabel(aoa, "º (99.9°)", CurrentMaxAoA);
            steer.Dim(maxAoA.gameObject, KodeWindow.CellOf(current), () => !Settings.LimitAoA);
            Layout fadeout = ModernRows.Field(steer, "#MechJeb_Ascent_label24", Settings.AOALimitFadeoutPressure, "Pa", 60); //Dynamic Pressure Fadeout
            w.Visible(aoa.gameObject, () => !psg);
            w.Visible(fadeout.gameObject, () => !psg && Settings.LimitAoA);

            Layout corrective = ModernRows.Switch(steer, "#MechJeb_Ascent_checkbox4", () => Settings.CorrectiveSteering,
                v => Settings.CorrectiveSteering = v); //Corrective steering
            Layout gain = ModernRows.Field(steer, "#MechJeb_Ascent_label25", Settings.CorrectiveSteeringGain, "", 50); // Gain
            w.Visible(corrective.gameObject, () => Settings.AscentType == AscentType.CLASSIC);
            w.Visible(gain.gameObject, () => Settings.AscentType == AscentType.CLASSIC && Settings.CorrectiveSteering);

            g = w.Group(r, "#MechJeb_NewUI_Autostage"); //Autostage
            w.SwitchRow(g, "#MechJeb_Ascent_checkbox5", () => Settings.Autostage, v => Settings.Autostage = v); //Autostage
            Layout autostage = KodeWindow.Panel(g);
            ModernRows.AutostageSettings(w, autostage, Core.Staging);
            w.Visible(autostage.gameObject, () => Settings.Autostage);

            g = w.Group(r, "#MechJeb_NewUI_Deployment"); //Deployment
            w.SwitchRow(g, "#MechJeb_Ascent_checkbox6", () => Settings.AutoDeploySolarPanels, v => Settings.AutoDeploySolarPanels = v); //Auto-deploy solar panels
            w.SwitchRow(g, "#MechJeb_Ascent_checkbox7", () => Settings.AutoDeployAntennas, v => Settings.AutoDeployAntennas = v); //Auto-deploy antennas

            g = w.Group(r, "#MechJeb_NewUI_Other"); //Other
            NodeUi.AutowarpToggle(w, g, Core, "#MechJeb_Ascent_checkbox8"); //Auto-warp
            UnityEngine.UI.Toggle skip = w.SwitchRow(g, "#MechJeb_Ascent_checkbox9", () => Settings.SkipCircularization,
                v => Settings.SkipCircularization = v); //Skip Circularization
            w.Visible(skip, () => !psg);
            UnityEngine.UI.Toggle rss = w.SwitchRow(g, "Module disabling does not kill throttle", () => Core.Settings.RssMode, v => Core.Settings.RssMode = v);
            w.Visible(rss, () => psg);
        }
    }
}

using System;
using KodeKit;
using KodeUI;
using MechJebLibBindings;
using Tone = KodeKit.KodeWindow.Tone;
using static MuMech.MechJebModuleThrustController;

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

        Tone LimitTone(LimitMode mode) => Thrust.Limiter == mode ? Tone.Good : Tone.Normal;

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
            LayoutPanel box = KodeWindow.Box(w.Root);

            // the thrust limiter items
            w.Toggle(box, "#MechJeb_Ascent_checkbox12", () => Thrust.LimitToPreventOverheats, v => Thrust.LimitToPreventOverheats = v,
                () => LimitTone(LimitMode.TEMPERATURE)); //Prevent engine overheats
            AscentUi.ToggledBox(w, box, "#MechJeb_Ascent_checkbox11", () => Thrust.LimitDynamicPressure, v => Thrust.LimitDynamicPressure = v,
                Thrust.MaxDynamicPressure, "Pa", 80, () => LimitTone(LimitMode.DYNAMIC_PRESSURE));
            AscentUi.ToggledBox(w, box, "#MechJeb_Ascent_checkbox16", () => Thrust.LimitAcceleration, v => Thrust.LimitAcceleration = v,
                Thrust.MaxAcceleration, "m/s²", 30, () => LimitTone(LimitMode.ACCELERATION));

            Layout throttle = KodeWindow.Panel(box);
            w.Visible(throttle.gameObject, () => !psg);
            AscentUi.ToggledBox(w, throttle, "#MechJeb_Ascent_checkbox17", () => Thrust.LimitThrottle, v => Thrust.LimitThrottle = v,
                Thrust.MaxThrottle, "%", 30,
                () => Thrust.Limiter == LimitMode.THROTTLE ? Thrust.MaxThrottle > 0d ? Tone.Good : Tone.Bad : Tone.Normal);

            Layout minThrottle = AscentUi.ToggledBox(w, box, "#MechJeb_Ascent_checkbox18", () => Thrust.LimiterMinThrottle,
                v => Thrust.LimiterMinThrottle = v, Thrust.MinThrottle, "%", 30, () =>
                {
                    // RF users should almost always have this set, "stock" users should almost always never have this set
                    bool warn = ReflectionUtils.IsLoadedRealFuels
                        ? !Thrust.LimiterMinThrottle || Thrust.MinThrottle <= 0d
                        : Thrust.LimiterMinThrottle && Thrust.MinThrottle > 0d;
                    return Thrust.Limiter == LimitMode.MIN_THROTTLE ? Tone.Good : warn ? Tone.Warn : Tone.Normal;
                });
            Tooltips.Add(minThrottle.GetComponentInChildren<ToggleLabelClick>().gameObject, "#MechJeb_Ascent_checkbox18_tooltip");

            // LimitElectricInfoItem (the green "engine running" state needs a private method; only the limiter state colours it here)
            Layout electricPanel = KodeWindow.Panel(box);
            w.Visible(electricPanel.gameObject, () => !psg);
            Layout electric = KodeWindow.Row(electricPanel);
            w.Toggle(electric, "#MechJeb_Ascent_checkbox20", () => Thrust.ElectricThrottle, v => Thrust.ElectricThrottle = v,
                () => Thrust.Limiter == LimitMode.ELECTRIC
                    ? Thrust.ThrottleLimit < 0.001 ? Tone.Bad : Tone.Warn
                    : Tone.Normal);
            AscentUi.Field(w, electric, Thrust.ElectricThrottleLo, 30);
            KodeWindow.Label(electric, "% Hi");
            AscentUi.Field(w, electric, Thrust.ElectricThrottleHi, 30);
            KodeWindow.Label(electric, "%");

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
            AscentUi.Field(w, limit, Settings.MaxAoA, 30);
            AscentUi.Live(w, limit, "º (99.9°)", CurrentMaxAoA);

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
            w.Toggle(warp, "#MechJeb_Ascent_checkbox8", () => Core.Node != null && Core.Node.Autowarp, v =>
            {
                if (Core.Node != null) Core.Node.Autowarp = v;
            }); //Auto-warp
            UnityEngine.UI.Toggle skip = w.Toggle(warp, "#MechJeb_Ascent_checkbox9", () => Settings.SkipCircularization,
                v => Settings.SkipCircularization = v); //Skip Circularization
            w.Visible(AscentUi.RowOf(skip), () => !psg);

            UnityEngine.UI.Toggle rss = w.Toggle(box, "Module disabling does not kill throttle", () => Core.Settings.RssMode, v => Core.Settings.RssMode = v);
            w.Visible(AscentUi.RowOf(rss), () => psg);
        }
    }
}

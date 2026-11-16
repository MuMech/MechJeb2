using System;
using KodeKit;
using KodeUI;
using Tone = KodeKit.KodeWindow.Tone;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleAscentSettingsMenu ("Ascent Settings") in KodeKit.
    /// The thrust limiter rows and the autostage settings are the ones of the Utilities window, bound to the ascent's own
    /// values (AscentSettings.ThrustLimits, StagingRules, Autowarp), which apply while an ascent autopilot is engaged.
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
        MechJebModulePSGSimulation Psgs => Core.PSGSimulation;

        const string ArefNote = "Aref is 0: PSG plans the ascent with no drag. Set the rocket's cross-section area.";

        bool psg;

        // the ascent's own limits are the ones that apply while an ascent autopilot is engaged
        bool OwnValuesApply() => Thrust.LimitsOverrideUser != null && ReferenceEquals(Thrust.LimitsOverrideUser, Core.Ascent);

        protected override void BeforeSync()
        {
            psg = Settings.AscentType == AscentType.PSG;
            if (psg) UpdateAero();
        }

        // what AutoAero filled Cd and Aref with. Rebuilt only when they change.
        string aeroCd = "", aeroAref = "";
        double aeroKey = double.NaN;

        void UpdateAero()
        {
            if (!Psgs.AutoAero)
                return;

            if (HighLogic.LoadedSceneIsFlight)
                Psgs.UpdateAutoAero();

            double key = Psgs.Cd * 7919 + Psgs.Aref;
            if (key == aeroKey) return;
            aeroKey = key;

            aeroCd = Psgs.Cd.Val.ToString("0.###");
            aeroAref = Psgs.Aref.Val.ToString("0.###");
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
            OptionRows.ThrustLimits(w, g, Thrust, Settings.ThrustLimits, OwnValuesApply, () => psg, false);

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

            // PSG: the Qα limit (PSG's angle-of-attack limit, beside Classic's) and the pitch-over before guidance takes over
            Layout qaRow = OptionRows.Field(steer, "#MechJeb_Ascent_label17", Psgs.LimitQa, "Pa-rad", 60); //Qα limit
            w.Attention(qaRow, () => PsgAttention.QaClamped(Core));
            Layout pitchStart = OptionRows.Field(steer, "#MechJeb_Ascent_label13", Settings.PitchStartHeight, "m", 50); //Booster Pitch start:
            Layout pitchRate = OptionRows.Field(steer, "#MechJeb_Ascent_label14", Settings.PitchRate, "°/s", 50); //Booster Pitch rate:
            foreach (Layout row in new[] { qaRow, pitchStart, pitchRate })
                w.Visible(row.gameObject, () => psg);
            bool Clamped() => Psgs.LimitQa < 0 || Psgs.LimitQa > 10000;
            Layout qa = w.StatusBar(g, () => Clamped() ? Tone.Bad : Tone.Warn);
            UIText clamped = GuidanceUi.Wrapped(qa, "Qα limit has been clamped to between 0 and 10000 Pa-rad");
            w.Visible(clamped.gameObject, Clamped);
            UIText recommended = GuidanceUi.Wrapped(qa, "#MechJeb_Ascent_label20"); //Qα limit is recommended to be 1000 to 4000 Pa-rad
            w.Visible(recommended.gameObject, () => !Clamped());
            w.Visible(qa.gameObject, () => psg && (Psgs.LimitQa < 1000 || Psgs.LimitQa > 4000));

            // PSG: the drag model it plans with, header and all in a panel that only PSG shows
            Layout dragModel = KodeWindow.Panel(r);
            w.Visible(dragModel.gameObject, () => psg);
            g = w.Group(dragModel, "#MechJeb_NewUI_DragModel"); //Drag model
            KodeWindow.PropertyGrid aero = w.Properties(g);
            OptionRows.Switch(aero, "Cd and Aref from the vessel", () => Psgs.AutoAero, v => Psgs.AutoAero = v);
            Layout cdField = OptionRows.Field(aero, "Cd", Psgs.Cd, "", 50);
            Layout arefField = OptionRows.Field(aero, "Aref", Psgs.Aref, "m²", 50);
            w.Attention(arefField, () => PsgAttention.ArefUnset(Core));
            w.Visible(cdField.gameObject, () => !Psgs.AutoAero);
            w.Visible(arefField.gameObject, () => !Psgs.AutoAero);
            // sized from templates: the values are still empty when the window is made under Classic or with the switch off
            Layout cdValue = aero.Value("Cd", () => aeroCd, null, "0.000");
            Layout arefValue = aero.Value("Aref", () => aeroAref, "m²", "000.000");
            w.Visible(cdValue.gameObject, () => Psgs.AutoAero);
            w.Visible(arefValue.gameObject, () => Psgs.AutoAero);
            UIText arefNote = aero.Note(ArefNote);
            KodeWindow.ToneText(arefNote, Tone.Caution);
            KodeWindow.WrapToWidth(arefNote);
            w.Visible(arefNote.transform.parent.gameObject, () => PsgAttention.ArefUnset(Core));

            g = w.Group(r, "#MechJeb_NewUI_Autostage"); //Autostage
            w.SwitchRow(g, "#MechJeb_Ascent_checkbox5", () => Settings.Autostage, v => Settings.Autostage = v); //Autostage
            Layout autostage = KodeWindow.Panel(g);
            OptionRows.AutostageSettings(w, autostage, Core.Staging, Settings.StagingRules);
            w.Visible(autostage.gameObject, () => Settings.Autostage);

            g = w.Group(r, "#MechJeb_NewUI_Deployment"); //Deployment
            w.SwitchRow(g, "#MechJeb_Ascent_checkbox6", () => Settings.AutoDeploySolarPanels, v => Settings.AutoDeploySolarPanels = v); //Auto-deploy solar panels
            w.SwitchRow(g, "#MechJeb_Ascent_checkbox7", () => Settings.AutoDeployAntennas, v => Settings.AutoDeployAntennas = v); //Auto-deploy antennas

            g = w.Group(r, "#MechJeb_NewUI_Other"); //Other
            w.SwitchRow(g, "#MechJeb_Ascent_checkbox8", () => Settings.Autowarp, v => Settings.Autowarp = v); //Auto-warp
            UnityEngine.UI.Toggle skip = w.SwitchRow(g, "#MechJeb_Ascent_checkbox9", () => Classic.SkipCircularization,
                v => Classic.SkipCircularization = v); //Skip Circularization
            w.Visible(skip, () => !psg);
            UnityEngine.UI.Toggle trajectory = w.SwitchRow(g, "Draw Trajectory on Map", () => Core.Guidance.ShouldDrawTrajectory,
                v => Core.Guidance.ShouldDrawTrajectory = v);
            w.Visible(trajectory, () => psg);
            UnityEngine.UI.Toggle rss = w.SwitchRow(g, "Module disabling does not kill throttle", () => Core.Settings.RssMode, v => Core.Settings.RssMode = v);
            w.Visible(rss, () => psg);
        }
    }
}

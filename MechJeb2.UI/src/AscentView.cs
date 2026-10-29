using System;
using KodeKit;
using KodeUI;
using MechJebLib.PSG;
using UnityEngine;
using Tone = KodeKit.KodeWindow.Tone;

namespace MuMech.UI
{
    /// <summary>Small widget helpers shared by the four Ascent views (GuiUtils.SimpleTextBox and friends).</summary>
    internal static class AscentUi
    {
        public static Color ToneColor(Theme th, Tone t) => t == Tone.Good ? th.Good : t == Tone.Warn ? th.Warn : t == Tone.Bad ? th.Bad : th.Text;

        /// <summary>GuiUtils.SimpleTextField: a field on an IEditable; red while the text does not parse.</summary>
        public static void Field(KodeWindow w, UIObject parent, IEditable ed, float width)
        {
            Func<bool> valid = ed is EditableDoubleMult m ? (Func<bool>)(() => m.IsParsed) : null;
            EditFields.Field(w, parent, () => ed.Text, s => ed.Text = s, width, valid);
        }

        /// <summary>A text whose colour follows a state (MechJeb's Green/Yellow/Red labels).</summary>
        public static UIText ToneText(KodeWindow w, UIObject parent, string text, Func<Tone> tone, bool flexible = false)
        {
            UIText t = KodeWindow.Label(parent, text, flexible);
            ApplyTone(w, t, tone);
            return t;
        }

        public static UIText ToneText(KodeWindow w, UIObject parent, string text, Tone tone, bool flexible = false) =>
            ToneText(w, parent, text, () => tone, flexible);

        public static void ApplyTone(KodeWindow w, UIText t, Func<Tone> tone)
        {
            Tone shown = tone();
            Action paint = () => t.tmpText.color = ToneColor(Theme.Current, shown);
            paint();
            w.Behaviour.AddRestyle(paint);
            w.AddSync(() =>
            {
                Tone n = tone();
                if (n == shown) return;
                shown = n;
                paint();
            });
        }

        /// <summary>A changing text in a fixed-size cell sized from <paramref name="template"/>.</summary>
        public static UIText Live(KodeWindow w, UIObject parent, string template, Func<string> get, bool flexible = false)
        {
            UIText t = KodeWindow.LiveLabel(parent, template, flexible);
            string last = null;
            w.AddSync(() =>
            {
                string s = get();
                if (s == last) return;
                last = s;
                t.Text(s);
            });
            return t;
        }

        /// <summary>GuiUtils.SimpleTextBox: label [field] unit. Returns the row (null when not framed).</summary>
        public static Layout SimpleBox(KodeWindow w, UIObject parent, string left, IEditable ed, string unit = null, float width = 100,
            bool framing = true, Func<Tone> leftTone = null)
        {
            Layout row = framing ? KodeWindow.Row(parent) : null;
            UIObject p = framing ? row : parent;
            if (!string.IsNullOrEmpty(left))
            {
                if (leftTone != null) ToneText(w, p, left, leftTone);
                else KodeWindow.Label(p, left);
            }

            Field(w, p, ed, width);
            if (!string.IsNullOrEmpty(unit)) KodeWindow.Label(p, unit);
            return row;
        }

        /// <summary>GuiUtils.ToggledTextBox: [x] label [field] unit</summary>
        public static Layout ToggledBox(KodeWindow w, UIObject parent, string key, Func<bool> get, Action<bool> set, IEditable ed, string unit,
            float width, Func<Tone> tone = null)
        {
            Layout row = KodeWindow.Row(parent);
            if (tone != null) w.Toggle(row, key, get, set, tone);
            else w.Toggle(row, key, get, set);
            Field(w, row, ed, width);
            if (!string.IsNullOrEmpty(unit)) KodeWindow.Label(row, unit);
            return row;
        }

        /// <summary>GuiUtils.ButtonTextBox: [button] [field] unit</summary>
        public static Layout ButtonBox(KodeWindow w, UIObject parent, string key, Action action, IEditable ed, string unit, float width)
        {
            Layout row = KodeWindow.Row(parent);
            KodeWindow.Button(row, key, action);
            Field(w, row, ed, width);
            if (!string.IsNullOrEmpty(unit)) KodeWindow.Label(row, unit);
            return row;
        }

        public static GameObject RowOf(UnityEngine.UI.Toggle t) => t.transform.parent.gameObject;
    }

    /// <summary>
    /// MechJebModuleAscentMenu ("Ascent Guidance") in KodeKit, row for row with its WindowGUI.
    /// Both ascent types are built once; the rows that only apply to one of them are shown or hidden.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleAscentMenu))]
    public class AscentView : KodeView
    {
        readonly MechJebModuleAscentMenu menu;

        public AscentView(MechJebModuleAscentMenu menu) : base(menu) => this.menu = menu;

        protected override float MinWidth => 275;

        MechJebCore Core => menu.Core;
        MechJebModuleAscentSettings Settings => Core.AscentSettings;
        MechJebModulePSGSimulation Psgs => Core.PSGSimulation;

        // per-frame facts, computed once in BeforeSync
        bool psg;
        bool optimize;
        bool landed;
        bool anyPlaneControl;
        bool targetExists;
        double incDelta;

        bool HasVessel => Core.vessel != null;
        double BodyRadius => HasVessel && Core.vessel.mainBody != null ? Core.vessel.mainBody.Radius : 0;

        // what WindowGUI did before and while drawing
        protected override void BeforeSync()
        {
            menu.UpdateStrings();
            psg = Settings.AscentType == AscentType.PSG;
            optimize = psg && Settings.OptimizeStageFlag;
            landed = HasVessel && Core.vessel.LandedOrSplashed;
            if (psg)
            {
                Core.StageStats.RequestUpdate();
                Core.StageStats.LiveSLT = true;
            }

            if (landed) menu.ResetLaunchWithoutTarget();
            anyPlaneControl = menu._launchingWithAnyPlaneControl;
            targetExists = landed && menu.AutoWarpTargetExists;
            incDelta = HasVessel ? menu.InclinationBelowLatitude(menu.InclinationSetting) : 0;
        }

        // "inc {0:F1}º below current latitude", formatted only when the shown tenth changes
        long deltaKey = long.MinValue;
        string deltaText;

        string DeltaText()
        {
            long key = (long)Math.Round(incDelta * 10);
            if (key != deltaKey || deltaText == null)
            {
                deltaKey = key;
                deltaText = KSP.Localization.Localizer.Format("#MechJeb_Ascent_label7", incDelta);
            }

            return deltaText;
        }

        Tone GuidanceTone()
        {
            if (Core.Guidance.IsStable()) return Tone.Good;
            if (Core.Guidance.IsInitializing() || Core.Guidance.Status == PSGStatus.FINISHED) return Tone.Warn;
            return Tone.Bad;
        }

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;
            // the strings of the status labels exist before the first sync (the cells are sized from them)
            menu.UpdateStrings();

            // VisibleSectionsGUIElements
            LayoutPanel top = KodeWindow.Box(r);
            UIButton disengage = KodeWindow.Button(top, "#MechJeb_Ascent_button1", () => menu._autopilot.Users.Remove(menu), true); //Disengage autopilot
            w.Visible(disengage.gameObject, () => menu._autopilot.Enabled);
            UIButton engage = KodeWindow.Button(top, "#MechJeb_Ascent_button2", () => menu._autopilot.Users.Add(menu), true); //Engage autopilot
            w.Visible(engage.gameObject, () => !menu._autopilot.Enabled);

            BuildTargeting(w, r);
            BuildSettingsToggles(w, r);
            BuildStatus(w, r);
            BuildAutoWarp(w, r);

            UIText apStatus = w.BoundLabel(r, () => menu.autopilotStatus ?? " ", true); //Autopilot status:
            w.Visible(apStatus.transform.parent.gameObject, () => menu._autopilot.Enabled);

            UIText disabled = AscentUi.ToneText(w, r, "#MechJeb_Ascent_label36", Tone.Bad); //CONTROL DISABLED (AVIONICS)
            w.Visible(disabled.gameObject, () => Core.DeactivateControl);

            UIText noTracking = KodeWindow.Paragraph(r, "#MechJeb_Ascent_label37"); //unable to circularize without an upgraded Tracking Station
            w.Visible(noTracking.gameObject, () => HasVessel && !Core.vessel.patchedConicsUnlocked() && !psg);

            Layout type = KodeWindow.Row(r);
            new ComboBox(w, type, () => (int)Settings.AscentType, i => Settings.AscentType = (AscentType)i, () => menu._ascentPathList);

            UnityEngine.UI.Toggle classicPath = w.Toggle(r, "#MechJeb_Ascent_checkbox10", () => menu._classicPathMenu.Enabled,
                v => menu._classicPathMenu.Enabled = v); //Edit ascent path
            w.Visible(AscentUi.RowOf(classicPath), () => Settings.AscentType == AscentType.CLASSIC);

            // RefreshRateGUI
            Layout refresh = KodeWindow.Row(r);
            KodeWindow.Label(refresh, "Update Interval");
            EditFields.Field(w, refresh, () => menu._refreshRate.Text, s =>
            {
                int old = menu._refreshRate;
                menu._refreshRate.Text = s;
                menu.RefreshRateChanged(old);
            }, 100);
            KodeWindow.Label(refresh, "Hz");
            w.Visible(refresh.gameObject, () => GuiUtils.ShowAdvancedWindowSettings);
        }

        // ShowTargetingGUIElements
        void BuildTargeting(KodeWindow w, UIObject r)
        {
            LayoutPanel box = KodeWindow.Box(r);

            // PSG with an optimized stage: periapsis, apoapsis, attach altitude, argument of periapsis and their warnings
            Layout opt = KodeWindow.Panel(box);
            w.Visible(opt.gameObject, () => optimize);
            AscentUi.SimpleBox(w, opt, "#MechJeb_Ascent_label1", Psgs.PeriapsisAltitude, "km"); //Target Periapsis
            AscentUi.SimpleBox(w, opt, "#MechJeb_Ascent_label2", Psgs.ApoapsisAltitude, "km"); //Target Apoapsis:
            AscentUi.ToggledBox(w, opt, "#MechJeb_Ascent_attachAlt", () => Psgs.BurnoutAltitudeFlag, v => Psgs.BurnoutAltitudeFlag = v,
                Psgs.BurnoutAltitude, "km", 100);
            AscentUi.ToggledBox(w, opt, "Arg Periapsis:", () => Psgs.ArgPFlag, v => Psgs.ArgPFlag = v, Psgs.ArgP, "°", 100);

            bool ApLtPe() => Psgs.ApoapsisAltitude + BodyRadius >= 0 && Psgs.ApoapsisAltitude < Psgs.PeriapsisAltitude;
            UIText l3 = AscentUi.ToneText(w, opt, "#MechJeb_Ascent_label3", Tone.Warn); //Ap < Pe: circularizing orbit
            w.Visible(l3.gameObject, ApLtPe);
            UIText high = AscentUi.ToneText(w, opt, "#MechJeb_Ascent_warnAttachAltHigh", Tone.Warn); //Attach > Ap: apoapsis insertion
            w.Visible(high.gameObject, () => !ApLtPe() && Psgs.BurnoutAltitudeFlag && Psgs.BurnoutAltitude > Psgs.ApoapsisAltitude);
            UIText l4 = AscentUi.ToneText(w, opt, "#MechJeb_Ascent_label4", Tone.Warn); //Hyperbolic target orbit (neg Ap)
            w.Visible(l4.gameObject, () => Psgs.ApoapsisAltitude + BodyRadius < 0);
            UIText low = AscentUi.ToneText(w, opt, "#MechJeb_Ascent_warnAttachAltLow", Tone.Warn); //Attach < Pe: periapsis insertion
            w.Visible(low.gameObject, () => Psgs.BurnoutAltitudeFlag && Psgs.BurnoutAltitude < Psgs.PeriapsisAltitude);

            // PSG without: flight path angle (unless a plane is controlled) and attach altitude
            Layout fixedPlan = KodeWindow.Panel(box);
            w.Visible(fixedPlan.gameObject, () => psg && !optimize);
            Layout fpa = AscentUi.SimpleBox(w, fixedPlan, "Flight Path Angle", Psgs.FPA, "°");
            w.Visible(fpa.gameObject, () => !anyPlaneControl);
            AscentUi.SimpleBox(w, fixedPlan, "#MechJeb_Ascent_attachAlt", Psgs.BurnoutAltitude, "km");

            // Classic
            Layout classic = KodeWindow.Panel(box);
            w.Visible(classic.gameObject, () => !psg);
            AscentUi.SimpleBox(w, classic, "#MechJeb_Ascent_label5", Settings.DesiredOrbitAltitude, "km"); //Orbit altitude

            // ShowInclinationGUIElement
            Layout inc = KodeWindow.Row(box);
            KodeWindow.Label(inc, "#MechJeb_Ascent_label6"); //Orbit inc.
            // the edited setting is the PSG one or the classic one
            EditFields.Field(w, inc, () => menu.InclinationSetting.Text, s => menu.InclinationSetting.Text = s, 75,
                () => menu.InclinationSetting.IsParsed);
            KodeWindow.Label(inc, "º");
            UIButton current = KodeWindow.Button(inc, "#MechJeb_Ascent_button13", () => menu.SetInclinationToLatitude(menu.InclinationSetting)); //Current
            Tooltips.Add(current.gameObject, MechJebModuleAscentMenu.InclinationCurrentTooltip);

            UIText below = w.BoundLabel(box, DeltaText, true);
            AscentUi.ApplyTone(w, below, () => Tone.Bad);
            w.Visible(below.transform.parent.gameObject, () => 2.001 < incDelta);
        }

        // the "Ascent Settings" and "PSG Settings" window toggles
        void BuildSettingsToggles(KodeWindow w, UIObject r)
        {
            LayoutPanel box = KodeWindow.Box(r);
            Layout row = KodeWindow.Row(box);
            w.Toggle(row, "Ascent Settings", () => menu._settingsMenu.Enabled, v => menu._settingsMenu.Enabled = v);
            UnityEngine.UI.Toggle psgSettings = w.Toggle(row, "PSG Settings", () => menu._psgSettingsMenu.Enabled, v => menu._psgSettingsMenu.Enabled = v);
            w.Visible(AscentUi.RowOf(psgSettings), () => psg);
        }

        // ShowStatusGUIElements (PSG only)
        void BuildStatus(KodeWindow w, UIObject r)
        {
            LayoutPanel box = KodeWindow.Box(r);
            w.Visible(box.gameObject, () => psg);

            for (int i = 0; i < MechJebModuleAscentMenu.MAX_PHASE_LINES; i++)
            {
                int k = i;
                UIText line = w.BoundLabel(box, () => k < menu.PhaseLineCount ? menu.PhaseLines[k] ?? "-" : "-", true);
                w.Visible(line.transform.parent.gameObject, () => k < menu.PhaseLineCount);
            }

            Layout vgo = KodeWindow.Row(box);
            AscentUi.Live(w, vgo, "vgo: 99999.9", () => menu.vgo ?? " ");
            AscentUi.Live(w, vgo, "heading: 360.0", () => menu.heading ?? " ", true);
            Layout tgo = KodeWindow.Row(box);
            AscentUi.Live(w, tgo, "tgo: 999.999", () => menu.tgo ?? " ");
            AscentUi.Live(w, tgo, "pitch: -90.0", () => menu.pitch ?? " ", true);

            UIText status = w.BoundLabel(box, () => menu.label26 ?? " ", true); //Guidance Status:
            AscentUi.ApplyTone(w, status, GuidanceTone);

            Layout conv = KodeWindow.Row(box);
            AscentUi.Live(w, conv, "converges: 999", () => menu.label27 ?? " "); //converges:
            AscentUi.Live(w, conv, "staleness: 99d 99h 99m 99s", () => menu.label29 ?? " ", true); //staleness:
            Layout n = KodeWindow.Row(box);
            AscentUi.Live(w, n, "n: 100(100)", () => menu.n ?? " ");
            AscentUi.Live(w, n, "infeasibility: -1.2345E-05", () => menu.znorm ?? " ", true);

            UIText failure = w.BoundLabel(box, () => menu.label30 ?? " ", true); //LAST FAILURE:
            AscentUi.ApplyTone(w, failure, () => Tone.Bad);
            w.Visible(failure.transform.parent.gameObject, () => Psgs.LastFailureMessage != null);
        }

        // ShowAutoWarpGUIElements (only when landed)
        void BuildAutoWarp(KodeWindow w, UIObject r)
        {
            LayoutPanel box = KodeWindow.Box(r);
            w.Visible(box.gameObject, () => landed);

            Layout countdown = KodeWindow.Row(box);
            KodeWindow.Label(countdown, "#MechJeb_Ascent_label33"); //Launch countdown:
            AscentUi.Field(w, countdown, Settings.WarpCountDown, 35);
            KodeWindow.Label(countdown, "s");
            w.Visible(countdown.gameObject, () => Core.Node != null && Core.Node.Autowarp);

            UIText invalid = AscentUi.ToneText(w, box, "#MechJeb_Ascent_warnInvalidTarget", Tone.Warn, true); // Target must be in the same sphere of influence.
            invalid.tmpText.enableWordWrapping = true;
            w.Visible(invalid.gameObject, () => !anyPlaneControl && !targetExists && Core.Target.NormalTargetExists);
            UIText select = KodeWindow.Label(box, "#MechJeb_Ascent_label34"); //Select a target for a timed launch.
            w.Visible(select.gameObject, () => !anyPlaneControl && !targetExists && !Core.Target.NormalTargetExists);

            // nothing being launched: the launch buttons
            Layout idle = KodeWindow.Panel(box);
            w.Visible(idle.gameObject, () => !anyPlaneControl);
            Layout plane = AscentUi.ButtonBox(w, idle, "#MechJeb_Ascent_button15", menu.LaunchToPlaneOfTarget, Settings.LaunchLANDifference, "º", 40); //Launch into plane of target
            w.Visible(plane.gameObject, () => targetExists);
            Layout targetLan = AscentUi.ButtonBox(w, idle, "#MechJeb_Ascent_LaunchToTargetLan", menu.LaunchToTargetLan, Settings.LaunchLANDifference, "º", 40); //Launch to target LAN
            w.Visible(targetLan.gameObject, () => targetExists && psg);
            Layout lan = AscentUi.ButtonBox(w, idle, "#MechJeb_Ascent_LaunchToLan", menu.LaunchToLan, Psgs.LAN, "º", 40); //Launch to LAN
            w.Visible(lan.gameObject, () => psg);
            UnityEngine.UI.Toggle relative = w.Toggle(idle, "Use LAN relative to launch site", () => Settings.RelativeLAN, v => Settings.RelativeLAN = v);
            w.Visible(AscentUi.RowOf(relative), () => psg);

            // launching: timer and abort
            Layout launching = KodeWindow.Panel(box);
            w.Visible(launching.gameObject, () => anyPlaneControl);
            w.BoundLabel(launching, () => string.IsNullOrEmpty(menu.launchTimer) ? " " : menu.launchTimer, true);
            KodeWindow.Button(launching, "#MechJeb_Ascent_button17", menu.AbortTimedLaunch); //Abort

            w.Toggle(box, "Override Warp to Plane", () => Settings.OverrideWarpToPlane, v => Settings.OverrideWarpToPlane = v);
        }
    }
}

using System;
using KodeKit;
using KodeUI;
using MechJebLib.PSG;
using UnityEngine;
using Tone = KodeKit.KodeWindow.Tone;

namespace MuMech.UI
{
    /// <summary>Small widget helpers shared by the four Ascent views (label, field and unit rows).</summary>
    internal static class AscentUi
    {
        /// <summary>A text in a state colour (MechJeb's Green/Yellow/Red labels).</summary>
        public static UIText ToneText(UIObject parent, string text, Tone tone, bool flexible = false)
        {
            UIText t = KodeWindow.Label(parent, text, flexible);
            KodeWindow.ToneText(t, tone);
            return t;
        }

        /// <summary>label [field] unit. Returns the row (null when not framed).</summary>
        public static Layout SimpleBox(KodeWindow w, UIObject parent, string left, IEditable ed, string unit = null, float width = 100,
            bool framing = true, Func<Tone> leftTone = null)
        {
            Layout row = framing ? KodeWindow.Row(parent) : null;
            UIObject p = framing ? row : parent;
            if (!string.IsNullOrEmpty(left))
            {
                UIText label = KodeWindow.Label(p, left);
                if (leftTone != null) w.ToneText(label, leftTone);
            }

            EditUi.Field(w, p, ed, width);
            if (!string.IsNullOrEmpty(unit)) KodeWindow.Label(p, unit);
            return row;
        }

        /// <summary>[x] label [field] unit</summary>
        public static Layout ToggledBox(KodeWindow w, UIObject parent, string key, Func<bool> get, Action<bool> set, IEditable ed, string unit,
            float width, Func<Tone> tone = null)
        {
            Layout row = KodeWindow.Row(parent);
            if (tone != null) w.Toggle(row, key, get, set, tone);
            else w.Toggle(row, key, get, set);
            EditUi.Field(w, row, ed, width);
            if (!string.IsNullOrEmpty(unit)) KodeWindow.Label(row, unit);
            return row;
        }

        /// <summary>[button] [field] unit</summary>
        public static Layout ButtonBox(KodeWindow w, UIObject parent, string key, Action action, IEditable ed, string unit, float width)
        {
            Layout row = KodeWindow.Row(parent);
            KodeWindow.Button(row, key, action);
            EditUi.Field(w, row, ed, width);
            if (!string.IsNullOrEmpty(unit)) KodeWindow.Label(row, unit);
            return row;
        }


    }

    /// <summary>
    /// MechJebModuleAscentMenu ("Ascent Guidance") in KodeKit, top to bottom: the ascent type, TARGET, LAUNCH (on the ground: the
    /// launch Engage starts and the countdown), Engage, the status bars, the buttons of the ascent's windows, and PSG's solver
    /// numbers in a fold. Both ascent types are built once; the rows that only apply to one of them are shown or hidden.
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

        // the module's per-frame work, before the bindings
        protected override void BeforeSync()
        {
            menu.UpdateStrings();
            psg = Settings.AscentType == AscentType.PSG;
            optimize = psg && Settings.OptimizeStageFlag;
            landed = HasVessel && Core.vessel.LandedOrSplashed;
            if (psg) Core.StageStats.RequestUpdate();

            anyPlaneControl = menu._launchingWithAnyPlaneControl;
            targetExists = landed && menu.AutoWarpTargetExists;
            incDelta = HasVessel ? menu.InclinationBelowLatitude(menu.InclinationSetting) : 0;
        }

        // "inc {0:F1}° below current latitude", formatted only when the shown tenth changes
        long deltaKey = long.MinValue;
        string deltaText;

        string DeltaText()
        {
            long key = (long)Math.Round(incDelta * 10);
            if (key != deltaKey || deltaText == null)
            {
                deltaKey = key;
                deltaText = KSP.Localization.Localizer.Format("#MechJeb_Ascent_label7", incDelta.ToString("F1"));
            }

            return deltaText;
        }

        Tone GuidanceTone()
        {
            if (Core.Guidance.IsStable()) return Tone.Good;
            if (Core.Guidance.IsInitializing() || Core.Guidance.Status == PSGStatus.FINISHED) return Tone.Caution;
            return Tone.Bad;
        }

        // the autopilot status line: plain, yellow or red with the autopilot's severity (read with the text, by UpdateStrings)
        Tone StatusTone() => menu.autopilotStatusLevel switch
        {
            AscentStatusLevel.ERROR => Tone.Bad,
            AscentStatusLevel.WARNING => Tone.Warn,
            _ => Tone.Normal
        };

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;
            // the strings of the status labels exist before the first sync (the cells are sized from them)
            menu.UpdateStrings();

            // the ascent type first: it decides every row below, and which windows the buttons open
            BuildType(w, r);
            BuildTargeting(w, r);
            BuildLaunch(w, r);

            Layout engageRow = KodeWindow.Row(r);
            UIButton disengage = KodeWindow.Button(engageRow, "#MechJeb_Ascent_button1", () => menu._autopilot.Users.Clear(), true); //Disengage autopilot: whoever engaged it (a kOS script too)
            w.Visible(disengage.gameObject, () => menu._autopilot.Enabled);
            UIButton engage = KodeWindow.Button(engageRow, "#MechJeb_Ascent_button2", menu.Engage, true); //Engage autopilot
            w.Visible(engage.gameObject, () => !menu._autopilot.Enabled);
            GuidanceUi.Enable(w, engage, () => menu.LaunchChoiceReady);
            KodeWindow.Role(engage, ButtonRole.Primary);
            KodeWindow.Role(disengage, ButtonRole.Danger);

            // the autopilot's state and the avionics warning, in status bars
            Layout statusBar = w.StatusBar(r, StatusTone);
            w.Visible(statusBar.gameObject, () => menu._autopilot.Enabled);
            UIText apStatus = w.Wrap(w.BoundLabel(statusBar, () => menu.autopilotStatus ?? " ", true)); //Autopilot status:
            w.ToneText(apStatus, StatusTone);

            Layout disabledBar = w.StatusBar(r, () => KodeWindow.Tone.Bad);
            w.Visible(disabledBar.gameObject, () => Core.DeactivateControl);
            AscentUi.ToneText(disabledBar, "#MechJeb_Ascent_label36", Tone.Bad); //CONTROL DISABLED (AVIONICS)

            UIText noTracking = KodeWindow.Paragraph(r, "#MechJeb_Ascent_label37"); //unable to circularize without an upgraded Tracking Station
            w.Visible(noTracking.gameObject, () => HasVessel && !Core.vessel.patchedConicsUnlocked() && !psg);

            BuildGuidanceStatus(w, r);
            BuildWindowButtons(w, r);
            BuildGuidanceDetails(w, r);

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

        // [ Classic | PSG ]: a change of type turns the engaged autopilot off, so the type is fixed while the ascent is engaged
        void BuildType(KodeWindow w, UIObject r)
        {
            string[] names = { KodeWindow.Loc("#MechJeb_NewUI_AscentClassic"), KodeWindow.Loc("#MechJeb_NewUI_AscentPSG") }; //Classic, PSG
            Layout type = w.Segmented(r, names, () => (int)Settings.AscentType, i =>
            {
                if (!menu._autopilot.Enabled) Settings.AscentType = (AscentType)i;
            });
            Tooltips.Add(type.gameObject, () => menu._autopilot.Enabled
                ? KodeWindow.Loc("#MechJeb_NewUI_AscentTypeLocked") //The ascent type can't change while the ascent autopilot is engaged.
                : KodeWindow.Loc("#MechJeb_NewUI_AscentTypeTooltip")); //Classic: the Classic Ascent Profile. PSG: Polynomial SQP Guidance (RSS/RO).
            CanvasGroup group = type.gameObject.GetComponent<CanvasGroup>() ?? type.gameObject.AddComponent<CanvasGroup>();
            bool locked = false;
            w.AddSync(() =>
            {
                bool l = menu._autopilot.Enabled;
                if (l == locked) return;
                locked = l;
                group.alpha = l ? 0.45f : 1;
                group.interactable = !l;
            });
        }

        // ShowTargetingGUIElements
        void BuildTargeting(KodeWindow w, UIObject r)
        {
            KodeWindow.Header(r, "#MechJeb_NewUI_Target"); //Target
            LayoutPanel box = KodeWindow.Box(r);

            // PSG with an optimized stage: periapsis, apoapsis, attach altitude, argument of periapsis and their warnings
            Layout opt = KodeWindow.Panel(box);
            w.Visible(opt.gameObject, () => optimize);
            AscentUi.SimpleBox(w, opt, "#MechJeb_Ascent_label1", Psgs.PeriapsisAltitude, "km"); //Target Periapsis
            AscentUi.SimpleBox(w, opt, "#MechJeb_Ascent_label2", Psgs.ApoapsisAltitude, "km"); //Target Apoapsis:

            // as the PSG simulation reads them: a negative apoapsis asks for a hyperbola, a lower positive one for a circle. The
            // circle is the default (apoapsis 0), so the note is dim, not a warning
            bool ApLtPe() => Psgs.ApoapsisAltitude >= 0 && Psgs.ApoapsisAltitude < Psgs.PeriapsisAltitude;
            UIText l3 = AscentUi.ToneText(opt, "#MechJeb_Ascent_label3", Tone.Dim); //Ap < Pe: circularizing orbit
            w.Visible(l3.gameObject, ApLtPe);

            AscentUi.ToggledBox(w, opt, "#MechJeb_Ascent_attachAlt", () => Psgs.BurnoutAltitudeFlag, v => Psgs.BurnoutAltitudeFlag = v,
                Psgs.BurnoutAltitude, "km", 100);
            AscentUi.ToggledBox(w, opt, "Arg Periapsis:", () => Psgs.ArgPFlag, v => Psgs.ArgPFlag = v, Psgs.ArgP, "°", 100);

            UIText high = AscentUi.ToneText(opt, "#MechJeb_Ascent_warnAttachAltHigh", Tone.Caution); //Attach > Ap: apoapsis insertion
            w.Visible(high.gameObject, () => Psgs.ApoapsisAltitude >= 0 && !ApLtPe() && Psgs.BurnoutAltitudeFlag &&
                Psgs.BurnoutAltitude > Psgs.ApoapsisAltitude);
            UIText l4 = AscentUi.ToneText(opt, "#MechJeb_Ascent_label4", Tone.Caution); //Hyperbolic target orbit (neg Ap)
            w.Visible(l4.gameObject, () => Psgs.ApoapsisAltitude < 0 && !TooShallow());
            // a negative apoapsis is the hyperbola's apoapsis radius, which has to be under minus the periapsis radius
            bool TooShallow() => Psgs.ApoapsisAltitude < 0 && Psgs.ApoapsisAltitude >= -(BodyRadius + Psgs.PeriapsisAltitude);
            UIText shallow = AscentUi.ToneText(opt, "Neg Ap must be below -(Pe + radius)", Tone.Bad);
            w.Visible(shallow.gameObject, TooShallow);
            UIText low = AscentUi.ToneText(opt, "#MechJeb_Ascent_warnAttachAltLow", Tone.Caution); //Attach < Pe: periapsis insertion
            w.Visible(low.gameObject, () => Psgs.BurnoutAltitudeFlag && Psgs.BurnoutAltitude < Psgs.PeriapsisAltitude);

            // PSG without: flight path angle (unless a plane is controlled) and attach altitude
            Layout fixedPlan = KodeWindow.Panel(box);
            w.Visible(fixedPlan.gameObject, () => psg && !optimize);
            Layout fpa = AscentUi.SimpleBox(w, fixedPlan, "Flight Path Angle", Psgs.FPA, "°");
            w.Visible(fpa.gameObject, () => !anyPlaneControl);
            AscentUi.SimpleBox(w, fixedPlan, "#MechJeb_Ascent_attachAlt", Psgs.BurnoutAltitude, "km");
            // why Pe and Ap are gone: nothing can be shut down early, so there is no orbit to aim at, only the energy to maximize
            UIText fixedNote = AscentUi.ToneText(fixedPlan, "Every stage has a fixed burn time: PSG reaches this burnout altitude and flight " +
                "path angle with all the energy the rocket has, and can't aim at a periapsis or apoapsis.", Tone.Dim, true);
            KodeWindow.WrapToWidth(fixedNote);

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
            KodeWindow.Label(inc, "°");
            UIButton current = KodeWindow.Button(inc, "#MechJeb_Ascent_button13", () => menu.SetInclinationToLatitude(menu.InclinationSetting)); //Current
            Tooltips.Add(current.gameObject, MechJebModuleAscentMenu.InclinationCurrentTooltip);

            UIText below = w.BoundLabel(box, DeltaText, true);
            KodeWindow.ToneText(below, Tone.Bad);
            w.Visible(below, () => 2.001 < incDelta);
            w.Attention(inc, () => 2.001 < incDelta);
        }

        // ---- LAUNCH: on the ground, the launch Engage starts ----

        static readonly MechJebModuleAscentMenu.LaunchKind[] launchKinds =
        {
            MechJebModuleAscentMenu.LaunchKind.NOW, MechJebModuleAscentMenu.LaunchKind.PLANE_OF_TARGET,
            MechJebModuleAscentMenu.LaunchKind.TARGET_LAN, MechJebModuleAscentMenu.LaunchKind.LAN
        };

        // the drop-down's entries: Now and Into the target's plane for both types, the two LAN launches for PSG
        string[] launchNames, launchNamesClassic, launchNamesPsg;

        string[] LaunchNames()
        {
            if (launchNames == null)
            {
                launchNames = new[]
                {
                    KodeWindow.Loc("#MechJeb_NewUI_LaunchNow"), //Now
                    KodeWindow.Loc("#MechJeb_NewUI_LaunchPlaneOfTarget"), //Into the target's plane
                    KodeWindow.Loc("#MechJeb_NewUI_LaunchTargetLan"), //To the target's LAN
                    KodeWindow.Loc("#MechJeb_NewUI_LaunchLan") //To a LAN
                };
                launchNamesClassic = new[] { launchNames[0], launchNames[1] };
                launchNamesPsg = launchNames;
            }

            return psg ? launchNamesPsg : launchNamesClassic;
        }

        MechJebModuleAscentMenu.LaunchKind Choice => menu.EffectiveLaunchChoice;

        bool TargetChoice => Choice == MechJebModuleAscentMenu.LaunchKind.PLANE_OF_TARGET || Choice == MechJebModuleAscentMenu.LaunchKind.TARGET_LAN;

        // ShowAutoWarpGUIElements (only when landed)
        void BuildLaunch(KodeWindow w, UIObject r)
        {
            UIText header = KodeWindow.Header(r, "#MechJeb_NewUI_Launch"); //Launch
            w.Visible(header.gameObject, () => landed);
            LayoutPanel box = KodeWindow.Box(r);
            w.Visible(box.gameObject, () => landed);

            // nothing being launched: what Engage will start
            Layout idle = KodeWindow.Panel(box);
            w.Visible(idle.gameObject, () => !anyPlaneControl);
            Layout choiceRow = KodeWindow.Row(idle);
            KodeWindow.Label(choiceRow, "#MechJeb_NewUI_Launch"); //Launch
            new ComboBox(w, choiceRow, () => (int)Choice, i => menu.LaunchChoice = (int)launchKinds[i], LaunchNames);
            w.Behaviour.AddRelocalize(() => launchNames = null);

            Layout offset = AscentUi.SimpleBox(w, idle, "#MechJeb_NewUI_LanOffset", Settings.LaunchLANDifference, "°", 60); //LAN offset
            w.Visible(offset.gameObject, () => TargetChoice);
            Layout lan = AscentUi.SimpleBox(w, idle, "#MechJeb_NewUI_Lan", Psgs.LAN, "°", 60); //LAN
            w.Visible(lan.gameObject, () => Choice == MechJebModuleAscentMenu.LaunchKind.LAN);

            UIText invalid = AscentUi.ToneText(idle, "#MechJeb_Ascent_warnInvalidTarget", Tone.Caution, true); // Target must be in the same sphere of influence.
            KodeWindow.WrapToWidth(invalid);
            w.Visible(invalid.gameObject, () => TargetChoice && !targetExists && Core.Target.NormalTargetExists);
            UIText select = KodeWindow.Paragraph(idle, "#MechJeb_Ascent_label34"); //No target: select one for a timed launch.
            KodeWindow.ToneText(select, Tone.Dim);
            w.Visible(select.gameObject, () => TargetChoice && !Core.Target.NormalTargetExists);

            UnityEngine.UI.Toggle overrideWarp = w.SwitchRow(idle, "#MechJeb_NewUI_LaunchAtOnce", () => Settings.OverrideWarpToPlane,
                v => Settings.OverrideWarpToPlane = v); //Launch at once, without waiting for the plane
            w.Visible(overrideWarp, () => Choice != MechJebModuleAscentMenu.LaunchKind.NOW);

            // the countdown: where warp stops before T-0, and under PSG when guidance starts
            Layout countdown = AscentUi.SimpleBox(w, box, "#MechJeb_NewUI_Countdown", Settings.WarpCountDown, "s", 60); //Countdown
            Tooltips.Add(countdown.gameObject, "#MechJeb_NewUI_CountdownTooltip");

            // launching: timer and abort
            Layout launching = KodeWindow.Panel(box);
            w.Visible(launching.gameObject, () => anyPlaneControl);
            w.BoundLabel(launching, () => string.IsNullOrEmpty(menu.launchTimer) ? " " : menu.launchTimer, true);
            UIButton abort = KodeWindow.Button(launching, "#MechJeb_Ascent_button17", menu.AbortTimedLaunch); //Abort
            KodeWindow.Role(abort, ButtonRole.Danger);
        }

        // PSG: guidance's status and its last failure stay in view; the solver's numbers fold away (BuildGuidanceDetails)
        void BuildGuidanceStatus(KodeWindow w, UIObject r)
        {
            // only while the ascent flies, or when guidance failed: on the pad, before anything runs, it has nothing to say
            Layout bar = w.StatusBar(r, GuidanceTone);
            w.Visible(bar.gameObject, () => psg && (menu._autopilot.Enabled || Psgs.LastFailureMessage != null));
            UIText status = w.BoundLabel(bar, () => menu.label26 ?? " ", true); //Guidance Status:
            w.ToneText(status, GuidanceTone);

            UIText reach = w.Wrap(w.BoundLabel(r, () => menu.ReachWarning ?? " ", true));
            KodeWindow.ToneText(reach, Tone.Bad);
            w.Visible(reach, () => psg && menu.ReachWarning != null);

            UIText failure = w.Wrap(w.BoundLabel(r, () => menu.label30 ?? " ", true)); //LAST FAILURE:
            KodeWindow.ToneText(failure, Tone.Bad);
            w.Visible(failure, () => psg && Psgs.LastFailureMessage != null);
        }

        // the windows of the ascent: Settings for both types, Stages and Solution for PSG, Path for Classic
        void BuildWindowButtons(KodeWindow w, UIObject r)
        {
            // one row across the window, the buttons sharing its width
            Layout buttons = KodeWindow.Row(r);
            // the PSG warnings on the button of the window that has their setting
            UIButton settingsButton = WindowButton(w, buttons, "#MechJeb_NewUI_AscentSettingsButton", menu._settingsMenu); //Settings
            w.Badge(settingsButton, () => psg && (PsgAttention.ArefUnset(Core) || PsgAttention.QaClamped(Core)));
            UIButton stages = WindowButton(w, buttons, "#MechJeb_NewUI_Stages", Core.GetComputerModule<MechJebModuleAscentPSGStagesMenu>()); //Stages
            w.Visible(stages.gameObject, () => psg);
            w.Badge(stages, () => PsgAttention.CoastTimesInverted(Core) || PsgAttention.CoastNoRelight(Core));
            UIButton solution = WindowButton(w, buttons, "#MechJeb_NewUI_SolutionButton", Core.GetComputerModule<MechJebModulePSGSolutionGraph>()); //Solution
            w.Visible(solution.gameObject, () => psg);
            UIButton path = WindowButton(w, buttons, "#MechJeb_NewUI_PathButton", menu._classicPathMenu); //Path
            w.Visible(path.gameObject, () => !psg);
        }

        /// <summary>A button that opens or closes another window, selected while that window is open.</summary>
        static UIButton WindowButton(KodeWindow w, UIObject parent, string text, DisplayModule window)
        {
            UIButton b = KodeWindow.Button(parent, text, () => window.Enabled = !window.Enabled, true);
            w.BindRole(b, () => window.Enabled ? ButtonRole.Selected : ButtonRole.Normal);
            return b;
        }

        // ShowStatusGUIElements (PSG only): the solver's numbers, in a fold whose state is saved
        void BuildGuidanceDetails(KodeWindow w, UIObject r)
        {
            Layout fold = KodeWindow.Panel(r);
            w.Visible(fold.gameObject, () => psg);
            Layout box = w.Section(fold, "#MechJeb_NewUI_GuidanceDetails", menu.GuidanceDetailsOpen); //Guidance details
            // the fold's state is the module's saved one: follow the header's clicks into it
            w.AddSync(() =>
            {
                bool open = box.gameObject.activeSelf;
                if (menu.GuidanceDetailsOpen != open) menu.GuidanceDetailsOpen = open;
            });

            for (int i = 0; i < MechJebModuleAscentMenu.MAX_PHASE_LINES; i++)
            {
                int k = i;
                UIText line = w.BoundLabel(box, () => k < menu.PhaseLineCount ? menu.PhaseLines[k] ?? "-" : "-", true);
                w.Visible(line, () => k < menu.PhaseLineCount);
            }

            Layout vgo = KodeWindow.Row(box);
            w.BoundLabel(vgo, "vgo: 99999.9", () => menu.vgo ?? " ");
            w.BoundLabel(vgo, "heading: 360.0", () => menu.heading ?? " ", true);
            Layout tgo = KodeWindow.Row(box);
            w.BoundLabel(tgo, "tgo: 999.999", () => menu.tgo ?? " ");
            w.BoundLabel(tgo, "pitch: -90.0", () => menu.pitch ?? " ", true);

            Layout conv = KodeWindow.Row(box);
            w.BoundLabel(conv, "converges: 999", () => menu.label27 ?? " "); //converges:
            w.BoundLabel(conv, "staleness: 99d 99h 99m 99s", () => menu.label29 ?? " ", true); //staleness:
            UIText optimizer = w.Wrap(w.BoundLabel(box, () => menu.optimizerState ?? " ", true));
            w.Visible(optimizer, () => menu.optimizerState != null);
            w.ToneText(optimizer, () => Psgs.State == OptimizerState.NO_PLAN || Psgs.State == OptimizerState.RETRYING ? Tone.Bad
                : Psgs.LastFailureMessage != null && Psgs.Staleness > 3 && Core.Guidance.Solution != null ? Tone.Warn : Tone.Normal);

            Layout n = KodeWindow.Row(box);
            w.BoundLabel(n, "n: 100(100)", () => menu.n ?? " ");
            w.BoundLabel(n, "infeasibility: -1.2345E-05", () => menu.znorm ?? " ", true);
        }
    }
}

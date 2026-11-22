using System.Collections.Generic;
using KodeKit;
using KodeKit.Headless;
using KodeUI;
using KSP.Localization;
using TMPro;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleSettings ("Settings") in KodeKit. Shown in flight and in the editor.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleSettings))]
    public class SettingsView : KodeView
    {
        readonly MechJebModuleSettings settings;

        public SettingsView(MechJebModuleSettings settings) : base(settings) => this.settings = settings;

        protected override float MinWidth => 200;

        MechJebCore Core => settings.Core;

        void Changed()
        {
            settings.Dirty = true;
            settings.ApplySettings();
        }

        // four groups: how the windows look, how they behave, the menu, and the two flight options; the restore button last
        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;
            MechJebModuleCustomWindowEditor editor = Core.GetComputerModule<MechJebModuleCustomWindowEditor>();
            MechJebModuleMenu menu = Core.GetComputerModule<MechJebModuleMenu>();

            UIObject look = w.Group(r, "#MechJeb_NewUI_Appearance"); //Appearance
            Layout scale = KodeWindow.Row(look);
            KodeWindow.Label(scale, "#MechJeb_Settings_label2", true); //"UI Scale:"
            for (int i = 0; i < MechJebModuleSettings.UiScalePresets.Length; i++)
            {
                double preset = MechJebModuleSettings.UiScalePresets[i];
                string name = MechJebModuleSettings.UiScaleNames[i];
                UIButton b = KodeWindow.Button(scale, name, () =>
                {
                    settings.UIScale.Text = name;
                    Changed();
                });
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                w.BindRole(b, () => settings.UIScale.Val == preset ? ButtonRole.Selected : ButtonRole.Normal);
            }
            // the typed scale reaches the setting when the edit ends: applied per keystroke, "0.9" would shrink every window
            // to the smallest scale at the "0"
            var typedScale = new EditableDouble(settings.UIScale.Val);
            bool scaleTyped = false;
            TMP_InputField scaleField = EditFields.Field(w, scale, () => scaleTyped ? typedScale.Text : settings.UIScale.Text, s =>
            {
                typedScale.Text = s;
                scaleTyped = true;
            }, 60, () => scaleTyped ? typedScale.IsParsed : settings.UIScale.IsParsed, label: "#MechJeb_Settings_label2"); //"UI Scale:"
            scaleField.onEndEdit.AddListener(_ =>
            {
                if (!scaleTyped) return;
                scaleTyped = false;
                settings.UIScale.Text = typedScale.Text;
                Changed();
            });

            // the new UI's look: its skin, spacing and section headers (KodeHost rebuilds the windows when the headers change)
            Layout skin = KodeWindow.Row(look);
            KodeWindow.Label(skin, "#MechJeb_NewUI_Skin");
            string[] skins = Locs("#MechJeb_NewUI_SkinClassic", "#MechJeb_NewUI_SkinFlat", "#MechJeb_NewUI_SkinGlass", "#MechJeb_NewUI_SkinZTheme");
            new ComboBox(w, skin, () => settings.UiSkin, i =>
            {
                settings.UiSkin = i;
                Changed();
            }, () => skins);

            // the new UI's window opacity (KodeHost applies it to the theme)
            Layout opacity = KodeWindow.Row(look);
            KodeWindow.Label(opacity, "#MechJeb_NewUI_Opacity");
            w.Slider(opacity, () => settings.UiOpacity, v =>
            {
                settings.UiOpacity = v;
                Changed();
            }, 0.3f, 1f);
            int shownPct = -1;
            string pctText = "";
            w.BoundLabel(opacity, () =>
            {
                int pct = Mathf.RoundToInt(settings.UiOpacity * 100);
                if (pct != shownPct)
                {
                    shownPct = pct;
                    pctText = pct + "%";
                }
                return pctText;
            }).transform.parent.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 40;

            w.Toggle(look, "#MechJeb_NewUI_Compact", () => settings.UiCompact, v =>
            {
                settings.UiCompact = v;
                Changed();
            });

            w.Toggle(look, "#MechJeb_NewUI_SectionHeaders", () => settings.UiSectionHeaders, v =>
            {
                settings.UiSectionHeaders = v;
                Changed();
            });

            UIObject windows = w.Group(r, "#MechJeb_NewUI_Windows"); //Windows
            RoverUi.ToggleRow(w, windows, editor, "Toggle:Settings.UseTitlebarDragging");
            w.Toggle(windows, "#MechJeb_Settings_checkbox1", () => settings.DontUseDropDownMenu, v => //"Replace drop down menu with arrow selector"
            {
                settings.DontUseDropDownMenu = v;
                Changed();
            });
            // it shows the refresh rate of Ascent Guidance and the custom windows, nothing else: named for that
            UnityEngine.UI.Toggle refresh = w.Toggle(windows, "#MechJeb_NewUI_ShowRefreshRate", () => settings.ShowAdvancedWindowSettings, v => //Show refresh-rate settings
            {
                settings.ShowAdvancedWindowSettings = v;
                Changed();
            });
            Tooltips.Add(refresh.gameObject, "#MechJeb_NewUI_ShowRefreshRate_tooltip");
            UnityEngine.UI.Toggle snap = w.Toggle(windows, "#MechJeb_NewUI_SnapWindows", () => settings.UiSnapWindows, v => //Snap windows to edges
            {
                settings.UiSnapWindows = v;
                Changed();
            });
            Tooltips.Add(snap.gameObject, "#MechJeb_NewUI_SnapWindows_tooltip");
            // off by default: a hovered planner operation draws its ghost orbit on the map (no node is placed)
            UnityEngine.UI.Toggle preview = w.Toggle(windows, "#MechJeb_NewUI_PlannerPreview", () => settings.UiPlannerPreview, v => //Preview maneuvers on the map
            {
                settings.UiPlannerPreview = v;
                Changed();
            });
            Tooltips.Add(preview.gameObject, "#MechJeb_NewUI_PlannerPreview_tooltip");
            // off by default: Utilities draws the throttle and the acceleration as meters against their limits (display only)
            UnityEngine.UI.Toggle meters = w.Toggle(windows, "#MechJeb_NewUI_ShowMeters", () => settings.UiMeters, v => //Show throttle and acceleration meters
            {
                settings.UiMeters = v;
                Changed();
            });
            Tooltips.Add(meters.gameObject, "#MechJeb_NewUI_ShowMeters_tooltip");

            RoverUi.ToggleRow(w, windows, editor, "Toggle:Settings.HideBrakeOnEject");

            UIObject menuGroup = w.Group(r, "#MechJeb_MenuHeader"); //Menu
            RoverUi.ToggleRow(w, menuGroup, editor, "Toggle:Menu.useAppLauncher");
            GameObject hideButton = RoverUi.ToggleRow(w, menuGroup, editor, "Toggle:Menu.hideButton");
            if (hideButton != null) w.Visible(hideButton, () => ToolbarManager.ToolbarAvailable || menu.useAppLauncher);
            MenuView.BuildMenuPosition(w, menuGroup, menu);
            // how long the menu takes to slide open or closed
            Layout anim = KodeWindow.Row(menuGroup);
            KodeWindow.Label(anim, "#MechJeb_MenuAnimationTime");
            w.Slider(anim, () => settings.MenuAnimationTime, v => settings.MenuAnimationTime = Mathf.Round(v * 20) / 20, 0f, 1f);
            float shownTime = -1;
            string timeText = "";
            w.BoundLabel(anim, () =>
            {
                if (settings.MenuAnimationTime != shownTime)
                {
                    shownTime = settings.MenuAnimationTime;
                    timeText = shownTime.ToString("0.00") + " s";
                }
                return timeText;
            }).transform.parent.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 40;
            Tooltips.Add(anim.gameObject, "#MechJeb_MenuAnimationTime_tooltip");


            UIObject flight = w.Group(r, "#MechJeb_NewUI_Flight"); //Flight
            RoverUi.ToggleRow(w, flight, editor, "Toggle:Settings.RssMode");
            w.Toggle(flight, "#MechJeb_Settings_checkbox2", () => Core.Warp.activateSASOnWarp, v => Core.Warp.activateSASOnWarp = v); //"Activate SAS on Warp"

            BuildShortcuts(w, r);

            BuildRestore(w, r);
        }

        // ---- keyboard shortcuts ----

        // static: a rebuild makes a new view, and the pick and the refusal must survive it (there is one Settings window)
        static int shortcutPick;     // the window picked in the drop-down (an index in Shortcuts.Actions)
        static string captureFor;    // the action whose last capture this window started (its refusal shows under it)
        int builtShortcuts = -1;     // the bindings' and actions' versions the rows were built for

        static int ShortcutsVersion() => unchecked(Shortcuts.Map.Version * 31 + Shortcuts.Actions.Version);

        protected override void BeforeSync()
        {
            // a binding made, refused or cleared, a capture started or ended, KSP's keys or the window list changed: the rows say so
            if (builtShortcuts >= 0 && ShortcutsVersion() != builtShortcuts) RebuildRequested = true;
        }

        /// <summary>
        /// The Shortcuts group: pick a window, press its button and then the chord (the next key, with Shift or Ctrl held, either side;
        /// Escape or a click cancels), or clear it. A refused chord and a binding that KSP's keys now collide with are said in words
        /// under the row. Then every binding, one a line. None is bound by default.
        /// </summary>
        void BuildShortcuts(KodeWindow w, UIObject r)
        {
            builtShortcuts = ShortcutsVersion();
            UIObject keys = w.Group(r, "#MechJeb_NewUI_Shortcuts"); //Keyboard shortcuts
            IReadOnlyList<UiAction> actions = Shortcuts.Actions.All;
            if (actions.Count == 0) return; // no core yet: the group's header only
            if (shortcutPick >= actions.Count) shortcutPick = 0;
            string[] labels = new string[actions.Count];
            for (int i = 0; i < labels.Length; i++) labels[i] = actions[i].Label;
            UiAction picked = actions[shortcutPick];
            ShortcutMap map = Shortcuts.Map;

            Layout pick = KodeWindow.Row(keys);
            // the rules in the tooltips, not a paragraph: the Settings window must still fit a 1080-line screen at scale 1.5
            UIText pickLabel = KodeWindow.Label(pick, "#MechJeb_NewUI_ShortcutAction"); //Action: (a window, or the command palette)
            pickLabel.tmpText.raycastTarget = true;
            Tooltips.Add(pickLabel.gameObject, "#MechJeb_NewUI_ShortcutsHint");
            new ComboBox(w, pick, () => shortcutPick, i =>
            {
                if (i == shortcutPick) return;
                shortcutPick = i;
                captureFor = null;
                map.CancelCapture();
                RebuildRequested = true;
            }, () => labels);

            Layout set = KodeWindow.Row(keys);
            KodeWindow.Label(set, "#MechJeb_NewUI_ShortcutKey"); //Shortcut:
            ShortcutBinding bound = map.Find(picked.Id);
            string chordText = map.Capturing == picked.Id ? Localizer.Format("#MechJeb_NewUI_ShortcutPress") //Press a key… (Esc cancels)
                : bound != null ? Shortcuts.Show(bound.Chord) : Localizer.Format("#MechJeb_NewUI_ShortcutNone"); //None
            UIButton chord = KodeWindow.Button(set, chordText, () =>
            {
                captureFor = picked.Id;
                map.BeginCapture(picked.Id);
            }, true);
            chord.gameObject.name = "ShortcutChord";
            // its text is the chord bound: named by its label
            KodeWindow.NameButton(chord, "#MechJeb_NewUI_ShortcutKey");
            Tooltips.Add(chord.gameObject, () => Localizer.Format("#MechJeb_NewUI_ShortcutKey_tooltip") + "\n" + Localizer.Format("#MechJeb_NewUI_ShortcutsHint"));
            UIButton clear = KodeWindow.Button(set, "#MechJeb_NewUI_ShortcutClear", () => //Clear
            {
                captureFor = null;
                map.CancelCapture();
                map.Clear(picked.Id);
            });
            clear.gameObject.name = "ShortcutClear";
            w.Visible(clear.gameObject, () => map.Find(picked.Id) != null);

            // why the last chord was refused, or why the bound one doesn't fire now
            string why = null;
            if (captureFor == picked.Id && map.Capturing == null && !map.LastCapture.Ok && !map.LastCaptured.IsEmpty)
                why = Localizer.Format("#MechJeb_NewUI_ShortcutRefused", Shortcuts.Show(map.LastCaptured), Shortcuts.Describe(map.LastCapture, map.LastCaptured));
            else if (bound != null && !bound.Live.Ok)
                why = Localizer.Format("#MechJeb_NewUI_ShortcutConflict", Shortcuts.Describe(bound.Live, bound.Chord));
            if (why != null)
            {
                UIText t = GuidanceUi.Wrapped(keys, why);
                t.gameObject.name = "ShortcutWhy";
                KodeWindow.ToneText(t, KodeWindow.Tone.Bad);
            }

            // every binding, each with its Clear: a binding whose window is gone (a custom window renamed or deleted) can't fire but
            // still holds its chord, and is cleared here
            foreach (ShortcutBinding b in map.Bindings)
            {
                string id = b.ActionId;
                Layout row = KodeWindow.Row(keys);
                UIText l = KodeWindow.Label(row, Localizer.Format("#MechJeb_NewUI_ShortcutLine", Shortcuts.ActionName(id), Shortcuts.Show(b.Chord)), true); //<<1>>: <<2>>
                l.gameObject.name = "ShortcutLine";
                if (!b.Live.Ok || map.IsOrphan(b)) KodeWindow.ToneText(l, KodeWindow.Tone.Bad);
                UIButton x = KodeWindow.Button(row, "#MechJeb_NewUI_ShortcutClear", () => //Clear
                {
                    if (captureFor == id) captureFor = null;
                    map.Clear(id);
                });
                x.gameObject.name = "ShortcutLineClear";
            }
        }

        /// <summary>
        /// "Restore factory default settings" deletes the global settings and this vessel type's, and can't be undone: it is the
        /// last row, and asks in a dialog (the confirm button in the Danger colour, Cancel the default) before it does.
        /// </summary>
        void BuildRestore(KodeWindow w, UIObject r)
        {
            UIButton ask = null;
            ask = KodeWindow.Button(r, "#MechJeb_NewUI_RestoreDefaults", () => //Restore factory default settings…
                Dialogs.Confirm(ask.gameObject, "#MechJeb_NewUI_RestoreTitle", //Restore factory default settings?
                    "#MechJeb_NewUI_RestoreWarning", //This deletes all MechJeb settings, ...
                    "#MechJeb_NewUI_RestoreConfirm", () => //Delete all MechJeb settings
                    {
                        settings.RestoreFactoryDefaults();
                        Toasts.Show("#MechJeb_NewUI_Toast_DefaultsRestored", ToastSeverity.Warning);
                    }), true);
        }

        static string[] Locs(params string[] keys)
        {
            var r = new string[keys.Length];
            for (int i = 0; i < keys.Length; i++) r[i] = Localizer.Format(keys[i]);
            return r;
        }
    }
}

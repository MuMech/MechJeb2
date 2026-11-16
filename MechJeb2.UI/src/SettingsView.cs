using KodeKit;
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
            }, 60, () => scaleTyped ? typedScale.IsParsed : settings.UIScale.IsParsed);
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

            BuildRestore(w, r);
        }

        // how long the confirmation waits for its second click
        const float ConfirmSeconds = 10f;

        float armedUntil = float.NegativeInfinity;

        bool Armed => Time.unscaledTime < armedUntil;

        /// <summary>
        /// "Restore factory default settings" deletes the global settings and this vessel type's, and can't be undone: it is the
        /// last row, and asks again (in the Danger colour, with Cancel) before it does; the question goes after ConfirmSeconds.
        /// </summary>
        void BuildRestore(KodeWindow w, UIObject r)
        {
            UIButton ask = KodeWindow.Button(r, "#MechJeb_NewUI_RestoreDefaults", () => armedUntil = Time.unscaledTime + ConfirmSeconds, true); //Restore factory default settings…
            w.Visible(ask.gameObject, () => !Armed);

            Layout confirm = KodeWindow.Panel(r);
            UIText warning = KodeWindow.Paragraph(confirm, "#MechJeb_NewUI_RestoreWarning"); //This deletes all MechJeb settings, ...
            KodeWindow.ToneText(warning, KodeWindow.Tone.Warn);
            Layout buttons = KodeWindow.Row(confirm);
            UIButton delete = KodeWindow.Button(buttons, "#MechJeb_NewUI_RestoreConfirm", () => //Delete all MechJeb settings
            {
                armedUntil = float.NegativeInfinity;
                settings.RestoreFactoryDefaults();
            }, true);
            KodeWindow.Role(delete, ButtonRole.Danger);
            KodeWindow.Button(buttons, "#MechJeb_NewUI_Cancel", () => armedUntil = float.NegativeInfinity); //Cancel
            // hidden once its texts are made (TMP sets up under an active parent)
            w.Visible(confirm.gameObject, () => Armed);
        }

        static string[] Locs(params string[] keys)
        {
            var r = new string[keys.Length];
            for (int i = 0; i < keys.Length; i++) r[i] = Localizer.Format(keys[i]);
            return r;
        }
    }
}

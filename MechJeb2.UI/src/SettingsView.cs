using KodeKit;
using KodeUI;
using KSP.Localization;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleSettings ("Settings") in KodeKit, row for row with its WindowGUI. Shown in flight and in the editor.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleSettings))]
    public class SettingsView : KodeView
    {
        readonly MechJebModuleSettings settings;

        public SettingsView(MechJebModuleSettings settings) : base(settings) => this.settings = settings;

        protected override float MinWidth => 200;

        MechJebCore Core => settings.Core;

        // cached "Current skin: <<1>>"
        int shownSkin = -1;
        string skinLabel;

        string SkinLabel()
        {
            if (settings.SkinId != shownSkin)
            {
                shownSkin = settings.SkinId;
                skinLabel = Localizer.Format("#MechJeb_Settings_label1", (GuiUtils.SkinType)settings.SkinId);
            }

            return skinLabel;
        }

        void Changed()
        {
            settings.Dirty = true;
            settings.ApplySettings();
        }

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            KodeWindow.Button(r, "#MechJeb_Settings_button1", settings.RestoreFactoryDefaults, true); //"\nRestore factory default settings\n"

            w.BoundLabel(r, SkinLabel, true);
            SkinButton(w, "#MechJeb_Settings_button2", GuiUtils.SkinType.MECH_JEB1); //"Use MechJeb 1 GUI skin"
            SkinButton(w, "#MechJeb_Settings_button3", GuiUtils.SkinType.DEFAULT);   //"Use MechJeb 2 GUI skin"
            SkinButton(w, "#MechJeb_Settings_button4", GuiUtils.SkinType.COMPACT);   //"Use MJ2 Compact GUI skin"

            Layout scale = KodeWindow.Row(r);
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
            EditFields.Field(w, scale, () => settings.UIScale.Text, s =>
            {
                settings.UIScale.Text = s;
                Changed();
            }, 60, () => settings.UIScale.IsParsed);

            w.Toggle(r, "#MechJeb_Settings_checkbox1", () => settings.DontUseDropDownMenu, v => //"Replace drop down menu with arrow selector"
            {
                settings.DontUseDropDownMenu = v;
                Changed();
            });
            w.Toggle(r, "Show Advanced Window Settings", () => settings.ShowAdvancedWindowSettings, v =>
            {
                settings.ShowAdvancedWindowSettings = v;
                Changed();
            });

            MechJebModuleCustomWindowEditor editor = Core.GetComputerModule<MechJebModuleCustomWindowEditor>();
            RoverUi.ToggleRow(w, r, editor, "Toggle:Settings.HideBrakeOnEject");
            RoverUi.ToggleRow(w, r, editor, "Toggle:Settings.UseTitlebarDragging");
            RoverUi.ToggleRow(w, r, editor, "Toggle:Menu.useAppLauncher");
            GameObject hideButton = RoverUi.ToggleRow(w, r, editor, "Toggle:Menu.hideButton");
            MechJebModuleMenu menu = Core.GetComputerModule<MechJebModuleMenu>();
            if (hideButton != null) w.Visible(hideButton, () => ToolbarManager.ToolbarAvailable || menu.useAppLauncher);

            KodeWindow.Header(r, "#MechJeb_MenuHeader", true); //Menu
            MenuView.BuildMenuPosition(w, r, menu);

            RoverUi.ToggleRow(w, r, editor, "Toggle:Settings.RssMode");

            w.Toggle(r, "#MechJeb_Settings_checkbox2", () => Core.Warp.activateSASOnWarp, v => Core.Warp.activateSASOnWarp = v); //"Activate SAS on Warp"

            w.Toggle(r, "#MechJeb_NewUI_UseNewUI", () => settings.Backend == MechJebModuleSettings.UiBackendType.Kode, v =>
            {
                settings.UiBackend = (int)(v ? MechJebModuleSettings.UiBackendType.Kode : MechJebModuleSettings.UiBackendType.Imgui);
                Changed();
            });

            // the new UI's window opacity (KodeHost applies it to the theme)
            Layout opacity = KodeWindow.Row(r);
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
            Tooltips.Add(opacity.gameObject, "How opaque the new UI's window backgrounds are. Lower lets more of the scene show through.");

            // the new UI's look: its skin, and the modern layout (KodeHost rebuilds the windows when the layout changes)
            Layout skin = KodeWindow.Row(r);
            KodeWindow.Label(skin, "#MechJeb_NewUI_Skin");
            string[] skins = Locs("#MechJeb_NewUI_SkinClassic", "#MechJeb_NewUI_SkinFlat", "#MechJeb_NewUI_SkinGlass", "#MechJeb_NewUI_SkinZTheme");
            new ComboBox(w, skin, () => settings.UiSkin, i =>
            {
                settings.UiSkin = i;
                Changed();
            }, () => skins);

            w.Toggle(r, "#MechJeb_NewUI_Compact", () => settings.UiCompact, v =>
            {
                settings.UiCompact = v;
                Changed();
            });

            w.Toggle(r, "#MechJeb_NewUI_ModernLayout", () => settings.UiModernLayout, v =>
            {
                settings.UiModernLayout = v;
                Changed();
            });
            GameObject headers = w.Toggle(r, "#MechJeb_NewUI_SectionHeaders", () => settings.UiSectionHeaders, v =>
            {
                settings.UiSectionHeaders = v;
                Changed();
            }).transform.parent.gameObject;
            w.Visible(headers, () => settings.UiModernLayout);
        }

        static string[] Locs(params string[] keys)
        {
            var r = new string[keys.Length];
            for (int i = 0; i < keys.Length; i++) r[i] = Localizer.Format(keys[i]);
            return r;
        }

        void SkinButton(KodeWindow w, string key, GuiUtils.SkinType skin)
        {
            UIButton b = KodeWindow.Button(w.Root, key, () =>
            {
                GuiUtils.LoadSkin(skin);
                settings.SkinId = (int)skin;
                Changed();
            }, true);
            w.Visible(b.gameObject, () => settings.SkinId != (int)skin);
        }
    }
}

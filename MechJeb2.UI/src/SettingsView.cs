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
            RegistryToggle(w, r, editor, "Toggle:Settings.HideBrakeOnEject");
            RegistryToggle(w, r, editor, "Toggle:Settings.UseTitlebarDragging");
            RegistryToggle(w, r, editor, "Toggle:Menu.useAppLauncher");
            GameObject hideButton = RegistryToggle(w, r, editor, "Toggle:Menu.hideButton");
            MechJebModuleMenu menu = Core.GetComputerModule<MechJebModuleMenu>();
            if (hideButton != null) w.Visible(hideButton, () => ToolbarManager.ToolbarAvailable || menu.useAppLauncher);

            MenuView.BuildMenuPosition(w, r, menu);

            RegistryToggle(w, r, editor, "Toggle:Settings.RssMode");

            w.Toggle(r, "#MechJeb_Settings_checkbox2", () => Core.Warp.activateSASOnWarp, v => Core.Warp.activateSASOnWarp = v); //"Activate SAS on Warp"

            w.Toggle(r, "Use the new UI for ported windows (experimental)", () => settings.Backend == MechJebModuleSettings.UiBackendType.Kode, v =>
            {
                settings.UiBackend = (int)(v ? MechJebModuleSettings.UiBackendType.Kode : MechJebModuleSettings.UiBackendType.Imgui);
                Changed();
            });

            // the new UI's window opacity (KodeHost applies it to the theme)
            Layout opacity = KodeWindow.Row(r);
            KodeWindow.Label(opacity, "New UI opacity");
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

        /// <summary>A ToggleInfoItem from the custom window registry, as InfoItem.DrawItem draws it. Returns its row.</summary>
        static GameObject RegistryToggle(KodeWindow w, UIObject parent, MechJebModuleCustomWindowEditor editor, string id)
        {
            if (!(editor.registry.Find(i => i.id == id) is ToggleInfoItem item)) return null;
            UnityEngine.UI.Toggle t = w.Toggle(parent, item.localizedName, () => item.Value, v => item.Value = v);
            GameObject row = t.transform.parent.gameObject;
            if (!string.IsNullOrEmpty(item.localizedTooltip))
                Tooltips.Add(row.GetComponentInChildren<ToggleLabelClick>().gameObject, item.localizedTooltip);
            return row;
        }
    }
}

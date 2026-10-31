using System.Collections.Generic;
using KodeKit;
using KodeUI;
using KSP.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleCustomWindowEditor ("Custom Window Editor") in KodeKit, row for row with its WindowGUI and DrawGUI's
    /// colour picker. The item lists are rebuilt (the whole view) when the edited window, its items or the category change.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleCustomWindowEditor))]
    public class CustomWindowEditorView : KodeView
    {
        readonly MechJebModuleCustomWindowEditor ed;

        // what the lists were built from
        MechJebModuleCustomInfoWindow builtWindow;
        InfoItem[] builtItems = new InfoItem[0];
        InfoItem.Category builtCategory;
        int builtWindowCount;

        public CustomWindowEditorView(MechJebModuleCustomWindowEditor ed) : base(ed) => this.ed = ed;

        protected override float MinWidth => 220;

        MechJebModuleCustomInfoWindow Edited => ed.editedWindow;
        List<ComputerModule> AllWindows => ed.Core.GetComputerModules<MechJebModuleCustomInfoWindow>();

        protected override void BeforeSync()
        {
            if (Edited == null) ed.editedWindow = ed.Core.GetComputerModule<MechJebModuleCustomInfoWindow>();
            if (Edited != builtWindow || ed.itemCategory != builtCategory || AllWindows.Count != builtWindowCount)
                RebuildRequested = true;
            else if (Edited != null && ItemsChanged())
                SyncContents(); // items added, removed or moved: update the rows in place, no rebuild
        }

        // the 'window contents' list: pooled rows, one per item of the edited window
        KodeWindow win;
        Layout contentsList;
        readonly List<UIButton> contentRows = new List<UIButton>();
        readonly List<InfoItem> rowItems = new List<InfoItem>();

        void SyncContents()
        {
            List<InfoItem> items = Edited.items;
            builtItems = items.ToArray();
            for (int i = 0; i < items.Count; i++)
            {
                if (i == contentRows.Count)
                {
                    int index = i;
                    UIButton row = KodeWindow.ListRow(contentsList, items[i].ListName, () => ed.selectedItemIndex = index);
                    win.AddSync(() => KodeWindow.SetSelected(row, ed.selectedItemIndex == index)); // GuiUtils.YellowLabel
                    // a header's name follows its text as it is typed
                    string shownText = null;
                    win.AddSync(() =>
                    {
                        if (index >= rowItems.Count || !(rowItems[index] is HeaderInfoItem h) || ReferenceEquals(h.Text, shownText)) return;
                        shownText = h.Text;
                        row.Text(h.ListName);
                    });
                    contentRows.Add(row);
                    rowItems.Add(items[i]);
                }
                else if (rowItems[i] != items[i])
                {
                    rowItems[i] = items[i];
                    contentRows[i].Text(items[i].ListName);
                }
                if (!contentRows[i].gameObject.activeSelf) contentRows[i].gameObject.SetActive(true);
            }
            for (int i = items.Count; i < contentRows.Count; i++)
                if (contentRows[i].gameObject.activeSelf) contentRows[i].gameObject.SetActive(false);
        }

        bool ItemsChanged()
        {
            if (Edited == null) return builtItems.Length != 0;
            List<InfoItem> items = Edited.items;
            if (items.Count != builtItems.Length) return true;
            for (int i = 0; i < items.Count; i++)
                if (items[i] != builtItems[i]) return true;
            return false;
        }

        protected override void BuildContents(KodeWindow w)
        {
            if (Edited == null) ed.editedWindow = ed.Core.GetComputerModule<MechJebModuleCustomInfoWindow>();
            builtWindow = Edited;
            builtItems = Edited != null ? Edited.items.ToArray() : new InfoItem[0];
            builtCategory = ed.itemCategory;
            builtWindowCount = AllWindows.Count;

            UIObject r = w.Root;

            Layout top = KodeWindow.Row(r);
            KodeWindow.Button(top, "#MechJeb_WindowEd_button1", ed.AddNewWindow, true); //New window
            if (Edited != null) KodeWindow.Button(top, "#MechJeb_WindowEd_button2", ed.RemoveCurrentWindow, true); //Delete window

            if (Edited != null) BuildEditedWindow(w, r);

            if (KodeWindow.ModernLayout)
            {
                // modern layout: every preset in a dropdown, and a button to create the chosen one
                KodeWindow.Header(r, "#MechJeb_WindowEd_label5"); //Window presets:
                var presetNames = new string[CustomWindowPresets.presets.Length];
                for (int i = 0; i < presetNames.Length; i++) presetNames[i] = CustomWindowPresets.presets[i].name;
                Layout pick = KodeWindow.Row(r);
                new ComboBox(w, pick, () => ed.presetIndex, i => ed.presetIndex = i, () => presetNames);
                KodeWindow.Button(pick, "#MechJeb_NewUI_Create", CreateFromPreset, ButtonRole.Primary); //Create
                return;
            }

            // presets
            UIText presets = KodeWindow.Label(r, "#MechJeb_WindowEd_label5", true); //Window presets:
            presets.tmpText.alignment = TextAlignmentOptions.Center;
            Layout presetRow = KodeWindow.Row(r);
            int count = CustomWindowPresets.presets.Length;
            KodeWindow.Button(presetRow, "<", () => ed.presetIndex = (ed.presetIndex - 1 + count) % count);
            UIButton preset = KodeWindow.Button(presetRow, CustomWindowPresets.presets[ed.presetIndex].name, CreateFromPreset, true);
            int shownPreset = ed.presetIndex;
            w.AddSync(() =>
            {
                if (ed.presetIndex == shownPreset) return;
                shownPreset = ed.presetIndex;
                preset.Text(CustomWindowPresets.presets[shownPreset].name);
            });
            KodeWindow.Button(presetRow, ">", () => ed.presetIndex = (ed.presetIndex + 1) % count);
        }

        void CreateFromPreset()
        {
            MechJebModuleCustomInfoWindow created = ed.CreateWindowFromSharingString(CustomWindowPresets.presets[ed.presetIndex].sharingString);
            if (created != null) ed.editedWindow = created;
        }

        /// <summary>The edited window's title, in a field; a change marks the window for saving.</summary>
        static void TitleField(KodeWindow w, UIObject parent, MechJebModuleCustomInfoWindow edited) =>
            EditFields.Field(w, parent, () => edited.title, s =>
            {
                if (edited.title == s) return;
                edited.title = s;
                edited.Dirty = true;
            }, 120);

        string[] titles = new string[0];

        /// <summary>The custom windows' titles, rebuilt only when one changes (a rename, a new window).</summary>
        string[] WindowTitles()
        {
            List<ComputerModule> all = AllWindows;
            bool same = titles.Length == all.Count;
            for (int i = 0; same && i < all.Count; i++)
                same = ReferenceEquals(titles[i], ((MechJebModuleCustomInfoWindow)all[i]).title);
            if (same) return titles;
            titles = new string[all.Count];
            for (int i = 0; i < all.Count; i++) titles[i] = ((MechJebModuleCustomInfoWindow)all[i]).title;
            return titles;
        }

        /// <summary>IMGUI's title row: Title: [&lt;] [title field] [&gt;], stepping through the custom windows.</summary>
        void TitleRow(KodeWindow w, UIObject r, MechJebModuleCustomInfoWindow edited, List<ComputerModule> all)
        {
            Layout title = KodeWindow.Row(r);
            KodeWindow.Label(title, "#MechJeb_WindowEd_Edtitle"); //Title:
            KodeWindow.Button(title, "<", () => ed.editedWindow = (MechJebModuleCustomInfoWindow)all[(all.IndexOf(edited) - 1 + all.Count) % all.Count]);
            TitleField(w, title, edited);
            KodeWindow.Button(title, ">", () => ed.editedWindow = (MechJebModuleCustomInfoWindow)all[(all.IndexOf(edited) + 1) % all.Count]);
        }

        void BuildEditedWindow(KodeWindow w, UIObject r)
        {
            MechJebModuleCustomInfoWindow edited = Edited;
            List<ComputerModule> all = AllWindows;

            if (KodeWindow.ModernLayout)
            {
                // modern layout: the edited window picked from a dropdown, its title in a field
                KodeWindow.Header(r, "#MechJeb_WindowEd_Edtitle"); //Title:
                Layout pick = KodeWindow.Row(r);
                new ComboBox(w, pick, () => all.IndexOf(edited), i => ed.editedWindow = (MechJebModuleCustomInfoWindow)all[i], WindowTitles);
                TitleField(w, pick, edited);
            }
            else
                TitleRow(w, r, edited, all);

            Layout showIn = KodeWindow.Row(r);
            KodeWindow.Label(showIn, "#MechJeb_WindowEd_label1"); //Show in:
            w.Toggle(showIn, "#MechJeb_WindowEd_checkbox1", () => edited.ShowInFlight, v => edited.ShowInFlight = v); //Flight
            w.Toggle(showIn, "#MechJeb_WindowEd_checkbox2", () => edited.ShowInEditor, v => edited.ShowInEditor = v); //Editor

            Layout flags = KodeWindow.Row(r);
            w.Toggle(flags, "#MechJeb_WindowEd_checkbox3", () => edited.IsOverlay, v => edited.IsOverlay = v); //Overlay
            w.Toggle(flags, "#MechJeb_WindowEd_checkbox4", () => edited.WindowLocked, v => edited.WindowLocked = v); //Locked

            // Compact: off, the new UI's global setting, or on, whichever that one is
            Layout compact = KodeWindow.Row(r);
            KodeWindow.Label(compact, "#MechJeb_WindowEd_checkbox5"); //Compact
            string[] compactNames =
            {
                Localizer.Format("#MechJeb_NewUI_CompactOff"), Localizer.Format("#MechJeb_NewUI_CompactFollow"), Localizer.Format("#MechJeb_NewUI_CompactOn")
            };
            w.Switch3(compact, compactNames, () => (int)edited.Compact, v => edited.Compact = (MechJebModuleCustomInfoWindow.CompactMode)v);

            // Color: [x] Text [x] Background (exclusive), with the picker under them
            Layout color = KodeWindow.Row(r);
            KodeWindow.Label(color, "#MechJeb_WindowEd_label2"); //Color:
            w.Toggle(color, "#MechJeb_WindowEd_checkbox6", () => ed.editingText, v => //Text
            {
                ed.editingText = v;
                if (v) ed.editingBackground = false;
            });
            w.Toggle(color, "#MechJeb_WindowEd_checkbox7", () => ed.editingBackground, v => //Background
            {
                ed.editingBackground = v;
                if (v) ed.editingText = false;
            });
            // the picker (and its textures) is made the first time a colour is edited
            w.LazyPanel(r, () => ed.editingText || ed.editingBackground, picker =>
                new KodeKit.ColorPicker(w, picker, () => ed.editingText ? edited.text : edited.backgroundColor, c =>
                {
                    if (ed.editingText) ed.SetTextColor(c);
                    else ed.SetBackgroundColor(c);
                }));

            // the window's items: click to select
            KodeWindow.Label(r, "#MechJeb_WindowEd_label3"); //Window contents (click to edit):
            win = w;
            KodeWindow.ScrollList(r, 100, out contentsList);
            contentRows.Clear();
            rowItems.Clear();
            SyncContents();

            Layout edit = KodeWindow.Row(r);
            KodeWindow.Button(edit, "#MechJeb_WindowEd_button3", ed.RemoveSelectedItem, true); //Remove
            KodeWindow.Button(edit, "#MechJeb_WindowEd_button4", () => ed.MoveSelectedItem(-1), true); //Move up
            KodeWindow.Button(edit, "#MechJeb_WindowEd_button5", () => ed.MoveSelectedItem(1), true); //Move down

            // the selected header's text
            Layout header = KodeWindow.Row(r);
            KodeWindow.Label(header, "#MechJeb_WindowEd_HeaderText"); //Header text:
            EditFields.Field(w, header, () => ed.SelectedHeader()?.Text ?? "", s => ed.SetHeaderText(ed.SelectedHeader(), s), 120);
            w.Visible(header.gameObject, () => ed.SelectedHeader() != null);

            // the registry: pick a category, click an item to add it
            KodeWindow.Label(r, "#MechJeb_WindowEd_label4"); //Click an item to add it to the info window:
            new ComboBox(w, r, () => (int)ed.itemCategory, i => ed.itemCategory = (InfoItem.Category)i,
                () => MechJebModuleCustomWindowEditor.categories);
            KodeWindow.ScrollList(r, 260, out Layout registry);
            foreach (InfoItem item in ed.ItemsOfCategory())
            {
                InfoItem add = item;
                KodeWindow.ListRow(registry, Localizer.Format(item.description), () => ed.AddItem(add));
            }
        }

    }
}

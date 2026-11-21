using System;
using System.Collections.Generic;
using KodeKit;
using KodeKit.Headless;
using KodeUI;
using KSP.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleCustomWindowEditor ("Custom Window Editor") in KodeKit, with its
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
        bool builtCategoryAll;
        int builtWindowCount;
        string appliedFilter;

        public CustomWindowEditorView(MechJebModuleCustomWindowEditor ed) : base(ed)
        {
            this.ed = ed;
            // the host builds a new view when the window is rebuilt (a category change): the checks live on the module
            if (!(ed.itemChecks is Checks c)) ed.itemChecks = c = new Checks(ed);
            checks = c;
        }

        /// <summary>The checked rows and the window they belong to, kept on the editor module across the view's rebuilds.</summary>
        sealed class Checks
        {
            public readonly SelectionSet<ItemKey> Set;
            public MechJebModuleCustomInfoWindow Window;

            public Checks(MechJebModuleCustomWindowEditor ed) =>
                Set = new SelectionSet<ItemKey>(() => ed.editedWindow != null ? ed.editedWindow.items.Count : 0, i => KeyOf(ed.editedWindow.items, i));
        }

        readonly Checks checks;

        /// <summary>
        /// A row of the edited window's contents, for its check box: the item, and its place among the rows that hold the same item
        /// (a registry item can be added twice), so a row keeps its check when rows above it are moved, added or removed.
        /// </summary>
        public struct ItemKey : IEquatable<ItemKey>
        {
            public InfoItem Item;
            public int Nth;

            public bool Equals(ItemKey o) => ReferenceEquals(Item, o.Item) && Nth == o.Nth;
            public override bool Equals(object o) => o is ItemKey k && Equals(k);
            public override int GetHashCode() => (Item != null ? Item.GetHashCode() : 0) * 31 + Nth;
        }

        static ItemKey KeyOf(List<InfoItem> items, int i)
        {
            InfoItem item = items[i];
            int nth = 0;
            for (int j = 0; j < i; j++)
                if (ReferenceEquals(items[j], item)) nth++;
            return new ItemKey { Item = item, Nth = nth };
        }

        // the contents' checked rows (the toggle column): kept across rebuilds of the same window, emptied when another window is edited
        SelectionSet<ItemKey> checkedItems => checks.Set;

        /// <summary>The window contents' checked rows (for tests and the agent bridge): Remove and Move act on them when there are any.</summary>
        public SelectionSet<ItemKey> CheckedItems => checkedItems;

        protected override float MinWidth => 220;

        MechJebModuleCustomInfoWindow Edited => ed.editedWindow;
        List<ComputerModule> AllWindows => ed.Core.GetComputerModules<MechJebModuleCustomInfoWindow>();

        protected override void BeforeSync()
        {
            if (Edited == null) ed.editedWindow = ed.Core.GetComputerModule<MechJebModuleCustomInfoWindow>();
            if (Edited != builtWindow || ed.itemCategory != builtCategory || ed.itemCategoryAll != builtCategoryAll || AllWindows.Count != builtWindowCount)
                RebuildRequested = true;
            else if (Edited != null && ItemsChanged())
                SyncContents(); // items added, removed or moved: update the rows in place, no rebuild
            // the filter set from outside the field (a read-out's menu shows its item here): rank the rows again
            if (!RebuildRequested && !ReferenceEquals(ed.itemFilter, appliedFilter))
            {
                appliedFilter = ed.itemFilter;
                if (registryList != null) ApplyRegistryFilter();
            }
        }

        // the 'window contents' list: a virtual list of the edited window's items, reordered by drag (or the Move buttons) and
        // filled by a drag from the registry list (or a click there)
        VirtualList<UIButton> contentsList;
        ReorderModel reorder;
        TransferModel transfer;
        ListEdits listEdits;
        ListDrag contentsDrag, registryDrag;
        int shownSelected = -1;
        string shownHeaderText;

        /// <summary>The window contents' virtual list, its reorder and the registry's transfer into it (for tests and the agent bridge).</summary>
        public VirtualList<UIButton> ContentsList => contentsList;

        public ListDrag ContentsDrag => contentsDrag;
        public ListDrag RegistryDrag => registryDrag;

        /// <summary>The items changed (a drop, a click, the buttons, an agent): bind the rows again, the view's place kept.</summary>
        void SyncContents()
        {
            // a drag's row indices are the old list's: a change under it (the Remove button, an agent) ends it without a drop
            contentsDrag?.Cancel();
            registryDrag?.Cancel();
            builtItems = Edited.items.ToArray();
            checkedItems.Prune();
            contentsList.SetCount(builtItems.Length);
            contentsList.Rebind();
        }

        // the selected row in the warning colour, and a selected header's name following its text as it is typed
        void SyncSelection()
        {
            int selected = ed.selectedItemIndex;
            if (selected != shownSelected)
            {
                UIButton was = contentsList.RowAt(shownSelected), now = contentsList.RowAt(selected);
                shownSelected = selected;
                if (was != null) KodeWindow.SetSelected(was, false);
                if (now != null) KodeWindow.SetSelected(now, true);
            }
            HeaderInfoItem h = ed.SelectedHeader();
            if (h != null && !ReferenceEquals(h.Text, shownHeaderText))
            {
                shownHeaderText = h.Text;
                contentsList.Rebind();
            }
        }

        // a row bound to item i: its selected look
        void BindContentsRow(UIButton row, int i) => KodeWindow.SetSelected(row, i == ed.selectedItemIndex);

        // The edited window's item list as one control on the editor's commit path: a move, an add or a removal is one commit, with
        // the source of the drag, click or agent that made it, so the path's observers (AnyCommit, the agent bridge's change feed)
        // see it and an undo stack can take it back once the editor opts in. Its value is the item count; its state is the list.
        CommitTarget<int> itemsTarget;

        /// <summary>The edited window's item list on the commit path (for tests and the agent bridge): its moves, adds and removals.</summary>
        public CommitTarget ItemsTarget => itemsTarget;

        /// <summary>
        /// What an undo of the item list restores: the edited window's items in their order, and the selection. Only while the same
        /// window is edited: another window's list is never written.
        /// </summary>
        sealed class ItemsState : IUndoState
        {
            readonly CustomWindowEditorView view;

            public ItemsState(CustomWindowEditorView view) => this.view = view;

            sealed class Shot
            {
                public MechJebModuleCustomInfoWindow Window;
                public InfoItem[] Items;
                public int Selected;

                public override bool Equals(object o)
                {
                    if (!(o is Shot s) || s.Window != Window || s.Selected != Selected || s.Items.Length != Items.Length) return false;
                    for (int i = 0; i < Items.Length; i++)
                        if (!ReferenceEquals(s.Items[i], Items[i])) return false;
                    return true;
                }

                public override int GetHashCode() => Items.Length;
            }

            public object Take()
            {
                MechJebModuleCustomInfoWindow w = view.Edited;
                return w == null ? null : new Shot { Window = w, Items = w.items.ToArray(), Selected = view.ed.selectedItemIndex };
            }

            public bool Restore(object state)
            {
                if (!(state is Shot s) || s.Window != view.Edited) return false;
                s.Window.items.Clear();
                s.Window.items.AddRange(s.Items);
                view.ed.selectedItemIndex = s.Selected;
                s.Window.Dirty = true;
                return true;
            }
        }

        int ItemCount() => Edited != null ? Edited.items.Count : 0;

        // a move of the edited window's items: the module's one edit, as a commit, then the list's rows follow
        void MoveItem(int from, int to)
        {
            itemsTarget.Apply(to, _ => ed.MoveItem(from, to), reorder.Source);
            contentsList.Removed(from, 1);
            contentsList.Inserted(to, 1);
            builtItems = Edited.items.ToArray();
        }

        // registry result `rank` into the edited window at `at`: the module's one edit, as a commit, then the list's rows follow
        void InsertItem(int rank, int at)
        {
            InfoItem item = RegistryItemAt(rank);
            itemsTarget.Apply(at, _ => ed.AddItem(item, at), transfer.Source);
            contentsList.Inserted(Math.Min(at, contentsList.Count), 1);
            builtItems = Edited.items.ToArray();
        }

        // the Remove button: the checked items out when there are any, else the selected item, as one commit (the list follows at
        // the next sync)
        void RemoveSelected()
        {
            if (checkedItems.Count > 0)
            {
                ChangeSource source = KodeWindow.Pointer;
                itemsTarget.Apply(checkedItems.Count, _ => checkedItems.RemoveSelected(ed.RemoveItem, source), source);
                return;
            }
            itemsTarget.Apply(ed.selectedItemIndex, _ => ed.RemoveSelectedItem(), KodeWindow.Pointer);
        }

        // the Move buttons: the checked items one place up or down as blocks when there are any (one commit), else the selected item
        void MoveSelected(int direction)
        {
            if (checkedItems.Count > 0)
            {
                itemsTarget.Apply(direction, d => checkedItems.MoveSelected(d, ed.MoveItem), KodeWindow.Pointer);
                return;
            }
            reorder.MoveBy(ed.selectedItemIndex, direction, KodeWindow.Pointer);
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
            builtCategoryAll = ed.itemCategoryAll;
            builtWindowCount = AllWindows.Count;
            appliedFilter = ed.itemFilter;
            if (checks.Window != Edited)
            {
                checkedItems.Clear(ChangeSource.Program);
                checks.Window = Edited;
            }
            else checkedItems.Prune();

            UIObject r = w.Root;

            Layout top = KodeWindow.Row(r);
            KodeWindow.Button(top, "#MechJeb_WindowEd_button1", ed.AddNewWindow, true); //New window
            if (Edited != null)
            {
                UIButton delete = null;
                delete = KodeWindow.Button(top, "#MechJeb_WindowEd_button2", () => ConfirmDelete(delete), ButtonRole.Danger, true); //Delete window
            }

            // sharing: the edited window's configuration to the clipboard, or a new window from it
            Layout share = KodeWindow.Row(r);
            if (Edited != null)
                Tooltips.Add(KodeWindow.Button(share, "#MechJeb_NewUI_ExportWindow", ExportEdited, true).gameObject, "#MechJeb_NewUI_ExportWindow_tooltip"); //Export
            Tooltips.Add(KodeWindow.Button(share, "#MechJeb_NewUI_ImportWindow", ImportFromClipboard, true).gameObject, "#MechJeb_NewUI_ImportWindow_tooltip"); //Import
            // the edited window's read-outs as a Markdown table (also in that window's title menu)
            if (Edited != null)
                Tooltips.Add(KodeWindow.Button(share, "#MechJeb_NewUI_CopyValuesMarkdown", () => //Copy values as Markdown
                        // the values the window draws while it is open, else those something computes anyway
                        InfoItemMenu.CopyWindowMarkdown(Edited, Edited.Enabled ? Edited.DrawnValue : null), true).gameObject,
                    "#MechJeb_NewUI_CopyWindowMarkdown_tooltip");

            if (Edited != null) BuildEditedWindow(w, r);

            // every preset in a dropdown, and a button to create the chosen one
            KodeWindow.Header(r, "#MechJeb_WindowEd_label5"); //Window presets:
            var presetNames = new string[CustomWindowPresets.presets.Length];
            for (int i = 0; i < presetNames.Length; i++) presetNames[i] = CustomWindowPresets.presets[i].name;
            Layout pick = KodeWindow.Row(r);
            new ComboBox(w, pick, () => ed.presetIndex, i => ed.presetIndex = i, () => presetNames, name: "#MechJeb_WindowEd_label5"); //Window presets
            KodeWindow.Button(pick, "#MechJeb_NewUI_Create", CreateFromPreset, ButtonRole.Primary); //Create
        }

        /// <summary>"Delete window" asks first: the window and its items go, and nothing brings them back.</summary>
        void ConfirmDelete(UIButton from)
        {
            MechJebModuleCustomInfoWindow doomed = Edited;
            if (doomed == null) return;
            Dialogs.Confirm(from.gameObject, "#MechJeb_NewUI_DeleteWindowTitle", //Delete window?
                Localizer.Format("#MechJeb_NewUI_DeleteWindowMessage", doomed.GetName()), //The custom window "<<1>>" and its items will be deleted...
                "#MechJeb_NewUI_Delete", () => //Delete
                {
                    // the window asked about: the editor is rebuilt (and the dialog dismissed) when it edits another
                    if (Edited != doomed) return;
                    string title = doomed.GetName();
                    ed.RemoveCurrentWindow();
                    Toasts.Show(Localizer.Format("#MechJeb_NewUI_Toast_WindowDeleted", title), ToastSeverity.Info);
                });
        }

        void ExportEdited()
        {
            MuUtils.SystemClipboard = Edited.ToSharingString();
            ScreenMessages.PostScreenMessage(Localizer.Format("#MechJeb_WindowEd_CustomInfoWindow_Scrmsg1", Edited.GetName()), 3.0f,
                ScreenMessageStyle.UPPER_RIGHT); //Configuration of <<1>> window copied to clipboard.
        }

        void ImportFromClipboard() => Open(ed.CreateWindowFromSharingString(MuUtils.SystemClipboard));

        void CreateFromPreset() => Open(ed.CreateWindowFromSharingString(CustomWindowPresets.presets[ed.presetIndex].sharingString));

        void Open(MechJebModuleCustomInfoWindow created)
        {
            if (created == null) return;
            ed.editedWindow = created;
            Toasts.Show(Localizer.Format("#MechJeb_NewUI_Toast_WindowCreated", created.GetName()), ToastSeverity.Success);
        }

        /// <summary>The edited window's title, in a field; a change marks the window for saving.</summary>
        static void TitleField(KodeWindow w, UIObject parent, MechJebModuleCustomInfoWindow edited) =>
            EditFields.Field(w, parent, () => edited.title, s =>
            {
                s = MuUtils.SanitizeConfigText(s);
                if (edited.title == s) return;
                edited.title = s;
                edited.Dirty = true;
            }, 120, label: "#MechJeb_WindowEd_Edtitle", kind: FieldKind.Text); //Title

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

        void BuildEditedWindow(KodeWindow w, UIObject r)
        {
            MechJebModuleCustomInfoWindow edited = Edited;
            List<ComputerModule> all = AllWindows;

            // the edited window picked from a dropdown, its title in a field
            KodeWindow.Header(r, "#MechJeb_NewUI_Window"); //Window
            Layout pick = KodeWindow.Row(r);
            new ComboBox(w, pick, () => all.IndexOf(edited), i => ed.editedWindow = (MechJebModuleCustomInfoWindow)all[i], WindowTitles, name: "#MechJeb_NewUI_Window"); //Window
            TitleField(w, pick, edited);

            // where and how it shows
            KodeWindow.Header(r, "#MechJeb_NewUI_Display"); //Display

            Layout showIn = KodeWindow.Row(r);
            KodeWindow.Label(showIn, "#MechJeb_WindowEd_label1"); //Show in:
            w.Toggle(showIn, "#MechJeb_WindowEd_checkbox1", () => edited.ShowInFlight, v => edited.ShowInFlight = v); //Flight
            w.Toggle(showIn, "#MechJeb_WindowEd_checkbox2", () => edited.ShowInEditor, v => edited.ShowInEditor = v); //Editor

            Layout flags = KodeWindow.Row(r);
            w.Toggle(flags, "#MechJeb_WindowEd_checkbox3", () => edited.IsOverlay, v => edited.IsOverlay = v); //Overlay
            w.Toggle(flags, "#MechJeb_WindowEd_checkbox4", () => edited.WindowLocked, v => edited.WindowLocked = v); //Locked
            Tooltips.Add(w.Toggle(flags, "#MechJeb_WindowEd_checkbox8", () => edited.ClickThrough, v => edited.ClickThrough = v).gameObject, "#MechJeb_WindowEd_checkbox8_tooltip"); //Click through

            // Compact: off, the new UI's global setting, or on, whichever that one is
            Layout compact = KodeWindow.Row(r);
            KodeWindow.Label(compact, "#MechJeb_WindowEd_checkbox5"); //Compact
            string[] compactNames =
            {
                Localizer.Format("#MechJeb_NewUI_CompactOff"), Localizer.Format("#MechJeb_NewUI_CompactFollow"), Localizer.Format("#MechJeb_NewUI_CompactOn")
            };
            w.Segmented(compact, compactNames, () => (int)edited.Compact, v => edited.Compact = (MechJebModuleCustomInfoWindow.CompactMode)v);

            // Width: Auto (fits the values) or Fixed, set with [−][field][+] px (1 px a press, 10 with Right Shift or Left Alt) or the window's resize grip
            Layout width = KodeWindow.Row(r);
            Tooltips.Add(KodeWindow.Label(width, "#MechJeb_NewUI_Width", true).gameObject, "#MechJeb_NewUI_Width_tooltip"); //Width
            string auto = Localizer.Format("#MechJeb_NewUI_WidthAuto"), fixedName = Localizer.Format("#MechJeb_NewUI_WidthFixed");
            w.Switch(width, () => edited.WidthFixed, v => edited.WidthFixed = v);
            w.BoundLabel(width, auto.Length > fixedName.Length ? auto : fixedName, () => edited.WidthFixed ? fixedName : auto);
            void Nudge(int d)
            {
                edited.FixedWidth = (edited.FixedWidth > 0 ? edited.FixedWidth : 250) + d;
                edited.WidthFixed = true;
            }
            // four digits at most (MAX_FIXED_WIDTH)
            float digits = TextMeasure.Measure("0000", w.Behaviour.Theme).x + 10;
            // the typed text stays until it is a width in range ("3" on the way to "320"); a nudge or the grip replaces it
            string typed = null;
            int seen = edited.FixedWidth;
            string seenText = seen > 0 ? seen.ToString() : "";
            w.Stepper(width, () =>
            {
                if (edited.FixedWidth != seen)
                {
                    seen = edited.FixedWidth;
                    seenText = seen > 0 ? seen.ToString() : "";
                    typed = null;
                }
                return typed ?? seenText;
            }, s =>
            {
                typed = s;
                if (!int.TryParse(s, out int v) || v < MechJebModuleCustomInfoWindow.MIN_FIXED_WIDTH || v > MechJebModuleCustomInfoWindow.MAX_FIXED_WIDTH)
                    return;
                edited.FixedWidth = seen = v;
                edited.WidthFixed = true;
            }, d => Nudge((int)d), () => 1, digits, label: "#MechJeb_NewUI_Label_FixedWidth"); //Fixed width
            KodeWindow.Label(width, "px");

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

            // the window's items: click to select, drag to reorder (the buttons stay, for one place at a time and for the bridge)
            // the label, and the toggle column's "all" box at the end of its line: check rows to remove or move them together (a
            // click, no modifier key)
            Layout checkAll = KodeWindow.Row(r);
            KodeWindow.Label(checkAll, "#MechJeb_WindowEd_label3", true); //Window contents (click to edit):
            Tooltips.Add(KodeWindow.RowOf(SelectionColumn.AllBox(w, checkAll, checkedItems, "#MechJeb_NewUI_SelectAll")), "#MechJeb_NewUI_SelectItems_tooltip"); //All
            shownSelected = -1;
            shownHeaderText = null;
            contentsList = VirtualList.ListRows(w, r, 200, edited.items.Count, i => edited.items[i].ListName, i => ed.selectedItemIndex = i, BindContentsRow);
            itemsTarget = w.Target("ItemList", ItemCount, _ => { }, "#MechJeb_WindowEd_label3");
            itemsTarget.Control = contentsList.View;
            itemsTarget.State = new ItemsState(this);
            reorder = new ReorderModel(() => edited.items.Count, MoveItem);
            // the list's edits, for a host (the agent bridge): moves, and adds from the registry list (made below)
            itemsTarget.Model = listEdits = new ListEdits
            {
                Reorder = reorder, Count = () => edited.items.Count, Label = i => edited.items[i].ListName, Selected = () => ed.selectedItemIndex,
                Select = i => ed.selectedItemIndex = i
            };
            contentsDrag = ListDrag.ForReorder(w, contentsList, reorder, i => edited.items[i].ListName);
            ItemCards(contentsList, i => i >= 0 && i < edited.items.Count ? edited.items[i] : null);
            SelectionColumn.Add(w, contentsList, checkedItems, "#MechJeb_NewUI_SelectItem", //Select
                i => i < edited.items.Count ? edited.items[i].ListName : null, "#MechJeb_NewUI_SelectRow"); //Select <<1>>
            w.AddSync(SyncSelection);

            Layout edit = KodeWindow.Row(r);
            UIButton remove = KodeWindow.Button(edit, "#MechJeb_WindowEd_button3", RemoveSelected, true); //Remove
            UIButton up = KodeWindow.Button(edit, "#MechJeb_WindowEd_button4", () => MoveSelected(-1), true); //Move up
            UIButton down = KodeWindow.Button(edit, "#MechJeb_WindowEd_button5", () => MoveSelected(1), true); //Move down
            // with rows checked, the buttons act on them and say how many: "Remove (3)"
            int shownChecked = 0;
            w.AddSync(() =>
            {
                int n = checkedItems.Count;
                if (n == shownChecked) return;
                shownChecked = n;
                remove.Text(n > 0 ? Localizer.Format("#MechJeb_NewUI_RemoveChecked", n) : KodeWindow.Loc("#MechJeb_WindowEd_button3")); //Remove (<<1>>)
                up.Text(n > 0 ? Localizer.Format("#MechJeb_NewUI_MoveUpChecked", n) : KodeWindow.Loc("#MechJeb_WindowEd_button4")); //Move up (<<1>>)
                down.Text(n > 0 ? Localizer.Format("#MechJeb_NewUI_MoveDownChecked", n) : KodeWindow.Loc("#MechJeb_WindowEd_button5")); //Move down (<<1>>)
            });
            // the selected value on the clipboard (also in a value's context menu, a right-click in its window)
            UIButton copy = KodeWindow.Button(edit, "#MechJeb_NewUI_Menu_CopyValue", () => InfoItemMenu.CopySelected(ed), true); //Copy value
            var copyButton = copy.GetComponent<UnityEngine.UI.Button>();
            w.AddSync(() =>
            {
                int i = ed.selectedItemIndex;
                bool value = i >= 0 && i < edited.items.Count && edited.items[i] is ValueInfoItem;
                if (copyButton.interactable != value) copyButton.interactable = value;
            });

            // the selected header's text
            Layout header = KodeWindow.Row(r);
            KodeWindow.Label(header, "#MechJeb_WindowEd_HeaderText"); //Header text:
            EditFields.Field(w, header, () => ed.SelectedHeader()?.Text ?? "", s => ed.SetHeaderText(ed.SelectedHeader(), s), 120, kind: FieldKind.Text);
            w.Visible(header.gameObject, () => ed.SelectedHeader() != null);

            // the registry: pick a category ("All" first: every item, as a tree by category), click an item to add it
            KodeWindow.Label(r, "#MechJeb_WindowEd_label4"); //Click an item to add it to the info window:
            string[] categoryNames = new string[MechJebModuleCustomWindowEditor.categories.Length + 1];
            categoryNames[0] = KodeWindow.Loc("#MechJeb_NewUI_CategoryAll"); //All
            Array.Copy(MechJebModuleCustomWindowEditor.categories, 0, categoryNames, 1, categoryNames.Length - 1);
            new ComboBox(w, r, () => ed.itemCategoryAll ? 0 : (int)ed.itemCategory + 1, i =>
            {
                ed.itemCategoryAll = i == 0;
                if (i > 0) ed.itemCategory = (InfoItem.Category)(i - 1);
            }, () => categoryNames, name: "#MechJeb_NewUI_Label_Category"); //Category

            // a filter over the category's items: type "apo" and only the matching rows stay, the best match first
            Layout filterRow = KodeWindow.Row(r);
            KodeWindow.Label(filterRow, "#MechJeb_NewUI_Filter"); //Filter:
            EditFields.Field(w, filterRow, () => ed.itemFilter, s =>
            {
                ed.itemFilter = s ?? "";
                ApplyRegistryFilter();
            }, 150, kind: FieldKind.Text);

            // a click adds the item at the end; a drag drops it where the indicator shows in the window's list
            transfer = new TransferModel(() => registryList.Count, () => edited.items.Count, InsertItem);
            listEdits.Transfer = transfer;
            listEdits.SourceCount = () => registryList.Count;
            listEdits.SourceLabel = RegistryLabelAt;
            if (ed.itemCategoryAll) BuildRegistryTree(w, r, edited);
            else BuildRegistryList(w, r, edited);
            registryList.EmptyLabel("#MechJeb_NewUI_FilterNone"); //No item matches.
            registryDrag = ListDrag.ForTransfer(w, registryList, contentsList, transfer, RegistryLabelAt);
            ItemCards(registryList, row => row >= 0 && row < registryList.Count ? RegistryItemAt(row) : null);
        }

        /// <summary>
        /// A hover card on each row of an item list (rows made later get one too): the item's name, its live value when a shown
        /// window or a running module computes it ("not computed" otherwise), its description and tooltip, beside the window.
        /// </summary>
        void ItemCards(VirtualList<UIButton> list, Func<int, InfoItem> itemAt)
        {
            // the row whose card is open and the index it was built for: a scroll or a rebind that gives that row another index
            // closes the card (it would show the item the row showed before)
            GameObject cardRow = null;
            int cardIndex = -1;
            void Attach(GameObject row) =>
                Tooltips.Add(row, card =>
                {
                    cardRow = row;
                    cardIndex = list.IndexOfObject(row);
                    InfoItemCard.Build(card, itemAt(cardIndex), ed.Core);
                }, null, Tooltips.BesideWindow((RectTransform)row.transform));
            foreach (GameObject row in list.RowObjects) Attach(row);
            list.RowCreated += Attach;
            list.RowBound += (row, i) =>
            {
                if (row != cardRow || i == cardIndex) return;
                TooltipTrigger t = row.GetComponent<TooltipTrigger>();
                if (t != null && Tooltips.CardTrigger == t) Tooltips.Close(t);
                cardRow = null;
            };
        }

        // one category: a virtual list (only the rows in view are built) bound to the filter's results by rank
        void BuildRegistryList(KodeWindow w, UIObject r, MechJebModuleCustomInfoWindow edited)
        {
            registryTree = null;
            var labels = new List<string>();
            var keys = new List<string>();
            registryItems.Clear();
            foreach (InfoItem item in ed.ItemsOfCategory())
            {
                registryItems.Add(item);
                labels.Add(item.localizedDescription);
                keys.Add(FuzzyKeys.FromToken(item.name));
            }
            registryFilter = new FuzzyFilter(labels.ToArray(), keys.ToArray());
            registryFilter.Apply(ed.itemFilter);
            registryList = VirtualList.ListRows(w, r, 160, registryFilter.Results.Count, RegistryLabelAt,
                rank => transfer.Transfer(rank, edited.items.Count, KodeWindow.Pointer));
        }

        // "All": every category a branch (closed until clicked), its items the leaves, on a virtual list of the visible rows; the
        // filter opens the branches with a match and closes the others; a branch row is neither added nor dragged
        void BuildRegistryTree(KodeWindow w, UIObject r, MechJebModuleCustomInfoWindow edited)
        {
            registryFilter = null;
            registryItems.Clear();
            if (!(ed.itemTreeState is TreeState state)) ed.itemTreeState = state = new TreeState();
            var tree = new TreeCollection<InfoItem>(state);
            string[] names = MechJebModuleCustomWindowEditor.categories;
            for (int c = 0; c < names.Length; c++)
            {
                string branch = "category:" + names[c];
                tree.AddBranch(branch, names[c]);
                foreach (InfoItem item in ed.ItemsOfCategory((InfoItem.Category)c))
                {
                    // the item's id is its key (unique in the registry; a duplicate gets the node count)
                    string key = item.id ?? item.name ?? "";
                    if (tree.Contains(key)) key += "#" + tree.Count;
                    tree.Add(key, item.localizedDescription, item, branch, FuzzyKeys.FromToken(item.name));
                    registryItems.Add(item);
                }
            }
            tree.Find(ed.itemFilter);
            UpdateBranchLabels(tree);
            registryTree = new TreeList<InfoItem>(w, r, 160, tree, row => row.Node.IsBranch ? BranchLabel(row.Node) : row.Node.Label,
                (node, row) => transfer.Transfer(row, edited.items.Count, KodeWindow.Pointer));
            registryTree.Folded += () => registryDrag?.Cancel();
            registryList = registryTree.List;
            transfer.Allow = row => row < tree.VisibleRows.Count && !tree.VisibleRows[row].Node.IsBranch;
        }

        // a branch's label: its category and its item count (the matches during a search), made when the search changes
        readonly Dictionary<TreeNode<InfoItem>, string> branchLabels = new Dictionary<TreeNode<InfoItem>, string>();

        string BranchLabel(TreeNode<InfoItem> node) => branchLabels.TryGetValue(node, out string s) ? s : node.Label;

        void UpdateBranchLabels(TreeCollection<InfoItem> tree)
        {
            tree.UpdateCounts();
            branchLabels.Clear();
            foreach (TreeNode<InfoItem> b in tree.Roots) branchLabels[b] = $"{b.Label} ({b.MatchCount})";
        }

        /// <summary>The registry item at a row of its list: a filter result's rank, or a visible row of the "All" tree (null on a branch).</summary>
        InfoItem RegistryItemAt(int row)
        {
            if (registryTree == null) return registryItems[registryFilter.Results[row].Index];
            IReadOnlyList<TreeRow<InfoItem>> rows = registryTree.Tree.VisibleRows;
            return row >= 0 && row < rows.Count ? rows[row].Node.Value : null;
        }

        string RegistryLabelAt(int row)
        {
            if (registryTree == null) return registryItems[registryFilter.Results[row].Index].localizedDescription;
            IReadOnlyList<TreeRow<InfoItem>> rows = registryTree.Tree.VisibleRows;
            return row >= 0 && row < rows.Count ? rows[row].Node.Label : "";
        }

        // the registry's items, their filter (one category) or tree ("All"), and the list that shows its rows
        readonly List<InfoItem> registryItems = new List<InfoItem>();
        FuzzyFilter registryFilter;
        TreeList<InfoItem> registryTree;
        VirtualList<UIButton> registryList;

        /// <summary>The registry's virtual list (for tests and the agent bridge): its pool, its model and its rows.</summary>
        public VirtualList<UIButton> RegistryList => registryList;

        /// <summary>The "All" category's tree (null for one category): its model and its folds (for tests and the agent bridge).</summary>
        public TreeList<InfoItem> RegistryTree => registryTree;

        /// <summary>The registry's transfer into the edited window (for the agent bridge: <see cref="TransferModel.Transfer"/>).</summary>
        public TransferModel Transfer => transfer;

        /// <summary>The edited window's reorder (for the agent bridge: <see cref="ReorderModel.Move"/>, <see cref="ReorderModel.MoveBy"/>).</summary>
        public ReorderModel Reorder => reorder;

        /// <summary>A keystroke in the filter: rank the rows again (or search the tree), show the results from the top.</summary>
        void ApplyRegistryFilter()
        {
            if (registryTree != null)
            {
                if (string.Equals(registryTree.Tree.Query, ed.itemFilter ?? "", StringComparison.Ordinal)) return;
                registryDrag?.Cancel();
                registryTree.Tree.Find(ed.itemFilter);
                UpdateBranchLabels(registryTree.Tree);
                registryTree.Refresh(true);
                return;
            }
            if (registryFilter == null || !registryFilter.Apply(ed.itemFilter)) return;
            registryDrag?.Cancel();
            registryList.SetCount(registryFilter.Results.Count);
            registryList.ScrollTo(0, ScrollAlign.Start);
            registryList.Rebind();
        }
    }
}

using System;
using System.Collections.Generic;
using KodeKit;
using KodeKit.Headless;
using KSP.Localization;

namespace MuMech.UI
{
    /// <summary>
    /// A read-out's context menu in a custom info window (a right-click on a value's row): copy the value, show the item in the
    /// window editor's lists, add it to another custom window. Each action has a second path that needs no menu: the editor's Copy
    /// value button on the selected item, the window's Edit icon, the editor's item lists.
    /// </summary>
    public static class InfoItemMenu
    {
        /// <summary>
        /// A custom window's read-outs as Markdown: its title as a heading, then a two-column table (name, value). A value is the
        /// text the window shows now (a snapshot, read by <paramref name="shown"/>: the window's last refresh); from the editor
        /// (<paramref name="shown"/> null, the window maybe closed), only a
        /// value something computes anyway (<see cref="InfoItemCard.Computed"/>), read fresh, else "not computed". A header item is
        /// a bold row; items the scene doesn't show and items with no value (a table, buttons) are left out.
        /// </summary>
        public static string WindowMarkdown(MechJebModuleCustomInfoWindow window, Func<ValueInfoItem, string> shown, out int rows)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("### ");
            MarkdownTable.Cell(sb, window.GetName());
            sb.Append("\n\n");
            MarkdownTable.Header(sb, new[] { Localizer.Format("#MechJeb_NewUI_MarkdownName"), Localizer.Format("#MechJeb_NewUI_MarkdownValue") }, //Name / Value
                new[] { false, true });
            rows = 0;
            bool editor = HighLogic.LoadedSceneIsEditor;
            var cells = new string[2];
            foreach (InfoItem item in window.items)
            {
                if (!(editor ? item.showInEditor : item.showInFlight)) continue;
                if (item is HeaderInfoItem h)
                {
                    if (h.Text.Length == 0) continue; // a separator
                    cells[0] = "**" + h.Text.TrimEnd(':', ' ') + "**";
                    cells[1] = "";
                }
                else if (item is ValueInfoItem v)
                {
                    cells[0] = v.localizedName;
                    if (shown != null) cells[1] = shown(v) ?? v.StringValue;
                    else if (InfoItemCard.Computed(v, window.Core))
                    {
                        v.UpdateItem();
                        cells[1] = v.StringValue;
                    }
                    else cells[1] = Localizer.Format("#MechJeb_NewUI_Card_NotComputed");
                }
                else continue;
                MarkdownTable.Row(sb, cells);
                rows++;
            }
            return sb.ToString();
        }

        /// <summary>Puts <see cref="WindowMarkdown"/> on the clipboard and says so.</summary>
        public static void CopyWindowMarkdown(MechJebModuleCustomInfoWindow window, Func<ValueInfoItem, string> shown)
        {
            if (window == null) return;
            MuUtils.SystemClipboard = WindowMarkdown(window, shown, out int rows);
            Toasts.Show(Localizer.Format("#MechJeb_NewUI_Toast_RowsCopiedMarkdown", rows)); //<<1>> rows copied as Markdown
        }

        /// <summary>
        /// The entries for <paramref name="item"/> shown in <paramref name="window"/>, its shown text read by <paramref name="shown"/>;
        /// <paramref name="itemList"/> gives another window's item list as a commit target (its add is a commit with the pick's source).
        /// </summary>
        public static List<MenuEntry> Entries(MechJebModuleCustomInfoWindow window, ValueInfoItem item, Func<string> shown,
            Func<MechJebModuleCustomInfoWindow, CommitTarget<int>> itemList)
        {
            MechJebCore core = window.Core;
            var entries = new List<MenuEntry>
            {
                MenuEntry.Caption(item.localizedName),
                Named(MenuEntry.Item("#MechJeb_NewUI_Menu_CopyValue", s => Copy(item, shown())), "copy") //Copy value
            };
            MechJebModuleCustomWindowEditor editor = core.GetComputerModule<MechJebModuleCustomWindowEditor>();
            if (editor == null) return entries;
            if (!editor.TechLocked)
                entries.Add(Named(MenuEntry.Item("#MechJeb_NewUI_Menu_ShowSource", s => ShowInEditor(editor, window, item)), "source")); //Show in the window editor

            // the other custom windows, greyed when they hold the item already
            List<ComputerModule> windows = core.GetComputerModules<MechJebModuleCustomInfoWindow>();
            bool any = false;
            for (int i = 0; i < windows.Count; i++)
            {
                if (!(windows[i] is MechJebModuleCustomInfoWindow other) || other == window) continue;
                if (!any)
                {
                    entries.Add(MenuEntry.Separator());
                    entries.Add(MenuEntry.Caption("#MechJeb_NewUI_Menu_AddTo")); //Add to a custom window:
                    any = true;
                }
                // a window's title is the player's text, never a key
                string title = other.GetName();
                if (title.Length > 1 && title[0] == '#') title = " " + title;
                entries.Add(Named(MenuEntry.Item(title, s => AddTo(editor, other, item, itemList?.Invoke(other), s), () => !other.items.Contains(item)), "add:" + i));
            }
            return entries;
        }

        static MenuEntry Named(MenuEntry e, string name)
        {
            e.Name = name;
            return e;
        }

        /// <summary>Puts <paramref name="text"/> (the value as shown) on the clipboard, with a toast that says so.</summary>
        public static void Copy(ValueInfoItem item, string text)
        {
            if (text == null) return;
            text = text.Trim();
            MuUtils.SystemClipboard = text;
            Toasts.Show(Localizer.Format("#MechJeb_NewUI_Toast_ValueCopied", item.localizedName, text)); //Copied <<1>>: <<2>>
        }

        /// <summary>
        /// The editor's Copy value: the selected item's value, read only when something computes it anyway
        /// (<see cref="InfoItemCard.Computed"/>), else a toast that it is not computed.
        /// </summary>
        public static void CopySelected(MechJebModuleCustomWindowEditor editor)
        {
            MechJebModuleCustomInfoWindow w = editor.editedWindow;
            int i = editor.selectedItemIndex;
            if (w == null || i < 0 || i >= w.items.Count || !(w.items[i] is ValueInfoItem item)) return;
            if (!InfoItemCard.Computed(item, editor.Core))
            {
                Toasts.Show(Localizer.Format("#MechJeb_NewUI_Card_NotComputed"), ToastSeverity.Warning);
                return;
            }
            item.UpdateItem();
            Copy(item, item.StringValue);
        }

        /// <summary>
        /// Adds <paramref name="item"/> at the end of <paramref name="window"/> through the editor module's add, as one commit of the
        /// window's item list (<paramref name="list"/>) with <paramref name="source"/>, and a toast.
        /// </summary>
        public static void AddTo(MechJebModuleCustomWindowEditor editor, MechJebModuleCustomInfoWindow window, InfoItem item, CommitTarget<int> list,
            ChangeSource source)
        {
            if (window.items.Contains(item)) return;
            if (list != null) list.Apply(window.items.Count, _ => editor.AddItem(window, item), source);
            else editor.AddItem(window, item);
            Toasts.Show(Localizer.Format("#MechJeb_NewUI_Toast_ItemAdded", item.localizedName, window.GetName()), ToastSeverity.Success); //<<1>> added to <<2>>
        }

        /// <summary>
        /// Opens the window editor on <paramref name="window"/> with <paramref name="item"/> selected, and its item lists on the item's
        /// category, filtered to it: where the item comes from and what it is.
        /// </summary>
        public static void ShowInEditor(MechJebModuleCustomWindowEditor editor, MechJebModuleCustomInfoWindow window, InfoItem item)
        {
            editor.Enabled = true;
            editor.editedWindow = window;
            editor.selectedItemIndex = window.items.IndexOf(item);
            editor.itemCategoryAll = false;
            editor.itemCategory = item.category;
            editor.itemFilter = item.localizedDescription ?? "";
        }
    }
}

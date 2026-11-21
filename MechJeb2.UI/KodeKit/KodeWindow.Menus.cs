using System;
using System.Collections.Generic;
using KodeKit.Headless;
using KodeUI;
using UnityEngine;

namespace KodeKit
{
    public partial class KodeWindow
    {
        // ---- context menus ----

        /// <summary>
        /// The title bar's context menu: a right-click on the title lists the title bar's icons (each entry named by its tooltip,
        /// greyed while its icon is) and Close while the close button is shown. Every entry is an action the title bar already has;
        /// a title bar with no icon and no close button opens no menu. Disable it for a window whose
        /// title bar must offer nothing more (a dialog).
        /// </summary>
        public ContextMenuTrigger TitleMenu { get; private set; }

        readonly List<KeyValuePair<UIButton, string>> titleIcons = new List<KeyValuePair<UIButton, string>>();
        readonly List<MenuEntry> titleItems = new List<MenuEntry>();

        /// <summary>
        /// An entry of the title bar's menu that is no icon's (a custom info window's Copy as Markdown), after the icons and before
        /// Close. Its action must also be reachable elsewhere (a button in another window): a menu is never the only way.
        /// </summary>
        public void AddTitleMenuItem(MenuEntry entry)
        {
            if (entry != null) titleItems.Add(entry);
        }
        Action closeAction;

        void InitTitleMenu(Layout titleRow, Action close)
        {
            closeAction = close;
            TitleMenu = titleRow.gameObject.AddComponent<ContextMenuTrigger>();
            TitleMenu.Label = () => TitleText != null ? TitleText.tmpText.text : null;
            TitleMenu.Entries = TitleMenuEntries;
        }

        void AddTitleMenuEntry(UIButton icon, string tooltip)
        {
            if (!string.IsNullOrEmpty(tooltip)) titleIcons.Add(new KeyValuePair<UIButton, string>(icon, tooltip));
        }

        List<MenuEntry> TitleMenuEntries()
        {
            var entries = new List<MenuEntry>();
            for (int i = 0; i < titleIcons.Count; i++)
            {
                UIButton icon = titleIcons[i].Key;
                if (icon == null || !icon.gameObject.activeInHierarchy) continue;
                var button = icon.GetComponent<UnityEngine.UI.Button>();
                // the icon's own click, under the pick's source
                entries.Add(MenuEntry.Item(titleIcons[i].Value, s =>
                {
                    using (CommitPath.As(s)) button.onClick.Invoke();
                }, () => button != null && button.IsInteractable() && icon.gameObject.activeInHierarchy));
            }
            if (titleItems.Count > 0 && entries.Count > 0) entries.Add(MenuEntry.Separator());
            entries.AddRange(titleItems);
            // Close only while the title bar has its close button: a window without one (the MechJeb menu, a dialog) is not closed
            // this way, and with no entry no menu opens
            if (CloseButton != null && CloseButton.gameObject.activeSelf)
            {
                if (entries.Count > 0) entries.Add(MenuEntry.Separator());
                MenuEntry close = MenuEntry.Item("#MechJeb_NewUI_Menu_Close", s => closeAction()); //Close
                close.Name = "close";
                entries.Add(close);
            }
            return entries;
        }

        /// <summary>
        /// A context menu on <paramref name="target"/> (a read-out's row): a right-click opens the entries <paramref name="entries"/>
        /// builds, about <paramref name="label"/>. The target catches the pointer through a <see cref="RaycastArea"/> (no image, no draw
        /// call) when it has no graphic of its own: add menus only where they are wanted, since each raycast target is tested on every
        /// pointer move over its canvas. A window that lets the pointer through (a click-through or overlay custom window) has none.
        /// </summary>
        public static ContextMenuTrigger ContextMenu(GameObject target, Func<string> label, Func<List<MenuEntry>> entries)
        {
            // a graphic of its own (a label) catches the pointer through it; else a graphic that draws nothing
            if (target.GetComponent<UnityEngine.UI.Graphic>() == null) target.AddComponent<RaycastArea>();
            var t = target.GetComponent<ContextMenuTrigger>() ?? target.AddComponent<ContextMenuTrigger>();
            t.Label = label;
            t.Entries = entries;
            return t;
        }
    }
}

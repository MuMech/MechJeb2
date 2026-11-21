using KSP.UI;
using UnityEngine;

namespace KodeKit
{
    /// <summary>The windows' canvas, and nested canvases that must draw above every window (combo popups, tooltips).</summary>
    public static class Overlay
    {
        // The overlays' order above the windows' canvas, one table so that two overlays never share an order (two canvases of the
        // same order draw in the order they were made in, which is no decision): tooltips over dialogs over toasts over the command palette over context menus over popups
        // over the windows. Toasts are under the dialogs: a dialog's blocker dims them and takes their clicks, so a toast never takes
        // a click through a modal dialog, and a toast posted by a dialog's action (a window deleted) shows as the dialog goes.

        /// <summary>An open drop-down's list (above every window).</summary>
        public const int PopupOrder = 100;

        /// <summary>A context menu: above the drop-downs' lists, under the toasts (a menu opens only from a window).</summary>
        public const int MenuOrder = 101;

        /// <summary>The command palette and its blocker: above the context menus, under the toasts and the dialogs.</summary>
        public const int PaletteOrder = 102;

        /// <summary>The toasts: above the popups, under the dialogs.</summary>
        public const int ToastOrder = 103;

        /// <summary>A modal dialog and its blocker: above the popups and the toasts, under the tooltips.</summary>
        public const int DialogOrder = 105;

        /// <summary>The tooltip: above everything, a dialog's buttons included.</summary>
        public const int TooltipOrder = 110;

        /// <summary>
        /// The canvas the windows live on. Its scale is KSP's UI scale: appCanvas adds KSP's App scale, which is for panels
        /// attached to the toolbar, so most mods keep their free windows off it.
        /// </summary>
        public static Canvas Root => UIMasterController.Instance.dialogCanvas;

        /// <summary>
        /// overrideSorting alone is not enough: a new canvas starts on the Default sorting layer, which sorts below
        /// KSP's canvas layers whatever the order. Copy the root canvas's sorting layer and go above its order.
        /// </summary>
        public static void Setup(Canvas c, Canvas root, int above)
        {
            c.overrideSorting = true;
            c.sortingLayerID = root.sortingLayerID;
            c.sortingOrder = root.sortingOrder + above;
        }
    }
}

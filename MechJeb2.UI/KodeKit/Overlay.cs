using KSP.UI;
using UnityEngine;

namespace KodeKit
{
    /// <summary>The windows' canvas, and nested canvases that must draw above every window (combo popups, tooltips).</summary>
    public static class Overlay
    {
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

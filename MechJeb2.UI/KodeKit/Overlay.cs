using UnityEngine;

namespace KodeKit
{
    /// <summary>Nested canvases that must draw above every window (combo popups, tooltips).</summary>
    public static class Overlay
    {
        /// <summary>
        /// overrideSorting alone is not enough: a new canvas starts on the Default sorting layer, which sorts below
        /// KSP's appCanvas layer whatever the order. Copy the root canvas's sorting layer and go above its order.
        /// </summary>
        public static void Setup(Canvas c, Canvas root, int above)
        {
            c.overrideSorting = true;
            c.sortingLayerID = root.sortingLayerID;
            c.sortingOrder = root.sortingOrder + above;
        }
    }
}

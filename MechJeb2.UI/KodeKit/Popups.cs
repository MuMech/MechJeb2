using KodeKit.Headless;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KodeKit
{
    /// <summary>
    /// The popups' shared stack and geometry: every KodeKit popup (a drop-down's list, a dialog) opens a layer on <see cref="Layers"/>,
    /// which closes it on an outside press, its owner closing or a scene change (<see cref="PopupLayers"/>), and places itself with
    /// <see cref="PopupPlacer"/> through the conversions here. Escape stays the keyboard scope's: a popup's scope closes its layer.
    /// </summary>
    public static class Popups
    {
        /// <summary>The open popups, bottom first. Public for tests and the agent bridge.</summary>
        public static readonly PopupLayers Layers = new PopupLayers();

        static bool sceneHooked;

        /// <summary>Opens a layer for <paramref name="owner"/> on the shared stack (a scene change closes it).</summary>
        public static PopupLayer Push(object owner, bool modal = false, PopupLayer parent = null)
        {
            if (!sceneHooked)
            {
                sceneHooked = true;
                GameEvents.onGameSceneLoadRequested.Add(_ => Layers.SceneChange());
            }
            return Layers.Push(owner, parent, modal);
        }

        /// <summary>A rect's own box in its local units (y up): the screen a popup is placed on, when the rect is its parent.</summary>
        public static FrameBox Box(Rect r) => new FrameBox(r.xMin, r.yMax, r.xMax, r.yMin);

        static readonly Vector3[] corners = new Vector3[4];

        /// <summary><paramref name="rt"/>'s box in <paramref name="space"/>'s local units (its world corners, brought into that space).</summary>
        public static FrameBox BoxIn(RectTransform rt, RectTransform space)
        {
            rt.GetWorldCorners(corners); // 0 = bottom-left, 2 = top-right
            Vector3 bl = space.InverseTransformPoint(corners[0]);
            Vector3 tr = space.InverseTransformPoint(corners[2]);
            return new FrameBox(bl.x, tr.y, tr.x, bl.y);
        }

        /// <summary>Puts a top-left-pivoted popup at its placement's top-left corner.</summary>
        public static void Put(RectTransform popup, FrameBox box) => popup.localPosition = new Vector3(box.Left, box.Top, 0);

        /// <summary>
        /// On a popup's full-screen blocker: a click there is an outside press on the shared stack, at the click's point in the
        /// blocker's units (its parent's: the popup canvas fills the windows' canvas).
        /// </summary>
        public sealed class OutsidePressCatcher : MonoBehaviour, IPointerClickHandler
        {
            public void OnPointerClick(PointerEventData e)
            {
                var rt = (RectTransform)transform.parent;
                Vector2 local = Vector2.zero;
                // the canvas's own camera, not the event's: a click sent through the event system without a raycast has none
                Canvas canvas = GetComponentInParent<Canvas>();
                Canvas root = canvas != null ? canvas.rootCanvas : null;
                Camera cam = root != null && root.renderMode != RenderMode.ScreenSpaceOverlay ? root.worldCamera : null;
                if (rt != null) RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, cam, out local);
                Layers.OutsidePress(local.x, local.y);
            }
        }
    }
}

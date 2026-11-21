#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

namespace KodeKit.Headless
{
    /// <summary>
    /// A box on the canvas, in the canvas's local units with y up (a RectTransform's <c>rect</c> in its parent's space):
    /// <see cref="Top"/> is above <see cref="Bottom"/>.
    /// </summary>
    public struct FrameBox
    {
        public float Left, Top, Right, Bottom;

        public FrameBox(float left, float top, float right, float bottom)
        {
            Left   = left;
            Top    = top;
            Right  = right;
            Bottom = bottom;
        }

        public float Width  => Right - Left;
        public float Height => Top - Bottom;

        public override string ToString() => $"[{Left}, {Top}, {Right}, {Bottom}]";
    }

    /// <summary>
    /// Where a window is and how the player moves it, without Unity: the behaviour of KodeKit's <c>WindowBehaviour</c> and
    /// <c>DragHandle</c>. The host loads the window's place with <see cref="Load"/> (its top-left corner in the canvas's local
    /// units, y up, its unscaled size, the MechJeb UI scale and the canvas's box), calls the model, and puts the window at
    /// <see cref="X"/>, <see cref="Y"/>. Converting screen points to canvas units stays with the host (it needs the camera).
    /// <list type="bullet">
    /// <item>The clamp keeps <see cref="MinVisible"/> units of the window on the canvas on the left, right and bottom, and the
    /// title bar never above the top: the window's top edge stays between the canvas's top and its bottom +
    /// <see cref="MinVisible"/> (IMGUI's <c>DisplayModule</c> clamp).</item>
    /// <item>The window's size on the canvas is its own size times <see cref="Scale"/> (MechJeb's UI scale, applied about the
    /// top-left corner).</item>
    /// <item>A drag keeps the window where it was relative to the press point: the window is at its start plus the pointer's
    /// move from where the button went down (<see cref="BeginDrag"/>, <see cref="DragTo"/>), so the pixels the event system
    /// takes before it starts a drag count, and many small moves don't drift.</item>
    /// <item>A clamp during a drag moves that anchor: dragging past an edge and back moves the window back at once.</item>
    /// <item>Snapping (<see cref="SnapDistance"/> &gt; 0): during a drag, an edge of the window within that distance of a canvas
    /// edge or of another window's edge moves onto it, the nearest edge on each axis. Another window's edges count only
    /// while the two windows face each other on the other axis (their spans overlap, give or take the distance). The window
    /// follows the pointer's unsnapped place, so a snap never sticks: moving on past the distance lets go. The clamp
    /// applies after the snap.</item>
    /// <item>Only drags snap: <see cref="MoveBy"/>, a placement and a clamp don't.</item>
    /// <item>The saved position is in MechJeb's IMGUI units: screen pixels from the top-left of the screen divided by the UI
    /// scale (<see cref="ToImgui"/>, <see cref="FromImgui"/>), so <c>DisplayModule.WindowVector</c> keeps its meaning.</item>
    /// </list>
    /// </summary>
    public sealed class WindowFrameModel : IDescribeParts
    {
        /// <summary>What the window is called, for <see cref="IDescribeParts"/> (null: the factory gave none).</summary>
        public string Label { get; set; }

        /// <summary>The window (grabbed while its title bar is dragged).</summary>
        public void Describe(List<PartDescription> into) =>
            into.Add(new PartDescription { Name = "window", Role = PartRole.Window, Label = Label, Flags = Dragging ? PartFlags.Grabbed : PartFlags.None });

        /// <summary>Canvas units of the window that stay on the canvas, like DisplayModule's clamp.</summary>
        public float MinVisible = 20;

        /// <summary>Canvas units within which a dragged window's edge snaps to an edge; 0 or less turns snapping off.</summary>
        public float SnapDistance;

        /// <summary>The window's top-left corner in the canvas's local units (y up).</summary>
        public float X { get; private set; }

        public float Y { get; private set; }

        /// <summary>The window's own size, before <see cref="Scale"/>.</summary>
        public float Width { get; private set; }

        public float Height { get; private set; }

        /// <summary>MechJeb's UI scale, applied about the top-left corner.</summary>
        public float Scale { get; private set; } = 1;

        /// <summary>The canvas the window is clamped to.</summary>
        public FrameBox Canvas { get; private set; }

        /// <summary>True between <see cref="BeginDrag"/> and <see cref="EndDrag"/>.</summary>
        public bool Dragging { get; private set; }

        /// <summary>The window's box on the canvas.</summary>
        public FrameBox Box => new FrameBox(X, Y, X + Width * Scale, Y - Height * Scale);

        // during a drag: the window's unsnapped place minus the pointer's (moved by the clamp), and the pointer
        float anchorX, anchorY, pointerX, pointerY;

        // the other windows' boxes, taken at the start of a drag (they don't move during it)
        readonly List<FrameBox> others = new List<FrameBox>();

        /// <summary>The other windows a drag snaps to (read at <see cref="BeginDrag"/>).</summary>
        public int OtherCount => others.Count;

        /// <summary>Loads the window's place as the host has it.</summary>
        public void Load(float x, float y, float width, float height, float scale, FrameBox canvas)
        {
            X      = x;
            Y      = y;
            Width  = width;
            Height = height;
            Scale  = scale;
            Canvas = canvas;
        }

        /// <summary>The window's top-left corner kept in the clamp. True when it moved.</summary>
        public bool Clamp()
        {
            float x = ClampX(X), y = ClampY(Y);
            if (x == X && y == Y) return false;
            X = x;
            Y = y;
            return true;
        }

        /// <summary>Puts the top-left corner at (x, y), clamped.</summary>
        public void Place(float x, float y)
        {
            X = ClampX(x);
            Y = ClampY(y);
        }

        /// <summary>Moves the window by (dx, dy) canvas units, clamped and without snapping.</summary>
        public void MoveBy(float dx, float dy) => Place(X + dx, Y + dy);

        /// <summary>Forgets the other windows (before adding them for a drag).</summary>
        public void ClearOthers() => others.Clear();

        /// <summary>Another window a drag can snap to, its box on the same canvas.</summary>
        public void AddOther(FrameBox box) => others.Add(box);

        /// <summary>
        /// Starts a drag from the window's current place, the button pressed at (pressX, pressY) canvas units (load the window
        /// and add the other windows first).
        /// </summary>
        public void BeginDrag(float pressX = 0, float pressY = 0)
        {
            Place(X, Y);
            anchorX  = X - pressX;
            anchorY  = Y - pressY;
            pointerX = pressX;
            pointerY = pressY;
            Dragging = true;
        }

        /// <summary>
        /// The pointer is at (x, y) canvas units during a drag: the window follows, snapped and clamped. Outside a drag it
        /// does nothing.
        /// </summary>
        public void DragTo(float x, float y)
        {
            if (!Dragging) return;
            pointerX = x;
            pointerY = y;
            float rx = anchorX + x, ry = anchorY + y;
            float cx = ClampX(rx), cy = ClampY(ry);
            anchorX += cx - rx;
            anchorY += cy - ry;
            if (SnapDistance > 0) Snap(ref cx, ref cy);
            X = ClampX(cx);
            Y = ClampY(cy);
        }

        /// <summary>The pointer moved by (dx, dy) canvas units: <see cref="DragTo"/> during a drag, else a clamped move.</summary>
        public void DragBy(float dx, float dy)
        {
            if (Dragging) DragTo(pointerX + dx, pointerY + dy);
            else MoveBy(dx, dy);
        }

        /// <summary>Ends a drag; the window stays where it is.</summary>
        public void EndDrag() => Dragging = false;

        float ClampX(float x)
        {
            float w   = Width * Scale;
            float min = Canvas.Left - w + MinVisible;
            float max = Canvas.Right - MinVisible;
            return Clamp(x, min, max);
        }

        float ClampY(float y)
        {
            float min = Canvas.Bottom + MinVisible; // the top edge can't go below the bottom + MinVisible
            float max = Canvas.Top;                 // the title bar never above the top of the canvas
            return Clamp(y, min, max);
        }

        // Mathf.Clamp's order: the minimum first, so a canvas narrower than MinVisible gives the minimum
        static float Clamp(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        void Snap(ref float x, ref float y)
        {
            float w = Width * Scale, h = Height * Scale;
            float d = SnapDistance;
            float left = x, right = x + w, top = y, bottom = y - h;

            // the shift that puts an edge on another, the smallest within d on each axis
            float bestX = float.PositiveInfinity, bestY = float.PositiveInfinity;
            Consider(ref bestX, Canvas.Left - left, d);
            Consider(ref bestX, Canvas.Right - right, d);
            Consider(ref bestY, Canvas.Top - top, d);
            Consider(ref bestY, Canvas.Bottom - bottom, d);

            for (int i = 0; i < others.Count; i++)
            {
                FrameBox o = others[i];
                // side by side: their vertical spans overlap
                if (bottom <= o.Top + d && top >= o.Bottom - d)
                {
                    Consider(ref bestX, o.Right - left, d); // to its right
                    Consider(ref bestX, o.Left - right, d); // to its left
                    Consider(ref bestX, o.Left - left, d);  // left edges aligned
                    Consider(ref bestX, o.Right - right, d);
                }

                // one above the other: their horizontal spans overlap
                if (left <= o.Right + d && right >= o.Left - d)
                {
                    Consider(ref bestY, o.Bottom - top, d); // below it
                    Consider(ref bestY, o.Top - bottom, d); // above it
                    Consider(ref bestY, o.Top - top, d);    // top edges aligned
                    Consider(ref bestY, o.Bottom - bottom, d);
                }
            }

            if (!float.IsPositiveInfinity(bestX)) x += bestX;
            if (!float.IsPositiveInfinity(bestY)) y += bestY;
        }

        static void Consider(ref float best, float shift, float d)
        {
            float a = Math.Abs(shift);
            if (a <= d && a < Math.Abs(best)) best = shift;
        }

        // ---- the saved position ----

        /// <summary>
        /// A screen point (pixels from the screen's top-left, y down) in MechJeb's IMGUI units: divided by the UI scale.
        /// </summary>
        public static void ToImgui(float screenX, float screenY, float mjScale, out float imguiX, out float imguiY)
        {
            imguiX = screenX / mjScale;
            imguiY = screenY / mjScale;
        }

        /// <summary>A saved IMGUI position as a screen point (pixels from the screen's top-left, y down).</summary>
        public static void FromImgui(float imguiX, float imguiY, float mjScale, out float screenX, out float screenY)
        {
            screenX = imguiX * mjScale;
            screenY = imguiY * mjScale;
        }

        /// <summary>A screen point with y down (IMGUI) as one with y up (uGUI), on a screen <paramref name="screenHeight"/> high.</summary>
        public static float FlipY(float y, float screenHeight) => screenHeight - y;
    }

    /// <summary>
    /// A resize grip's drag, without Unity: the behaviour of MechJeb's <c>ResizeGrip</c>. It reports the size the window's
    /// right and bottom edges would give, in the window's own units.
    /// <list type="bullet">
    /// <item>The drag is measured from where the button went down, not where the drag began: the event system starts a drag
    /// only past its threshold, and those first pixels count.</item>
    /// <item>The grip keeps the pointer's offset from the window's corner, so grabbing it a few units inside the corner
    /// doesn't make the window jump.</item>
    /// <item>The size never goes below <see cref="MinWidth"/>, <see cref="MinHeight"/> (no minimum unless set: a pointer
    /// past the window's left or top edge gives a negative size); the window clamps it to its own limits.</item>
    /// <item>Points are from the window's top-left corner, x right and y down (<see cref="FromLocal"/> converts a
    /// RectTransform's local point).</item>
    /// </list>
    /// </summary>
    public sealed class ResizeModel : IDescribeParts
    {
        /// <summary>What the grip's window is called, for <see cref="IDescribeParts"/> (null: the factory gave none).</summary>
        public string Label { get; set; }

        /// <summary>The grip (a separator: it resizes the window; grabbed while dragged).</summary>
        public void Describe(List<PartDescription> into) =>
            into.Add(new PartDescription
            {
                Name = "grip", Role = PartRole.Separator, Label = string.IsNullOrEmpty(Label) ? null : "resize " + Label,
                Flags = Dragging ? PartFlags.Grabbed : PartFlags.None
            });

        public float MinWidth = float.NegativeInfinity, MinHeight = float.NegativeInfinity;

        /// <summary>True between <see cref="Begin"/> and <see cref="End"/>.</summary>
        public bool Dragging { get; private set; }

        public float Width  { get; private set; }
        public float Height { get; private set; }

        float offsetX, offsetY;

        /// <summary>
        /// The button went down at (pressX, pressY) on a window of the given size: the drag starts, at that size (clamped to
        /// the minimum).
        /// </summary>
        public void Begin(float pressX, float pressY, float width, float height)
        {
            offsetX  = width - pressX;
            offsetY  = height - pressY;
            Dragging = true;
            Set(width, height);
        }

        /// <summary>The pointer is at (x, y): the size it gives. False outside a drag.</summary>
        public bool DragTo(float x, float y)
        {
            if (!Dragging) return false;
            Set(x + offsetX, y + offsetY);
            return true;
        }

        /// <summary>Ends the drag. False when none was going on.</summary>
        public bool End()
        {
            if (!Dragging) return false;
            Dragging = false;
            return true;
        }

        void Set(float w, float h)
        {
            Width  = Math.Max(MinWidth, w);
            Height = Math.Max(MinHeight, h);
        }

        /// <summary>
        /// A point in a RectTransform's local space (y up, about its pivot) as one from its top-left corner, y down.
        /// </summary>
        public static void FromLocal(float localX, float localY, float pivotX, float pivotY, float width, float height, out float x, out float y)
        {
            x = localX + pivotX * width;
            y = (1 - pivotY) * height - localY;
        }
    }
}

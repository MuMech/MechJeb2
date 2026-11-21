using System;
using KodeUI;
using UnityEngine;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>Detaches a window's undo stack from the history when the window is destroyed (a rebuild makes a new window).</summary>
    public class UndoLifetime : MonoBehaviour
    {
        public Headless.UndoStack Stack;

        void OnDestroy() => Stack?.Detach();
    }

    public partial class KodeWindow
    {
        /// <summary>Every window's undo stack, ordered: an undo by source across windows (an agent or a test reverting its own changes).</summary>
        public static readonly Headless.UndoHistory History = new Headless.UndoHistory();

        /// <summary>
        /// This window's undo stack, fed by <see cref="Commits"/>. It belongs to this window instance: a rebuilt window (a language
        /// switch, a layout change) starts with an empty one, since its commit keys and controls are new.
        /// </summary>
        public Headless.UndoStack Undo { get; private set; }

        /// <summary>What the last undo or redo of this window did (for tests).</summary>
        public Headless.UndoResult LastUndo { get; private set; } = Headless.UndoResult.Nothing;

        void InitUndo(string title)
        {
            Undo = new Headless.UndoStack(Commits, 50, History, title);
            Undo.Failed += (e, x) =>
            {
                Debug.LogError($"[KodeKit] the undo or redo of {e} threw; the entry stays:");
                Debug.LogException(x);
            };
            Root.gameObject.AddComponent<UndoLifetime>().Stack = Undo;
        }

        /// <summary>Undoes (or redoes) this window's latest edit, of any source.</summary>
        public Headless.UndoResult UndoOrRedo(bool redo)
        {
            // the player's click on the icon (or an agent's, through a host's scope): the undo's commit names its actor
            using (Headless.CommitPath.As(Pointer))
                LastUndo = redo ? Undo.Redo() : Undo.Undo();
            return LastUndo;
        }

        /// <summary>
        /// Turns the window's undo on (<see cref="Headless.UndoStack.Recording"/>) and adds undo and redo icons to the title bar,
        /// greyed while there is nothing to undo or redo (or the stack's guard holds it). Set the stack's guard first, and mark
        /// not <see cref="Headless.CommitTarget.Undoable"/> any control whose setter engages a flight mode or an autopilot. Undo has
        /// no keys: with no field focused, Ctrl and Z are KSP's throttle keys, and an edit ends (Enter) before its mistake is seen.
        /// </summary>
        public void UndoButtons()
        {
            Undo.Recording = true;
            UndoButton(TitleIcon(UndoIcons.Undo, "#MechJeb_NewUI_Undo_tooltip", () => UndoOrRedo(false)), () => Undo.CanUndo()); //Undo this window's last change
            UndoButton(TitleIcon(UndoIcons.Redo, "#MechJeb_NewUI_Redo_tooltip", () => UndoOrRedo(true)), () => Undo.CanRedo()); //Redo the last undone change
        }

        /// <summary>The undo title icon (null until <see cref="UndoButtons"/>).</summary>
        public UIButton UndoIcon { get; private set; }

        /// <summary>The redo title icon (null until <see cref="UndoButtons"/>).</summary>
        public UIButton RedoIcon { get; private set; }

        void UndoButton(UIButton b, Func<bool> can)
        {
            if (UndoIcon == null) UndoIcon = b;
            else RedoIcon = b;
            var button = b.GetComponent<Button>();
            // a CanvasGroup dims the icon: the theme repaints the icon's colour, not the group's alpha
            var group = b.gameObject.AddComponent<CanvasGroup>();
            bool shown = can();
            button.interactable = shown;
            group.alpha = shown ? 1 : 0.35f;
            syncs.Add(() =>
            {
                bool c = can();
                if (c == shown) return;
                shown = c;
                button.interactable = c;
                group.alpha = c ? 1 : 0.35f;
            });
        }
    }

    /// <summary>The undo and redo arrows (KSP's fonts have no ↶ ↷), drawn once like KodeUI's sprites.</summary>
    public static class UndoIcons
    {
        static Sprite undo, redo;

        // Unity's == (not ??): a sprite destroyed with its scene's objects is made again
        /// <summary>An arrow curving up and back to the left.</summary>
        public static Sprite Undo => undo != null ? undo : undo = Make(false);

        /// <summary>An arrow curving up and on to the right.</summary>
        public static Sprite Redo => redo != null ? redo : redo = Make(true);

        static Sprite Make(bool mirror)
        {
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                var p = new Vector2((x + 0.5f) / n, (y + 0.5f) / n);
                if (mirror) p.x = 1 - p.x;
                px[y * n + x] = new Color(1, 1, 1, Mathf.Clamp01(0.5f - Distance(p) * n));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100);
        }

        // y up: an arc over the top from the right (-40°) round to the left (180°), and a head at its left end pointing down
        static float Distance(Vector2 p)
        {
            var c = new Vector2(0.54f, 0.44f);
            const float r = 0.26f, width = 0.12f;
            const float from = -40 * Mathf.Deg2Rad, to = Mathf.PI;
            Vector2 d = p - c;
            float a = Mathf.Atan2(d.y, d.x);
            float arc;
            if (a >= from && a <= to) arc = Mathf.Abs(d.magnitude - r) - width / 2;
            else
            {
                Vector2 end = c + new Vector2(Mathf.Cos(from), Mathf.Sin(from)) * r;
                arc = (p - end).magnitude - width / 2;
            }
            // the head: a triangle under the arc's left end
            Vector2 h1 = new Vector2(0.10f, 0.46f), h2 = new Vector2(0.46f, 0.46f), h3 = new Vector2(0.28f, 0.20f);
            float head = Mathf.Max(Edge(p, h1, h3), Mathf.Max(Edge(p, h3, h2), Edge(p, h2, h1)));
            return Mathf.Min(arc, head);
        }

        // the distance outside a counter-clockwise triangle's edge
        static float Edge(Vector2 p, Vector2 from, Vector2 to)
        {
            Vector2 e = (to - from).normalized;
            return Vector2.Dot(p - from, new Vector2(e.y, -e.x));
        }
    }
}

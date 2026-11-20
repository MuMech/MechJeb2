using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// uGUI's colour-tint transition without its tween. <c>Selectable.Transition.ColorTint</c> runs
    /// <c>Graphic.CrossFadeColor</c> on every state change, which builds a tween callback and starts a coroutine: about 1 KB of
    /// garbage per pointer event on every button, toggle and segment (FINDINGS rule 52). This sets the Selectable's transition
    /// to None and paints the same <see cref="ColorBlock"/> colours itself, at once, into the target graphic's canvas
    /// renderer, as the transition does at the end of its fade. The states follow Unity's order: disabled, pressed, selected,
    /// highlighted, normal. Pointer and selection events arrive here as they do at the Selectable; a change of
    /// <c>interactable</c> (code or a CanvasGroup) is caught by one shared check a frame over the live tints.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InstantTint : UIBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        Selectable selectable;
        ColorBlock colors;
        bool inside, down, selected, paintedInteractable;
        Color painted;

        static readonly List<InstantTint> live = new List<InstantTint>();
        static Watcher watcher;

        /// <summary>Tints <paramref name="s"/> with <paramref name="cb"/> instead of its colour-tint transition.</summary>
        public static void Use(Selectable s, ColorBlock cb)
        {
            s.colors = cb; // kept for whoever reads it
            s.transition = Selectable.Transition.None;
            InstantTint t = s.GetComponent<InstantTint>() ?? s.gameObject.AddComponent<InstantTint>();
            t.selectable = s;
            t.colors = cb;
            t.Paint(true);
        }

        /// <summary>Hands the Selectable back to its own transition (a skin that swaps sprites instead).</summary>
        public static void Off(Selectable s)
        {
            InstantTint t = s.GetComponent<InstantTint>();
            if (t == null) return;
            if (s.targetGraphic != null) s.targetGraphic.canvasRenderer.SetColor(Color.white);
            Destroy(t);
        }

        void Paint(bool force = false)
        {
            if (selectable == null) return;
            Graphic g = selectable.targetGraphic;
            if (g == null) return;
            paintedInteractable = selectable.IsInteractable();
            Color c = !paintedInteractable ? colors.disabledColor
                : down ? colors.pressedColor
                : selected ? colors.selectedColor
                : inside ? colors.highlightedColor
                : colors.normalColor;
            c *= colors.colorMultiplier;
            if (!force && c == painted) return;
            painted = c;
            g.canvasRenderer.SetColor(c);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            inside = down = false;
            live.Add(this);
            if (watcher == null)
            {
                var go = new GameObject("KodeKit InstantTint");
                DontDestroyOnLoad(go);
                watcher = go.AddComponent<Watcher>();
            }
            Paint(true);
        }

        protected override void OnDisable()
        {
            live.Remove(this);
            base.OnDisable();
        }

        public void OnPointerEnter(PointerEventData e)
        {
            inside = true;
            Paint();
        }

        public void OnPointerExit(PointerEventData e)
        {
            inside = false;
            Paint();
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            down = true;
            Paint();
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            down = false;
            Paint();
        }

        public void OnSelect(BaseEventData e)
        {
            selected = true;
            Paint();
        }

        public void OnDeselect(BaseEventData e)
        {
            selected = false;
            Paint();
        }

        /// <summary>Repaints the tints whose Selectable became (non-)interactable since their last paint: a field read each.</summary>
        sealed class Watcher : MonoBehaviour
        {
            void LateUpdate()
            {
                for (int i = 0; i < live.Count; i++)
                {
                    InstantTint t = live[i];
                    if (t.selectable != null && t.selectable.IsInteractable() != t.paintedInteractable) t.Paint();
                }
            }
        }
    }
}

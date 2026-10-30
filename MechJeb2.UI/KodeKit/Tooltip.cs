using System;
using KodeUI;
using KSP.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// Shows a tooltip after the pointer rests on this object for Tooltips.Delay seconds (GuiUtils uses 1 s). A press
    /// hides it (Tooltips.Update). Not an IPointerDownHandler: on a button's label that would make the label the pressed
    /// object, and the button would never get its click.
    /// </summary>
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Func<string> Text;

        public void OnPointerEnter(PointerEventData e) => Tooltips.Schedule(this, e.position);
        public void OnPointerExit(PointerEventData e) => Tooltips.Cancel(this);
        void OnDisable() => Tooltips.Cancel(this);
    }

    /// <summary>
    /// One tooltip panel for all windows, on its own overlay canvas (UI layer, above windows and combo popups),
    /// placed at the pointer and kept on screen. Text may be a '#key' (localized when shown).
    /// </summary>
    public class Tooltips : MonoBehaviour
    {
        public static float Delay = 1f;
        public static int MaxWidth = 300;

        static Tooltips instance;
        TooltipTrigger pending;
        float showAt;
        Vector2 pointer;
        RectTransform panel;
        LayoutElement width;
        TextMeshProUGUI label;
        Image background;

        public static TooltipTrigger Add(GameObject target, string textOrKey) => string.IsNullOrEmpty(textOrKey) ? null : Add(target, () => textOrKey);

        public static TooltipTrigger Add(GameObject target, Func<string> text)
        {
            var t = target.GetComponent<TooltipTrigger>() ?? target.AddComponent<TooltipTrigger>();
            t.Text = text;
            // the target must catch the pointer for enter/exit
            Graphic g = target.GetComponent<Graphic>();
            if (g != null) g.raycastTarget = true;
            return t;
        }

        public static bool Visible => instance != null && instance.panel != null && instance.panel.gameObject.activeSelf;
        public static string ShownText => Visible ? instance.label.text : null;
        public static RectTransform Panel => instance?.panel;

        internal static void Schedule(TooltipTrigger t, Vector2 screenPos)
        {
            Instance.pending = t;
            Instance.pointer = screenPos;
            Instance.showAt = Time.unscaledTime + Delay;
            Instance.Hide();
        }

        internal static void Cancel(TooltipTrigger t)
        {
            if (instance == null || instance.pending != t) return;
            instance.pending = null;
            instance.Hide();
        }

        static Tooltips Instance
        {
            get
            {
                if (instance != null) return instance;
                var go = new GameObject("KodeKit.Tooltips");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<Tooltips>();
                return instance;
            }
        }

        void Build()
        {
            Canvas app = UIMasterController.Instance.appCanvas;
            var root = new GameObject("TooltipCanvas", typeof(RectTransform)) { layer = app.gameObject.layer };
            var rt = (RectTransform)root.transform;
            rt.SetParent(app.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            Overlay.Setup(root.AddComponent<Canvas>(), app, 110); // above combo popups

            LayoutPanel p = UIKit.CreateUI<LayoutPanel>(rt, "Tooltip");
            p.Vertical().ControlChildSize(true, true).ChildForceExpand(false, false).Padding(6, 6, 3, 3);
            p.PreferredSizeFitter(true, true).Anchor(AnchorPresets.BottomLeft).Pivot(PivotPresets.TopLeft);
            p.Finish();
            panel = p.rectTransform;
            background = p.BackGround;
            background.raycastTarget = false;
            width = p.gameObject.AddComponent<LayoutElement>();

            UIText t = p.Add<UIText>();
            t.Finish();
            label = (TextMeshProUGUI)t.tmpText;
            label.raycastTarget = false;
            label.enableWordWrapping = true;
            p.gameObject.SetActive(false);
        }

        void Hide()
        {
            if (panel != null) panel.gameObject.SetActive(false);
        }

        void Update()
        {
            if (pending == null) return;
            // any press cancels the tooltip under the pointer, shown or still waiting
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                pending = null;
                Hide();
                return;
            }
            if (Visible || Time.unscaledTime < showAt) return;
            if (!pending.isActiveAndEnabled) { pending = null; return; }
            string text = KodeWindow.Loc(pending.Text?.Invoke());
            if (string.IsNullOrEmpty(text)) return;
            Show(text);
        }

        void Show(string text)
        {
            if (panel == null) Build();
            Theme th = Theme.Current ?? Theme.KodeDefault();
            th.Tooltip.ApplyImage(background);
            th.ApplyText(label);
            label.text = text;
            width.preferredWidth = Mathf.Min(MaxWidth, TextMeasure.Measure(text, th).x + 1);
            panel.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);

            // below-right of the pointer, kept on screen
            var parent = (RectTransform)panel.parent;
            Camera cam = UIMasterController.Instance.appCanvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, pointer + new Vector2(12, -16), cam, out Vector2 local);
            Rect r = parent.rect;
            Vector2 size = panel.rect.size;
            local.x = Mathf.Clamp(local.x, r.xMin, r.xMax - size.x);
            local.y = Mathf.Clamp(local.y, r.yMin + size.y, r.yMax);
            panel.localPosition = local;
        }
    }
}

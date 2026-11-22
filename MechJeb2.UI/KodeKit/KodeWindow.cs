using System;
using System.Collections.Generic;
using KodeUI;
using KSP.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// A short highlight after a click, so an action with no visible result still shows it happened. Disabled between
    /// flashes, so the hundreds of idle buttons of the open windows cost no Update call: the button's onClick starts it.
    /// </summary>
    public class ClickFlash : MonoBehaviour
    {
        public Image Target;
        public static Color FlashColor = new Color(0.55f, 1f, 0.55f, 1f);
        const float Duration = 0.35f;
        float start = -1;
        Color baseColor, written;

        /// <summary>Adds a flash to the button, started by its onClick, so a click of a disabled button shows none.</summary>
        public static ClickFlash Add(Button button, Image target)
        {
            var flash = button.gameObject.AddComponent<ClickFlash>();
            flash.Target = target;
            flash.enabled = false;
            // a view may destroy the flash (segmented bars, chips): the listener stays on the button
            button.onClick.AddListener(() => { if (flash != null) flash.Flash(); });
            return flash;
        }

        public void Flash()
        {
            if (Target == null || !gameObject.activeInHierarchy) return;
            if (start < 0) written = baseColor = Target.color;
            start = Time.unscaledTime;
            enabled = true;
        }

        void Update()
        {
            if (start < 0)
            {
                enabled = false;
                return;
            }
            // repainted since the last frame (a selection turning green, a role change): fade back to the new colour
            if (Target.color != written) baseColor = Target.color;
            float t = (Time.unscaledTime - start) / Duration;
            if (t >= 1)
            {
                Target.color = baseColor;
                start = -1;
                enabled = false;
                return;
            }
            Target.color = written = Color.Lerp(FlashColor, baseColor, t * t);
        }

        void OnDisable()
        {
            if (start < 0 || Target == null) return;
            Target.color = baseColor;
            start = -1;
        }
    }

    /// <summary>
    /// A window's root. Not KodeUI's Window: that adds a Titlebar which moves the window itself, bypassing KodeKit's clamp,
    /// window lock and position save. KodeKit windows have their own title row and drag through DragHandle only.
    /// </summary>
    public class KodeRootWindow : LayoutPanel
    {
    }

    /// <summary>Calls Click when the object is clicked (labels that act like buttons).</summary>
    public class ClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public Action Click;
        public void OnPointerClick(PointerEventData e) => Click?.Invoke();
    }

    /// <summary>
    /// Hold-to-repeat behaviour for GUILayout.RepeatButton parity: fires on the press, then every 0.2 s while held. The
    /// first action runs from the press itself, so a press and release in the same frame (a tap, a low frame rate) still
    /// counts. Real time, so it repeats at the same rate in time warp.
    /// </summary>
    public class RepeatTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action Action;

        /// <summary>When set, a hold is one commit on this target (its first action and every repeat).</summary>
        public Headless.CommitTarget Group;
        const float Interval = 0.2f;
        bool held;
        float next;
        // the press's source (Pointer, or a host's scope around the press): the repeats keep it, so a hold stays one commit
        Headless.ChangeSource source;
        Selectable button;

        void Awake() => button = GetComponent<Selectable>();

        public void OnPointerDown(PointerEventData e)
        {
            // like Unity's Button: the left button only, and nothing while the button is disabled (a right drag turns the camera)
            if (e.button != PointerEventData.InputButton.Left) return;
            if (button != null && !button.IsInteractable()) return;
            Release();
            held = true;
            source = KodeWindow.Pointer;
            Group?.BeginGroup();
            next = Time.unscaledTime + Interval;
            Action?.Invoke();
        }

        // another button's release (a right click during the hold) leaves the left button's repeat running
        public void OnPointerUp(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left) Release();
        }
        public void OnPointerExit(PointerEventData e) => Release();
        void OnDisable() => Release();

        void Release()
        {
            if (!held) return;
            held = false;
            Group?.EndGroup();
        }

        void Update()
        {
            if (!held || Time.unscaledTime < next) return;
            if (button != null && !button.IsInteractable())
            {
                Release();
                return;
            }
            next = Time.unscaledTime + Interval;
            using (Headless.CommitPath.As(source))
                Action?.Invoke();
        }
    }

    /// <summary>
    /// Makes a press-and-drag on its object one gesture of <see cref="Slider"/> (or one group of <see cref="Target"/>): one commit. It
    /// begins on the press, taking in the change the press itself made (uGUI's slider moves before this hears the press), and ends on
    /// the release or when hidden.
    /// </summary>
    public class CommitGroupHandle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>The slider's model, whose drag the press begins and the release ends.</summary>
        public Headless.SliderModel Slider;

        /// <summary>For a control with no model of its own (the colour picker's area): the target whose group the gesture holds.</summary>
        public Headless.CommitTarget Target;
        bool held;

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            if (Slider != null)
            {
                Slider.EndDrag();
                Slider.BeginDrag();
                return;
            }
            if (Target == null) return;
            Release();
            held = true;
            Target.BeginGroup(true);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left) Release();
        }

        void OnDisable() => Release();

        void Release()
        {
            Slider?.EndDrag();
            if (!held) return;
            held = false;
            Target?.EndGroup();
        }
    }

    /// <summary>
    /// A retained window plus the bindings that push model changes into it. MechJeb-style widgets
    /// (toggled text box, numeric field, arrow selector, selection grid) are built on KodeUI here,
    /// because KodeUI has none of them. Every widget follows Theme.Current and re-localizes on a
    /// language switch; text starting with '#' is a localization key.
    /// </summary>
    public partial class KodeWindow
    {
        public KodeRootWindow Root;
        readonly List<Action> syncs = new List<Action>();

        static Theme T => Theme.Current ?? SetDefaultTheme();

        /// <summary>The theme now (Theme.Current, KodeKit's default until one is set): the theme of a widget outside a window.</summary>
        internal static Theme ThemeNow => T;

        static Theme SetDefaultTheme()
        {
            Theme.Set(Theme.KodeDefault());
            return Theme.Current;
        }

        /// <summary>Register a per-frame update for a custom widget.</summary>
        public void AddSync(Action sync) => syncs.Add(sync);

        /// <summary>Called after the user changes a value through a widget, so the owner can persist it.</summary>
        public Action Edited;

        /// <summary>
        /// The path every widget edit of this window takes (<see cref="Headless.CommitPath"/>): the widget's setter, then
        /// <see cref="Edited"/>, then a commit with its source for the observers (<see cref="AnyCommit"/>, an undo stack, the agent
        /// bridge). Each widget that takes a setter is one target on it.
        /// </summary>
        public Headless.CommitPath Commits { get; }

        // AnyCommit's observers, replaced on each change so one may unsubscribe while being called
        static Action<KodeWindow, Headless.Commit>[] anyCommit = new Action<KodeWindow, Headless.Commit>[0];

        /// <summary>
        /// Every commit of every KodeKit window, with its window: one subscription for an observer of the whole UI. An observer
        /// that throws is logged and the others are still called. It is static: an observer unsubscribes when it goes (in its
        /// OnDestroy), and copies what it keeps, since a kept commit or window keeps the window's controls alive.
        /// </summary>
        public static event Action<KodeWindow, Headless.Commit> AnyCommit
        {
            add
            {
                if (value == null) return;
                var a = new Action<KodeWindow, Headless.Commit>[anyCommit.Length + 1];
                Array.Copy(anyCommit, a, anyCommit.Length);
                a[anyCommit.Length] = value;
                anyCommit = a;
            }
            remove
            {
                int i = Array.LastIndexOf(anyCommit, value);
                if (i < 0) return;
                var a = new Action<KodeWindow, Headless.Commit>[anyCommit.Length - 1];
                Array.Copy(anyCommit, a, i);
                Array.Copy(anyCommit, i + 1, a, i, anyCommit.Length - i - 1);
                anyCommit = a;
            }
        }

        // AnySettled's observers, as AnyCommit's
        static Action<KodeWindow, Headless.Commit>[] anySettled = new Action<KodeWindow, Headless.Commit>[0];

        /// <summary>
        /// Every settled commit of every KodeKit window (<see cref="Headless.CommitPath.Settled"/>), with its window: what an observer of
        /// the whole UI keeps (the agent bridge's change feed). The same rules as <see cref="AnyCommit"/>.
        /// </summary>
        public static event Action<KodeWindow, Headless.Commit> AnySettled
        {
            add
            {
                if (value == null) return;
                var a = new Action<KodeWindow, Headless.Commit>[anySettled.Length + 1];
                Array.Copy(anySettled, a, anySettled.Length);
                a[anySettled.Length] = value;
                anySettled = a;
            }
            remove
            {
                int i = Array.LastIndexOf(anySettled, value);
                if (i < 0) return;
                var a = new Action<KodeWindow, Headless.Commit>[anySettled.Length - 1];
                Array.Copy(anySettled, a, i);
                Array.Copy(anySettled, i + 1, a, i, anySettled.Length - i - 1);
                anySettled = a;
            }
        }

        void ReportAnySettled(Headless.Commit c)
        {
            Action<KodeWindow, Headless.Commit>[] called = anySettled;
            for (int i = 0; i < called.Length; i++)
            {
                try
                {
                    called[i](this, c);
                }
                catch (Exception e)
                {
                    ObserverFailed(c, e);
                }
            }
        }

        static int serials;

        /// <summary>This window's number among the windows made since the game started (1 for the first): an agent's name for it.</summary>
        public int Serial { get; } = ++serials;

        /// <summary>
        /// The window's name in every language, for the agent bridge (its "module", which a step's "within" matches whole,
        /// case-insensitive): a display module's type name ("MechJebModuleManeuverPlanner"), or "CustomInfoWindow:" and a custom
        /// window's saved id. Null: the window has none (it is named by its title or its ref only).
        /// </summary>
        public string ModuleName { get; set; }

        void ReportAnyCommit(Headless.Commit c)
        {
            Action<KodeWindow, Headless.Commit>[] called = anyCommit;
            for (int i = 0; i < called.Length; i++)
            {
                try
                {
                    called[i](this, c);
                }
                catch (Exception e)
                {
                    ObserverFailed(c, e);
                }
            }
        }

        // an observer of a commit threw: the setter has run and the widget goes on, so the failure is only logged
        static void ObserverFailed(Headless.Commit c, Exception e)
        {
            Debug.LogError($"[KodeKit] an observer of the commit {c} threw (the change was made, the other observers still ran):");
            Debug.LogException(e);
        }

        /// <summary>A widget's setter as a target on <see cref="Commits"/>. <paramref name="persist"/>: its edits call <see cref="Edited"/>.</summary>
        internal Headless.CommitTarget<T> Target<T>(string kind, Func<T> get, Action<T> set, string label = null, bool persist = true) =>
            Commits.Target(kind, get, set, label, null, persist);

        /// <summary>
        /// Removes the commit targets whose controls are under <paramref name="root"/> (a row about to be destroyed, a list rebuilt in
        /// place): the window's path keeps no closure of a destroyed control, and its undo stack drops their entries.
        /// </summary>
        public void ForgetTargets(Transform root)
        {
            if (root == null) return;
            IReadOnlyList<Headless.CommitTarget> all = Commits.Targets;
            for (int i = all.Count - 1; i >= 0; i--)
                if (all[i].Control is Component c && c != null && c.transform.IsChildOf(root)) Commits.Remove(all[i]);
        }

        /// <summary>The source a widget's own handler reports: <paramref name="own"/>, or the one a host imposes (<see cref="Headless.CommitPath.As"/>).</summary>
        internal static Headless.ChangeSource Source(Headless.ChangeSource own) => Headless.CommitPath.Resolve(own);

        /// <summary>A click's source: Pointer, or the one a host imposes.</summary>
        internal static Headless.ChangeSource Pointer => Headless.CommitPath.Resolve(Headless.ChangeSource.Pointer);

        HashSet<Action> failed; // the syncs that threw, so each is logged once

        /// <summary>
        /// Runs the per-frame updates. One that throws is logged the first time and the others still run, so a broken
        /// readout does not freeze the rest of the window. It is still called every frame: it recovers when its cause
        /// goes (a target that comes back).
        /// </summary>
        public void Sync()
        {
            for (int i = 0; i < syncs.Count; i++)
            {
                try
                {
                    syncs[i]();
                }
                catch (Exception e)
                {
                    if (failed == null) failed = new HashSet<Action>();
                    if (!failed.Add(syncs[i])) continue;
                    Debug.LogError($"[KodeKit] an update of window \"{(TitleText != null ? TitleText.tmpText.text : Root.name)}\" threw (logged once, the others still run):");
                    Debug.LogException(e);
                }
            }
        }

        public KodeWindow(RectTransform canvas, string title, float minWidth, Vector2 pos)
        {
            Root = UIKit.CreateUI<KodeRootWindow>(canvas, title);
            Commits = new Headless.CommitPath(title, () => Time.frameCount) { Edited = () => Edited?.Invoke(), Describer = describeTarget, ShowLabel = showLabel };
            Commits.ObserverFailed += ObserverFailed;
            Root.Vertical().ControlChildSize(true, true).ChildForceExpand(false, false);
            Root.PreferredSizeFitter(true, true).Anchor(AnchorPresets.TopLeft).Pivot(PivotPresets.TopLeft);
            Root.Finish();
            // the undo stack first: an AnyCommit observer that reads the history sees the commit recorded
            InitUndo(title);
            Commits.Committed += ReportAnyCommit;
            Commits.Settled += ReportAnySettled;
            Root.gameObject.AddComponent<LayoutElement>().minWidth = minWidth;
            Root.rectTransform.anchoredPosition = new Vector2(pos.x, -pos.y);

            Behaviour = Root.gameObject.AddComponent<WindowBehaviour>();
            Behaviour.Window = this;
            Behaviour.Frame.Label = Loc(title);
            Behaviour.AddRelocalize(Relabel);
            KodeRootWindow root = Root;
            Styled(Root.gameObject, t =>
            {
                t.Window.ApplyImage(root.BackGround);
                root.Padding(t.Padding, t.Padding, 2, t.Padding).Spacing(t.Spacing);
            });

            Layout titleRow = Row(Root);
            TitleRow = titleRow;
            titleRow.gameObject.AddComponent<DragHandle>().Target = Behaviour;
            // the rest of the window drags too (GUI.DragWindow), unless MechJeb's "titlebar only" setting is on
            BodyDrag = Root.gameObject.AddComponent<DragHandle>();
            BodyDrag.Target = Behaviour;
            BodyDrag.Enabled = () => !TitleBarOnly();
            UIText titleText = Label(titleRow, title, true);
            // the title on the left, where the eye starts, and the title-bar buttons as icons
            titleText.tmpText.alignment = TextAlignmentOptions.Left;
            titleText.tmpText.raycastTarget = true; // so the title bar catches drags
            TitleText = titleText;
            Action close = () => (OnClose ?? (() => Root.gameObject.SetActive(false)))();
            CloseButton = IconButton(titleRow, Sprites.Cross, close);
            NameButton(CloseButton, "Close");
            FlatIcon(CloseButton, true);
            // a right-click on the title: the title bar's actions in a menu (its icons' and Close)
            InitTitleMenu(titleRow, close);
            // made on screen: an observer hears it opened (its OnEnable ran before the window was bound to it)
            Behaviour.ReportShown();
        }

        /// <summary>The title label (a window can show something other than its module's name, e.g. the menu's version).</summary>
        public UIText TitleText { get; }

        /// <summary>The title bar's close button.</summary>
        public UIButton CloseButton { get; }

        public WindowBehaviour Behaviour { get; }

        /// <summary>What the close button does; hides the window when null.</summary>
        public Action OnClose;

        /// <summary>The drag handle on the window body (dragging anywhere but a control moves the window).</summary>
        public DragHandle BodyDrag { get; }

        /// <summary>True when only the title bar drags windows (MechJeb's "Use only the titlebar for window dragging").</summary>
        public static Func<bool> TitleBarOnly = () => false;

        /// <summary>The title bar (title text and close button); also the drag handle.</summary>
        public Layout TitleRow { get; }

        /// <summary>
        /// A small flat button in the title bar, before the close button, like the title-bar icons.
        /// </summary>
        public UIButton TitleButton(string text, Action onClick)
        {
            UIButton b = Button(TitleRow, text, onClick);
            FlatIcon(b, false);
            b.transform.SetSiblingIndex(CloseButton.transform.GetSiblingIndex());
            return b;
        }

        /// <summary>
        /// GUILayout.HorizontalSlider: a flexible-width slider bound to a value. <paramref name="label"/> names it (a '#key' or a text; null:
        /// the label right before it in its row, <see cref="LabelBefore"/>).
        /// </summary>
        public Slider Slider(UIObject parent, Func<float> get, Action<float> set, float min = 0, float max = 1, string label = null)
        {
            label = label ?? LabelBefore(parent);
            GameObject go = DefaultControls.CreateSlider(new DefaultControls.Resources());
            go.transform.SetParent(parent.transform, false);
            // DefaultControls builds on layer 0, which UIMainCamera culls (FINDINGS rule 9)
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = parent.gameObject.layer;
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.minWidth = 40;
            var slider = NoSelect(go.GetComponent<Slider>());
            slider.minValue = min;
            slider.maxValue = max;
            // uGUI's navigation would move a selected slider with the arrow keys, which are KSP's camera keys: the slider's keys are its
            // model's, for a host that routes them through a keyboard scope
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            Headless.CommitTarget<float> target = Target("Slider", get, set);
            target.Control = slider;
            // the behaviour: a drag (one commit, with the click on the track that may start it: uGUI's slider moves before the handle
            // hears the press) and an agent's moves both go through the model
            var model = new Headless.SliderModel(get, target.Set, min, max, 0, () => target.BeginGroup(true), target.EndGroup);
            target.Model = model;
            LabelTarget(target, label);
            slider.SetValueWithoutNotify(model.Shown);
            void Show()
            {
                if (!Mathf.Approximately(slider.value, model.Shown)) slider.SetValueWithoutNotify(model.Shown);
            }
            slider.onValueChanged.AddListener(v =>
            {
                // drawn again after every set: a refused set, or a setter that keeps the old value (the model's Shown doesn't change
                // then, so no ShownChanged), puts back the control uGUI moved
                model.Set(v, Pointer);
                Show();
            });
            model.Value.ShownChanged += Show;
            go.AddComponent<CommitGroupHandle>().Slider = model;
            // thin track with a round handle: DefaultControls' 10 px bar and 20 px square handle are too heavy
            Image[] images = go.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                var rt = img.rectTransform;
                switch (img.gameObject.name)
                {
                    case "Background":
                        rt.anchorMin = new Vector2(0, 0.5f);
                        rt.anchorMax = new Vector2(1, 0.5f);
                        rt.sizeDelta = new Vector2(0, 3);
                        break;
                    case "Fill":
                        rt.parent.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f);
                        rt.parent.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.5f);
                        rt.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(-12, 3);
                        rt.sizeDelta = new Vector2(6, 0);
                        break;
                    case "Handle":
                        img.sprite = Sprites.Circle;
                        break;
                }
                if (img.gameObject.name != "Handle") img.sprite = null;
            }
            Styled(go, th =>
            {
                le.preferredHeight = th.FontSize;
                foreach (Image img in images)
                {
                    if (img.gameObject.name == "Handle")
                    {
                        img.color = th.Text;
                        img.rectTransform.sizeDelta = new Vector2(th.FontSize - 2, 0);
                        img.rectTransform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(-(th.FontSize - 2), 0);
                    }
                    else img.color = img.gameObject.name == "Fill" ? th.Good * 0.8f : new Color(1, 1, 1, 0.25f);
                }
            });
            syncs.Add(() => model.Sync());
            return slider;
        }

        // ---- panels and lists ----

        /// <summary>A vertical group that fills the width (GUILayout.BeginVertical): rows stacked, each as wide as the panel.</summary>
        public static Layout Panel(UIObject parent)
        {
            Layout l = parent.Add<Layout>("Panel");
            l.Vertical().ControlChildSize(true, true).ChildForceExpand(true, false);
            l.FlexibleLayout(true, false);
            l.Finish();
            return l;
        }

        /// <summary>
        /// A vertical scroll list of a fixed height (GUILayout.BeginScrollView) on the dark list backdrop, with a scrollbar
        /// shown when needed. Rows go in <paramref name="content"/>; text changes inside relayout only the list.
        /// </summary>
        public static ScrollView ScrollList(UIObject parent, float height, out Layout content, int spacing = 0)
        {
            ScrollView sv = parent.Add<ScrollView>("Scroll");
            sv.PreferredHeight(height);
            sv.FlexibleLayout(true, false);
            sv.Horizontal(false);
            sv.Vertical(true);

            sv.BackgroundColor(ListBackground);
            ScrollRect sr = sv.ScrollRect;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 25;
            sr.inertia = false;

            content = sv.Content;
            content.Vertical().ControlChildSize(true, true).ChildForceExpand(true, false).Spacing(spacing);
            content.PreferredSizeFitter(true, false);
            RectTransform c = content.rectTransform;
            c.anchorMin = new Vector2(0, 1);
            c.anchorMax = new Vector2(1, 1);
            c.pivot = new Vector2(0.5f, 1);
            c.sizeDelta = Vector2.zero;
            c.anchoredPosition = Vector2.zero;

            // the viewport (which masks the rows) leaves room for the scrollbar on the right
            sv.Viewport.rectTransform.offsetMin = Vector2.zero;
            sv.Viewport.rectTransform.offsetMax = new Vector2(-12, 0);

            UIScrollbar bar = sv.Add<UIScrollbar>("Bar");
            bar.Direction(Scrollbar.Direction.BottomToTop);
            RectTransform br = bar.rectTransform;
            br.anchorMin = new Vector2(1, 0);
            br.anchorMax = new Vector2(1, 1);
            br.pivot = new Vector2(1, 1);
            br.sizeDelta = new Vector2(10, 0);
            br.anchoredPosition = Vector2.zero;
            sv.VerticalScrollbar = bar;
            NoSelect(bar.GetComponent<Scrollbar>());
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            Image barImage = bar.GetComponent<Image>();
            Image handleImage = bar.scrollbar.handleRect.GetComponent<Image>();
            Styled(bar.gameObject, th =>
            {
                barImage.color = new Color(0, 0, 0, 0.35f);
                handleImage.color = new Color(th.Text.r, th.Text.g, th.Text.b, 0.6f);
            });
            return sv;
        }

        // ---- lists ----

        /// <summary>A dark backdrop for a vertical list of rows (ListRow), like IMGUI's scroll views.</summary>
        public static Layout ListBox(UIObject parent)
        {
            LayoutPanel box = parent.Add<LayoutPanel>("List");
            box.Vertical().ControlChildSize(true, true).ChildForceExpand(true, false).Spacing(0).Padding(0);
            box.FlexibleLayout(true, false);
            box.Finish();
            box.BackGround.sprite = null;
            box.BackGround.color = ListBackground;
            box.BackGround.enabled = true;
            return box;
        }

        public static Color ListBackground = new Color(0.07f, 0.08f, 0.09f, 0.85f);

        /// <summary>
        /// A flat, left-aligned clickable row for lists (item pickers, help topics): no button frame, a hover highlight
        /// and a hairline separator. SetSelected marks the selected row (yellow text and a bar on the left).
        /// </summary>
        public static UIButton ListRow(UIObject parent, string text, Action onClick)
        {
            UIButton b = parent.Add<UIButton>("ListRow");
            b.Text("").OnClick(() =>
            {
                ReportPress(b);
                onClick();
            });
            b.FlexibleLayout(true, false);
            b.Finish();
            b.Content.Padding(8, 8, 2, 2);
            b.Content.ChildAlignment(TextAnchor.MiddleLeft);
            TMP_Text label = b.GetComponentInChildren<TMP_Text>();
            label.alignment = TextAlignmentOptions.Left;
            SetText(label, text);
            NameButtonByKey(b, text);
            var button = NoSelect(b.GetComponent<Button>());
            var img = b.GetComponent<Image>();

            // separator and selection bar: drawn over the row, outside its layout
            Image line = Overlay(b, "Separator", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1));
            Image bar = Overlay(b, "SelectBar", new Vector2(0, 0), new Vector2(0, 1), new Vector2(3, 0));
            bar.color = new Color(0.95f, 0.82f, 0.35f);
            bar.gameObject.SetActive(false);

            Styled(b.gameObject, th =>
            {
                th.ApplyText(label);
                line.color = new Color(1, 1, 1, 0.07f);
            });
            // a wash under the pointer; the selected row's text in its bar's colour (On)
            StyledPart.Add(b.gameObject, Headless.StyleParts.ListRow, img, button, new[] { label });
            return b;
        }

        /// <summary>Mark a ListRow as selected or not.</summary>
        public static void SetSelected(UIButton row, bool selected)
        {
            Transform bar = row.transform.Find("SelectBar");
            if (bar == null || bar.gameObject.activeSelf == selected) return;
            bar.gameObject.SetActive(selected);
            row.GetComponent<StyledPart>()?.Set(Headless.StyleFlags.On, selected);
        }

        /// <summary>A flat clickable column header (stage stats): dim text that brightens on hover, no button frame.</summary>
        public static UIButton HeaderButton(UIObject parent, string text, Action onClick)
        {
            UIButton b = ListRow(parent, text, onClick);
            b.FlexibleLayout(false, false);
            b.Content.Padding(2, 2, 1, 1);
            b.Content.ChildAlignment(TextAnchor.MiddleRight);
            TMP_Text label = b.GetComponentInChildren<TMP_Text>();
            label.alignment = TextAlignmentOptions.Right;
            b.GetComponent<StyledPart>().Part = Headless.StyleParts.HeaderButton;
            Styled(b.gameObject, th =>
            {
                b.transform.Find("Separator").GetComponent<Image>().color = new Color(1, 1, 1, 0.25f);
                // one line of the main font plus the padding, whatever the text: Δ and Σ come from a fallback font with a
                // taller line, and headers sit side by side
                b.LayoutElement.preferredHeight = TextMeasure.Measure("0", th).y + 2;
            });
            return b;
        }

        static Image Overlay(UIObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent.transform, false);
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0, 0);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        static readonly Dictionary<string, bool> sectionState = new Dictionary<string, bool>();

        /// <summary>
        /// A panel shown while <paramref name="visible"/> holds, its contents made by <paramref name="build"/> the first time
        /// it shows rather than with the window: a window with many alternative panels (the planner's operations, the
        /// attitude controllers) then builds only the one in view when it opens.
        /// </summary>
        public Layout LazyPanel(UIObject parent, Func<bool> visible, Action<Layout> build)
        {
            Layout panel = Panel(parent);
            Visible(panel.gameObject, visible);
            BuildWhenShown(panel, build);
            return panel;
        }

        /// <summary>A collapsible section whose contents are made the first time it is open (a folded section costs nothing until then).</summary>
        public Layout Section(UIObject parent, string title, bool open, Action<Layout> build, string key = null)
        {
            Layout content = SectionCore(parent, title, open, key ?? title, true);
            BuildWhenShown(content, build);
            return content;
        }

        // a folded section's content is filled while active (TMP needs an active parent, FINDINGS rule 4) and folded in
        // the window's first sync, which runs before the window is first drawn
        void FoldInFirstSync(Layout content, Action folded)
        {
            bool done = false;
            syncs.Add(() =>
            {
                if (done) return;
                done = true;
                content.gameObject.SetActive(false);
                folded?.Invoke();
            });
        }

        // builds now if the panel is on screen, else in the first sync where it is (TMP needs active parents to set up,
        // FINDINGS rule 4): a lazy panel inside a folded section waits for the section too. Its fields are named in its own scope,
        // numbered now, so they have the same names in a rebuilt window whichever panel is shown first (focus recovery)
        void BuildWhenShown(Layout panel, Action<Layout> build)
        {
            string scope = NextPanelScope();
            if (panel.gameObject.activeInHierarchy)
            {
                BuildInScope(scope, build, panel);
                return;
            }
            bool built = false;
            syncs.Add(() =>
            {
                if (built || !panel.gameObject.activeInHierarchy) return;
                built = true;
                BuildInScope(scope, build, panel);
            });
        }

        /// <summary>
        /// A collapsible section: a header row ("-"/"+" and the title) and a content panel it shows or hides. Returns the
        /// content panel. The open/closed state is remembered per key for the session (key defaults to the title).
        /// </summary>
        public Layout Section(UIObject parent, string title, bool open = true, string key = null) => SectionCore(parent, title, open, key ?? title, false);

        // lazy: a folded section starts inactive and its contents are made when it opens; else they are made now, open
        Layout SectionCore(UIObject parent, string title, bool open, string key, bool lazy) => FoldSection(parent, title, open, key, lazy);

        /// <summary>A static text that wraps to the window width (help texts, long descriptions).</summary>
        public static UIText Paragraph(UIObject parent, string text) => WrapToWidth(Label(parent, text, true));

        /// <summary>A label wraps in the width its parent gives it: its text no longer widens the window, which it would on one line.</summary>
        public static UIText WrapToWidth(UIText t)
        {
            t.tmpText.enableWordWrapping = true;
            var le = t.GetComponent<LayoutElement>() ?? t.gameObject.AddComponent<LayoutElement>();
            le.minWidth = 0;
            le.preferredWidth = 0;
            le.flexibleWidth = 1;
            return t;
        }

        // ---- theme and localization plumbing ----

        /// <summary>Apply a theme-dependent look now and again on every theme change (via the owning window).</summary>
        public static void Styled(GameObject go, Action<Theme> apply)
        {
            WindowBehaviour wb = go.GetComponentInParent<WindowBehaviour>();
            apply(wb != null ? wb.Theme ?? T : T);
            wb?.AddRestyle(() => apply(wb.Theme ?? T));
        }

        /// <summary>Text, or the localized string when it is a '#key'.</summary>
        public static string Loc(string text) => KeepLastLine(text != null && text.Length > 1 && text[0] == '#' ? Localizer.Format(text) : text);

        // TMP's preferred height ignores a trailing empty line, IMGUI shows it ("\nRestore factory default settings\n" is
        // a 3-line button): a space after the last newline keeps the line
        static string KeepLastLine(string s) => s != null && s.Length > 0 && s[s.Length - 1] == '\n' ? s + " " : s;

        /// <summary>
        /// Tooltip text for a localization key (null: none). Every label, toggle and button whose text is a '#key'
        /// gets that tooltip automatically, so tooltips can be added for a whole UI from one table.
        /// </summary>
        public static Func<string, string> TooltipFor;

        /// <summary>Set a text from a literal or a '#key'; keys are re-applied on a language switch.</summary>
        public static void SetText(TMP_Text t, string textOrKey)
        {
            t.text = Loc(textOrKey);
            if (textOrKey == null || textOrKey.Length < 2 || textOrKey[0] != '#') return;
            if (TooltipFor?.Invoke(textOrKey) != null && t.GetComponent<TooltipTrigger>() == null)
            {
                t.raycastTarget = true;
                // looked up when shown, so it follows a language switch like the label
                Tooltips.Add(t.gameObject, () => TooltipFor?.Invoke(textOrKey));
            }
            WindowBehaviour wb = t.GetComponentInParent<WindowBehaviour>();
            if (wb == null) return;
            wb.AddRelocalize(() => t.text = Loc(textOrKey));
            wb.TextKeys[t] = textOrKey;
        }

        // ---- layout ----

        public static Layout Row(UIObject parent)
        {
            Layout row = parent.Add<Layout>("Row");
            row.Horizontal().ControlChildSize(true, true).ChildForceExpand(false, false).Spacing(4).ChildAlignment(TextAnchor.MiddleLeft);
            row.FlexibleLayout(true, false);
            row.Finish();
            return row;
        }

        public static LayoutPanel Box(UIObject parent)
        {
            LayoutPanel box = parent.Add<LayoutPanel>("Box");
            box.Vertical().ControlChildSize(true, true).ChildForceExpand(false, false).Spacing(2).Padding(4);
            box.FlexibleLayout(true, false);
            box.Finish();
            Styled(box.gameObject, t => t.Box.ApplyImage(box.BackGround));
            return box;
        }

        public static void FlexibleSpace(UIObject parent) => parent.Add<UIEmpty>().FlexibleLayout(true, false).Finish();

        public static void Space(UIObject parent, float w) => parent.Add<UIEmpty>().PreferredWidth(w).Finish();

        // ---- widgets ----

        public static UIText Label(UIObject parent, string text, bool flexible = false)
        {
            UIText t = parent.Add<UIText>();
            if (flexible) t.FlexibleLayout(true, false);
            t.Finish();
            t.tmpText.enableWordWrapping = false;
            t.tmpText.alignment = TextAlignmentOptions.Left; // middle-left: rows are taller than the text, keep it centred like IMGUI
            SetText(t.tmpText, text);
            var lineLe = t.gameObject.GetComponent<LayoutElement>() ?? t.gameObject.AddComponent<LayoutElement>();
            Styled(t.gameObject, th =>
            {
                th.ApplyText(t.tmpText);
                lineLe.minHeight = th.LineHeight; // IMGUI pads labels to a fixed row height
            });
            return t;
        }

        /// <summary>
        /// A layout cell of fixed height (and fixed or flexible width) whose child text does not drive layout.
        /// Size it from a template string as wide as the widest expected value; it re-sizes on theme change.
        /// </summary>
        public static UIText LiveLabel(UIObject parent, string template, bool flexible) => IsolatedLabel(parent, template, flexible);

        const string LiveCellName = "LiveCell";

        /// <summary>The object that holds a label's place in its row: the fixed-size cell of a live or bound label, else the label.</summary>
        public static GameObject CellOf(UIText t)
        {
            Transform p = t.transform.parent;
            return p != null && p.name == LiveCellName ? p.gameObject : t.gameObject;
        }

        /// <summary>The row of a toggle made by Toggle, CheckBox or SwitchRow: its box or switch and its label.</summary>
        public static GameObject RowOf(Toggle t) => t.transform.parent.gameObject;

        static UIText IsolatedLabel(UIObject parent, string text, bool flexible)
        {
            UIEmpty cell = parent.Add<UIEmpty>(LiveCellName);
            UIText t = cell.Add<UIText>();
            t.Text(text).Anchor(AnchorPresets.StretchAll);
            t.Finish();
            t.tmpText.enableWordWrapping = false;
            t.tmpText.alignment = TextAlignmentOptions.Left; // middle-left
            t.tmpText.overflowMode = TextOverflowModes.Ellipsis;
            t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;

            var le = cell.gameObject.AddComponent<LayoutElement>();
            if (flexible) le.flexibleWidth = 1;
            cell.Finish();
            Styled(cell.gameObject, th =>
            {
                th.ApplyText(t.tmpText);
                // measure the template on a copy of the settings, not on the live text (KSP's TMP overwrites the buffer)
                Vector2 size = TextMeasure.Measure(text, th);
                le.preferredHeight = Mathf.Max(size.y, th.LineHeight);
                if (!flexible) le.preferredWidth = size.x + 24;
            });
            return t;
        }

        /// <summary>
        /// A label that changes, in a cell sized from <paramref name="template"/> (the longest text it shows), so the text
        /// never drives the layout. The getter is called every frame: return cached strings.
        /// </summary>
        public UIText BoundLabel(UIObject parent, string template, Func<string> value, bool flexible = false)
        {
            UIText t = LiveLabel(parent, template, flexible);
            string last = value();
            t.Text(last);
            syncs.Add(() =>
            {
                string s = value();
                if (s == last) return;
                last = s;
                t.Text(s);
            });
            return t;
        }

        public UIText BoundLabel(UIObject parent, Func<string> value, bool flexible = false)
        {
            // a fixed-size box with no layout group: a text change stops LayoutRebuilder.MarkLayoutForRebuild at the
            // label instead of rebuilding the whole window
            UIText t = IsolatedLabel(parent, value(), flexible);
            string last = t.tmpText.text;
            syncs.Add(() =>
            {
                string s = value();
                if (s == last) return;
                last = s;
                t.Text(s);
            });
            return t;
        }

        /// <summary>
        /// Let a flexible BoundLabel wrap onto more lines when its text is wider than its cell, as IMGUI's labels do (an
        /// autopilot's status), instead of ending in an ellipsis. The cell grows in height. The text is measured at the
        /// cell's width only when the text or the width changes, and the layout is touched only when the line count does,
        /// so a value that keeps its lines costs no layout. For string texts (BoundLabel), not CharText ones.
        /// </summary>
        public UIText Wrap(UIText t)
        {
            GameObject cell = CellOf(t);
            var le = cell.GetComponent<LayoutElement>() ?? cell.AddComponent<LayoutElement>();
            var rt = (RectTransform)cell.transform;
            t.tmpText.enableWordWrapping = true;
            t.tmpText.overflowMode = TextOverflowModes.Overflow;
            string measured = null;
            float measuredWidth = -1;
            // a theme change sizes the cell from its first text again: measure again
            Behaviour.AddRestyle(() => measured = null);
            syncs.Add(() =>
            {
                float width = rt.rect.width;
                string s = t.tmpText.text;
                if (width <= 0 || s == measured && width == measuredWidth) return;
                measured = s;
                measuredWidth = width;
                Theme th = ThemeOf(cell);
                float height = Mathf.Max(TextMeasure.Measure(s ?? "", th, width).y, th.LineHeight);
                if (Mathf.Abs(height - le.preferredHeight) > 0.5f) le.preferredHeight = height;
            });
            return t;
        }

        /// <summary>
        /// Keeps uGUI's selection off a widget: a click doesn't select it (uGUI selects on pointer down only when the
        /// navigation mode isn't None), so a later Enter or Space (uGUI's Submit) doesn't click it again and the arrows
        /// don't move it. Every KodeKit button, toggle, switch, slider and scrollbar takes this; only text fields take
        /// uGUI's selection, and KodeKit's keyboard handling goes through its own scopes. A click on one of these still
        /// takes the selection off a focused field (the input module deselects whatever the press isn't on).
        /// </summary>
        public static T NoSelect<T>(T s) where T : Selectable
        {
            s.navigation = new Navigation { mode = Navigation.Mode.None };
            return s;
        }

        public static UIButton Button(UIObject parent, string text, Action onClick, bool flexible = false)
        {
            UIButton b = parent.Add<UIButton>();
            b.Text("").OnClick(() =>
            {
                ReportPress(b);
                onClick();
            });
            if (flexible) b.FlexibleLayout(true, false);
            b.Finish();
            b.Content.Padding(6, 6, 1, 1);
            // the text at its preferred width, not KodeUI's flexible one, so an icon stays beside it: a wide button centres the two
            b.ChildText.FlexibleLayout(false, false);
            b.Content.ChildAlignment(TextAnchor.MiddleCenter);
            TMP_Text label = b.GetComponentInChildren<TMP_Text>();
            label.alignment = TextAlignmentOptions.Center; // like GUILayout.Button
            SetText(label, text);
            NameButtonByKey(b, text);
            var button = NoSelect(b.GetComponent<Button>());
            var img = b.GetComponent<Image>();
            ClickFlash.Add(button, img);
            var buttonLe = b.gameObject.GetComponent<LayoutElement>() ?? b.gameObject.AddComponent<LayoutElement>();
            Styled(b.gameObject, th =>
            {
                th.Button.ApplySelectable(button, img);
                th.ApplyText(label, th.Button.TextColor);
                buttonLe.minHeight = th.LineHeight;
            });
            return b;
        }

        /// <summary>
        /// Names a button for the agent bridge: its id is <paramref name="name"/> (a '#key', or a text that is the same in every
        /// language) in its window's build scope (<see cref="Headless.CommitPath.NameScope"/>), numbered when the window has another
        /// button of the name (<see cref="Headless.ControlMatch.ButtonIds"/>). The button factories name a button whose text is a
        /// '#key' by it; a view names the others (an icon button). Stored once, on the button; read only when the bridge lists the window.
        /// </summary>
        public static void NameButton(UIButton b, string name)
        {
            if (b == null || string.IsNullOrEmpty(name)) return;
            ButtonName n = b.GetComponent<ButtonName>() ?? b.gameObject.AddComponent<ButtonName>();
            n.Name = ScopeOf(b.transform) + name;
        }

        static void NameButtonByKey(UIButton b, string textOrKey)
        {
            if (textOrKey != null && textOrKey.Length > 1 && textOrKey[0] == '#') NameButton(b, textOrKey);
        }

        // the build scope of the window being built under t (a lazy panel's "p3/"); "" outside a window. Walks up with GetComponent,
        // which also finds an inactive window's behaviour (GetComponentInParent doesn't)
        static string ScopeOf(Transform t)
        {
            for (; t != null; t = t.parent)
            {
                WindowBehaviour wb = t.GetComponent<WindowBehaviour>();
                if (wb != null) return wb.Window != null ? wb.Window.Commits.NameScope ?? "" : "";
            }
            return "";
        }

        /// <summary>A button showing only a sprite (e.g. <see cref="Sprites.Arrow"/>), sized like a one-character button.</summary>
        public static UIButton IconButton(UIObject parent, Sprite icon, Action onClick)
        {
            UIButton b = Button(parent, "", onClick);
            b.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
            AddIcon(b, icon, false);
            return b;
        }

        /// <summary>
        /// A sprite inside a button, before or after its label, tinted with the button's text colour and as high as a
        /// capital letter (themes re-apply both).
        /// </summary>
        public static Image AddIcon(UIButton b, Sprite icon, bool before)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement)) { layer = b.gameObject.layer };
            go.transform.SetParent(b.Content.transform, false);
            if (before) go.transform.SetSiblingIndex(0);
            var img = go.GetComponent<Image>();
            img.sprite = icon;
            img.raycastTarget = false;
            img.preserveAspect = true;
            var le = go.GetComponent<LayoutElement>();
            Styled(go, th =>
            {
                float size = Mathf.Round(th.FontSize * 0.8f);
                le.minWidth = le.preferredWidth = le.minHeight = le.preferredHeight = size;
                img.color = th.Button.TextColor;
            });
            return img;
        }

        /// <summary>
        /// IMGUI's big state buttons: a GUIStyle with padding 8 on every side (SmartASS, SmartRcs, Translatron), so the
        /// button is the text's height plus 16.
        /// </summary>
        public static UIButton Padded(UIButton b, int padding = 8)
        {
            b.Content.Padding(padding, padding, padding, padding);
            return b;
        }

        /// <summary>The row's children take the height of the tallest one (GUILayout.ExpandHeight in a horizontal group).</summary>
        public static Layout EqualHeights(Layout row)
        {
            row.ChildForceExpand(false, true);
            return row;
        }

        public static UIButton RepeatButton(UIObject parent, string text, Action action)
        {
            UIButton b = Button(parent, text, () => { });
            b.gameObject.AddComponent<RepeatTrigger>().Action = action;
            return b;
        }

        /// <summary>A state shown by a text colour (Theme.ToneColor): Normal, green, yellow, red, dimmed, or orange (Caution).</summary>
        public enum Tone { Normal, Good, Warn, Bad, Dim, Caution }

        /// <summary>A toggle whose label colour follows a state (MechJeb's Green/Yellow/RedToggle styles).</summary>
        public Toggle Toggle(UIObject parent, string text, Func<bool> get, Action<bool> set, Func<Tone> tone) =>
            ToneLabel(Toggle(parent, text, get, set), tone);

        /// <summary>A checkbox whose label colour follows a state.</summary>
        public Toggle CheckBox(UIObject parent, string text, Func<bool> get, Action<bool> set, Func<Tone> tone) =>
            ToneLabel(CheckBox(parent, text, get, set), tone);

        Toggle ToneLabel(Toggle toggle, Func<Tone> tone)
        {
            ToneText(LabelOf(toggle), tone);
            return toggle;
        }

        /// <summary>The label of a toggle made by Toggle, CheckBox or SwitchToggle (the text that flips it).</summary>
        public static TMP_Text LabelOf(Toggle toggle) => toggle.transform.parent.GetComponentInChildren<ToggleLabelClick>().GetComponent<TMP_Text>();

        /// <summary>A switch and its label. The label flips it too.</summary>
        public Toggle Toggle(UIObject parent, string text, Func<bool> get, Action<bool> set, Func<bool> green = null) =>
            SwitchToggle(parent, text, get, set, green);

        /// <summary>A checkbox and its label (the menu's window list keeps checkboxes; <see cref="Toggle(UIObject, string, Func{bool}, Action{bool}, Func{bool})"/> is a switch).</summary>
        /// <param name="name">Its name when <paramref name="text"/> is empty (a check box in a table's cell; null: the label right before it).</param>
        public Toggle CheckBox(UIObject parent, string text, Func<bool> get, Action<bool> set, Func<bool> green = null, string name = null) =>
            CheckBox(parent, text, get, set, green, name, false);

        /// <summary>
        /// A selection check box (a row's in a toggle column: <see cref="SelectionColumn"/>; or a
        /// list's "all" box): named <paramref name="name"/> (a '#key' or a text), with no label unless <paramref name="text"/> is given,
        /// in a row that doesn't stretch. Its changes are view state, not a setting: its commits neither save the window's settings nor
        /// go on its undo stack. No key or modifier is read: a click toggles it.
        /// </summary>
        public Toggle SelectBox(UIObject parent, Func<bool> get, Action<bool> set, string name, string text = null) =>
            CheckBox(parent, text, get, set, null, name, true);

        Toggle CheckBox(UIObject parent, string text, Func<bool> get, Action<bool> set, Func<bool> green, string name, bool select)
        {
            if (string.IsNullOrEmpty(text)) name = name ?? LabelBefore(parent);
            Layout row = Row(parent);
            if (select) row.FlexibleLayout(false, false);
            UIToggle t = row.Add<UIToggle>();
            t.PreferredSize(16, 16).Finish();
            t.CheckMark.Scale(0.625f);
            // a selection box without a text has no label: a row's own text is its label
            UIText label = select && string.IsNullOrEmpty(text) ? null : Label(row, text ?? "", !select);

            Toggle toggle = NoSelect(t.GetComponent<Toggle>());
            // a selection box ticks at once (no fade): a row's state is read at a glance, and a shot shows it
            if (select) toggle.toggleTransition = UnityEngine.UI.Toggle.ToggleTransition.None;
            if (label != null) label.gameObject.AddComponent<ToggleLabelClick>().Toggle = toggle;
            Headless.CommitTarget<bool> target = Target("CheckBox", get, set, null, !select);
            target.Control = toggle;
            // the behaviour: a click (uGUI's toggle, or its label) and an agent's flip both set the model, which calls the setter once
            var model = new Headless.ToggleModel(Headless.ToggleKind.CheckBox, get, target.Set);
            target.Model = model;
            LabelTarget(target, string.IsNullOrEmpty(text) ? name : text);
            toggle.SetIsOnWithoutNotify(model.On);
            void Show()
            {
                if (toggle.isOn != model.On) toggle.SetIsOnWithoutNotify(model.On);
            }
            toggle.onValueChanged.AddListener(v =>
            {
                // drawn again after every set: a refused set, or a setter that keeps the old value (the model's Shown doesn't change
                // then, so no ShownChanged), puts back the control uGUI moved
                model.Set(v, Pointer);
                Show();
            });
            model.Value.ShownChanged += Show;

            var box = t.GetComponent<Image>();
            var check = (Image)toggle.graphic;
            Styled(t.gameObject, th =>
            {
                th.Toggle.ApplySelectable(toggle, box);
                th.Check.ApplyImage(check);
                t.PreferredSize(th.FontSize + 2, th.FontSize + 2);
            });

            syncs.Add(() => model.Sync());
            if (green != null) ToneText(label, () => green() ? Tone.Good : Tone.Normal);
            return toggle;
        }

        /// <summary>
        /// [&lt;] label [&gt;]: steps through <paramref name="count"/> entries, wrapping around. <paramref name="name"/> names the control
        /// (a '#key' or a text; null: the label right before it, <see cref="LabelBefore"/>); <paramref name="label"/> is the entry shown.
        /// </summary>
        public void ArrowSelector(UIObject parent, Func<int> get, Action<int> set, int count, Func<string> label, string name = null) =>
            ArrowSelector(parent, get, set, () => count, label, name);

        /// <summary>ArrowSelector over a list whose length changes (e.g. maneuver nodes).</summary>
        public void ArrowSelector(UIObject parent, Func<int> get, Action<int> set, Func<int> count, Func<string> label, string name = null)
        {
            name = name ?? LabelBefore(parent);
            Layout row = Row(parent);
            Headless.CommitTarget<int> target = Target("ArrowSelector", get, set);
            target.Control = row;
            // its entry count, for a host's select by "#n"
            target.Model = new Headless.IndexChoice(count);
            LabelTarget(target, name);
            Button(row, "<", () =>
            {
                int n = count();
                if (n > 0) target.Set((Mathf.Clamp(get(), 0, n - 1) - 1 + n) % n, Pointer);
            });
            BoundLabel(row, label, true).tmpText.alignment = TextAlignmentOptions.Center;
            Button(row, ">", () =>
            {
                int n = count();
                if (n > 0) target.Set((Mathf.Clamp(get(), 0, n - 1) + 1) % n, Pointer);
            });
        }

        /// <summary>GUILayout.SelectionGrid with one row: a button per item, the selected one tinted green (<see cref="Segmented"/> is the joined control).</summary>
        /// <param name="padding">0: normal buttons; else like <see cref="Padded"/>.</param>
        /// <param name="label">The choice's name (a '#key' or a text; null: the label right before it in its row).</param>
        public void SelectionGrid(UIObject parent, string[] items, Func<int> get, Action<int> set, int padding = 0, string label = null)
        {
            label = label ?? LabelBefore(parent);
            Headless.CommitTarget<int> target = Target("SelectionGrid", get, set);
            target.Model = new Headless.IndexChoice(() => items.Length);
            LabelTarget(target, label);
            var images = new Image[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                int k = i;
                UIButton b = Button(parent, items[i], () => target.Set(k, Pointer), true);
                if (i == 0) target.Control = b;
                if (padding > 0) Padded(b, padding);
                images[i] = b.GetComponent<Image>();
            }

            int shown = -1;
            Action paint = () =>
            {
                for (int i = 0; i < images.Length; i++)
                    images[i].color = i == shown ? new Color(0.5f, 1f, 0.5f) : T.Button.Tint;
            };
            Behaviour.AddRestyle(paint);
            syncs.Add(() =>
            {
                int v = get();
                if (v == shown) return;
                shown = v;
                paint();
            });
        }

        /// <summary>Shows a live or bound label's cell (or a plain label) while <paramref name="visible"/> holds.</summary>
        public void Visible(UIText t, Func<bool> visible) => Visible(CellOf(t), visible);

        /// <summary>Shows a toggle's whole row while <paramref name="visible"/> holds.</summary>
        public void Visible(Toggle t, Func<bool> visible) => Visible(RowOf(t), visible);

        public void Visible(GameObject go, Func<bool> visible)
        {
            bool shown = visible();
            go.SetActive(shown);
            syncs.Add(() =>
            {
                bool v = visible();
                if (v == shown) return;
                shown = v;
                go.SetActive(v);
            });
        }
    }

    /// <summary>
    /// Measures text with a hidden TMP object: KSP's TMP overwrites an object's own text buffer in
    /// GetPreferredValues(string), so visible texts must never be used for measuring.
    /// </summary>
    public static class TextMeasure
    {
        static TextMeshProUGUI measurer;

        public static Vector2 Measure(string text, Theme theme)
        {
            if (measurer == null)
            {
                var go = new GameObject("KodeKit.TextMeasure", typeof(RectTransform));
                UnityEngine.Object.DontDestroyOnLoad(go);
                measurer = go.AddComponent<TextMeshProUGUI>();
                measurer.enableWordWrapping = false;
                measurer.enabled = false; // never drawn
            }
            theme.ApplyText(measurer);
            return measurer.GetPreferredValues(text);
        }

        /// <summary>The size of text word-wrapped at maxWidth.</summary>
        public static Vector2 Measure(string text, Theme theme, float maxWidth)
        {
            Measure("", theme);
            measurer.enableWordWrapping = true;
            Vector2 size = measurer.GetPreferredValues(text, maxWidth, 0);
            measurer.enableWordWrapping = false;
            return size;
        }
    }
}

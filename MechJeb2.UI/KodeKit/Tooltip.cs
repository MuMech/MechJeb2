using System;
using KodeKit.Headless;
using KodeUI;
using KSP.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// Shows a tooltip, or a hover card, after the pointer rests on this object for Tooltips.Delay seconds (GuiUtils uses 1 s).
    /// A press hides it (Tooltips.Update). Not an IPointerDownHandler: on a button's label that would make the label the pressed
    /// object, and the button would never get its click.
    /// </summary>
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>A plain tooltip's text (a '#key' is localized when shown); null or empty shows nothing.</summary>
        public Func<string> Text;

        /// <summary>A hover card's builder (set: this trigger shows a card, not <see cref="Text"/>).</summary>
        public Action<HoverCard> Card;

        /// <summary>
        /// What the hover is on, when several objects share one card (a table's row over its cells): moving between them keeps the
        /// card. Null: this trigger.
        /// </summary>
        public object Key;

        /// <summary>The card's anchor, in the given space's units (y up): the card opens beside it. Null: this object's own rect.</summary>
        public Func<RectTransform, FrameBox> Anchor;

        /// <summary>What <see cref="Tooltips.Hover"/> knows this trigger by: its <see cref="Key"/>, or itself.</summary>
        public object HoverKey => Key ?? this;

        public void OnPointerEnter(PointerEventData e) => Tooltips.Enter(this, e.position);
        public void OnPointerExit(PointerEventData e) => Tooltips.Leave(this);
        void OnDisable() => Tooltips.Cancel(this);
    }

    /// <summary>
    /// One tooltip panel for all windows, on its own overlay canvas (UI layer, above windows and combo popups), placed at the
    /// pointer and kept on screen. Text may be a '#key' (localized when shown). A trigger with a card builder shows a hover card
    /// instead (<see cref="HoverCard"/>, one panel kept for all): filled when it opens, placed beside its anchor by <see cref="PopupPlacer"/> at its window's
    /// scale, its live values written 4 times a second. When either opens and closes is <see cref="Hover"/>'s
    /// (<see cref="HoverModel"/>): a tooltip closes as the pointer leaves, a card <see cref="CardCloseDelay"/> later.
    /// </summary>
    public class Tooltips : MonoBehaviour
    {
        public static float Delay = 1f;
        public static int MaxWidth = 300;

        /// <summary>Seconds a hover card stays after the pointer left its target (moving to the next row switches at once).</summary>
        public static float CardCloseDelay = 0.15f;

        /// <summary>The widest a hover card is, in its own units (its lines wrap): the tooltip's cap.</summary>
        public static float CardMaxWidth = 300;

        /// <summary>Seconds between two writes of a shown card's live values (4 a second).</summary>
        public static float LiveInterval = 0.25f;

        static Tooltips instance;
        readonly HoverModel hover = new HoverModel();
        TooltipTrigger entered; // the trigger the pointer entered last (the one whose card or tooltip opens)
        Vector2 pointer;
        RectTransform root, panel;
        LayoutElement width;
        TextMeshProUGUI label;
        Image background;
        string follow; // the text that follows the pointer, or null

        // the card shown, if any
        HoverCard card;
        RectTransform cardPanel;
        TooltipTrigger cardTrigger;
        Vector2 cardPlacedSize;
        float nextLive;

        /// <summary>
        /// Shows <paramref name="text"/> next to the pointer at once, and keeps it there every frame until this is called with
        /// null (a label for what the pointer is over, such as the map's position picker). Meanwhile no hover tooltip shows.
        /// </summary>
        public static void FollowPointer(string text)
        {
            if (text == null && (instance == null || instance.follow == null)) return;
            Instance.follow = text;
            if (text == null) instance.Hide();
        }

        public static TooltipTrigger Add(GameObject target, string textOrKey) => string.IsNullOrEmpty(textOrKey) ? null : Add(target, () => textOrKey);

        public static TooltipTrigger Add(GameObject target, Func<string> text)
        {
            TooltipTrigger t = Trigger(target);
            t.Text = text;
            return t;
        }

        /// <summary>
        /// A hover card on <paramref name="target"/>: <paramref name="build"/> fills it each time it opens. Objects that share a
        /// <paramref name="key"/> share the card (a row's cells); <paramref name="anchor"/> gives the box it opens beside (default:
        /// the target's rect).
        /// </summary>
        public static TooltipTrigger Add(GameObject target, Action<HoverCard> build, object key = null, Func<RectTransform, FrameBox> anchor = null)
        {
            TooltipTrigger t = Trigger(target);
            t.Card = build;
            t.Key = key;
            t.Anchor = anchor;
            return t;
        }

        /// <summary>
        /// An anchor for a row's card: the row's height across its window's width, so the card opens beside the window (right,
        /// else left) and covers none of it.
        /// </summary>
        public static Func<RectTransform, FrameBox> BesideWindow(RectTransform row) => space =>
        {
            FrameBox r = Popups.BoxIn(row, space);
            WindowBehaviour wb = row.GetComponentInParent<WindowBehaviour>();
            if (wb == null) return r;
            FrameBox w = Popups.BoxIn((RectTransform)wb.transform, space);
            return new FrameBox(w.Left, r.Top, w.Right, r.Bottom);
        };

        static TooltipTrigger Trigger(GameObject target)
        {
            var t = target.GetComponent<TooltipTrigger>() ?? target.AddComponent<TooltipTrigger>();
            // the target must catch the pointer for enter/exit
            Graphic g = target.GetComponent<Graphic>();
            if (g != null) g.raycastTarget = true;
            return t;
        }

        public static bool Visible => instance != null && instance.panel != null && instance.panel.gameObject.activeSelf;
        public static string ShownText => Visible ? instance.label.text : null;
        public static RectTransform Panel => instance?.panel;

        /// <summary>The hover card shown now (null: none).</summary>
        public static HoverCard ShownCard => instance?.card;

        /// <summary>The shown hover card's panel (null: none): its box, its scale.</summary>
        public static RectTransform CardPanel => instance?.cardPanel;

        /// <summary>The trigger whose card is shown (null: none).</summary>
        public static TooltipTrigger CardTrigger => instance?.cardTrigger;

        /// <summary>Where the shown card was placed last (PopupPlacer's result), in the tooltip canvas's units.</summary>
        public static PopupPlacement CardPlacement { get; private set; }

        /// <summary>The tooltip canvas (the space of <see cref="CardPlacement"/>); null until a tooltip or card was shown.</summary>
        public static RectTransform CanvasRect => instance?.root;

        /// <summary>When tooltips and cards open and close (for tests and the agent bridge: a hover's state).</summary>
        public static HoverModel Hover => Instance.hover;

        static double Now => Time.unscaledTime;

        internal static void Enter(TooltipTrigger t, Vector2 screenPos)
        {
            Tooltips i = Instance;
            i.entered = t;
            i.pointer = screenPos;
            i.Apply(i.hover.Enter(t.HoverKey, Now, Delay, t.Card != null ? CardCloseDelay : 0));
        }

        internal static void Leave(TooltipTrigger t)
        {
            if (instance == null) return;
            instance.Apply(instance.hover.Leave(t.HoverKey, Now));
        }

        internal static void Cancel(TooltipTrigger t)
        {
            if (instance == null) return;
            // another object of the same key may still be under the pointer: only the one entered last, or the card's, cancels
            if (instance.entered != t && instance.cardTrigger != t) return;
            instance.Apply(instance.hover.Cancel(t.HoverKey));
        }

        /// <summary>
        /// Closes <paramref name="t"/>'s tooltip or card, shown or waiting (its content went stale: a list row bound to another
        /// item); nothing opens again until the pointer leaves and enters again.
        /// </summary>
        public static void Close(TooltipTrigger t) => Cancel(t);

        /// <summary>A press of a pointer button, as <see cref="Update"/> reads one: hides the tooltip or card under the pointer.</summary>
        public static void Press()
        {
            if (instance == null) return;
            instance.Apply(instance.hover.Press(Now));
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
            if (root == null)
            {
                Canvas canvas = Overlay.Root;
                var go = new GameObject("TooltipCanvas", typeof(RectTransform)) { layer = canvas.gameObject.layer };
                root = (RectTransform)go.transform;
                root.SetParent(canvas.transform, false);
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
                root.offsetMin = root.offsetMax = Vector2.zero;
                Overlay.Setup(go.AddComponent<Canvas>(), canvas, Overlay.TooltipOrder); // above everything
            }
            if (panel != null) return;

            LayoutPanel p = UIKit.CreateUI<LayoutPanel>(root, "Tooltip");
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
            // a card that follows the pointer is its owner's to end
            if (followOwner == null) CloseCard();
        }

        void Apply(HoverChange change)
        {
            switch (change)
            {
                case HoverChange.Opened:
                case HoverChange.Switched:
                    Hide();
                    Open();
                    break;
                case HoverChange.Closed:
                    Hide();
                    break;
            }
        }

        // the card or tooltip of the trigger the pointer entered last opens
        void Open()
        {
            TooltipTrigger t = entered;
            if (t == null || !ReferenceEquals(t.HoverKey, hover.Shown) || !t.isActiveAndEnabled || followOwner != null)
            {
                hover.Cancel(hover.Shown);
                return;
            }
            if (t.Card != null)
            {
                ShowCard(t);
                return;
            }
            string text = KodeWindow.Loc(t.Text?.Invoke());
            if (string.IsNullOrEmpty(text))
            {
                hover.Decline(); // tried again at the next frame, while the pointer stays (a value that is cut off only later)
                return;
            }
            Show(text);
        }

        void Update()
        {
            if (follow != null)
            {
                if (hover.Hovered != null) Apply(hover.Reset());
                pointer = Input.mousePosition;
                if (Visible && label.text == follow) Place();
                else Show(follow);
                return;
            }

            if (followOwner != null)
            {
                // the card follows its owner's pointer: no hover card or tooltip meanwhile
                if (hover.Hovered != null) Apply(hover.Reset());
                if (followScale == null || !followScale.gameObject.activeInHierarchy) EndFollowCard(followOwner);
                return;
            }

            if (hover.Hovered == null) return;
            // any press cancels the tooltip or card under the pointer, shown or still waiting
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                Press();
                return;
            }
            Apply(hover.Tick(Now));
            if (card == null) return;
            if (cardTrigger == null || !cardTrigger.isActiveAndEnabled)
            {
                Apply(hover.Reset());
                return;
            }
            if (Time.unscaledTime >= nextLive)
            {
                nextLive = Time.unscaledTime + LiveInterval;
                card.Tick();
            }
            // its window was rescaled (MechJeb's UI scale), or a live value changed its size (at the last layout): place it again,
            // clear of its anchor and on screen
            float scale = CardScale(cardTrigger);
            if (scale != cardPanel.localScale.x)
            {
                cardPanel.localScale = new Vector3(scale, scale, 1);
                PlaceCard();
            }
            else if (cardPanel.rect.size != cardPlacedSize) PlaceCard();
        }

        void Show(string text)
        {
            Build();
            Theme th = Theme.Current ?? Theme.KodeDefault();
            th.Tooltip.ApplyImage(background);
            th.ApplyText(label);
            label.text = text;
            // the panel's width: the text's and the panel's own padding (the LayoutElement replaces the group's width, padding included)
            int padding = panel.GetComponent<LayoutGroup>().padding.horizontal;
            width.preferredWidth = Mathf.Min(MaxWidth, TextMeasure.Measure(text, th).x + padding + 1);
            panel.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            Place();
        }

        void Place()
        {
            // below-right of the pointer, kept on screen
            var parent = (RectTransform)panel.parent;
            Camera cam = Overlay.Root.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, pointer + new Vector2(12, -16), cam, out Vector2 local);
            // the pointer placement is the tooltip's own; the clamp on screen is the popups' (PopupPlacer)
            Vector2 size = panel.rect.size;
            var box = new FrameBox(local.x, local.y, local.x + size.x, local.y - size.y);
            Popups.Put(panel, PopupPlacer.Clamp(box, Popups.Box(parent.rect)));
        }

        // ---- hover cards ----

        // the one card panel, made at the first card and kept (its lines and rows are reused)
        HoverCard cardContent;
        RectTransform cardRoot;
        LayoutElement cardWidth;

        void ShowCard(TooltipTrigger t)
        {
            Build();
            EnsureCardPanel();
            Theme th = KodeWindow.ThemeOf(t.gameObject);
            th.Tooltip.ApplyImage(cardRoot.GetComponent<LayoutPanel>().BackGround);
            cardWidth.preferredWidth = -1;
            cardWidth.minWidth = -1; // a following card's floor
            cardContent.Begin(th);
            cardRoot.gameObject.SetActive(true);
            cardPanel = cardRoot;
            cardTrigger = t;
            card = cardContent;
            try
            {
                t.Card(card);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                CloseCard();
                hover.Cancel(hover.Shown);
                return;
            }
            if (card.IsEmpty)
            {
                // nothing to show for this target (a tree's branch row): no card until the pointer enters again
                CloseCard();
                hover.Cancel(hover.Shown);
                return;
            }
            card.Seal();
            card.Tick();
            nextLive = Time.unscaledTime + LiveInterval;

            float scale = CardScale(t);
            cardPanel.localScale = new Vector3(scale, scale, 1);
            // no wider than the cap, in the card's own units: its lines wrap
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardPanel);
            if (cardPanel.rect.width > CardMaxWidth)
            {
                cardWidth.preferredWidth = CardMaxWidth;
                LayoutRebuilder.ForceRebuildLayoutImmediate(cardPanel);
            }
            PlaceCard();
        }

        void EnsureCardPanel()
        {
            if (cardRoot != null) return;
            LayoutPanel p = UIKit.CreateUI<LayoutPanel>(root, "HoverCard");
            p.Vertical().ControlChildSize(true, true).ChildForceExpand(false, false).Padding(8, 8, 4, 4).Spacing(2);
            p.PreferredSizeFitter(true, true).Anchor(AnchorPresets.BottomLeft).Pivot(PivotPresets.TopLeft);
            p.Finish();
            cardWidth = p.gameObject.AddComponent<LayoutElement>();
            cardRoot = p.rectTransform;
            cardContent = new HoverCard(p);
        }

        // the card is drawn at its trigger's window's scale (the tooltip canvas is at scale 1 while a window can be at 1.5)
        float CardScale(TooltipTrigger t)
        {
            float scale = t.transform.lossyScale.x / root.lossyScale.x;
            return scale > 0 ? scale : 1;
        }

        // beside its anchor, on its right (on its left when there is no room), clamped on screen
        void PlaceCard()
        {
            Vector2 size = cardPanel.rect.size;
            cardPlacedSize = size;
            FrameBox anchor = cardTrigger.Anchor != null ? cardTrigger.Anchor(root) : Popups.BoxIn((RectTransform)cardTrigger.transform, root);
            PopupPlacement placed = PopupPlacer.Place(anchor, size.x, size.y, Popups.Box(root.rect), cardPanel.localScale.x, PopupSide.Right,
                PopupAlign.Start, 4, 4);
            CardPlacement = placed;
            Popups.Put(cardPanel, placed.Box);
        }

        void CloseCard()
        {
            if (cardPanel != null) cardPanel.gameObject.SetActive(false);
            cardPanel = null;
            card = null;
            cardTrigger = null;
            followOwner = null;
            followScale = null;
        }

        // ---- a card that follows the pointer (a graph's read-out) ----

        object followOwner;
        RectTransform followScale; // the object whose window gives the card its scale and theme
        float followWidth;         // the widest the card was while it followed: it never narrows under a moving pointer

        /// <summary>What the card that follows the pointer belongs to (null: none shows).</summary>
        public static object FollowCardOwner => instance?.followOwner;

        /// <summary>
        /// Shows the hover card beside the pointer at <paramref name="screen"/> at once, for <paramref name="owner"/>, filled by
        /// <paramref name="build"/> (as a hover card's builder), drawn at the scale and in the theme of <paramref name="scaleOf"/>'s
        /// window. It stays until <see cref="EndFollowCard"/>; its owner moves it and rewrites its values with
        /// <see cref="MoveFollowCard"/>. Meanwhile no hover card or tooltip opens. Calling it again (the rows changed) refills it.
        /// A builder that adds nothing shows nothing.
        /// </summary>
        public static void FollowCard(object owner, RectTransform scaleOf, Action<HoverCard> build, Vector2 screen)
        {
            if (owner == null || scaleOf == null) return;
            Tooltips i = Instance;
            bool same = ReferenceEquals(i.followOwner, owner);
            // a hover card or tooltip shown or waiting goes: the following card takes the panel
            i.followOwner = null;
            i.Apply(i.hover.Reset());
            i.CloseCard();
            if (!same) i.followWidth = 0;
            i.ShowFollowCard(owner, scaleOf, build, screen);
        }

        /// <summary>
        /// Puts <paramref name="owner"/>'s following card beside the pointer at <paramref name="screen"/>; with
        /// <paramref name="values"/>, writes its live values first. Nothing for another owner.
        /// </summary>
        public static void MoveFollowCard(object owner, Vector2 screen, bool values)
        {
            if (instance == null || owner == null || !ReferenceEquals(instance.followOwner, owner) || instance.card == null) return;
            instance.MoveFollow(screen, values);
        }

        /// <summary>Hides <paramref name="owner"/>'s following card (nothing for another owner's).</summary>
        public static void EndFollowCard(object owner)
        {
            if (instance == null || owner == null || !ReferenceEquals(instance.followOwner, owner)) return;
            instance.CloseCard();
            instance.followWidth = 0;
        }

        void ShowFollowCard(object owner, RectTransform scaleOf, Action<HoverCard> build, Vector2 screen)
        {
            Build();
            EnsureCardPanel();
            Theme th = KodeWindow.ThemeOf(scaleOf.gameObject);
            th.Tooltip.ApplyImage(cardRoot.GetComponent<LayoutPanel>().BackGround);
            cardWidth.preferredWidth = -1;
            cardWidth.minWidth = followWidth > 0 ? followWidth : -1;
            cardContent.Begin(th);
            cardRoot.gameObject.SetActive(true);
            cardPanel = cardRoot;
            card = cardContent;
            followOwner = owner;
            followScale = scaleOf;
            try
            {
                build(card);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                CloseCard();
                return;
            }
            if (card.IsEmpty)
            {
                CloseCard();
                return;
            }
            card.Seal();
            float scale = scaleOf.lossyScale.x / root.lossyScale.x;
            cardPanel.localScale = scale > 0 ? new Vector3(scale, scale, 1) : Vector3.one;
            MoveFollow(screen, true);
        }

        void MoveFollow(Vector2 screen, bool values)
        {
            if (values)
            {
                card.Tick();
                // its new size now, not at the canvas's next layout: placed with the old one, a card flipped left of the pointer
                // would jump. It keeps the widest width it had (its minimum width), so a value that gets shorter doesn't make it
                // narrow and wide in turn
                LayoutRebuilder.ForceRebuildLayoutImmediate(cardPanel);
                float w = cardPanel.rect.width;
                if (w > followWidth)
                {
                    followWidth = w;
                    cardWidth.minWidth = w;
                }
            }

            Camera cam = Overlay.Root.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, cam, out Vector2 local);
            Vector2 size = cardPanel.rect.size;
            cardPlacedSize = size;
            // right of the pointer, its top at the pointer's height; on its left where the screen has no room
            var at = new FrameBox(local.x, local.y, local.x, local.y);
            PopupPlacement placed = PopupPlacer.Place(at, size.x, size.y, Popups.Box(root.rect), cardPanel.localScale.x, PopupSide.Right,
                PopupAlign.Start, FollowOffset, 4);
            CardPlacement = placed;
            Popups.Put(cardPanel, placed.Box);
        }

        /// <summary>How far a following card stands off the pointer, in the tooltip canvas's units.</summary>
        public static float FollowOffset = 16;
    }
}

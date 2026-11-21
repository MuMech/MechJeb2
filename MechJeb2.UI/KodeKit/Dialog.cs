using System;
using System.Collections.Generic;
using KodeKit.Headless;
using KodeUI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// KodeKit's modal dialogs: a <see cref="DialogModel"/> drawn over every window and drop-down, one at a time
    /// (<see cref="Queue"/>). <see cref="Confirm"/> asks before a destructive action, <see cref="Prompt"/> asks for a text (a rename).
    /// <list type="bullet">
    /// <item>The dialog is a window centred on the screen, on its own canvas on <see cref="Overlay.Root"/> (above the combo popups and the
    /// toasts, under the tooltips: <see cref="Overlay.DialogOrder"/>), at its owner's scale and in its owner's theme. A dimmed full-screen
    /// blocker behind it takes every click meant for the windows, the toasts and the game; a click there does nothing.</item>
    /// <item>It owns the keyboard through a <see cref="KeyboardScope"/> on the shared stack (FINDINGS rule 61): KSP's keyboard lock while it
    /// is open, uGUI's navigation off, Enter presses the focused button (the default one first), Escape cancels, Tab, Shift+Tab, Left and
    /// Right move the focus among its buttons (Tab also into and out of a prompt's field) and wrap. The lock goes once Escape and Enter are up, so an Escape never pauses KSP
    /// (FINDINGS rule 43). A dialog opened over an open drop-down sits on top of the drop-down's scope: its Escape closes the dialog
    /// only.</item>
    /// <item>Opening it takes the focus from any field (the field's edit ends); a prompt's field gets the focus, its Enter confirms and its
    /// Escape cancels the dialog.</item>
    /// <item>Its owner (the button that opened it) hidden or destroyed dismisses it: no action runs. A scene change dismisses every
    /// dialog. The dialog shown is a modal layer on the shared popup stack (<see cref="Popups.Layers"/>): a press outside it closes
    /// nothing, the scene change comes from there, and a drop-down opened while it is shown is above it on the layer stack (on screen the popup canvas, order 100, draws under the dialog's, 105: no dialog holds a drop-down today).</item>
    /// <item>It is centred on the screen by <see cref="PopupPlacer.Centre"/>, on whole units.</item>
    /// <item>Nothing runs per frame while no dialog is shown or waiting.</item>
    /// </list>
    /// </summary>
    public static class Dialogs
    {
        /// <summary>Sorting order above <see cref="Overlay.Root"/> (<see cref="Overlay.DialogOrder"/>): above the combo popups and the toasts, under the tooltips.</summary>
        public const int SortingOrder = Overlay.DialogOrder;

        /// <summary>The dialogs, shown one at a time.</summary>
        public static readonly DialogQueue Queue = MakeQueue();

        /// <summary>Whether a dialog is shown.</summary>
        public static bool IsOpen => Queue.Current != null;

        /// <summary>The dialog shown, as drawn (null: none). For tests and the agent bridge.</summary>
        public static DialogView View { get; private set; }

        /// <summary>The dialog shown's layer on <see cref="Popups.Layers"/> (null: none shown).</summary>
        public static PopupLayer Layer { get; private set; }

        static DialogQueue MakeQueue()
        {
            var q = new DialogQueue();
            q.Opened += Open;
            q.Hidden += Hide;
            return q;
        }

        /// <summary>
        /// Asks before <paramref name="onConfirm"/>: a dialog with <paramref name="confirmText"/> and Cancel. Destructive: the confirm button
        /// in the danger colour, Cancel the default. Returns the dialog, or null when it was refused (the same question already asked).
        /// </summary>
        public static DialogModel Confirm(GameObject owner, string title, string message, string confirmText, Action onConfirm, bool destructive = true)
        {
            var d = new DialogModel(title, message)
            {
                ConfirmText = confirmText ?? "#MechJeb_NewUI_OK",
                CancelText  = "#MechJeb_NewUI_Cancel",
                Destructive = destructive,
                OnConfirm   = onConfirm
            };
            return Show(d, owner) ? d : null;
        }

        /// <summary>
        /// Asks for a text: a dialog with a field starting at <paramref name="initial"/>; <paramref name="onConfirm"/> gets the text, which
        /// <paramref name="valid"/> (null: any) must accept for the confirm button to work.
        /// </summary>
        public static DialogModel Prompt(GameObject owner, string title, string message, string initial, Func<string, bool> valid, string confirmText,
            Action<string> onConfirm)
        {
            var d = new DialogModel(title, message)
            {
                ConfirmText = confirmText ?? "#MechJeb_NewUI_OK",
                CancelText  = "#MechJeb_NewUI_Cancel"
            };
            d.MakePrompt(initial, valid);
            d.OnConfirm = () => onConfirm?.Invoke(d.Input);
            return Show(d, owner) ? d : null;
        }

        /// <summary>
        /// Shows <paramref name="d"/> now, or after the dialog shown. Its owner, when given, sets its scale and theme and dismisses it when
        /// hidden or destroyed. False: refused (<see cref="DialogQueue.Show"/>).
        /// </summary>
        public static bool Show(DialogModel d, GameObject owner)
        {
            if (owner != null)
            {
                d.Owner = owner;
                if (d.OwnerAlive == null) d.OwnerAlive = () => owner != null && owner.activeInHierarchy;
            }
            // logged, and not thrown on: the dialog has ended either way (a second Show of the same dialog doesn't log twice)
            d.ActionFailed -= LogFailure;
            d.ActionFailed += LogFailure;
            // its description's label as shown (the title may be a '#key')
            d.Label = KodeWindow.Loc(d.Title);
            Host.Make();
            bool shown = Queue.Show(d);
            Host.Follow();
            return shown;
        }

        static void Open(DialogModel d)
        {
            // the keyboard leaves any field: its edit ends (its scope closes before the dialog's opens)
            EventSystem es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject != null && !es.alreadySelecting) es.SetSelectedGameObject(null);
            Host.Keys.Lock();
            PopupLayer layer = Layer = Popups.Push(d.Owner, true);
            // a scene change: every dialog goes, the waiting ones too (the other ways out are the dialog's own: its model ends it)
            layer.Dismissed += reason =>
            {
                if (reason == PopupDismiss.SceneChange) Queue.DismissAll();
                else if (reason == PopupDismiss.OwnerClosed) d.Dismiss();
            };
            try
            {
                View = new DialogView(d, d.Owner as GameObject);
                Log(d, "shown");
            }
            catch (Exception e)
            {
                Debug.LogError("[KodeKit] a dialog could not be drawn, dismissed:");
                Debug.LogException(e);
                View?.Destroy();
                View = null;
                d.Dismiss();
            }
            Host.Follow();
        }

        static void Hide(DialogModel d)
        {
            Log(d, "ended: " + d.Result + " (" + d.ResultSource + ")");
            PopupLayer layer = Layer;
            Layer = null;
            layer?.Dismiss(d.Result == DialogResult.Dismissed ? PopupDismiss.OwnerClosed : PopupDismiss.Program);
            if (View != null && View.Model == d)
            {
                View.Destroy();
                View = null;
            }
            // the lock waits for the key that ended it; the next dialog, if any, takes the scope again
            Host.Keys.UnlockAfterKeys();
            Host.Follow();
        }

        static void LogFailure(DialogModel d, Exception e)
        {
            Debug.LogError($"[KodeKit] the action of dialog \"{KodeWindow.Loc(d.Title)}\" ({d.Result}) threw; the dialog has ended:");
            Debug.LogException(e);
        }

        static void Log(DialogModel d, string what) => Debug.Log($"[KodeKit] dialog \"{KodeWindow.Loc(d.Title)}\" {what}");

        /// <summary>The dialogs' object: their keyboard scope, which outlives each dialog, and their per-frame driver.</summary>
        sealed class Host : MonoBehaviour
        {
            static Host instance;

            public static KeyboardScope Keys => instance.keys;

            KeyboardScope keys;

            public static void Make()
            {
                if (instance != null) return;
                var go = new GameObject("KodeKitDialogs");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<Host>();
                instance.keys = go.AddComponent<KeyboardScope>();
                KeyScope scope = instance.keys.Configure(ScopeKeys.Enter | ScopeKeys.Escape | ScopeKeys.Tab | ScopeKeys.Left | ScopeKeys.Right, true);
                scope.Key += k =>
                {
                    bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    Queue.Current?.Key(k, shift, KodeWindow.Source(ChangeSource.Keyboard));
                };
                // Escape closed the scope: the dialog is cancelled (its lock waits for the key's release)
                scope.Dismissed += () => Queue.Current?.Cancel(KodeWindow.Source(ChangeSource.Keyboard));
                instance.enabled = false;
            }

            /// <summary>Runs per frame only while a dialog is shown or waiting.</summary>
            public static void Follow()
            {
                if (instance != null && instance.enabled != Queue.Busy) instance.enabled = Queue.Busy;
            }

            void Update()
            {
                Queue.Tick();
                View?.Sync();
                Follow();
            }
        }
    }

    /// <summary>A <see cref="DialogModel"/> as drawn: its canvas, blocker, window, buttons and prompt field.</summary>
    public sealed class DialogView
    {
        public DialogModel Model { get; }

        /// <summary>The dialog's own canvas (on <see cref="Overlay.Root"/>).</summary>
        public GameObject Overlay { get; }

        /// <summary>The full-screen blocker behind the window.</summary>
        public GameObject Blocker { get; }

        public KodeWindow Window { get; }

        /// <summary>The prompt's field (null: no input).</summary>
        public TMP_InputField Field { get; }

        readonly Dictionary<DialogStop, UIButton> buttons = new Dictionary<DialogStop, UIButton>();
        readonly Dictionary<DialogStop, StyledPart> frames = new Dictionary<DialogStop, StyledPart>();
        readonly GameObject owner;
        readonly RectTransform canvasRt;
        float scale = 1;
        int shownVersion = int.MinValue;
        Vector2 placedSize = new Vector2(-1, -1);

        /// <summary>The button of a stop (null: the dialog has none).</summary>
        public UIButton Button(DialogStop stop) => buttons.TryGetValue(stop, out UIButton b) ? b : null;

        internal DialogView(DialogModel d, GameObject owner)
        {
            Model = d;
            this.owner = owner;
            Canvas root = KodeKit.Overlay.Root;
            canvasRt = (RectTransform)root.transform;
            int layer = root.gameObject.layer; // UIMainCamera culls other layers

            Overlay = new GameObject("KodeKitDialog", typeof(RectTransform)) { layer = layer };
            var rt = (RectTransform)Overlay.transform;
            rt.SetParent(canvasRt, false);
            Stretch(rt);
            KodeKit.Overlay.Setup(Overlay.AddComponent<Canvas>(), root, Dialogs.SortingOrder);
            Overlay.AddComponent<GraphicRaycaster>();

            Blocker = new GameObject("Blocker", typeof(RectTransform)) { layer = layer };
            var brt = (RectTransform)Blocker.transform;
            brt.SetParent(rt, false);
            Stretch(brt);
            Blocker.AddComponent<Image>().color = new Color(0, 0, 0, 0.35f);

            Window = new KodeWindow(rt, d.Title, 340, Vector2.zero);
            KodeWindow w = Window;
            // the owner's theme (a custom window's own Compact setting), set before the widgets are made
            w.Behaviour.ThemeOverride = () => owner != null ? KodeWindow.ThemeOf(owner) : null;
            w.Root.name = "Dialog";
            w.TitleRow.GetComponent<DragHandle>().Enabled = () => false;
            w.BodyDrag.Enabled = () => false;
            // no close button: Cancel is the way out, and the title keeps the whole width
            w.CloseButton.gameObject.SetActive(false);
            // no title menu either: its Close would be a way out besides Cancel, and the menu would draw under the dialog
            w.TitleMenu.enabled = false;

            if (!string.IsNullOrEmpty(d.Message)) KodeWindow.Paragraph(w.Root, d.Message);

            if (d.HasInput)
            {
                Layout row = KodeWindow.Row(w.Root);
                // a Tab typed as the field hands the focus on is not part of the text
                Field = EditFields.Field(w, row, () => d.Input, s => d.Input = s.IndexOf('\t') < 0 ? s : s.Replace("\t", ""), 300, () => d.Valid, kind: FieldKind.Text);
                TMP_InputField field = Field;
                // a click in the field puts the model's focus there
                field.onSelect.AddListener(_ => d.Focus(DialogStop.Input));
                // the focused field's scope covers the dialog's: it routes Tab (Shift: back) on to the model
                field.GetComponent<FieldKeyboardLock>().Route(ScopeKeys.Tab, _ =>
                {
                    if (field != null && field.isFocused) d.Key(ScopeKeys.Tab, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift), KodeWindow.Source(ChangeSource.Keyboard));
                });
                // KSP's TMP sends onSubmit for Escape too, with wasCanceled set
                field.onSubmit.AddListener(_ =>
                {
                    if (!field.wasCanceled) d.Press(DialogStop.Input, KodeWindow.Source(ChangeSource.Keyboard));
                });
                // the field's Escape puts its text back and ends its edit: it cancels the dialog too, in one key
                field.onEndEdit.AddListener(_ =>
                {
                    if (field.wasCanceled) d.Cancel(KodeWindow.Source(ChangeSource.Keyboard));
                });
            }

            Layout buttonRow = KodeWindow.Row(w.Root);
            KodeWindow.FlexibleSpace(buttonRow);
            IReadOnlyList<DialogStop> stops = d.Stops;
            for (int i = 0; i < stops.Count; i++)
            {
                DialogStop stop = stops[i];
                if (stop == DialogStop.Input) continue;
                string text = stop == DialogStop.Confirm ? d.ConfirmText : stop == DialogStop.Other ? d.OtherText : d.CancelText;
                UIButton b = KodeWindow.Button(buttonRow, text, () => d.Press(stop, KodeWindow.Pointer));
                if (stop == DialogStop.Confirm) KodeWindow.Role(b, d.Destructive ? ButtonRole.Danger : ButtonRole.Primary);
                buttons[stop] = b;
                // the keyboard focus: the field's frame, 2 px outside the button
                frames[stop] = StyledPart.FocusFrame(b.transform, 2);
            }

            // the window and its title were styled before the theme override was set
            w.Behaviour.Restyle();

            FollowScale();

            if (d.HasInput && d.Focused == DialogStop.Input && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(Field.gameObject);
            LayoutRebuilder.ForceRebuildLayoutImmediate(w.Root.rectTransform);
            Sync();
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // the owner's scale (MechJeb's UI scale), followed when it changes while the dialog is open
        void FollowScale()
        {
            float canvasScale = canvasRt != null ? canvasRt.lossyScale.x : 0;
            float s = owner != null && canvasScale > 0 ? owner.transform.lossyScale.x / canvasScale : 1;
            if (s <= 0) s = 1;
            if (Mathf.Abs(s - scale) < 1e-4f && Mathf.Abs(Window.Behaviour.Scale - s) < 1e-4f) return;
            scale = s;
            Window.Behaviour.Scale = s;
            placedSize = new Vector2(-1, -1);
            placed = false;
        }

        /// <summary>
        /// Once a frame: the window's bindings, the focus and the confirm button's state, the owner's scale, the place in the middle of
        /// the screen.
        /// </summary>
        internal void Sync()
        {
            if (Window.Root == null) return;
            Window.Sync();
            FollowScale();
            if (shownVersion != Model.Version)
            {
                shownVersion = Model.Version;
                DialogStop focused = Model.Focused;
                FollowFocus(focused);
                foreach (KeyValuePair<DialogStop, StyledPart> f in frames)
                    if (f.Value != null)
                        f.Value.Set(Headless.StyleFlags.Focus, f.Key == focused);
                UIButton confirm = Button(DialogStop.Confirm);
                if (confirm != null)
                {
                    var button = confirm.GetComponent<UnityEngine.UI.Button>();
                    if (button.interactable != Model.CanConfirm) button.interactable = Model.CanConfirm;
                }
            }
            RectTransform rt = Window.Root.rectTransform;
            Vector2 size = rt.rect.size;
            if (size == placedSize) return;
            placedSize = size;
            // a window someone else moved (a test placing its shot) stays where it was put
            if (placed && rt.localPosition != placedAt) return;
            // centred, on whole units
            PopupPlacement at = PopupPlacer.Centre(size.x, size.y, Popups.Box(((RectTransform)rt.parent).rect), scale);
            Placement = at;
            Popups.Put(rt, at.Box);
            placedAt = rt.localPosition;
            Dialogs.Layer?.Place(at.Box);
            placed = true;
        }

        bool placed;
        Vector3 placedAt;

        /// <summary>Where the dialog was last centred.</summary>
        public PopupPlacement Placement { get; private set; }

        // the field has the keyboard while the model's focus is on the input, and lets it go when the focus moves to a button
        void FollowFocus(DialogStop focused)
        {
            EventSystem es = EventSystem.current;
            if (Field == null || es == null || es.alreadySelecting) return;
            bool selected = es.currentSelectedGameObject == Field.gameObject;
            if (focused == DialogStop.Input && !selected) es.SetSelectedGameObject(Field.gameObject);
            else if (focused != DialogStop.Input && selected) es.SetSelectedGameObject(null);
        }

        internal void Destroy()
        {
            if (Overlay != null) UnityEngine.Object.Destroy(Overlay);
        }
    }
}

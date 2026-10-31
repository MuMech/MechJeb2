using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace KodeUI
{
    public class Titlebar : Layout, ILayoutElement, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        RectTransform canvasRectTransform;
        RectTransform windowRect;
        Vector2 dragAnchor;
        Vector2 preferredSize;
        UIText titleText;

        public override void CreateUI()
        {
            base.CreateUI();

            Anchor(AnchorPresets.HorStretchTop);
            SizeDelta(0, 20);

            gameObject.AddComponent<Touchable>();

            Canvas canvas = GetComponentInParent<Canvas>();
            canvasRectTransform = canvas.GetComponent<RectTransform>();

            preferredSize = new Vector2 (-1, -1);
        }

        public override void Style()
        {
            base.Style();
        }

        public Titlebar Window(UIObject window)
        {
            windowRect = window.rectTransform;
            return this;
        }

        public Titlebar Title (string title)
        {
            if (titleText) {
                titleText.Text (title);
            } else {
                Add<UIText>(out titleText, "TitleText").Text(title).Alignment(TextAlignmentOptions.Top).Anchor(AnchorPresets.StretchAll).SizeDelta(0, 0).FlexibleLayout(true, false).Pivot(PivotPresets.TopCenter).BlocksRaycasts(false).Finish();
                titleText.tmpText.raycastTarget = false;
            }
            return this;
        }

        void DoDrag(PointerEventData eventData)
        {
            Vector2 currentPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out currentPos);
            Vector3 localDelta = currentPos - dragAnchor;
            windowRect.localPosition += localDelta;
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            DoDrag(eventData);
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            DoDrag(eventData);
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            DoDrag(eventData);
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (windowRect) {
                if (eventData.button == PointerEventData.InputButton.Left) {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out dragAnchor);
                    windowRect.SetAsLastSibling();
                } else if (eventData.button == PointerEventData.InputButton.Middle) {
                    windowRect.SetAsFirstSibling();
                }
            }
        }

        public float flexibleWidth { get { return 1; } }
        public float flexibleHeight { get { return -1; } }
        public float preferredWidth { get { return preferredSize.y; } }
        public float preferredHeight { get { return preferredSize.y; } }
        public float minWidth { get { return preferredSize.y; } }
        public float minHeight { get { return preferredSize.y; } }
        public int layoutPriority { get { return 0; } }
        public void CalculateLayoutInputHorizontal()
        {
            if (titleText) {
                preferredSize = titleText.tmpText.GetPreferredValues ();
            }
        }
        public void CalculateLayoutInputVertical()
        {
            if (titleText) {
                preferredSize = titleText.tmpText.GetPreferredValues ();
            }
        }
    }
}

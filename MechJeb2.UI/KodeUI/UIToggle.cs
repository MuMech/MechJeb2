using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace KodeUI
{
    public class UIToggle : UIObject
    {
        protected UIImage checkMark;
        protected Image image;
        protected Toggle toggle;

        public UIImage CheckMark { get { return checkMark; } }
        public Image Image { get { return image; } }

        public bool interactable
        {
            get { return toggle.interactable; }
            set { toggle.interactable = value; }
        }

        public bool isOn
        {
            get { return toggle.isOn; }
            set { toggle.isOn = value; }
        }

        public override void CreateUI()
        {
            Pivot(PivotPresets.MiddleCenter);
            toggle = gameObject.AddComponent<Toggle>();

            image = gameObject.AddComponent<Image>();
            toggle.targetGraphic = image;

            Add<UIImage>(out checkMark, "CheckMark").Anchor(AnchorPresets.StretchAll).Pivot(PivotPresets.MiddleCenter).SizeDelta(0, 0).Finish();
            toggle.graphic = checkMark.image;
        }

        public UIToggle OnValueChanged(UnityAction<bool> action)
        {
            toggle.onValueChanged.AddListener(action);
            return this;
        }

        public override void Style()
        {
            image.sprite = style.sprite;
            image.color = style.color ?? UnityEngine.Color.white;
            image.type = style.type ?? UnityEngine.UI.Image.Type.Sliced;

            toggle.colors = style.stateColors ?? ColorBlock.defaultColorBlock;
            toggle.transition = style.transition ?? Selectable.Transition.ColorTint;
            if (style.stateSprites.HasValue) {
                toggle.spriteState = style.stateSprites.Value;
            }

        }

        public UIToggle SetIsOnWithoutNotify(bool on)
        {
            toggle.SetIsOnWithoutNotify(on);
            return this;
        }

        public UIToggle Group(ToggleGroup group)
        {
            toggle.group = group;
            return this;
        }

		public void OnPointerClick(PointerEventData eventData)
		{
			toggle.OnPointerClick (eventData);
		}
    }
}

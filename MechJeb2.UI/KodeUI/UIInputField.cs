using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace KodeUI
{
	public class UIInputField : UIObject, ILayoutElement
	{
		Image background;
		TMP_InputField inputField;
		UIRectMask textArea;
		UIText childText;
		UIText childPlaceholder;

		Vector2 minSize;
		Vector2 preferredSize;

		public void CalculateLayoutInputHorizontal()
		{
			var taRect = textArea.rectTransform;
			float cm = LayoutUtility.GetMinSize(childText.rectTransform, 0);
			float cp = LayoutUtility.GetPreferredSize(childText.rectTransform, 0);
			minSize.x = cm + taRect.offsetMin.x - taRect.offsetMax.x;
			preferredSize.x = cp + taRect.offsetMin.x - taRect.offsetMax.x;
		}

		public void CalculateLayoutInputVertical()
		{
			var taRect = textArea.rectTransform;
			float cm = LayoutUtility.GetMinSize(childText.rectTransform, 1);
			float cp = LayoutUtility.GetPreferredSize(childText.rectTransform, 1);
			minSize.y = cm + taRect.offsetMin.y - taRect.offsetMax.y;
			preferredSize.y = cp + taRect.offsetMin.y - taRect.offsetMax.y;
		}

		public int layoutPriority { get { return 0; } }
		public float minWidth { get { return minSize.x; } }
		public float preferredWidth { get { return preferredSize.x; } }
		public float flexibleWidth	{ get { return -1; } }
		public float minHeight { get { return minSize.y; } }
		public float preferredHeight { get { return preferredSize.y; } }
		public float flexibleHeight  { get { return -1; } }

		public bool interactable
		{
			get { return inputField.interactable; }
			set { inputField.interactable = value; }
		}

		public string text
		{
			get { return inputField.text; }
			set { inputField.text = value; }
		}

		public UIInputField Text (string txt)
		{
			text = txt;
			return this;
		}

		public override void CreateUI()
		{
			Add<UIRectMask>(out textArea, "Text Area")
				.Anchor(Vector2.zero, Vector2.one)
				.SizeDelta(0, 0)
				.Offset(new Vector2(10, 6), new Vector2(-10, -7))
				.Add<UIText>(out childPlaceholder, "Placeholder")
					.Text("Enter text...")
					.Anchor(Vector2.zero, Vector2.one)
					.Offset(Vector2.zero, Vector2.zero)
					.Finish()
				.Add<UIText>(out childText, "Text")
					.Text("")
					.Anchor(Vector2.zero, Vector2.one)
					.Offset(Vector2.zero, Vector2.zero)
					.Finish()
				;

			childPlaceholder.tmpText.enableWordWrapping = false;
			childPlaceholder.tmpText.extraPadding = true;
			childText.tmpText.enableWordWrapping = false;
			childText.tmpText.extraPadding = true;
			childText.tmpText.richText = true;

			background = gameObject.AddComponent<Image>();

			inputField = gameObject.AddComponent<TMP_InputField>();
			inputField.textViewport = textArea.rectTransform;
			inputField.textComponent = childText.tmpText;
			inputField.placeholder = childPlaceholder.tmpText;

			if (gameObject.activeInHierarchy) {
				SetActive(false);
				SetActive(true);
			}
		}

		public override void Style()
		{
			background.sprite = style.sprite;
			background.color = style.color ?? UnityEngine.Color.white;
			background.type = style.type ?? Image.Type.Sliced;
		}

		public UIInputField CaretBlinkRate (float rate)
		{
			inputField.caretBlinkRate = rate;
			return this;
		}

		public UIInputField CaretWidth (int width)
		{
			inputField.caretWidth = width;
			return this;
		}

		public UIInputField Placeholder (Graphic graphic)
		{
			inputField.placeholder = graphic;
			return this;
		}

		public UIInputField CaretColor (Color color)
		{
			inputField.caretColor = color;
			return this;
		}

		public UIInputField SelectionColor (Color color)
		{
			inputField.selectionColor = color;
			return this;
		}

		public UIInputField OnEndEdit (UnityAction<string> evt)
		{
			inputField.onEndEdit.AddListener(evt);
			return this;
		}

		public UIInputField OnSubmit (UnityAction<string> evt)
		{
			inputField.onSubmit.AddListener(evt);
			return this;
		}

		public UIInputField OnFocusGained (UnityAction<string> evt)
		{
			inputField.onSelect.AddListener(evt);
			return this;
		}

		public UIInputField OnFocusLost (UnityAction<string> evt)
		{
			inputField.onDeselect.AddListener(evt);
			return this;
		}

		public UIInputField OnValueChanged (UnityAction<string> evt)
		{
			inputField.onValueChanged.AddListener(evt);
			return this;
		}

		public UIInputField OnValidateInput (TMP_InputField.OnValidateInput evt)
		{
			inputField.onValidateInput = evt;
			return this;
		}

		public UIInputField CharacterLimit (int limit)
		{
			inputField.characterLimit = limit;
			return this;
		}

		public UIInputField ContentType (TMP_InputField.ContentType type)
		{
			inputField.contentType = type;
			return this;
		}

		public UIInputField LineType (TMP_InputField.LineType type)
		{
			inputField.lineType = type;
			return this;
		}

		public UIInputField InputType (TMP_InputField.InputType type)
		{
			inputField.inputType = type;
			return this;
		}
	}
}

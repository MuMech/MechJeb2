using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace KodeUI
{
	public class UIInputField : Layout
	{
		private TMP_InputField inputField;
		UIRectMask textArea;
		UIText childText;
		UIText childPlaceholder;

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
			Horizontal().ChildForceExpand(false, false).ControlChildSize(true, true).PreferredSizeFitter(true,true).Pivot(PivotPresets.TopLeft);

			Add<UIRectMask>(out textArea, "Text Area")
				.Horizontal().ChildForceExpand(false, false).ControlChildSize(true, true).PreferredSizeFitter(true,true).Pivot(PivotPresets.TopLeft)
				.Anchor(Vector2.zero, Vector2.one)
				.Offset(new Vector2(10, 6), new Vector2(-10, -7))
				.Width(0).Height(0)
				.Add<UIText>(out childPlaceholder, "Placeholder")
					.Text("Enter text...")
					.Anchor(Vector2.zero, Vector2.one)
					.Offset(Vector2.zero, Vector2.zero)
					.Width(0).Height(0)
					.Finish()
				.Add<UIText>(out childText, "Text")
					.Text("")
					.Anchor(Vector2.zero, Vector2.one)
					.Offset(Vector2.zero, Vector2.zero)
					.Width(0).Height(0)
					.Finish()
				.Finish();

			childPlaceholder.tmpText.enableWordWrapping = false;
			childPlaceholder.tmpText.extraPadding = true;
			childText.tmpText.enableWordWrapping = false;
			childText.tmpText.extraPadding = true;
			childText.tmpText.richText = true;

			inputField = gameObject.AddComponent<TMP_InputField>();
			inputField.textViewport = textArea.rectTransform;
			inputField.textComponent = childText.tmpText;
			inputField.placeholder = childPlaceholder.tmpText;
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

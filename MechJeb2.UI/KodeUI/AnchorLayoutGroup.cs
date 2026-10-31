using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.AnimatedValues;
#endif

namespace KodeUI
{
	[System.Serializable]
    public class AnchorLayoutGroup : HorizontalOrVerticalLayoutGroup
    {
		[SerializeField] bool m_doMinWidth = false;
		[SerializeField] bool m_doMinHeight = false;
		[SerializeField] bool m_doPreferredWidth = false;
		[SerializeField] bool m_doPreferredHeight = false;
		[SerializeField] bool m_doFlexibleWidth = false;
		[SerializeField] bool m_doFlexibleHeight = false;

		public bool doMinWidth { get { return m_doMinWidth; } set { m_doMinWidth = value; } }
		ILayoutElement m_minWidthSource;
		public bool doMinHeight { get { return m_doMinHeight; } set { m_doMinHeight = value; } }
		ILayoutElement m_minHeightSource;
		public bool doPreferredWidth { get { return m_doPreferredWidth; } set { m_doPreferredWidth = value; } }
		ILayoutElement m_preferredWidthSource;
		public bool doPreferredHeight { get { return m_doPreferredHeight; } set { m_doPreferredHeight = value; } }
		ILayoutElement m_preferredHeightSource;
		public bool doFlexibleWidth { get { return m_doFlexibleWidth; } set { m_doFlexibleWidth = value; } }
		ILayoutElement m_flexibleWidthSource;
		public bool doFlexibleHeight { get { return m_doFlexibleHeight; } set { m_doFlexibleHeight = value; } }
		ILayoutElement m_flexibleHeightSource;

		/*float _minWidth;
		float _preferredWidth;
		float _flexibleWidth;
		float _minHeight;
		float _preferredHeight;
		float _flexibleHeight;
		int _layoutPriority;*/

		//public override void CalculateLayoutInputHorizontal() { }
		public override void CalculateLayoutInputVertical()
		{
			CalcAlongAxis(1, false);
		}

		float GetMaxProperty (System.Func<ILayoutElement, float> property, out ILayoutElement source)
		{
			source = null;
			float maxVal = -1; // negative considered to be off
			for (int i = 0; i < rectChildren.Count; i++) {
				ILayoutElement newSource;
				var rect = rectChildren[i];
				float newVal = LayoutUtility.GetLayoutProperty (rect, property, -1, out newSource);
				if (newVal > maxVal) {
					maxVal = newVal;
					source = newSource;
				}
			}
			return maxVal;
		}

		public override float minWidth
		{
			get { return m_doMinWidth ? GetMaxProperty (e => e.minWidth, out m_minWidthSource) : -1; }
		}

		public override float preferredWidth
		{
			get { return m_doPreferredWidth ? GetMaxProperty (e => e.preferredWidth, out m_preferredWidthSource) : -1; }
		}

		public override float flexibleWidth
		{
			get { return m_doFlexibleWidth ? GetMaxProperty (e => e.flexibleWidth, out m_flexibleWidthSource) : -1; }
		}

		public override float minHeight
		{
			get { return m_doMinHeight ? GetMaxProperty (e => e.minHeight, out m_minHeightSource) : -1; }
		}

		public override float preferredHeight
		{
			get { return m_doPreferredHeight ? GetMaxProperty (e => e.preferredHeight, out m_preferredHeightSource) : -1; }
		}

		public override float flexibleHeight
		{
			get { return m_doFlexibleHeight ? GetMaxProperty (e => e.flexibleHeight, out m_flexibleHeightSource) : -1; }
		}

		public override void SetLayoutHorizontal() { }
		public override void SetLayoutVertical() { }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(AnchorLayoutGroup), true)]
    [CanEditMultipleObjects]
    public class AnchorLayoutGroupEditor : Editor
    {
        SerializedProperty m_doMinWidth;
        SerializedProperty m_doMinHeight;
        SerializedProperty m_doPreferredWidth;
        SerializedProperty m_doPreferredHeight;
        SerializedProperty m_doFlexibleWidth;
        SerializedProperty m_doFlexibleHeight;

        protected virtual void OnEnable()
        {
            m_doMinWidth = serializedObject.FindProperty("m_doMinWidth");
            m_doMinHeight = serializedObject.FindProperty("m_doMinHeight");
            m_doPreferredWidth = serializedObject.FindProperty("m_doPreferredWidth");
            m_doPreferredHeight = serializedObject.FindProperty("m_doPreferredHeight");
            m_doFlexibleWidth = serializedObject.FindProperty("m_doFlexibleWidth");
            m_doFlexibleHeight = serializedObject.FindProperty("m_doFlexibleHeight");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

			LayoutElementField(m_doMinWidth);
			LayoutElementField(m_doMinHeight);
			LayoutElementField(m_doPreferredWidth);
			LayoutElementField(m_doPreferredHeight);
			LayoutElementField(m_doFlexibleWidth);
			LayoutElementField(m_doFlexibleHeight);

            serializedObject.ApplyModifiedProperties();
        }

        void LayoutElementField(SerializedProperty property)
        {
            Rect position = EditorGUILayout.GetControlRect();

            // Label
            GUIContent label = EditorGUI.BeginProperty(position, null, property);

            // Rects
            Rect fieldPosition = EditorGUI.PrefixLabel(position, label);

            Rect toggleRect = fieldPosition;
            toggleRect.width = 16;

            Rect floatFieldRect = fieldPosition;
            floatFieldRect.xMin += 16;

            // Checkbox
			EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUI.ToggleLeft(toggleRect, GUIContent.none, property.boolValue);
			if (EditorGUI.EndChangeCheck()) {
				property.boolValue = enabled;
			}

            EditorGUI.EndProperty();
        }
    }
#endif
}

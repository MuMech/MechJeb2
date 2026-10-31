// From Fattie on stackoverflow https://stackoverflow.com/questions/36888780/how-to-make-an-invisible-transparent-button-work
// but with some mods
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeUI
{

#if UNITY_EDITOR
	using UnityEditor;

	[CustomEditor(typeof(Touchable))]
	public class Touchable_Editor : Editor
	{
		public override void OnInspectorGUI(){}
	}
#endif

	public class Touchable:Graphic
	{
		protected override void UpdateGeometry() { }
	}
}

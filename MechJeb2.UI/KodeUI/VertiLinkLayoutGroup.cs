// From Fattie on stackoverflow https://stackoverflow.com/questions/36888780/how-to-make-an-invisible-transparent-button-work
// but with some mods
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace KodeUI
{
	public class VertiLinkLayoutGroup : VerticalLayoutGroup
	{
		public override void CalculateLayoutInputVertical()
		{
			var groups = GetComponentsInChildren<LayoutGroup>();
			var texts = GetComponentsInChildren<TextMeshProUGUI>();
			for (int i = 0; i < texts.Length; i++) {
				if (texts[i].overflowMode != TextOverflowModes.Linked
					|| texts[i].isLinkedTextComponent) {
					continue;
				}
				for (var t = texts[i]; t; t = t.linkedTextComponent as TextMeshProUGUI) {
					t.Rebuild(CanvasUpdate.PreRender);
				}
			}
			for (int i = 0; i < groups.Length; i++) {
				var g = groups[i];
				if (g == this) {
					continue;
				}
				g.CalculateLayoutInputVertical();
			}
			base.CalculateLayoutInputVertical();
		}
	}
}

using UnityEngine;
using UnityEngine.UI;

namespace KodeUI
{
	public class LayoutDebug : MonoBehaviour
	{
		public float minWidth { get; private set; }
		public ILayoutElement minWidthSource { get; private set; }

		public float preferredWidth { get; private set; }
		public ILayoutElement preferredWidthSource { get; private set; }

		public float flexibleWidth { get; private set; }
		public ILayoutElement flexibleWidthSource { get; private set; }


		public float minHeight { get; private set; }
		public ILayoutElement minHeightSource { get; private set; }

		public float preferredHeight { get; private set; }
		public ILayoutElement preferredHeightSource { get; private set; }

		public float flexibleHeight { get; private set; }
		public ILayoutElement flexibleHeightSource { get; private set; }

		void UpdateValues ()
		{
			ILayoutElement source;

			RectTransform rect = transform as RectTransform;
			if (rect == null) {
				return;
			}
			minWidth = LayoutUtility.GetLayoutProperty(rect, e => e.minWidth, 0, out source);
			minWidthSource = source;

			preferredWidth = LayoutUtility.GetLayoutProperty(rect, e => e.preferredWidth, 0, out source);
			preferredWidthSource = source;

			flexibleWidth = LayoutUtility.GetLayoutProperty(rect, e => e.flexibleWidth, 0, out source);
			flexibleWidthSource = source;

			minHeight = LayoutUtility.GetLayoutProperty(rect, e => e.minHeight, 0, out source);
			minHeightSource = source;

			preferredHeight = LayoutUtility.GetLayoutProperty(rect, e => e.preferredHeight, 0, out source);
			preferredHeightSource = source;

			flexibleHeight = LayoutUtility.GetLayoutProperty(rect, e => e.flexibleHeight, 0, out source);
			flexibleHeightSource = source;
		}

		void Start ()
		{
			UpdateValues ();
		}

		void LateUpdate ()
		{
			UpdateValues ();
		}

		public void LogValues ()
		{
			Debug.Log("[LayoutDebug] minWidth: {minWidth} from {minWidthSource}");
			Debug.Log("[LayoutDebug] preferredWidth: {preferredWidth} from {preferredWidthSource}");
			Debug.Log("[LayoutDebug] flexibleWidth: {flexibleWidth} from {flexibleWidthSource}");
			Debug.Log("[LayoutDebug] minHeight: {minHeight} from {minHeightSource}");
			Debug.Log("[LayoutDebug] preferredHeight: {preferredHeight} from {preferredHeightSource}");
			Debug.Log("[LayoutDebug] flexibleHeight: {flexibleHeight} from {flexibleHeightSource}");
		}
	}
}

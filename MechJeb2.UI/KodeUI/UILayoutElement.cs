using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KodeUI
{
    public abstract class UILayoutElement : UIObject, ILayoutElement, ILayoutIgnorer
    {
		public virtual void CalculateLayoutInputHorizontal() { }
		public virtual void CalculateLayoutInputVertical() { }

		bool _ignoreLayout;
		public virtual bool ignoreLayout
		{
			get { return _ignoreLayout; }
			set {
				_ignoreLayout = value;
				SetDirty();
			}
		}

		float _minWidth;
		public virtual float minWidth
		{
			get { return _minWidth; }
			set {
				_minWidth = value;
				SetDirty();
			}
		}

		float _preferredWidth;
		public virtual float preferredWidth
		{
			get { return _preferredWidth; }
			set {
				_preferredWidth = value;
				SetDirty();
			}
		}

		float _flexibleWidth;
		public virtual float flexibleWidth
		{
			get { return _flexibleWidth; }
			set {
				_flexibleWidth = value;
				SetDirty();
			}
		}

		float _minHeight;
		public virtual float minHeight
		{
			get { return _minHeight; }
			set {
				_minHeight = value;
				SetDirty();
			}
		}

		float _preferredHeight;
		public virtual float preferredHeight
		{
			get { return _preferredHeight; }
			set {
				_preferredHeight = value;
				SetDirty();
			}
		}

		float _flexibleHeight;
		public virtual float flexibleHeight
		{
			get { return _flexibleHeight; }
			set {
				_flexibleHeight = value;
				SetDirty();
			}
		}

		int _layoutPriority;
		public virtual int layoutPriority
		{
			get { return _layoutPriority; }
			set {
				_layoutPriority = value;
				SetDirty();
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			SetDirty();
		}

		protected override void OnTransformParentChanged()
		{
			SetDirty();
		}

		protected override void OnDisable()
		{
			SetDirty();
			base.OnDisable();
		}

		protected override void OnDidApplyAnimationProperties()
		{
			SetDirty();
		}

		protected override void OnBeforeTransformParentChanged()
		{
			SetDirty();
		}
#if UNITY_EDITOR
		protected override void OnValidate()
		{
			SetDirty();
		}
#endif

		protected void SetDirty()
		{
			if (!IsActive()) {
				return;
			}
			LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
		}
    }
}

using System;
using UnityEngine;

namespace KodeUI
{
    public class UIKit
    {
        public static T CreateUI<T>(RectTransform rectTransform, string id) where T : UIObject
        {
            var childObj = new GameObject(id, typeof(RectTransform));
            childObj.layer = rectTransform.gameObject.layer;
            childObj.transform.SetParent(rectTransform, true);
            RectTransform rect = childObj.GetComponent<RectTransform>();
            T child = childObj.AddComponent<T>();

            rect.anchoredPosition3D = Vector3.zero;

            child.SetupStyle();
            child.CreateUI();
            return child;
        }

        public interface IListObject
        {
            int Count { get; }
            RectTransform RectTransform { get; }
            void Create (int index);
            void Update (GameObject obj, int index);
        }

        public static void UpdateListContent(IListObject listObject)
        {
            var listRect = listObject.RectTransform;
            int childCount = listRect.childCount;
            int childIndex = 0;
            int itemCount = listObject.Count;
            int itemIndex = 0;

            while (childIndex < childCount && itemIndex < itemCount) {
                var go = listRect.GetChild(childIndex++).gameObject;
                listObject.Update (go, itemIndex++);
            }
            while (childIndex < childCount) {
                var go = listRect.GetChild(childIndex++).gameObject;
                UnityEngine.Object.Destroy(go);
            }
            while (itemIndex < itemCount) {
                listObject.Create (itemIndex++);
            }
        }
    }
}

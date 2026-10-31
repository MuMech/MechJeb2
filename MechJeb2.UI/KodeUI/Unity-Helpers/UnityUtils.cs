// From https://github.com/mikecann/Unity-Helpers

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityHelpers
{
    public class UnityUtils
    {
        public static List<SpriteRenderer> GetSpriteRenderersUnderScreenPoint(Vector2 point)
        {
            var ray = Camera.main.ScreenPointToRay(point);
            var hits = new List<RaycastHit2D>(Physics2D.GetRayIntersectionAll(ray, Mathf.Infinity));
            var allRenderers = hits.ConvertAll(hit => hit.transform.gameObject.GetComponent<SpriteRenderer>());
            allRenderers.RemoveAll(r => r == null);

            var layers = new Dictionary<int, List<SpriteRenderer>>();
            allRenderers.ForEach(r =>
            {
                if (!layers.ContainsKey(r.sortingLayerID)) layers[r.sortingLayerID] = new List<SpriteRenderer>();
                layers[r.sortingLayerID].Add(r);
            });

            var sortedRenderers = new List<SpriteRenderer>();
            var sortedLayers = new List<int>(layers.Keys);
            sortedLayers.Sort((a, b) => b - a);
            sortedLayers.ForEach(key => layers[key].Sort((a, b) => b.sortingOrder - a.sortingOrder));
            sortedLayers.ForEach(key => sortedRenderers.AddRange(layers[key]));

            return sortedRenderers;
        }
        
        public static GameObject GetObjectUnderScreenPoint(Vector2 screenPos)
        {
            var r = GetSpriteRenderersUnderScreenPoint(screenPos);
            if (r.Count == 0) return null;
            return r[0].gameObject;
        }

        public static Texture2D MakeTexture2D(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }


        /// <summary>
        /// Copies a component using reflection into a destination. 
        /// Borrowed from: http://answers.unity3d.com/questions/458207/copy-a-component-at-runtime.html
        /// </summary>
        /// <param name="original">The original component to copy from</param>
        /// <param name="destination">The destination object to copy the componet to</param>
        /// <param name="illegalDeclaringTypes">A List of declaring types from which the property or field should not be copied from</param>
        /// <param name="illegalDeclaringTypes">If attribute is defined on property then ignore property</param>
        /// <returns></returns>
        public static Component CopyComponent(Component original, GameObject destination, Type ignoreAttribute, List<Type> illegalDeclaringTypes)
        {
            var type = original.GetType();
            var copy = destination.AddComponent(type);

            foreach (var field in type.GetFields())
            {
                if (illegalDeclaringTypes.Contains(field.DeclaringType)) continue;     
                field.SetValue(copy, field.GetValue(original));
            }

            foreach (var property in type.GetProperties())
            {
                if (property == null) continue;
                if (!property.CanWrite) continue;
                if (Attribute.IsDefined(property, ignoreAttribute)) continue;
                if(illegalDeclaringTypes.Contains(property.DeclaringType)) continue;               
                property.SetValue(copy, property.GetValue(original, null), null);
            }

            return copy;
        }        

        public static T Instantiate<T>() where T : Component
        {
            var obj = new GameObject(typeof(T).Name);
            return obj.AddComponent<T>();
        }

        public static T Instantiate<T>(GameObject prefab) where T : Component
        {
            return Instantiate(prefab).GetComponent<T>();
        }

        public static T Load<T>(string resourcePath) where T : Component
        {
            var obj = Instantiate((GameObject)Resources.Load(resourcePath));
            return obj.GetComponent<T>();
        }

        public static GameObject Load(string resourcePath)
        {
            return Instantiate((GameObject)Resources.Load(resourcePath));
        }

        public static void Log(string message, params object[] args)
        {
            Debug.Log(string.Format(message, args));
        }

        public static void LogError(string message, params object[] args)
        {
            Debug.LogError(string.Format(message, args));
        }

        public static GameObject Instantiate(GameObject prefab)
        {
#if UNITY_EDITOR            
            return (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
#else
            return (GameObject)GameObject.Instantiate(prefab);
#endif
        }

        /// <summary>
        /// NOT TESTED!
        /// Wraps a given float around between a lower and upper bound
        /// </summary>
        /// <param name="value">the value to wrap</param>
        /// <param name="lower">the lower bound</param>
        /// <param name="upper">the upper bound</param>
        /// <returns></returns>
        public static float Wrap(float value, int lower, int upper)
        {
            float distance = upper - lower;
            float times = (float)System.Math.Floor((value - lower) / distance);
            return value - (times * distance);
        }

        /// <summary>
        /// NOT TESTED!
        /// Finds all objects in the game that match the given type be it an object or interface
        /// </summary>
        /// <typeparam name="T">The type to search for</typeparam>
        /// <returns></returns>
        public static List<T> FindAllComponentsOrInterfaces<T>()
        {
            return GameObject.FindObjectsOfType<Component>().OfType<T>().ToList();
        }
    }
}

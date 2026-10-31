// From https://github.com/mikecann/Unity-Helpers

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityHelpers;

/// <summary>
/// Putting these in the global namespace so that they can be accessed from anywhere in the game without importing
/// </summary>
public static class UnityExtensions
{
    private static List<Component> componentCache = new List<Component>();

    /// <summary>
    /// A shorter way of testing if a game object has a component
    /// </summary>
    /// <typeparam name="T">Component type</typeparam>
    /// <param name="obj">The object to check on</param>
    /// <returns></returns>
    public static bool Has<T>(this GameObject obj) where T : Component
    {
        return obj.GetComponent<T>() != null;
    }

    /// <summary>
    /// A slightly shorter way to get a component from an object
    /// </summary>
    /// <typeparam name="T">Component type</typeparam>
    /// <param name="obj">The object to get from</param>
    /// <returns></returns>
    public static T Get<T>(this GameObject obj) where T : Component
    {
        return obj.GetComponent<T>();
    }

    /// <summary>
    /// A slightly shorter way to get a component from an object
    /// </summary>
    /// <typeparam name="T">Component type</typeparam>
    /// <param name="obj">The object to get from</param>
    /// <returns></returns>
    public static T[] GetAll<T>(this GameObject obj) where T : Component
    {
        return obj.GetComponents<T>();
    }

    // <summary>
    /// A shorter way of testing if a game object has a component
    /// </summary>
    /// <typeparam name="T">Component type</typeparam>
    /// <param name="obj">The component to check on</param>
    /// <returns></returns>
    public static bool Has<T>(this Component obj) where T : Component
    {
        return obj.GetComponent<T>() != null;
    }

    /// <summary>
    /// A slightly shorter way to get a component from an object
    /// </summary>
    /// <typeparam name="T">Component type</typeparam>
    /// <param name="obj">The component to get from</param>
    /// <returns></returns>
    public static T Get<T>(this Component obj) where T : Component
    {
        return obj.GetComponent<T>();
    }

    /// <summary>
    /// A slightly shorter way to get a component from an object
    /// </summary>
    /// <typeparam name="T">Component type</typeparam>
    /// <param name="obj">The component to get from</param>
    /// <returns></returns>
    public static T[] GetAll<T>(this Component obj) where T : Component
    {
        return obj.GetComponents<T>();
    }

    /// <summary>
    /// Checks to see if the game object has a component, accepts interfaces
    /// </summary>
    /// <typeparam name="T">Type of component to check for (can be an interface)</typeparam>
    /// <returns>Whether the object has the component</returns>
    public static bool HasComponentOrInterface<T>(this GameObject obj) where T : class
    {
        obj.GetComponents<Component>(componentCache);
        return componentCache.OfType<T>().Count() > 0;
    }

    /// <summary>
    /// Gets a component on a game object, accepts interfaces
    /// </summary>
    /// <typeparam name="T">Type of component to check for (can be an interface)</typeparam>
    /// <param name="inObj"></param>
    /// <returns>The component, null if it doesnt exist</returns>
    public static T GetComponentOrInterface<T>(this GameObject obj) where T : class
    {
        obj.GetComponents<Component>(componentCache);
        return componentCache.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Gets all components of a given type, accepts interfaces
    /// </summary>
    /// <typeparam name="T">Type of components to get (accepts interfaces)</typeparam>
    /// <returns>Enumerable of components</returns>
    public static IEnumerable<T> GetAllComponentsOrInterfaces<T>(this GameObject obj) where T : class
    {
        return obj.GetComponents<Component>().OfType<T>();
    }

    /// <summary>
    /// Gets all components of a given type including children, accepts interfaces
    /// </summary>
    /// <typeparam name="T">Type of components to get (accepts interfaces)</typeparam>
    /// <returns>Enumerable of components</returns>
    public static IEnumerable<T> GetAllComponentsOrInterfacesInChildren<T>(this GameObject obj) where T : class
    {
        return obj.GetComponentsInChildren<Component>().OfType<T>();
    }

    /// <summary>
    /// Checks to see if the game object has a component, accepts interfaces
    /// </summary>
    /// <typeparam name="T">Type of component to check for (can be an interface)</typeparam>
    /// <returns>Whether the object has the component</returns>
    public static bool HasComponentOrInterface<T>(this Component component) where T : class
    {
        return HasComponentOrInterface<T>(component.gameObject);
    }

    /// <summary>
    /// Checks to see if the game object this component belongs to has a component, accepts interfaces
    /// </summary>
    /// <typeparam name="T">Type of component to check for (can be an interface)</typeparam>
    /// <returns>Whether the object has the component</returns>
    public static T GetComponentOrInterface<T>(this Component component) where T : class
    {
        return GetComponentOrInterface<T>(component.gameObject);
    }

    /// <summary>
    /// Gets all components of a given type, accepts interfaces
    /// </summary>
    /// <typeparam name="T">Type of components to get (accepts interfaces)</typeparam>
    /// <returns>Enumerable of components</returns>
    public static IEnumerable<T> GetAllComponentsOrInterfaces<T>(this Component component) where T : class
    {
        return GetAllComponentsOrInterfaces<T>(component.gameObject);
    }

    /// <summary>
    /// Gets all components of a given type in itself and the children, accepts interfaces
    /// </summary>
    /// <typeparam name="T">Type of components to get (accepts interfaces)</typeparam>
    /// <returns>Enumerable of components</returns>
    public static IEnumerable<T> GetAllComponentsOrInterfacesInChildren<T>(this Component component) where T : class
    {
        return GetAllComponentsOrInterfacesInChildren<T>(component.gameObject);
    }

    /// <summary>
    /// A shortcut for creating a new game object then adding a component then adding it to a parent object
    /// </summary>
    /// <typeparam name="T">Type of component</typeparam>
    /// <returns>The new component</returns>
    public static T AddChild<T>(this GameObject parent) where T : Component
    {
        return AddChild<T>(parent, typeof(T).Name);
    }

    /// <summary>
    /// A shortcut for adding a given game object as a child
    /// </summary>
    /// <returns>This gameobject</returns>
    public static GameObject AddChild(this GameObject parent, GameObject child, bool worldPositionStays = false)
    {
        child.transform.SetParent(parent.transform, worldPositionStays);
        return parent;
    }

    /// <summary>
    /// A shortcut for creating a new game object then adding a component then adding it to a parent object
    /// </summary>
    /// <typeparam name="T">Type of component</typeparam>
    /// <param name="name">Name of the child game object</param>
    /// <returns>The new component</returns>
    public static T AddChild<T>(this GameObject parent, string name) where T : Component
    {
        var obj = AddChild(parent, name, typeof(T));
        return obj.GetComponent<T>();
    }

    /// <summary>
    /// A shortcut for creating a new game object with a number of components and adding it as a child
    /// </summary>
    /// <param name="components">A list of component types</param>
    /// <returns>The new game object</returns>
    public static GameObject AddChild(this GameObject parent, params Type[] components)
    {
        return AddChild(parent, "Game Object", components);
    }

    /// <summary>
    /// A shortcut for creating a new game object with a number of components and adding it as a child
    /// </summary>
    /// <param name="name">The name of the new game object</param>
    /// <param name="components">A list of component types</param>
    /// <returns>The new game object</returns>
    public static GameObject AddChild(this GameObject parent, string name, params Type[] components)
    {
        var obj = new GameObject(name, components);
        if (parent != null)
        {
            if (obj.transform is RectTransform) obj.transform.SetParent(parent.transform, true);
            else obj.transform.parent = parent.transform;
        }
        return obj;
    }

    /// <summary>
    /// Loads a resource and adds it as a child
    /// </summary>
    /// <param name="resourcePath">The path to the resource to load</param>
    /// <returns></returns>
    public static GameObject LoadChild(this GameObject parent, string resourcePath)
    {
        var obj = (GameObject)GameObject.Instantiate(Resources.Load(resourcePath));
        if (obj != null && parent != null)
        {
            if (obj.transform is RectTransform) obj.transform.SetParent(parent.transform, true);
            else obj.transform.parent = parent.transform;
        }
        return obj;
    }

    /// <summary>
    /// Loads a resource and adds it as a child
    /// </summary>
    /// <param name="resourcePath">The path to the resource to load</param>
    /// <returns></returns>
    public static GameObject LoadChild(this Transform parent, string resourcePath)
    {
        var obj = (GameObject)GameObject.Instantiate(Resources.Load(resourcePath));
        if (obj != null && parent != null)
        {
            if (obj.transform is RectTransform) obj.transform.SetParent(parent, true);
            else obj.transform.parent = parent;
        }
        return obj;
    }

    /// <summary>
    /// Destroys all the children of a given gameobject
    /// </summary>
    /// <param name="obj">The parent game object</param>
    public static void DestroyAllChildrenImmediately(this GameObject obj)
    {
        DestroyAllChildrenImmediately(obj.transform);
    }

    /// <summary>
    /// Destroys all the children of a given transform
    /// </summary>
    /// <param name="obj">The parent transform</param>
    public static void DestroyAllChildrenImmediately(this Transform trans)
    {
        while (trans.childCount != 0)
            GameObject.DestroyImmediate(trans.GetChild(0).gameObject);
    }

    /// <summary>
    /// Focuses the camera on a point in 2D space (just transforms the x and y to match the target)
    /// </summary>
    /// <param name="camera"></param>
    /// <param name="gameObject"></param>
    public static void FocusOn2D(this Camera camera, GameObject target)
    {
        camera.transform.position = new Vector3(target.transform.localPosition.x, target.transform.localPosition.y, Camera.main.transform.position.z);
    }

    /// <summary>
    /// Converts a uint to a color
    /// </summary>
    /// <param name="color">The uint to convert</param>
    /// <returns></returns>
    public static uint ToUInt(this Color color)
    {
        Color32 c32 = color;
        return (uint)((c32.a << 24) | (c32.r << 16) |
                      (c32.g << 8) | (c32.b << 0));
    }

    /// <summary>
    /// Converts a uint to a color32
    /// </summary>
    /// <param name="color">The uint to convert</param>
    /// <returns></returns>
    public static Color32 ToColor32(this uint color)
    {
        return new Color32()
        {
            a = (byte)(color >> 24),
            r = (byte)(color >> 16),
            g = (byte)(color >> 8),
            b = (byte)(color >> 0)
        };
    }

    /// <summary>
    /// Converts a uint to a color
    /// </summary>
    /// <param name="color">The uint to convert</param>
    /// <returns></returns>
    public static Color ToColor(this uint color)
    {
        return ToColor32(color);
    }

    /// <summary>
    /// Converts a color to its hex counterpart
    /// </summary>
    /// <param name="color">The color to convert</param>
    /// <returns></returns>
    public static string ToHex(this Color32 color)
    {
        return "#" + color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
    }

    /// <summary>
    /// Converts a timespan to a readable format
    /// </summary>
    /// <param name="span"></param>
    /// <returns></returns>
    public static string ToReadableString(this TimeSpan span)
    {
        string formatted = string.Format("{0}{1}{2}{3}",
            span.Duration().Days > 0 ? string.Format("{0:0} day{1}, ", span.Days, span.Days == 1 ? String.Empty : "s") : string.Empty,
            span.Duration().Hours > 0 ? string.Format("{0:0} hour{1}, ", span.Hours, span.Hours == 1 ? String.Empty : "s") : string.Empty,
            span.Duration().Minutes > 0 ? string.Format("{0:0} minute{1}, ", span.Minutes, span.Minutes == 1 ? String.Empty : "s") : string.Empty,
            span.Duration().Seconds > 0 ? string.Format("{0:0} second{1}", span.Seconds, span.Seconds == 1 ? String.Empty : "s") : string.Empty);

        if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

        if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";

        return formatted;
    }

    /// <summary>
    /// Converts a timespan to a readable format but in a shorter form
    /// </summary>
    /// <param name="span"></param>
    /// <returns></returns>
    public static string ToReadableStringShortForm(this TimeSpan span)
    {
        string formatted = string.Format("{0}{1}{2}{3}",
            span.Duration().Days > 0 ? string.Format("{0:0}d, ", span.Days) : string.Empty,
            span.Duration().Hours > 0 ? string.Format("{0:0}h, ", span.Hours) : string.Empty,
            span.Duration().Minutes > 0 ? string.Format("{0:0}m, ", span.Minutes) : string.Empty,
            span.Duration().Seconds > 0 ? string.Format("{0:0}s", span.Seconds) : string.Empty);

        if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

        if (string.IsNullOrEmpty(formatted)) formatted = "0s";

        return formatted;
    }

    /// <summary>
    /// Randomly picks one elements from the enumerable
    /// </summary>
    /// <typeparam name="T">The type of the item</typeparam>
    /// <param name="items">The items to random from</param>
    /// <returns></returns>
    public static T RandomOne<T>(this IEnumerable<T> items)
    {
        if (items == null) throw new ArgumentException("Cannot randomly pick an item from the list, the list is null!");
        if (items.Count() == 0) throw new ArgumentException("Cannot randomly pick an item from the list, there are no items in the list!");
        var r = UnityEngine.Random.Range(0, items.Count());
        return items.ElementAt(r);
    }

    /// <summary>
    /// Borrowed from http://stackoverflow.com/questions/56692/random-weighted-choice
    /// Randomly picks one element from the enumerable, taking into account a weight
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sequence"></param>
    /// <param name="weightSelector"></param>
    /// <returns></returns>
    public static T WeightedRandomOne<T>(this IEnumerable<T> sequence, Func<T, float> weightSelector)
    {
        float totalWeight = sequence.Sum(weightSelector);
        // The weight we are after...
        float itemWeightIndex = UnityEngine.Random.value * totalWeight;
        float currentWeightIndex = 0;

        foreach (var item in from weightedItem in sequence select new { Value = weightedItem, Weight = weightSelector(weightedItem) })
        {
            currentWeightIndex += item.Weight;

            // If we've hit or passed the weight we are after for this item then it's the one we want....
            if (currentWeightIndex >= itemWeightIndex)
                return item.Value;

        }

        return default(T);
    }

    /// <summary>
    /// Loops over each item in the list and logs out a particular value, handy for debugging!
    /// </summary>
    /// <typeparam name="T">The type of the item</typeparam>
    /// <param name="items">The list of items</param>
    /// <param name="objectToLog">A callback in which you will return the item to log</param>
    /// <returns>The original input list</returns>
    public static IEnumerable<T> DebugLog<T>(this IEnumerable<T> items, Func<T,object> objectToLog)
    {
        var count = 0;
        foreach (var item in items)
        {
            var obj = objectToLog(item);
            Debug.Log(String.Format("Item[{0}]: {1}", count, obj));
        }
        return items;
    }

    /// <summary>
    /// Adds a listner to a UnityEvent which is removed as soon as the event is invoked
    /// </summary>
    /// <param name="evnt">the event to listen for</param>
    /// <param name="callback">the callback to call</param>
    /// <returns></returns>
    public static UnityEvent AddOnce(this UnityEvent evnt, Action callback)
    {
        UnityAction a = null;
        a = () =>
        {
            evnt.RemoveListener(a);
            callback();
        };

        evnt.AddListener(a);
        return evnt;
    }

    /// <summary>
    /// Adds a listner to a UnityEvent which is removed as soon as the event is invoked
    /// </summary>
    /// <param name="evnt">the event to listen for</param>
    /// <param name="callback">the callback to call</param>
    /// <returns></returns>
    public static UnityEvent<T> AddOnce<T>(this UnityEvent<T> evnt, Action<T> callback)
    {
        UnityAction<T> a = null;
        a = obj =>
        {
            evnt.RemoveListener(a);
            callback(obj);
        };

        evnt.AddListener(a);
        return evnt;
    }

    /// <summary>
    /// Returns the orthographic bounds for a camera 
    /// !! NOT UNIT TESTED !!
    /// Borrowed from: http://answers.unity3d.com/questions/501893/calculating-2d-camera-bounds.html
    /// </summary>
    /// <param name="camera">the camera to get the bounds on</param>
    /// <returns>the orthographic bounds on a camera</returns>
    public static Bounds OrthographicBounds(this Camera camera)
    {
        float screenAspect = (float)Screen.width / (float)Screen.height;
        float cameraHeight = camera.orthographicSize * 2;
        Bounds bounds = new Bounds(
            camera.transform.position,
            new Vector3(cameraHeight * screenAspect, cameraHeight, 0));
        return bounds;
    }

    /// <summary>
    /// Tests if two bounds intersect. This is neccessary as there appears to be a bug with normal Bounds Intersection.
    /// !! NOT UNIT TESTED !!
    /// </summary>
    /// <param name="a">First bound</param>
    /// <param name="b">Second bound</param>
    /// <returns></returns>
    public static bool Intersects2(this Bounds a, Bounds b)
    {
        return !(b.min.x > a.max.x ||
               b.max.x < a.min.x ||
               b.min.y > a.max.y ||
               b.max.y < a.min.y);
    }

    /// <summary>
    /// A simple clone method for a Vector3
    /// </summary>
    /// <param name="v">The vector to clone</param>
    /// <returns></returns>
    public static Vector3 Clone(this Vector3 v)
    {
        return new Vector3(v.x, v.y, v.z);
    }

    /// <summary>
    /// A simple clone method for a Vector2
    /// </summary>
    /// <param name="v">The vector to clone</param>
    /// <returns></returns>
    public static Vector2 Clone(this Vector2 v)
    {
        return new Vector2(v.x, v.y);
    }

    /// <summary>
    /// Simple method to turn a v2 into a v3
    /// </summary>
    /// <param name="v">The vector to convert</param>
    /// <returns></returns>
    public static Vector3 ToVector3(this Vector2 v)
    {
        return new Vector3(v.x, v.y, 0);
    }

    /// <summary>
    /// Simple method to turn a v3 into a v2
    /// </summary>
    /// <param name="v">The vector to convert</param>
    /// <returns></returns>
    public static Vector2 ToVector2(this Vector3 v)
    {
        return new Vector2(v.x, v.y);
    }

    /// <summary>
    /// Simple method to set the x value on the Vector2, returns a new Vector2
    /// </summary>
    /// <param name="v">the vector</param>
    /// <param name="x">the x value to set</param>
    public static Vector2 SetX(this Vector2 v, float x)
    {
        return new Vector2(x, v.y);
    }

    /// <summary>
    /// Simple method to set the y value on the Vector2, returns a new Vector2
    /// </summary>
    /// <param name="v">the vector</param>
    /// <param name="x">the y value to set</param>
    public static Vector2 SetY(this Vector2 v, float y)
    {
        return new Vector2(v.x, y);
    }

#if (UNITY_EDITOR)
    /// <summary>
    /// Quick shuffle of a list 
    /// Borrowed from: http://stackoverflow.com/questions/273313/randomize-a-listt-in-c-sharp
    /// </summary>
    /// <typeparam name="T">the type of the list</typeparam>
    /// <param name="list">the list to shuffle</param>
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
#endif

    /// <summary>
    /// Takes a list, creates a copy and returns the newly shuffled list
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    public static List<T> Randomise<T>(this List<T> inList)
    {
        var list = new List<T>(inList);
        list.Shuffle();
        return list;
    }

    /// <summary>
    /// Simple method to set the x value on the Vector3, returns a new vector3
    /// </summary>
    /// <param name="v">the vector</param>
    /// <param name="x">the x value to set</param>
    public static Vector3 SetX(this Vector3 v, float x)
    {
        return new Vector3(x, v.y, v.z);
    }

    /// <summary>
    /// Simple method to set the y value on the Vector3, returns a new vector3
    /// </summary>
    /// <param name="v">the vector</param>
    /// <param name="x">the y value to set</param>
    public static Vector3 SetY(this Vector3 v, float y)
    {
        return new Vector3(v.x, y, v.z);
    }

    /// <summary>
    /// Simple method to set the z value on the Vector3, returns a new vector3
    /// </summary>
    /// <param name="v">the vector</param>
    /// <param name="x">the z value to set</param>
    public static Vector3 SetZ(this Vector3 v, float z)
    {
        return new Vector3(v.x, v.y, z);
    }

    /// <summary>
    /// NOT TESTED !!
    /// Converts a 3D bounds to a 2D rect
    /// </summary>
    /// <param name="b">The bounds to convert</param>
    /// <returns>the rect</returns>
    public static Rect ToRect(this Bounds b)
    {
        return new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
    }

    /// <summary>
    /// Instantiates an instance of this prefab
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static GameObject Instantiate(this GameObject prefab)
    {
        return UnityUtils.Instantiate(prefab);
    }

    /// <summary>
    /// Instantiates an instance of this prefab and retuns a component on it
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static T Instantiate<T>(this GameObject prefab) where T : Component
    {
        return UnityUtils.Instantiate<T>(prefab);
    }

    /// <summary>
    /// Deactivates the gameobject this component is attached to
    /// </summary>
    /// <returns></returns>
    public static void Deactivate(this Component component)
    {
        component.gameObject.SetActive(false);
    }

    /// <summary>
    /// Activate the gameobject this component is attached to
    /// </summary>
    /// <returns></returns>
    public static void Activate(this Component component)
    {
        component.gameObject.SetActive(true);
    }

    /// <summary>
    /// Loads all the objects in a given path, instantiates them then adds them as children
    /// </summary>
    /// <typeparam name="T">The type of the child to return</typeparam>
    /// <param name="parentObject">The parent object to add the children to</param>
    /// <param name="prefabsPath">The path from which to load the prefabs</param>
    /// <returns></returns>
    public static List<T> LoadAllAsChildren<T>(this GameObject parentObject, string prefabsPath) where T : Component
    {
        var prefabs = Resources.LoadAll<T>(prefabsPath);
        var children = new List<T>();
        foreach(var prefab in prefabs)
        {
            var instance = UnityUtils.Instantiate<T>(prefab.gameObject);
            instance.transform.parent = parentObject.transform;
            children.Add(instance);
        }
        return children;
    }

    /// <summary>
    /// Recursively looks up the tree to see if this object is parents by the supplied
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool IsParentedBy(this GameObject obj, GameObject parent)
    {
        if (obj.transform.parent == null)
            return false;
        if (obj.transform.parent.gameObject == parent)
            return true;

        return obj.transform.parent.gameObject.IsParentedBy(parent);
    }
}

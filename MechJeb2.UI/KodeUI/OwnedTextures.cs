using System.Collections.Generic;
using UnityEngine;

namespace KodeUI
{
    /// <summary>Destroys the textures made for a widget with it: Unity keeps a texture alive until it is destroyed, so every window rebuild leaked them.</summary>
    public class OwnedTextures : MonoBehaviour
    {
        readonly List<Texture> textures = new List<Texture>();

        public static T Own<T>(Component owner, T texture) where T : Texture
        {
            OwnedTextures o = owner.GetComponent<OwnedTextures>() ?? owner.gameObject.AddComponent<OwnedTextures>();
            o.textures.Add(texture);
            return texture;
        }

        void OnDestroy()
        {
            foreach (Texture t in textures)
                if (t != null) Destroy(t);
        }
    }
}

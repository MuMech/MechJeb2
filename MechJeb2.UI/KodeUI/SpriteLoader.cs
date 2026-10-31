using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KodeUI
{
    public class SpriteLoader
	{
		static Dictionary<string,Sprite> sprites = new Dictionary<string, Sprite> ();

		public static Sprite GetSprite(string name)
		{
			Sprite s = null;
			sprites.TryGetValue (name, out s);
			return Sprite.Instantiate(s);
		}

		public static IEnumerator LoadSprites()
		{
			var dbase = GameDatabase.Instance;
			var node_list = dbase.GetConfigNodes ("KodeUI_Sprite");
			for (int i = 0; i < node_list.Length; i++) {
				var node = node_list[i];
				string name = node.GetValue ("name");
				if (String.IsNullOrEmpty (name)) {
					Debug.Log ("[KodeUI.SpriteLoader] skipping unnamed sprite");
					continue;
				}

				string textureUrl = node.GetValue ("textureUrl");
				Texture2D tex = dbase.GetTexture(textureUrl, false);
				if (tex == null) {
					Debug.LogFormat ("[KodeUI.SpriteLoader] error loading texture {0}", textureUrl);
					continue;
				}

				Rect rect = new Rect (0, 0, tex.width, tex.height);
				node.TryGetValue ("rect", ref rect);

				Vector2 pivot = new Vector2 (0.5f, 0.5f);
				node.TryGetValue ("pivot", ref pivot);

				float pixelsPerUnit = 100;
				node.TryGetValue ("pixelsPerUnit", ref pixelsPerUnit);

				uint extrude = 0;
				node.TryGetValue ("extrude", ref extrude);

				string type_str = "";
				SpriteMeshType type = SpriteMeshType.Tight;
				node.TryGetValue ("type", ref type_str);
				type = KodeUI_Utils.ToEnum<SpriteMeshType> (type_str, type);

				Vector4 border = Vector4.zero;
				node.TryGetValue ("border", ref border);

				Debug.LogFormat ("[KodeUI.SpriteLoader] {0}: {1} {2} {3} {4} {5} {6} {7}",
								 name, textureUrl, rect, pivot, pixelsPerUnit,
								 extrude, type, border);
				sprites[name] = Sprite.Create (tex, rect, pivot, pixelsPerUnit,
											   extrude, type, border);
				yield return null;
			}
		}
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KodeUI
{
    public class ImageLoader
	{
		class ImageData {
			public Image.Type type;
			public string sprite;
		}
		static Dictionary<string,ImageData> images = new Dictionary<string, ImageData> ();

		public static void SetupImage(Image image, string name)
		{
			ImageData id = null;
			if (images.TryGetValue (name, out id)) {
				Sprite sprite = SpriteLoader.GetSprite (id.sprite);
				if (sprite == null) {
					Debug.LogFormat ("[KodeUI.ImageLoader] sprite not found:{0}", id.sprite);
				}
				image.sprite = sprite;
				image.type = id.type;
			}
		}

		public static IEnumerator LoadImages()
		{
			var dbase = GameDatabase.Instance;
			var node_list = dbase.GetConfigNodes ("KodeUI_Image");
			for (int i = 0; i < node_list.Length; i++) {
				var node = node_list[i];
				string name = node.GetValue ("name");
				if (String.IsNullOrEmpty (name)) {
					Debug.Log ("[KodeUI.ImageLoader] skipping unnamed image");
					continue;
				}

				string sprite = node.GetValue ("sprite");

				string type_str = "";
				Image.Type type = Image.Type.Simple;
				node.TryGetValue ("type", ref type_str);
				type = KodeUI_Utils.ToEnum<Image.Type> (type_str, type);

				Debug.LogFormat ("[KodeUI.ImageLoader] {0}: {1} {2}",
								 name, sprite, type);
				ImageData id = new ImageData();
				id.sprite = sprite;
				id.type = type;
				images[name] = id;
				yield return null;
			}
		}
    }
}

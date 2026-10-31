using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeUI
{
    public class Skin
    {
		Dictionary<string, Style> styles;

		public Style defaultStyle { get; set; }

		public Style this[string name]
		{
			get {
				Style style;
				if (!styles.TryGetValue (name, out style)) {
					style = defaultStyle;
				}
				if (style == null) {
					return new Style();
				}
				return new Style (style);
			}
		}

		public bool ContainsStyle (string name)
		{
			return styles.ContainsKey (name);
		}

		public Skin()
		{
			styles = new Dictionary<string, Style> ();
		}

		public Skin(Skin skin)
		{
			defaultStyle = skin.defaultStyle;
			styles = new Dictionary<string, Style> (skin.styles);
		}

		public Skin (ConfigNode node)
		{
			styles = new Dictionary<string, Style> ();
			Load (node);
		}

		void Load (ConfigNode node)
		{
			foreach (var styleNode in node.GetNodes ("Style")) {
				string name = styleNode.GetValue ("name");
				if (String.IsNullOrEmpty (name) || name == "default") {
					Style style = defaultStyle;
					if (style == null) {
						style = new Style ();
					}
					style.Load (node);
					defaultStyle = style;
				} else {
					styles[name] = new Style (styleNode);
				}
			}
		}

		public static Skin defaultSkin { get; private set; }

		public static Dictionary<string, Skin> skins { get; private set; }

		public static Skin GetSkin(string name)
		{
			if (String.IsNullOrEmpty (name)) {
				return null;
			}
			Skin s;
			skins.TryGetValue (name, out s);
			Debug.Log ($"[Skin] GetSkin {name} {s}");
			return s;
		}

		public static IEnumerator LoadSkins()
		{
			skins = new Dictionary<string, Skin> ();

			var dbase = GameDatabase.Instance;
			var node_list = dbase.GetConfigNodes ("KodeUI_Skin");
			defaultSkin = new Skin ();
			for (int i = 0; i < node_list.Length; i++) {
				Skin skin;
				var node = node_list[i];
				string name = node.GetValue ("name");
				if (String.IsNullOrEmpty (name) || name == "default") {
					skin = defaultSkin;
					skin.Load (node);
				} else {
					skin = new Skin(node);
					skins[name] = skin;
				}
				yield return null;
			}
		}
    }
}

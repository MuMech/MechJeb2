using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace KodeUI
{
    public class Style
    {
		public ColorBlock? stateColors { get; set; }
		public Color? color { get; set; }
		public Sprite sprite { get; set; }
		public SpriteState? stateSprites { get; set; }
		public Selectable.Transition? transition { get; set; }
		public Image.Type? type { get; set; }
		public float? fontSize { get; set; }
		public TextAlignmentOptions? alignment { get; set; }
		public Vector4? margin { get; set; }
		public RectOffset padding { get; set; }
		public float? spacing { get; set; }

		static Vector4? ParseVector4 (string str)
		{
			if (String.IsNullOrEmpty (str)) {
				return null;
			}
			Vector4 vec;
			if (!ParseExtensions.TryParseVector4 (str, out vec)) {
				return null;
			}
			return vec;
		}

		static RectOffset ParseRectOffset (string str)
		{
			if (String.IsNullOrEmpty (str)) {
				return null;
			}
			Vector4 vec;
			if (!ParseExtensions.TryParseVector4 (str, out vec)) {
				return null;
			}
			return new RectOffset ((int) vec.x, (int) vec.y, (int) vec.z, (int) vec.w);
		}

		static Color? ParseColor (string str)
		{
			if (String.IsNullOrEmpty (str)) {
				return null;
			}
			Color color;
			if (!ParseExtensions.TryParseColor (str, out color)
				&& !ColorUtility.TryParseHtmlString (str, out color)) {
				return null;
			}
			return color;
		}

		static float? ParseFloat (string str)
		{
			if (String.IsNullOrEmpty (str)) {
				return null;
			}
			float val;
			float.TryParse (str, out val);
			return val;
		}

		static ColorBlock? ParseColorBlock (ConfigNode node)
		{
			if (node == null) {
				return null;
			}
			var colorBlock = ColorBlock.defaultColorBlock;
			{if (ParseColor(node.GetValue ("normalColor")) is Color c) {
				colorBlock.normalColor = c;
			}}
			{if (ParseColor(node.GetValue ("highlightedColor")) is Color c) {
				colorBlock.highlightedColor = c;
			}}
			{if (ParseColor(node.GetValue ("pressedColor")) is Color c) {
				colorBlock.pressedColor = c;
			}}
			{if (ParseColor(node.GetValue ("selectedColor")) is Color c) {
				colorBlock.selectedColor = c;
			}}
			{if (ParseColor(node.GetValue ("disabledColor")) is Color c) {
				colorBlock.disabledColor = c;
			}}
			{if (ParseFloat(node.GetValue ("colorMultiplier")) is float f) {
				colorBlock.colorMultiplier = f;
			}}
			{if (ParseFloat(node.GetValue ("fadeDuration")) is float f) {
				colorBlock.fadeDuration = f;
			}}
			return colorBlock;
		}

		static SpriteState? ParseSpriteState (ConfigNode node)
		{
			if (node == null) {
				return null;
			}
			SpriteState sprites = new SpriteState();

			{if (SpriteLoader.GetSprite (node.GetValue ("highlightedSprite")) is Sprite s) {
				sprites.highlightedSprite = s;
			}}
			{if (SpriteLoader.GetSprite (node.GetValue ("pressedSprite")) is Sprite s) {
				sprites.pressedSprite = s;
			}}
			{if (SpriteLoader.GetSprite (node.GetValue ("selectedSprite")) is Sprite s) {
				sprites.selectedSprite = s;
			}}
			{if (SpriteLoader.GetSprite (node.GetValue ("disabledSprite")) is Sprite s) {
				sprites.disabledSprite = s;
			}}
			return sprites;
		}

		static TextAlignmentOptions? ParseAlignment (string str)
		{
			if (String.IsNullOrEmpty (str)) {
				return null;
			}
			TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft;
			alignment = KodeUI_Utils.ToEnum<TextAlignmentOptions> (str, alignment);
			return alignment;
		}

		static Selectable.Transition? ParseTransition (string str)
		{
			if (String.IsNullOrEmpty (str)) {
				return null;
			}
			Selectable.Transition transition = Selectable.Transition.ColorTint;
			transition = KodeUI_Utils.ToEnum<Selectable.Transition> (str, transition);
			return transition;
		}

		static Image.Type? ParseImageType (string str)
		{
			if (String.IsNullOrEmpty (str)) {
				return null;
			}
			Image.Type transition = Image.Type.Simple;//Simple, Sliced, Tiled, Filled
			transition = KodeUI_Utils.ToEnum<Image.Type> (str, transition);
			return transition;
		}

		public Style ()
		{
			stateColors = ColorBlock.defaultColorBlock;
			color = Color.white;
			// sprite can default to null
			// stateSprites can default to null
			transition = Selectable.Transition.ColorTint;
		}

		public Style (ConfigNode node)
		{
			Load (node);
		}

		public Style (Style style)
		{
			stateColors = style.stateColors;
			color = style.color;
			sprite = style.sprite;
			stateSprites = style.stateSprites;
			transition = style.transition;
			type = style.type;
			fontSize = style.fontSize;
			alignment = style.alignment;
			margin = style.margin;
			padding = style.padding;
			spacing = style.spacing;
		}

		public Style Merge (Style overrideStyle)
		{
			{if (overrideStyle.stateColors is ColorBlock cb) {
				stateColors = cb;
			}}
			{if (overrideStyle.color is Color c) {
				color = c;
			}}
			{if (overrideStyle.sprite is Sprite s) {
				sprite = s;
			}}
			{if (overrideStyle.stateSprites is SpriteState s) {
				stateSprites = s;
			}}
			{if (overrideStyle.transition is Selectable.Transition t) {
				transition = t;
			}}
			{if (overrideStyle.type is Image.Type t) {
				type = t;
			}}
			{if (overrideStyle.fontSize is float f) {
				fontSize = f;
			}}
			{if (overrideStyle.alignment is TextAlignmentOptions a) {
				alignment = a;
			}}
			{if (overrideStyle.margin is Vector4 v) {
				margin = v;
			}}
			{if (overrideStyle.padding is RectOffset ro) {
				padding = ro;
			}}
			{if (overrideStyle.spacing is float f) {
				spacing = f;
			}}
			return this;
		}

		public void Load (ConfigNode node)
		{
			{if (ParseColorBlock (node.GetNode ("stateColors")) is ColorBlock cb) {
				stateColors = cb;
			}}
			{if (ParseColor (node.GetValue ("color")) is Color c) {
				color = c;
			}}
			{if (SpriteLoader.GetSprite (node.GetValue ("sprite")) is Sprite s) {
				sprite = s;
			}}
			{if (ParseSpriteState (node.GetNode ("stateSprites")) is SpriteState s) {
				stateSprites = s;
			}}
			{if (ParseTransition (node.GetValue ("transition")) is Selectable.Transition t) {
				transition = t;
			}}
			{if (ParseImageType (node.GetValue ("type")) is Image.Type t) {
				type = t;
			}}
			{if (ParseFloat (node.GetValue ("fontSize")) is float f) {
				fontSize = f;
			}}
			{if (ParseAlignment (node.GetValue ("alignment")) is TextAlignmentOptions a) {
				alignment = a;
			}}
			{if (ParseVector4 (node.GetValue ("margin")) is Vector4 v) {
				margin = v;
			}}
			{if (ParseRectOffset (node.GetValue ("padding")) is RectOffset ro) {
				padding = ro;
			}}
			{if (ParseFloat (node.GetValue ("spacing")) is float f) {
				spacing = f;
			}}
		}
    }
}

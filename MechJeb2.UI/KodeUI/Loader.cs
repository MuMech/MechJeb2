using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KodeUI {

	[KSPAddon (KSPAddon.Startup.Instantly, false)]
	public class KodeUI_Loader: MonoBehaviour
	{

		public class Loader: LoadingSystem
		{
			public bool done;

			public override bool IsReady ()
			{
				return done;
			}

			public override float ProgressFraction ()
			{
				return 0;
			}

			public override string ProgressTitle ()
			{
				return "KodeUI Resources";
			}

			IEnumerator Load ()
			{
				yield return StartCoroutine (SpriteLoader.LoadSprites ());
				yield return StartCoroutine (ImageLoader.LoadImages ());
				yield return StartCoroutine (Skin.LoadSkins ());
				done = true;
			}

			public override void StartLoad ()
			{
				done = false;
				StartCoroutine (Load ());
			}
		}

		void Awake ()
		{
			List<LoadingSystem> list = LoadingScreen.Instance.loaders;
			if (list != null) {
				// first, check to see if our loader is already in the list
				for (int i = 0; i < list.Count; i++) {
					if (list[i] is Loader) {
						(list[i] as Loader).done = false;
						return;
					}
				}
				// if not, insert it after the part loader
				for (int i = 0; i < list.Count; i++) {
					if (list[i] is PartLoader) {
						GameObject go = new GameObject("KodeUI_Loader");
						Loader loader = go.AddComponent<Loader> ();
						list.Insert (i + 1, loader);
						break;
					}
				}
			}
		}
	}
}

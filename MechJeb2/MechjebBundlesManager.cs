using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

namespace MuMech
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class MechJebBundlesManager : MonoBehaviour
    {
        private const string shaderBundle = "shaders.bundle";
        private string shaderPath;

        private const string diffuseAmbientName = "Assets/Shaders/MJ_DiffuseAmbiant.shader";
        private const string diffuseAmbientIgnoreZName = "Assets/Shaders/MJ_DiffuseAmbiantIgnoreZ.shader";

        public static Shader diffuseAmbient;
        public static Shader diffuseAmbientIgnoreZ;
        public static Texture2D comboBoxBackground;

        void Awake()
        {
            var gameDataPath = KSPUtil.ApplicationRootPath + "/GameData/MechJeb2/Bundles/";
            shaderPath = gameDataPath + shaderBundle;
        }

        IEnumerator Start()
        {
            // We do this in MainMenu because something is going on in that scene that kills anything loaded with a bundle
            if (diffuseAmbient)
            {
                MechJebCore.print("Shaders already loaded");
            }

            MechJebCore.print("Loading Shaders Bundles");

            // Load the font asset bundle
            var bundleLoadRequest = AssetBundle.LoadFromFileAsync(shaderPath);
            yield return bundleLoadRequest;

            var assetBundle = bundleLoadRequest.assetBundle;
            if (assetBundle == null)
            {
                MechJebCore.print("Failed to load AssetBundle " + shaderPath);
                yield break;
            }

            var assetLoadRequest = assetBundle.LoadAssetAsync<Shader>(diffuseAmbientName);
            yield return assetLoadRequest;
            diffuseAmbient = assetLoadRequest.asset as Shader;

            assetLoadRequest = assetBundle.LoadAssetAsync<Shader>(diffuseAmbientIgnoreZName);
            yield return assetLoadRequest;
            diffuseAmbientIgnoreZ = assetLoadRequest.asset as Shader;

            assetBundle.Unload(false);
            MechJebCore.print("Loaded Shaders Bundles");

            comboBoxBackground = new Texture2D(16, 16, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp
            };

            for (var x = 0; x < comboBoxBackground.width; x++)
            {
                for (var y = 0; y < comboBoxBackground.height; y++)
            {
                if (x == 0 || x == comboBoxBackground.width-1 || y == 0 || y == comboBoxBackground.height-1)
                    {
                        comboBoxBackground.SetPixel(x, y, new Color(0, 0, 0, 1));
                    }
                    else
                    {
                        comboBoxBackground.SetPixel(x, y, new Color(0.05f, 0.05f, 0.05f, 0.95f));
                    }
                }
            }

            comboBoxBackground.Apply();
        }
    }
}



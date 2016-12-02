using UnityEditor;
using System.IO;

public class CreateAssetBundles
{
    [MenuItem("MechJeb/Build AssetBundles")]
    static void BuildAllAssetBundles()
    {
        // The project settings has DX9, DX11, GL2 and GLCore set for Windows. That should add the shader for all platform when building for Windows
        
        Directory.CreateDirectory("AssetBundles");

        AssetBundleBuild[] buildMap = new AssetBundleBuild[1];

        buildMap[0].assetBundleName = "Shaders.bundle";

        string[] shadersAssets = new string[3];
        shadersAssets[0] = "Assets/Shaders/MJ_DiffuseAmbiant.shader";
        shadersAssets[1] = "Assets/Shaders/MJ_DiffuseAmbiantIgnoreZ.shader";
        shadersAssets[2] = "Assets/Shaders/MJ_SelfIlluminSpecular.shader";

        buildMap[0].assetNames = shadersAssets;

        BuildPipeline.BuildAssetBundles("AssetBundles", buildMap, BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.StandaloneWindows);
    }
}
using UnityEngine;
using System.IO;
using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;

//Static class for ease of access
public static class Assets
{
    public static AssetBundle icons;
    public static AssetBundle prefabs;
    public const string iconsName = "icons";
    public const string prefabsName = "prefabs";

    //The direct path to your AssetBundle
    public static string IconAssetBundlePath
    {
        get
        {
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ExamplePlugin.ExamplePlugin.PInfo.Location), iconsName);
        }
    }

    public static string PrefabAssetBundlePath
    {
        get
        {
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ExamplePlugin.ExamplePlugin.PInfo.Location), prefabsName);
        }
    }

    public static void Init()
    {
        //Loads the assetBundle from the Path, and stores it in the static field.
        icons = AssetBundle.LoadFromFile(IconAssetBundlePath);
        prefabs = AssetBundle.LoadFromFile(PrefabAssetBundlePath);
    }
}
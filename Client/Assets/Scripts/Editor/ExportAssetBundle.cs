using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ExportAssetBundle : Editor
{

    [MenuItem("AssetBundle/Build AssetBundle")]
    static void ExportResource()
    {
        string path = "Assets/__Test/BuildedAssetBundle/";
        Object[] selection = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);

        Debug.Log(selection.Length);


        foreach (var obj in selection)
        {
            Debug.Log(obj.name);
        }

        BuildPipeline.BuildAssetBundles(path,BuildAssetBundleOptions.None,BuildTarget.Android);
        //AssetBundleBuild buildMap = new AssetBundleBuild();

        //string[] buildAssets = new string[selection.Length];


        //BuildPipeline.BuildAssetBundles()


        //BuildPipeline.BuildAssetBundle(null, selection, path,
        //    BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets,
        //    BuildTarget.Android);
    }
}

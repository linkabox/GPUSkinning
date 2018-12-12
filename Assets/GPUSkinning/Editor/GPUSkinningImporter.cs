using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GPUSkinningImporter : AssetPostprocessor
{
	static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
	{
		foreach (string assetPath in importedAssets)
		{
			if (assetPath.EndsWith("_AnimData.asset"))
			{
				var animData = (GPUSkinningAnimation)AssetDatabase.LoadMainAssetAtPath(assetPath);
				GPUSkinningPlayerResources.SetMaterialBoneData(animData, animData.material);
				Debug.Log("Reimported animData: " + assetPath);
			}
		}
	}
}

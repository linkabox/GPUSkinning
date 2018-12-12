using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GPUSkinningShaderEditor : ShaderGUI
{
	MaterialProperty _boneTexture = null;
	MaterialProperty _boneTextureParams = null;

	public bool FindProperties(MaterialProperty[] props)
	{
		_boneTexture = FindProperty("_boneTexture", props, false);
		_boneTextureParams = FindProperty("_boneTextureParams", props, false);
		return _boneTexture != null && _boneTextureParams != null;
	}

	public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
	{
		base.OnGUI(materialEditor, props);

		//Extra GUI
		if (FindProperties(props))
		{
			Material material = materialEditor.target as Material;

			EditorGUILayout.Space();
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PrefixLabel("BoneData:");
			var animData = (GPUSkinningAnimation)EditorGUILayout.ObjectField(null, typeof(GPUSkinningAnimation), false);
			EditorGUI.BeginDisabledGroup(true);
			materialEditor.ShaderProperty(_boneTexture, "boneTexture:");
			materialEditor.ShaderProperty(_boneTextureParams, "boneTextureParams:");
			EditorGUI.EndDisabledGroup();

			if (EditorGUI.EndChangeCheck())
			{
				GPUSkinningPlayerResources.SetMaterialBoneData(animData, material);
			}
		}
	}
}

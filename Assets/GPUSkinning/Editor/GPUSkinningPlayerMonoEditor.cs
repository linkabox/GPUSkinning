using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GPUSkinningPlayerMono))]
public class GPUSkinningPlayerMonoEditor : Editor
{
	private GPUSkinningPlayerMono _player;
	private string[] clipsName = null;

	private SerializedProperty animDataProp;
	private SerializedProperty lodProp;
	private SerializedProperty cullingProp;
	private SerializedProperty defaultClipProp;

	private static readonly GUIContent LodDesc = new GUIContent("LOD Enabled");
	private static readonly GUIContent CullingModeDesc = new GUIContent("Culling Mode");

	public override void OnInspectorGUI()
	{
		base.DrawDefaultInspector();

		if (_player == null)
		{
			return;
		}

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(animDataProp);
		if (EditorGUI.EndChangeCheck())
		{
			GPUSkinningAnimation animData = animDataProp.objectReferenceValue as GPUSkinningAnimation;
			if (animData != null)
			{
				var meshFilter = _player.GetComponent<MeshFilter>();
				if (meshFilter != null)
				{
					meshFilter.sharedMesh = animData.defaultMesh;
				}

				var meshRenderer = _player.GetComponent<MeshRenderer>();
				if (meshRenderer != null)
				{
					meshRenderer.sharedMaterial = animData.material;
				}
			}
			serializedObject.ApplyModifiedProperties();
		}

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(lodProp, LodDesc);
		if (EditorGUI.EndChangeCheck())
		{
			if (Application.isPlaying)
			{
				_player.Player.LODEnabled = lodProp.boolValue;
			}
		}

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(cullingProp, CullingModeDesc);
		if (EditorGUI.EndChangeCheck())
		{
			if (Application.isPlaying)
			{
				_player.Player.CullingMode =
					cullingProp.enumValueIndex == 0 ? GPUSKinningCullingMode.AlwaysAnimate :
					cullingProp.enumValueIndex == 1 ? GPUSKinningCullingMode.CullUpdateTransforms : GPUSKinningCullingMode.CullCompletely;
			}
		}

		if (clipsName != null)
		{
			EditorGUI.BeginChangeCheck();
			defaultClipProp.intValue = EditorGUILayout.Popup("Default Playing", defaultClipProp.intValue, clipsName);
			if (EditorGUI.EndChangeCheck())
			{
				if (Application.isPlaying)
				{
					_player.Player.Play(defaultClipProp.intValue);
				}
			}
		}
		serializedObject.ApplyModifiedProperties();

		if (Application.isPlaying)
		{
			EditorGUILayout.BeginVertical("HelpBox");
			EditorGUILayout.PrefixLabel("All Clips:");
			if (_player.Player != null)
			{
				for (var i = 0; i < _player.Player.RefRes.animData.clips.Length; i++)
				{
					var clip = _player.Player.RefRes.animData.clips[i];
					if (GUILayout.Button(clip.name))
					{
						_player.Player.CrossFade(i, 0.2f);
					}
				}
			}

			EditorGUILayout.EndVertical();
		}
	}

	private void OnEnable()
	{
		_player = target as GPUSkinningPlayerMono;
		animDataProp = serializedObject.FindProperty("animData");
		lodProp = serializedObject.FindProperty("lodEnabled");
		cullingProp = serializedObject.FindProperty("cullingMode");
		defaultClipProp = serializedObject.FindProperty("defaultPlayingClipIndex");

		GPUSkinningAnimation animData = animDataProp.objectReferenceValue as GPUSkinningAnimation;
		if (animData != null && animData.clips != null)
		{
			var clips = new string[animData.clips.Length];
			for (int i = 0; i < animData.clips.Length; ++i)
			{
				clips[i] = animData.clips[i].name;
			}
			this.clipsName = clips;

			defaultClipProp.intValue = Mathf.Clamp(defaultClipProp.intValue, 0, animData.clips.Length);
		}
		else
		{
			this.clipsName = null;
			defaultClipProp.intValue = 0;
		}
	}

	private void OnDisable()
	{

	}
}

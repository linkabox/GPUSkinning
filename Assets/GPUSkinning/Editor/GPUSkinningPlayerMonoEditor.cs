using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GPUSkinningPlayerMono))]
public class GPUSkinningPlayerMonoEditor : Editor
{
	private GPUSkinningPlayerMono _player;
	private float time = 0;
	private string[] clipsName = null;

	private SerializedProperty animDataProp;
	private SerializedProperty lodProp;
	private SerializedProperty cullingProp;
	private SerializedProperty defaultClipProp;
	private SerializedProperty radiusProp;

	private static readonly GUIContent LodDesc = new GUIContent("LOD Enabled");
	private static readonly GUIContent CullingModeDesc = new GUIContent("Culling Mode");
	private static readonly GUIContent RadiusDesc = new GUIContent("Culling Radius");

	public override void OnInspectorGUI()
	{
		if (_player == null)
		{
			return;
		}

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(animDataProp);
		if (EditorGUI.EndChangeCheck())
		{
			serializedObject.ApplyModifiedProperties();
			_player.DeletePlayer();
			_player.InitRes();
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

		GPUSkinningAnimation animData = animDataProp.objectReferenceValue as GPUSkinningAnimation;
		if (clipsName == null && animData != null)
		{
			List<string> list = new List<string>();
			for (int i = 0; i < animData.clips.Length; ++i)
			{
				list.Add(animData.clips[i].name);
			}
			clipsName = list.ToArray();

			defaultClipProp.intValue = Mathf.Clamp(defaultClipProp.intValue, 0, animData.clips.Length);
		}
		if (clipsName != null)
		{
			EditorGUI.BeginChangeCheck();
			defaultClipProp.intValue = EditorGUILayout.Popup("Default Playing", defaultClipProp.intValue, clipsName);
			if (EditorGUI.EndChangeCheck())
			{
				_player.Player.Play(clipsName[defaultClipProp.intValue]);
			}
		}

		EditorGUILayout.PropertyField(radiusProp, RadiusDesc);

		serializedObject.ApplyModifiedProperties();
	}

	private void OnEnable()
	{
		_player = target as GPUSkinningPlayerMono;
		if (_player != null)
		{
			_player.InitRes();
		}

		animDataProp = serializedObject.FindProperty("animData");
		lodProp = serializedObject.FindProperty("lodEnabled");
		cullingProp = serializedObject.FindProperty("cullingMode");
		defaultClipProp = serializedObject.FindProperty("defaultPlayingClipIndex");
		radiusProp = serializedObject.FindProperty("sphereRadius");

		time = Time.realtimeSinceStartup;
		EditorApplication.update += UpdateHandler;
	}

	private void OnDisable()
	{
		EditorApplication.update -= UpdateHandler;
	}

	private void UpdateHandler()
	{
		float deltaTime = Time.realtimeSinceStartup - time;
		time = Time.realtimeSinceStartup;

		if (_player != null)
		{
			_player.Update_Editor(deltaTime);
		}

		foreach (var sceneView in SceneView.sceneViews)
		{
			if (sceneView is SceneView)
			{
				(sceneView as SceneView).Repaint();
			}
		}
	}
}

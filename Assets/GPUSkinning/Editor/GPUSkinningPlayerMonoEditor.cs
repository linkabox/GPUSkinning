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

	public override void OnInspectorGUI()
	{
		if (_player == null)
		{
			return;
		}

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("anim"));
		if (EditorGUI.EndChangeCheck())
		{
			serializedObject.ApplyModifiedProperties();
			_player.DeletePlayer();
			_player.InitRes();
		}

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("mesh"));
		if (EditorGUI.EndChangeCheck())
		{
			serializedObject.ApplyModifiedProperties();
			_player.DeletePlayer();
			_player.InitRes();
		}

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("mtrl"));
		if (EditorGUI.EndChangeCheck())
		{
			serializedObject.ApplyModifiedProperties();
			_player.DeletePlayer();
			_player.InitRes();
		}

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("boneTexture"));
		if (EditorGUI.EndChangeCheck())
		{
			serializedObject.ApplyModifiedProperties();
			_player.DeletePlayer();
			_player.InitRes();
		}

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("rootMotionEnabled"), new GUIContent("Apply Root Motion"));
		if (EditorGUI.EndChangeCheck())
		{
			if (Application.isPlaying)
			{
				_player.Player.RootMotionEnabled = serializedObject.FindProperty("rootMotionEnabled").boolValue;
			}
		}

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("lodEnabled"), new GUIContent("LOD Enabled"));
		if (EditorGUI.EndChangeCheck())
		{
			if (Application.isPlaying)
			{
				_player.Player.LODEnabled = serializedObject.FindProperty("lodEnabled").boolValue;
			}
		}

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("cullingMode"), new GUIContent("Culling Mode"));
		if (EditorGUI.EndChangeCheck())
		{
			if (Application.isPlaying)
			{
				_player.Player.CullingMode =
					serializedObject.FindProperty("cullingMode").enumValueIndex == 0 ? GPUSKinningCullingMode.AlwaysAnimate :
					serializedObject.FindProperty("cullingMode").enumValueIndex == 1 ? GPUSKinningCullingMode.CullUpdateTransforms : GPUSKinningCullingMode.CullCompletely;
			}
		}

		GPUSkinningAnimation anim = serializedObject.FindProperty("anim").objectReferenceValue as GPUSkinningAnimation;
		SerializedProperty defaultPlayingClipIndex = serializedObject.FindProperty("defaultPlayingClipIndex");
		if (clipsName == null && anim != null)
		{
			List<string> list = new List<string>();
			for (int i = 0; i < anim.clips.Length; ++i)
			{
				list.Add(anim.clips[i].name);
			}
			clipsName = list.ToArray();

			defaultPlayingClipIndex.intValue = Mathf.Clamp(defaultPlayingClipIndex.intValue, 0, anim.clips.Length);
		}
		if (clipsName != null)
		{
			EditorGUI.BeginChangeCheck();
			defaultPlayingClipIndex.intValue = EditorGUILayout.Popup("Default Playing", defaultPlayingClipIndex.intValue, clipsName);
			if (EditorGUI.EndChangeCheck())
			{
				_player.Player.Play(clipsName[defaultPlayingClipIndex.intValue]);
			}
		}

		serializedObject.ApplyModifiedProperties();
	}

	private void Awake()
	{
		_player = target as GPUSkinningPlayerMono;
		time = Time.realtimeSinceStartup;
		EditorApplication.update += UpdateHandler;

		if (_player != null)
		{
			_player.InitRes();
		}
	}

	private void OnDestroy()
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

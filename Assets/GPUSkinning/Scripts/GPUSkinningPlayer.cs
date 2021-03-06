﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
[System.Serializable]
#endif
public class GPUSkinningPlayer
{
	public delegate void OnAnimEvent(GPUSkinningPlayer player, int eventId);

	private GameObject go = null;

	private Transform transform = null;

	private MeshRenderer mr = null;

	private MeshFilter mf = null;

	private float time = 0;

	private float timeDiff = 0;

	private Action onPlayFinish;

	private float crossFadeTime = -1;

	private float crossFadeProgress = 0;

	private float lastPlayedTime = 0;

	private GPUSkinningClip lastPlayedClip = null;

	private int lastPlayingFrameIndex = -1;

	private GPUSkinningClip _lastPlayingClip = null;

	public GPUSkinningClip LastPlayingClip
	{
		get { return _lastPlayingClip; }
	}

	private GPUSkinningClip _playingClip = null;

	public GPUSkinningClip PlayingClip
	{
		get { return _playingClip; }
	}

	private GPUSkinningPlayerResources _res = null;

	public GPUSkinningPlayerResources RefRes
	{
		get { return _res; }
	}

	private MaterialPropertyBlock mpb = null;

	//private int rootMotionFrameIndex = -1;

	public event OnAnimEvent onAnimEvent;

	private bool rootMotionEnabled = false;
	public bool RootMotionEnabled
	{
		get
		{
			return rootMotionEnabled;
		}
		set
		{
			//rootMotionFrameIndex = -1;
			rootMotionEnabled = value;
		}
	}

	private GPUSKinningCullingMode cullingMode = GPUSKinningCullingMode.CullUpdateTransforms;
	public GPUSKinningCullingMode CullingMode
	{
		get
		{
			return Application.isPlaying ? cullingMode : GPUSKinningCullingMode.AlwaysAnimate;
		}
		set
		{
			cullingMode = value;
		}
	}

	private bool _visible = false;
	public bool Visible
	{
		get
		{
			return _visible;
		}
		set
		{
			_visible = value;
		}
	}

	private bool lodEnabled = true;
	public bool LODEnabled
	{
		get
		{
			return lodEnabled;
		}
		set
		{
			lodEnabled = value;
			_res.LODSettingChanged(this);
		}
	}

	private bool isPlaying = false;
	public bool IsPlaying
	{
		get
		{
			return isPlaying;
		}
	}

	public string PlayingClipName
	{
		get
		{
			return _playingClip == null ? null : _playingClip.name;
		}
	}

	public Vector3 Position
	{
		get
		{
			return transform == null ? Vector3.zero : transform.position;
		}
	}

	public Vector3 LocalPosition
	{
		get
		{
			return transform == null ? Vector3.zero : transform.localPosition;
		}
	}

	private List<GPUSkinningPlayerJoint> joints = null;
	public List<GPUSkinningPlayerJoint> Joints
	{
		get
		{
			return joints;
		}
	}

	public GPUSkinningWrapMode WrapMode
	{
		get
		{
			return _playingClip == null ? GPUSkinningWrapMode.Once : _playingClip.wrapMode;
		}
	}

	public bool IsTimeAtTheEndOfLoop
	{
		get
		{
			if (_playingClip == null)
			{
				return false;
			}
			else
			{
				return GetFrameIndex() == ((int)(_playingClip.length * _playingClip.fps) - 1);
			}
		}
	}

	public float NormalizedTime
	{
		get
		{
			if (_playingClip == null)
			{
				return 0;
			}
			else
			{
				return (float)GetFrameIndex() / (float)((int)(_playingClip.length * _playingClip.fps) - 1);
			}
		}
		set
		{
			if (_playingClip != null)
			{
				float v = Mathf.Clamp01(value);
				if (WrapMode == GPUSkinningWrapMode.Once)
				{
					this.time = v * _playingClip.length;
				}
				else if (WrapMode == GPUSkinningWrapMode.Loop)
				{
					if (_playingClip.individualDifferenceEnabled)
					{
						this.time = _playingClip.length + v * _playingClip.length - this.timeDiff;
					}
					else
					{
						this.time = v * _playingClip.length;
					}
				}
				else
				{
					throw new System.NotImplementedException();
				}
			}
		}
	}

	public GPUSkinningPlayer(GameObject attachToThisGo, GPUSkinningPlayerResources res)
	{
		go = attachToThisGo;
		transform = go.transform;
		this._res = res;

		mr = go.GetComponent<MeshRenderer>();
		if (mr == null)
		{
			mr = go.AddComponent<MeshRenderer>();
		}
		mf = go.GetComponent<MeshFilter>();
		if (mf == null)
		{
			mf = go.AddComponent<MeshFilter>();
		}

		GPUSkinningMaterial mtrl = GetCurrentMaterial();
		mr.sharedMaterial = mtrl == null ? null : mtrl.material;
		mf.sharedMesh = res.animData.defaultMesh;

		mpb = new MaterialPropertyBlock();

		ConstructJoints();
	}

	public void Play(int index, Action onFinish = null)
	{
		GPUSkinningClip[] clips = _res.animData.clips;
		if (clips == null) return;

		GPUSkinningClip targetClip = null;
		if (index >= 0 && index < clips.Length)
		{
			targetClip = clips[index];
		}

		if (targetClip != null)
		{
			SetNewPlayingClip(targetClip, onFinish);
		}
	}

	public void Play(string clipName, Action onFinish = null)
	{
		GPUSkinningClip[] clips = _res.animData.clips;
		if (clips == null) return;

		int targetIndex = -1;
		for (var i = 0; i < clips.Length; i++)
		{
			if (clips[i].name == clipName)
			{
				targetIndex = i;
				break;
			}
		}

		Play(targetIndex, onFinish);
	}

	public void CrossFade(int index, float fadeLength, Action onFinish = null)
	{
		GPUSkinningClip[] clips = _res.animData.clips;
		if (clips == null) return;

		if (_playingClip == null)
		{
			Play(index, onFinish);
		}
		else
		{
			GPUSkinningClip targetClip = null;
			if (index >= 0 && index < clips.Length)
			{
				targetClip = clips[index];
			}

			if (targetClip != null)
			{
				if (_playingClip != targetClip)
				{
					crossFadeProgress = 0;
					crossFadeTime = fadeLength;
					SetNewPlayingClip(targetClip, onFinish);
				}
				else
				{
					SetNewPlayingClip(targetClip, onFinish);
				}
			}
		}
	}

	public void CrossFade(string clipName, float fadeLength, Action onFinish = null)
	{
		GPUSkinningClip[] clips = _res.animData.clips;
		if (clips == null) return;

		if (_playingClip == null)
		{
			Play(clipName, onFinish);
		}
		else
		{
			int targetIndex = -1;
			for (var i = 0; i < clips.Length; i++)
			{
				if (clips[i].name == clipName)
				{
					targetIndex = i;
					break;
				}
			}

			CrossFade(targetIndex, fadeLength, onFinish);
		}
	}

	public void Stop()
	{
		isPlaying = false;
	}

	public void Resume()
	{
		if (_playingClip != null)
		{
			isPlaying = true;
		}
	}

	public void SetLODMesh(Mesh mesh)
	{
		if (!LODEnabled)
		{
			mesh = _res.animData.defaultMesh;
		}

		if (mf != null && mf.sharedMesh != mesh)
		{
			mf.sharedMesh = mesh;
		}
	}

	public void Update(float timeDelta)
	{
		Update_Internal(timeDelta);
	}

	private void FillEvents(GPUSkinningClip clip, GPUSkinningBetterList<GPUSkinningAnimEvent> events)
	{
		events.Clear();
		if (clip != null && clip.events != null && clip.events.Length > 0)
		{
			events.AddRange(clip.events);
		}
	}

	private void SetNewPlayingClip(GPUSkinningClip clip, Action onFinish = null)
	{
		lastPlayedClip = _playingClip;
		lastPlayedTime = GetCurrentTime();

		isPlaying = true;
		_playingClip = clip;
		//rootMotionFrameIndex = -1;
		time = 0;
		timeDiff = Random.Range(0, _playingClip.length);
		if (clip.wrapMode == GPUSkinningWrapMode.Once)
		{
			onPlayFinish = onFinish;
		}
		else
		{
			onPlayFinish = null;
		}
	}

	private void Update_Internal(float timeDelta)
	{
		if (!isPlaying || _playingClip == null)
		{
			return;
		}

		GPUSkinningMaterial currMtrl = GetCurrentMaterial();
		if (currMtrl == null)
		{
			return;
		}

		if (mr.sharedMaterial != currMtrl.material)
		{
			mr.sharedMaterial = currMtrl.material;
		}

		if (_playingClip.wrapMode == GPUSkinningWrapMode.Loop)
		{
			time += timeDelta;
			if (time > _playingClip.length)
			{
				time = 0;
			}
			UpdateMaterial(timeDelta, currMtrl);
		}
		else if (_playingClip.wrapMode == GPUSkinningWrapMode.Once)
		{
			if (time > _playingClip.length)
			{
				time = _playingClip.length;
			}
			else
			{
				time += timeDelta;
				if (time >= _playingClip.length)
				{
					OnPlayClipFinish();
				}
				UpdateMaterial(timeDelta, currMtrl);
			}
		}

		crossFadeProgress += timeDelta;
		lastPlayedTime += timeDelta;
	}

	private void OnPlayClipFinish()
	{
		isPlaying = false;
		if (onPlayFinish != null)
		{
			Action onFinish = this.onPlayFinish;
			this.onPlayFinish = null;
			onFinish();
		}
	}

	private void UpdateEvents(GPUSkinningClip playingClip, int playingFrameIndex, GPUSkinningClip corssFadeClip, int crossFadeFrameIndex)
	{
		UpdateClipEvent(playingClip, playingFrameIndex);
		UpdateClipEvent(corssFadeClip, crossFadeFrameIndex);
	}

	private void UpdateClipEvent(GPUSkinningClip clip, int frameIndex)
	{
		if (clip == null || clip.events == null || clip.events.Length == 0)
		{
			return;
		}

		GPUSkinningAnimEvent[] events = clip.events;
		int numEvents = events.Length;
		for (int i = 0; i < numEvents; ++i)
		{
			if (events[i].frameIndex == frameIndex && onAnimEvent != null)
			{
				onAnimEvent(this, events[i].eventId);
				break;
			}
		}
	}

	private void UpdateMaterial(float deltaTime, GPUSkinningMaterial currMtrl)
	{
		int frameIndex = GetFrameIndex();
		if (_lastPlayingClip == _playingClip && lastPlayingFrameIndex == frameIndex)
		{
			_res.Update(deltaTime, currMtrl);
			return;
		}
		_lastPlayingClip = _playingClip;
		lastPlayingFrameIndex = frameIndex;

		//float blend_crossFade = 1;
		int frameIndex_crossFade = -1;
		GPUSkinningFrame frame_crossFade = null;
		if (_res.IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
		{
			frameIndex_crossFade = GetCrossFadeFrameIndex();
			frame_crossFade = lastPlayedClip.frames[frameIndex_crossFade];
			//blend_crossFade = res.CrossFadeBlendFactor(crossFadeProgress, crossFadeTime);
		}

		GPUSkinningFrame frame = _playingClip.frames[frameIndex];
		if (Visible ||
			CullingMode == GPUSKinningCullingMode.AlwaysAnimate)
		{
			_res.Update(deltaTime, currMtrl);
			_res.UpdatePlayingData(
				mpb, _playingClip, frameIndex, frame, _playingClip.rootMotionEnabled && rootMotionEnabled,
				lastPlayedClip, frameIndex_crossFade, crossFadeTime, crossFadeProgress
			);
			mr.SetPropertyBlock(mpb);
			UpdateJoints(frame);
		}

		//if (playingClip.rootMotionEnabled && rootMotionEnabled && frameIndex != rootMotionFrameIndex)
		//{
		//    if (CullingMode != GPUSKinningCullingMode.CullCompletely)
		//    {
		//        rootMotionFrameIndex = frameIndex;
		//        DoRootMotion(frame_crossFade, 1 - blend_crossFade, false);
		//        DoRootMotion(frame, blend_crossFade, true);
		//    }
		//}

		UpdateEvents(_playingClip, frameIndex, frame_crossFade == null ? null : lastPlayedClip, frameIndex_crossFade);
	}

	private GPUSkinningMaterial GetCurrentMaterial()
	{
		if (_res == null)
		{
			return null;
		}
#if DISABLE_SKIN_BLEND
		return _res.GetMaterial(GPUSkinningPlayerResources.MaterialState.BlendOff);
#else
		if (_playingClip == null)
		{
			return _res.GetMaterial(GPUSkinningPlayerResources.MaterialState.BlendOff);
		}

		if (_res.IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
		{
			return _res.GetMaterial(GPUSkinningPlayerResources.MaterialState.BlendOn);
		}

		return _res.GetMaterial(GPUSkinningPlayerResources.MaterialState.BlendOff);
#endif
	}

	private void DoRootMotion(GPUSkinningFrame frame, float blend, bool doRotate)
	{
		if (frame == null)
		{
			return;
		}

		Quaternion deltaRotation = frame.rootMotionDeltaPositionQ;
		Vector3 newForward = deltaRotation * transform.forward;
		Vector3 deltaPosition = newForward * frame.rootMotionDeltaPositionL * blend;
		transform.Translate(deltaPosition, Space.World);

		if (doRotate)
		{
			transform.rotation *= frame.rootMotionDeltaRotation;
		}
	}

	private float GetCurrentTime()
	{
		float time = 0;
		if (WrapMode == GPUSkinningWrapMode.Once)
		{
			time = this.time;
		}
		else if (WrapMode == GPUSkinningWrapMode.Loop)
		{
			time = this.time + (_playingClip.individualDifferenceEnabled ? this.timeDiff : 0);
		}
		else
		{
			throw new System.NotImplementedException();
		}
		return time;
	}

	private int GetFrameIndex()
	{
		float time = GetCurrentTime();
		if (Mathf.Abs(_playingClip.length - time) < 0.01f)
		{
			return GetTheLastFrameIndex_WrapMode_Once(_playingClip);
		}
		else
		{
			return GetFrameIndex_WrapMode_Loop(_playingClip, time);
		}
	}

	private int GetCrossFadeFrameIndex()
	{
		if (lastPlayedClip == null)
		{
			return 0;
		}

		if (lastPlayedClip.wrapMode == GPUSkinningWrapMode.Once)
		{
			if (lastPlayedTime >= lastPlayedClip.length)
			{
				return GetTheLastFrameIndex_WrapMode_Once(lastPlayedClip);
			}
			else
			{
				return GetFrameIndex_WrapMode_Loop(lastPlayedClip, lastPlayedTime);
			}
		}
		else if (lastPlayedClip.wrapMode == GPUSkinningWrapMode.Loop)
		{
			return GetFrameIndex_WrapMode_Loop(lastPlayedClip, lastPlayedTime);
		}
		else
		{
			throw new System.NotImplementedException();
		}
	}

	private int GetTheLastFrameIndex_WrapMode_Once(GPUSkinningClip clip)
	{
		return (int)(clip.length * clip.fps) - 1;
	}

	private int GetFrameIndex_WrapMode_Loop(GPUSkinningClip clip, float time)
	{
		return (int)(time * clip.fps) % (int)(clip.length * clip.fps);
	}

	private void UpdateJoints(GPUSkinningFrame frame)
	{
		if (joints == null || frame.jointMatrices == null || frame.jointMatrices.Length == 0)
		{
			return;
		}

		//GPUSkinningBone[] bones = res.anim.bones;
		int numJoints = joints.Count;
		for (int i = 0; i < numJoints; ++i)
		{
			GPUSkinningPlayerJoint joint = joints[i];
			Transform jointTransform = Application.isPlaying ? joint.Transform : joint.transform;
			if (jointTransform != null)
			{
				// TODO: Update Joint when Animation Blend

				Matrix4x4 jointMatrix = frame.jointMatrices[i];
				//if (playingClip.rootMotionEnabled && rootMotionEnabled)
				//{
				//	jointMatrix = frame.rootMotionInv * jointMatrix;
				//}

				jointTransform.localPosition = jointMatrix.MultiplyPoint(Vector3.zero);

				Vector3 jointDir = jointMatrix.MultiplyVector(Vector3.right);
				Quaternion jointRotation = Quaternion.FromToRotation(Vector3.right, jointDir);
				jointTransform.localRotation = jointRotation;
			}
			else
			{
				joints.RemoveAt(i);
				--i;
				--numJoints;
			}
		}
	}

	private void ConstructJoints()
	{
		if (joints == null)
		{
			GPUSkinningPlayerJoint[] existingJoints = go.GetComponentsInChildren<GPUSkinningPlayerJoint>();

			GPUSkinningBone[] bones = _res.animData.bones;
			int numBones = bones == null ? 0 : bones.Length;
			for (int i = 0; i < numBones; ++i)
			{
				GPUSkinningBone bone = bones[i];
				if (bone.isExposed)
				{
					if (joints == null)
					{
						joints = new List<GPUSkinningPlayerJoint>();
					}

					bool inTheExistingJoints = false;
					if (existingJoints != null)
					{
						for (int j = 0; j < existingJoints.Length; ++j)
						{
							if (existingJoints[j] != null && existingJoints[j].BoneGUID == bone.guid)
							{
								if (existingJoints[j].BoneIndex != i)
								{
									existingJoints[j].Init(i, bone.guid);
									GPUSkinningUtil.MarkAllScenesDirty();
								}
								joints.Add(existingJoints[j]);
								existingJoints[j] = null;
								inTheExistingJoints = true;
								break;
							}
						}
					}

					if (!inTheExistingJoints)
					{
						GameObject jointGo = new GameObject(bone.name);
						jointGo.transform.parent = go.transform;
						jointGo.transform.localPosition = Vector3.zero;
						jointGo.transform.localScale = Vector3.one;

						GPUSkinningPlayerJoint joint = jointGo.AddComponent<GPUSkinningPlayerJoint>();
						joints.Add(joint);
						joint.Init(i, bone.guid);
						GPUSkinningUtil.MarkAllScenesDirty();
					}
				}
			}

			if (!Application.isPlaying)
			{
#if UNITY_EDITOR
				UnityEditor.EditorApplication.CallbackFunction DelayCall = null;
				DelayCall = () =>
				{
					UnityEditor.EditorApplication.delayCall -= DelayCall;
					DeleteInvalidJoints(existingJoints);
				};
				UnityEditor.EditorApplication.delayCall += DelayCall;
#endif
			}
			else
			{
				DeleteInvalidJoints(existingJoints);
			}
		}
	}

	private void DeleteInvalidJoints(GPUSkinningPlayerJoint[] joints)
	{
		if (joints != null)
		{
			for (int i = 0; i < joints.Length; ++i)
			{
				if (joints[i] != null)
				{
					for (int j = 0; j < joints[i].transform.childCount; ++j)
					{
						Transform child = joints[i].transform.GetChild(j);
						child.parent = go.transform;
						child.localPosition = Vector3.zero;
					}
					Object.DestroyImmediate(joints[i].transform.gameObject);
					GPUSkinningUtil.MarkAllScenesDirty();
				}
			}
		}
	}

	public void Destroy()
	{
		if (_res != null)
		{
			_res.Destroy();
			_res = null;
		}
	}
}

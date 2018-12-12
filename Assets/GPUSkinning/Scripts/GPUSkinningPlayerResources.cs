using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
[System.Serializable]
#endif
public class GPUSkinningPlayerResources
{
	private static readonly int shaderPropID_BoneTexture = Shader.PropertyToID("_boneTexture");

	private static readonly int shaderPropID_BoneTextureParams = Shader.PropertyToID("_boneTextureParams");

	private static readonly int shaderPorpID_FrameInfo = Shader.PropertyToID("_frameInfo");

#if !DISABLE_SKIN_BLEND
	private static readonly int shaderPorpID_BlendInfo = Shader.PropertyToID("_blendInfo");
#endif

	//private static int shaderPropID_RootMotion = Shader.PropertyToID("_rootMotion");
	//private static int shaderPropID_Blend_RootMotion = Shader.PropertyToID("_blendRootMotion");

	public enum MaterialState
	{
#if DISABLE_SKIN_BLEND
		BlendOff,
		Count = 1
#else
		BlendOff,
		BlendOn,
		Count = 2
#endif
	}

	public GPUSkinningAnimation animData = null;

	public List<GPUSkinningPlayerMono> players = new List<GPUSkinningPlayerMono>();

	private CullingGroup _cullingGroup = null;

	private GPUSkinningBetterList<BoundingSphere> _cullingBounds = new GPUSkinningBetterList<BoundingSphere>(100);

	private GPUSkinningMaterial[] mtrls = null;

#if DISABLE_SKIN_BLEND
	private static readonly string[] ShaderKeywords = { "BLEND_OFF", };
#else
	private static readonly string[] ShaderKeywords = { "BLEND_OFF", "BLEND_ON", };
#endif

	private readonly GPUSkinningExecuteOncePerFrame _executeOncePerFrame = new GPUSkinningExecuteOncePerFrame();

	private bool _dispose;

	//private float time = 0;
	//public float Time
	//{
	//	get
	//	{
	//		return time;
	//	}
	//	set
	//	{
	//		time = value;
	//	}
	//}

	public GPUSkinningPlayerResources(GPUSkinningAnimation animData, HideFlags matHideFlags)
	{
		this.animData = animData;
		InitMaterial(animData.material, matHideFlags);
	}

	public void AddPlayer(GPUSkinningPlayerMono player)
	{
		players.Add(player);
		AddCullingBounds();
	}

	public void Destroy()
	{
		if (_dispose) return;

		animData = null;

		if (_cullingBounds != null)
		{
			_cullingBounds.Release();
			_cullingBounds = null;
		}

		DestroyCullingGroup();

		if (mtrls != null)
		{
			for (int i = 0; i < mtrls.Length; ++i)
			{
				mtrls[i].Destroy();
				mtrls[i] = null;
			}
			mtrls = null;
		}

		if (players != null)
		{
			players.Clear();
			players = null;
		}

		_dispose = true;
	}

	public BoundingSphere GetCullingBounds(int index)
	{
		if (players != null && _cullingBounds != null && index < players.Count)
		{
			return _cullingBounds[index];
		}

		return new BoundingSphere();
	}

	public void AddCullingBounds()
	{
		if (_cullingGroup == null)
		{
			_cullingGroup = new CullingGroup();
			_cullingGroup.targetCamera = Camera.main;
			_cullingGroup.SetBoundingDistances(animData.lodDistances);
			_cullingGroup.SetDistanceReferencePoint(Camera.main.transform);
			_cullingGroup.onStateChanged = OnLodCullingGroupOnStateChangedHandler;
		}

		_cullingBounds.Add(new BoundingSphere());
		_cullingGroup.SetBoundingSpheres(_cullingBounds.buffer);
		_cullingGroup.SetBoundingSphereCount(players.Count);
	}

	public void RemoveCullingBounds(int index)
	{
		_cullingBounds.RemoveAt(index);
		_cullingGroup.SetBoundingSpheres(_cullingBounds.buffer);
		_cullingGroup.SetBoundingSphereCount(players.Count);
	}

	public void LODSettingChanged(GPUSkinningPlayer player)
	{
		if (player.LODEnabled)
		{
			int numPlayers = players.Count;
			for (int i = 0; i < numPlayers; ++i)
			{
				if (players[i].Player == player)
				{
					int distanceIndex = _cullingGroup.GetDistance(i);
					SetLODMeshByDistanceIndex(distanceIndex, players[i].Player);
					break;
				}
			}
		}
		else
		{
			player.SetLODMesh(null);
		}
	}

	private void OnLodCullingGroupOnStateChangedHandler(CullingGroupEvent evt)
	{
		GPUSkinningPlayerMono player = players[evt.index];
		if (evt.isVisible)
		{
			SetLODMeshByDistanceIndex(evt.currentDistance, player.Player);
			player.Player.Visible = true;
		}
		else
		{
			player.Player.Visible = false;
		}
	}

	private void DestroyCullingGroup()
	{
		if (_cullingGroup != null)
		{
			_cullingGroup.Dispose();
			_cullingGroup = null;
		}
	}

	private void SetLODMeshByDistanceIndex(int index, GPUSkinningPlayer player)
	{
		Mesh lodMesh = null;
		if (index == 0)
		{
			lodMesh = this.animData.defaultMesh;
		}
		else
		{
			Mesh[] lodMeshes = animData.lodMeshes;
			lodMesh = lodMeshes == null || lodMeshes.Length == 0 ? this.animData.defaultMesh : lodMeshes[Mathf.Min(index - 1, lodMeshes.Length - 1)];
			if (lodMesh == null) lodMesh = this.animData.defaultMesh;
		}
		player.SetLODMesh(lodMesh);
	}

	private void UpdateCullingBounds()
	{
		int numPlayers = players.Count;
		for (int i = 0; i < numPlayers; ++i)
		{
			GPUSkinningPlayerMono player = players[i];
			BoundingSphere bounds = _cullingBounds[i];
			bounds.position = player.Player.Position;
			bounds.radius = player.cullingRadius;
			_cullingBounds[i] = bounds;
		}
	}

	public void Update(float deltaTime, GPUSkinningMaterial mtrl)
	{
		if (_executeOncePerFrame.CanBeExecute())
		{
			_executeOncePerFrame.MarkAsExecuted();
			UpdateCullingBounds();
		}

		//if (mtrl.executeOncePerFrame.CanBeExecute())
		//{
		//	mtrl.executeOncePerFrame.MarkAsExecuted();
		//	mtrl.material.SetTexture(shaderPropID_BoneTexture, this.animData.boneTexture);
		//	mtrl.material.SetVector(shaderPropID_BoneTextureParams,
		//		new Vector4(animData.textureWidth, animData.textureHeight, animData.bones.Length * 3/*treat 3 pixels as a float3x4*/, 0));
		//}
	}

	public void UpdatePlayingData(
		MaterialPropertyBlock mpb, GPUSkinningClip playingClip, int frameIndex, GPUSkinningFrame frame, bool rootMotionEnabled,
		GPUSkinningClip lastPlayedClip, int frameIndex_crossFade, float crossFadeTime, float crossFadeProgress)
	{
		mpb.SetVector(shaderPorpID_FrameInfo, new Vector4(frameIndex, playingClip.pixelSegmentation, 0, 0));
		//if (rootMotionEnabled)
		//{
		//	Matrix4x4 rootMotionInv = frame.rootMotionInv;
		//	mpb.SetMatrix(shaderPropID_RootMotion, rootMotionInv);
		//}

#if DISABLE_SKIN_BLEND

#else
		if (IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
		{
			//if (lastPlayedClip.rootMotionEnabled)
			//{
			//	mpb.SetMatrix(shaderPropID_Blend_RootMotion, lastPlayedClip.frames[frameIndex_crossFade].rootMotionInv);
			//}

			mpb.SetVector(shaderPorpID_BlendInfo,
				new Vector4(frameIndex_crossFade, lastPlayedClip.pixelSegmentation, CrossFadeBlendFactor(crossFadeProgress, crossFadeTime)));
		}
		else
		{
			mpb.SetVector(shaderPorpID_BlendInfo,
				new Vector4(0, 0, 1, 0));
		}
#endif
	}

	public float CrossFadeBlendFactor(float crossFadeProgress, float crossFadeTime)
	{
		return Mathf.Clamp01(crossFadeProgress / crossFadeTime);
	}

	public bool IsCrossFadeBlending(GPUSkinningClip lastPlayedClip, float crossFadeTime, float crossFadeProgress)
	{
#if DISABLE_SKIN_BLEND
		return false;
#else
		return lastPlayedClip != null && crossFadeTime > 0 && crossFadeProgress <= crossFadeTime;
#endif
	}

	public GPUSkinningMaterial GetMaterial(MaterialState state)
	{
		return mtrls[(int)state];
	}

	public void InitMaterial(Material originalMaterial, HideFlags hideFlags)
	{
		if (mtrls != null)
		{
			return;
		}

		mtrls = new GPUSkinningMaterial[(int)MaterialState.Count];

		for (int i = 0; i < mtrls.Length; ++i)
		{
			mtrls[i] = new GPUSkinningMaterial() { material = new Material(originalMaterial) };
			mtrls[i].material.name = originalMaterial.name + "_" + animData.skinQuality + "_" + ShaderKeywords[i];
			mtrls[i].material.hideFlags = hideFlags;
			//mtrls[i].material.enableInstancing = true; // enable instancing in Unity 5.6
			mtrls[i].material.EnableKeyword(animData.skinQuality.ToString());
			mtrls[i].material.SetTexture(shaderPropID_BoneTexture, this.animData.boneTexture);
			mtrls[i].material.SetVector(shaderPropID_BoneTextureParams,
				new Vector4(animData.textureWidth, animData.textureHeight, animData.bones.Length * 3/*treat 3 pixels as a float3x4*/, 0));
			EnableKeywords(i, mtrls[i]);
		}
	}

	private void EnableKeywords(int ki, GPUSkinningMaterial mtrl)
	{
		for (int i = 0; i < mtrls.Length; ++i)
		{
			if (i == ki)
			{
				mtrl.material.EnableKeyword(ShaderKeywords[i]);
			}
			else
			{
				mtrl.material.DisableKeyword(ShaderKeywords[i]);
			}
		}
	}

	public void ChangeShader(Shader shader)
	{
		for (int i = 0; i < mtrls.Length; ++i)
		{
			mtrls[i].material.shader = shader;
			mtrls[i].material.SetTexture(shaderPropID_BoneTexture, this.animData.boneTexture);
			mtrls[i].material.SetVector(shaderPropID_BoneTextureParams,
				new Vector4(animData.textureWidth, animData.textureHeight, animData.bones.Length * 3/*treat 3 pixels as a float3x4*/, 0));
		}
	}
}

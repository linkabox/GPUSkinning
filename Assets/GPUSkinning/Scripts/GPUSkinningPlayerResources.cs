using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUSkinningPlayerResources
{
	public enum MaterialState
	{
		BlendOff,
		BlendOn,
		Count = 2
	}

	public GPUSkinningAnimation anim = null;

	public Mesh mesh = null;

	public Texture2D boneTexture = null;

	public List<GPUSkinningPlayerMono> players = new List<GPUSkinningPlayerMono>();

	private CullingGroup _cullingGroup = null;

	private GPUSkinningBetterList<BoundingSphere> _cullingBounds = new GPUSkinningBetterList<BoundingSphere>(100);

	private GPUSkinningMaterial[] mtrls = null;

	private static readonly string[] ShaderKeywords = { "BLEND_OFF", "BLEND_ON", };

	private readonly GPUSkinningExecuteOncePerFrame _executeOncePerFrame = new GPUSkinningExecuteOncePerFrame();

	private float time = 0;
	public float Time
	{
		get
		{
			return time;
		}
		set
		{
			time = value;
		}
	}

	private static int shaderPropID_BoneTexture = -1;

	private static int shaderPropID_BoneTextureParams = 0;

	private static int shaderPorpID_FrameInfo = 0;

	//private static int shaderPropID_RootMotion = 0;

	private static int shaderPorpID_BlendInfo = 0;

	//private static int shaderPropID_Blend_RootMotion = 0;

	public GPUSkinningPlayerResources()
	{
		if (shaderPropID_BoneTexture == -1)
		{
			shaderPropID_BoneTexture = Shader.PropertyToID("_boneTexture");
			shaderPropID_BoneTextureParams = Shader.PropertyToID("_boneTextureParams");
			shaderPorpID_FrameInfo = Shader.PropertyToID("_frameInfo");
			shaderPorpID_BlendInfo = Shader.PropertyToID("_blendInfo");
			//shaderPropID_RootMotion = Shader.PropertyToID("_rootMotion");
			//shaderPropID_Blend_RootMotion = Shader.PropertyToID("_blendRootMotion");
		}
	}

	public void Destroy()
	{
		anim = null;
		mesh = null;

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

		//if (boneTexture != null)
		//{
		//    Object.DestroyImmediate(boneTexture);
		//    boneTexture = null;
		//}
		boneTexture = null;

		if (players != null)
		{
			players.Clear();
			players = null;
		}
	}

	public void AddCullingBounds()
	{
		if (_cullingGroup == null)
		{
			_cullingGroup = new CullingGroup();
			_cullingGroup.targetCamera = Camera.main;
			_cullingGroup.SetBoundingDistances(anim.lodDistances);
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
			lodMesh = this.mesh;
		}
		else
		{
			Mesh[] lodMeshes = anim.lodMeshes;
			lodMesh = lodMeshes == null || lodMeshes.Length == 0 ? this.mesh : lodMeshes[Mathf.Min(index - 1, lodMeshes.Length - 1)];
			if (lodMesh == null) lodMesh = this.mesh;
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
			bounds.radius = anim.sphereRadius;
			_cullingBounds[i] = bounds;
		}
	}

	public void Update(float deltaTime, GPUSkinningMaterial mtrl)
	{
		if (_executeOncePerFrame.CanBeExecute())
		{
			_executeOncePerFrame.MarkAsExecuted();
			time += deltaTime;
			UpdateCullingBounds();
		}

		if (mtrl.executeOncePerFrame.CanBeExecute())
		{
			mtrl.executeOncePerFrame.MarkAsExecuted();
			mtrl.material.SetTexture(shaderPropID_BoneTexture, boneTexture);
			mtrl.material.SetVector(shaderPropID_BoneTextureParams,
				new Vector4(anim.textureWidth, anim.textureHeight, anim.bones.Length * 3/*treat 3 pixels as a float3x4*/, 0));
		}
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
	}

	public float CrossFadeBlendFactor(float crossFadeProgress, float crossFadeTime)
	{
		return Mathf.Clamp01(crossFadeProgress / crossFadeTime);
	}

	public bool IsCrossFadeBlending(GPUSkinningClip lastPlayedClip, float crossFadeTime, float crossFadeProgress)
	{
		return lastPlayedClip != null && crossFadeTime > 0 && crossFadeProgress <= crossFadeTime;
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
			mtrls[i].material.name = anim.skinQuality + "_" + ShaderKeywords[i];
			mtrls[i].material.hideFlags = hideFlags;
			mtrls[i].material.enableInstancing = true; // enable instancing in Unity 5.6
			mtrls[i].material.EnableKeyword(anim.skinQuality.ToString());
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
}

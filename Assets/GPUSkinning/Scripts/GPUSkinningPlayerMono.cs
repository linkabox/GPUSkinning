using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class GPUSkinningPlayerMono : MonoBehaviour
{
	[HideInInspector]
	[SerializeField]
	private GPUSkinningAnimation anim = null;

	[HideInInspector]
	[SerializeField]
	private Mesh mesh = null;

	[HideInInspector]
	[SerializeField]
	private Material mtrl = null;

	[HideInInspector]
	[SerializeField]
	private Texture2D boneTexture = null;

	[HideInInspector]
	[SerializeField]
	private int defaultPlayingClipIndex = 0;

	[HideInInspector]
	[SerializeField]
	private bool rootMotionEnabled = false;

	[HideInInspector]
	[SerializeField]
	private bool lodEnabled = true;

	[HideInInspector]
	[SerializeField]
	private GPUSKinningCullingMode cullingMode = GPUSKinningCullingMode.CullUpdateTransforms;

	private GPUSkinningPlayer player = null;
	public GPUSkinningPlayer Player
	{
		get
		{
			return player;
		}
	}

	public void InitRes(GPUSkinningAnimation anim = null, Mesh mesh = null, Material mtrl = null, Texture2D boneTexture = null)
	{
		if (player != null)
		{
			return;
		}

		if (anim != null)
			this.anim = anim;
		if (mesh != null)
			this.mesh = mesh;
		if (mtrl != null)
			this.mtrl = mtrl;
		if (boneTexture != null)
			this.boneTexture = boneTexture;

		var res = new GPUSkinningPlayerResources
		{
			anim = this.anim,
			mesh = this.mesh,
			boneTexture = this.boneTexture
		};
		res.InitMaterial(this.mtrl, HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor);
		Init(res);
	}

	public void Init(GPUSkinningPlayerResources res = null)
	{
		if (player != null)
		{
			return;
		}

		if (anim != null && mesh != null && mtrl != null && boneTexture != null)
		{
			if (res == null)
			{
				GPUSkinningPlayerMgr.Instance.Register(anim, mesh, mtrl, boneTexture, this, out res);
			}

			player = new GPUSkinningPlayer(gameObject, res);
			player.RootMotionEnabled = rootMotionEnabled;
			player.LODEnabled = lodEnabled;
			player.CullingMode = cullingMode;

			if (anim != null && anim.clips != null && anim.clips.Length > 0)
			{
				int defaultClip = Mathf.Clamp(defaultPlayingClipIndex, 0, anim.clips.Length);
				player.Play(anim.clips[defaultClip].name);
			}
		}
	}

#if UNITY_EDITOR
	public void DeletePlayer()
	{
		if (player != null)
		{
			player.Destroy();
		}
		player = null;
	}

	public void Update_Editor(float deltaTime)
	{
		if (player != null && !Application.isPlaying)
		{
			player.Update_Editor(deltaTime);
		}
	}

	//private void OnValidate()
	//{
	//	if (!Application.isPlaying)
	//	{
	//		Init();
	//		Update_Editor(0);
	//	}
	//}
#endif

	private void Awake()
	{
		Init();
#if UNITY_EDITOR
		Update_Editor(0);
#endif
	}

	private void Update()
	{
		if (player != null)
		{
#if UNITY_EDITOR
			if (Application.isPlaying)
			{
				player.Update(Time.deltaTime);
			}
			else
			{
				player.Update_Editor(0);
			}
#else
            player.Update(Time.deltaTime);
#endif
		}
	}

	private void OnDestroy()
	{
#if UNITY_EDITOR
		if (Application.isPlaying)
		{
			if (!GPUSkinningPlayerMgr.IsDestroy())
			{
				GPUSkinningPlayerMgr.Instance.Unregister(this);
			}
		}
		else
		{
			//Editor Mode Manual Destroy
			DeletePlayer();
		}
#else
		if (!GPUSkinningPlayerMgr.IsDestroy())
		{
			GPUSkinningPlayerMgr.Instance.Unregister(this);
		}
#endif

		player = null;
		anim = null;
		mesh = null;
		mtrl = null;
		boneTexture = null;
	}
}

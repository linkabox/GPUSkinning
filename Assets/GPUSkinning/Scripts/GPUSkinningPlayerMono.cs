using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
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

	public void Init(GPUSkinningAnimation anim, Mesh mesh, Material mtrl, Texture2D boneTexture)
	{
		if (player != null)
		{
			return;
		}

		this.anim = anim;
		this.mesh = mesh;
		this.mtrl = mtrl;
		this.boneTexture = boneTexture;
		Init();
	}

	public void Init()
	{
		if (player != null)
		{
			return;
		}

		bool isPlaying = Application.isPlaying;
		if (anim != null && mesh != null && mtrl != null && boneTexture != null)
		{
			GPUSkinningPlayerResources res = null;

			if (isPlaying)
			{
				GPUSkinningPlayerMgr.Instance.Register(anim, mesh, mtrl, boneTexture, this, out res);
			}
			else
			{
				res = new GPUSkinningPlayerResources();
				res.anim = anim;
				res.mesh = mesh;
				res.InitMaterial(mtrl, HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor);
				res.boneTexture = boneTexture;
			}

			player = new GPUSkinningPlayer(gameObject, res);
			player.RootMotionEnabled = isPlaying && rootMotionEnabled;
			player.LODEnabled = isPlaying && lodEnabled;
			player.CullingMode = cullingMode;

			if (anim != null && anim.clips != null && anim.clips.Length > 0)
			{
				player.Play(anim.clips[Mathf.Clamp(defaultPlayingClipIndex, 0, anim.clips.Length)].name);
			}
		}
	}

#if UNITY_EDITOR
	public void DeletePlayer()
	{
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
			player.Res.Destroy();
		}

		player = null;
		anim = null;
		mesh = null;
		mtrl = null;
		boneTexture = null;

#if UNITY_EDITOR
		if (!Application.isPlaying)
		{
			Resources.UnloadUnusedAssets();
			UnityEditor.EditorUtility.UnloadUnusedAssetsImmediate();
		}
#endif
	}
}

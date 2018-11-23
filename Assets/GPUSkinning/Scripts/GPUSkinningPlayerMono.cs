using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class GPUSkinningPlayerMono : MonoBehaviour
{
	[HideInInspector]
	[SerializeField]
	private GPUSkinningAnimation animData = null;

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

	public void InitRes(GPUSkinningAnimation anim = null)
	{
		if (player != null)
		{
			return;
		}

		if (anim != null)
			this.animData = anim;

		if (this.animData != null)
		{
			var res = new GPUSkinningPlayerResources
			{
				animData = this.animData,
			};
			res.InitMaterial(this.animData.material, HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor);
			Init(res);
		}
	}

	public void Init(GPUSkinningPlayerResources res = null)
	{
		if (player != null)
		{
			return;
		}

		if (animData != null)
		{
			if (res == null)
			{
				GPUSkinningPlayerMgr.Instance.Register(animData, this, out res);
			}

			player = new GPUSkinningPlayer(gameObject, res);
			player.RootMotionEnabled = rootMotionEnabled;
			player.LODEnabled = lodEnabled;
			player.CullingMode = cullingMode;

			if (animData != null && animData.clips != null && animData.clips.Length > 0)
			{
				int defaultClip = Mathf.Clamp(defaultPlayingClipIndex, 0, animData.clips.Length);
				player.Play(animData.clips[defaultClip].name);
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
		animData = null;
	}
}

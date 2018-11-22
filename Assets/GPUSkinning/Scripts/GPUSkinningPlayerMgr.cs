using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUSkinningPlayerMgr : Singleton<GPUSkinningPlayerMgr>
{
	private readonly List<GPUSkinningPlayerResources> _playerResList = new List<GPUSkinningPlayerResources>();

	public void Register(GPUSkinningAnimation anim, Mesh mesh, Material originalMtrl, Texture2D boneTexture, GPUSkinningPlayerMono player, out GPUSkinningPlayerResources result)
	{
		result = null;

		if (anim == null || originalMtrl == null || boneTexture == null || player == null)
		{
			return;
		}

		GPUSkinningPlayerResources res = null;

		int numItems = _playerResList.Count;
		for (int i = 0; i < numItems; ++i)
		{
			if (_playerResList[i].anim.guid == anim.guid)
			{
				res = _playerResList[i];
				break;
			}
		}

		if (res == null)
		{
			res = new GPUSkinningPlayerResources();
			_playerResList.Add(res);
		}

		if (res.anim == null)
		{
			res.anim = anim;
		}

		if (res.mesh == null)
		{
			res.mesh = mesh;
		}

		res.InitMaterial(originalMtrl, HideFlags.None);

		if (res.boneTexture == null)
		{
			res.boneTexture = boneTexture; //GPUSkinningUtil.CreateTexture2D(textureRawData, anim);
		}

		if (!res.players.Contains(player))
		{
			res.players.Add(player);
			res.AddCullingBounds();
		}

		result = res;
	}

	public void Unregister(GPUSkinningPlayerMono player)
	{
		if (player == null)
		{
			return;
		}

		int numItems = _playerResList.Count;
		for (int i = 0; i < numItems; ++i)
		{
			int playerIndex = _playerResList[i].players.IndexOf(player);
			if (playerIndex != -1)
			{
				_playerResList[i].players.RemoveAt(playerIndex);
				_playerResList[i].RemoveCullingBounds(playerIndex);
				if (_playerResList[i].players.Count == 0)
				{
					_playerResList[i].Destroy();
					_playerResList.RemoveAt(i);
				}
				break;
			}
		}
	}

	public override void OnDispose()
	{
		Debug.Log("GPUSkinningPlayerMgr OnDispose:" + _playerResList.Count);

		foreach (var res in _playerResList)
		{
			res.Destroy();
		}
		_playerResList.Clear();
	}
}

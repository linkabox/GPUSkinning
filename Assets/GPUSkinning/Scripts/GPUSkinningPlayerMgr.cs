using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUSkinningPlayerMgr : Singleton<GPUSkinningPlayerMgr>
{
	public bool showCullingBounds;

	private readonly List<GPUSkinningPlayerResources> _refResList = new List<GPUSkinningPlayerResources>();

	public GPUSkinningPlayerResources FindRefRes(string guid)
	{
		int numItems = _refResList.Count;
		for (int i = 0; i < numItems; ++i)
		{
			if (_refResList[i].animData.guid == guid)
			{
				return _refResList[i];
			}
		}

		return null;
	}

	public void Register(GPUSkinningAnimation animData, GPUSkinningPlayerMono player, out GPUSkinningPlayerResources result)
	{
		result = null;

		if (animData == null || player == null)
		{
			return;
		}

		GPUSkinningPlayerResources res = FindRefRes(animData.guid);
		if (res == null)
		{
			res = new GPUSkinningPlayerResources(animData, HideFlags.None);
			_refResList.Add(res);
		}

		if (!res.players.Contains(player))
		{
			res.AddPlayer(player);
		}

		result = res;
	}

	public void Unregister(GPUSkinningPlayerMono player)
	{
		if (player == null)
		{
			return;
		}

		int count = _refResList.Count;
		for (int i = 0; i < count; ++i)
		{
			int playerIndex = _refResList[i].players.IndexOf(player);
			if (playerIndex != -1)
			{
				_refResList[i].players.RemoveAt(playerIndex);
				_refResList[i].RemoveCullingBounds(playerIndex);
				if (_refResList[i].players.Count == 0)
				{
					_refResList[i].Destroy();
					_refResList.RemoveAt(i);
				}
				break;
			}
		}
	}

	public override void OnDispose()
	{
		Debug.Log("GPUSkinningPlayerMgr OnDispose:" + _refResList.Count);

		foreach (var res in _refResList)
		{
			res.Destroy();
		}
		_refResList.Clear();
	}

	void OnDrawGizmos()
	{
		if (showCullingBounds)
		{
			if (_refResList != null)
			{
				for (var index = 0; index < _refResList.Count; index++)
				{
					var res = _refResList[index];
					if (res.players != null)
					{
						int count = res.players.Count;
						for (int i = 0; i < count; i++)
						{
							var bSphere = res.GetCullingBounds(i);
							Gizmos.color = Color.red;
							Gizmos.DrawWireSphere(bSphere.position, bSphere.radius);
						}
					}
				}
			}

		}
	}
}

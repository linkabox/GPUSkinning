using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUSkinningPlayerMgr : Singleton<GPUSkinningPlayerMgr>
{
	public bool showCullingBounds;

	private List<GPUSkinningPlayerResources> refResList = new List<GPUSkinningPlayerResources>();
	private List<GPUSkinningPlayerMono> allMonoPlayers = new List<GPUSkinningPlayerMono>();

	public GPUSkinningPlayerResources FindRefRes(string guid)
	{
		int numItems = refResList.Count;
		for (int i = 0; i < numItems; ++i)
		{
			if (refResList[i].animData.guid == guid)
			{
				return refResList[i];
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
			refResList.Add(res);
		}

		if (!res.players.Contains(player))
		{
			res.AddPlayer(player);
		}

		allMonoPlayers.Add(player);

		result = res;
	}

	public void Unregister(GPUSkinningPlayerMono player)
	{
		if (player == null) return;

		int count = refResList.Count;
		for (int i = 0; i < count; ++i)
		{
			int playerIndex = refResList[i].players.IndexOf(player);
			if (playerIndex != -1)
			{
				refResList[i].players.RemoveAt(playerIndex);
				refResList[i].RemoveCullingBounds(playerIndex);
				if (refResList[i].players.Count == 0)
				{
					refResList[i].Destroy();
					refResList.RemoveAt(i);
				}
				break;
			}
		}

		allMonoPlayers.Remove(player);
	}

	void Update()
	{
		float deltaTime = Time.deltaTime;
		foreach (var monoPlayer in allMonoPlayers)
		{
			monoPlayer.ManualUpdate(deltaTime);
		}
	}

	public override void OnDispose()
	{
		Debug.Log("GPUSkinningPlayerMgr OnDispose:" + refResList.Count);

		foreach (var res in refResList)
		{
			res.Destroy();
		}
		refResList.Clear();
		allMonoPlayers.Clear();
	}

#if UNITY_EDITOR
	void OnDrawGizmos()
	{
		if (showCullingBounds)
		{
			if (refResList != null)
			{
				for (var index = 0; index < refResList.Count; index++)
				{
					var res = refResList[index];
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
#endif
}

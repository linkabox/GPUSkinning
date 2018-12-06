
using UnityEngine;
using System.Collections;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	private static T _instance;

	private static object _lock = new object();

	public static T Instance
	{
		get
		{
			lock (_lock)
			{
				if (_instance == null)
				{
					_instance = (T)FindObjectOfType(typeof(T));

					if (FindObjectsOfType(typeof(T)).Length > 1)
					{
						return _instance;
					}

					if (_instance == null)
					{
						_destroy = false;
						GameObject singleton = new GameObject();
						_instance = singleton.AddComponent<T>();
						singleton.name = "[Singleton]" + typeof(T).ToString();

						DontDestroyOnLoad(singleton);
					}
				}

				return _instance;
			}
		}
	}

	private static bool _destroy = true;

	public void OnDestroy()
	{
		_destroy = true;
		OnDispose();
	}

	public abstract void OnDispose();

	public static bool IsDestroy()
	{
		return _destroy;
	}
}

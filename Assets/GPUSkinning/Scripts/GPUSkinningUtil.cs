using UnityEngine;
using System.Collections;
using System.Security.Cryptography;

public static class GPUSkinningUtil
{
	public static Color PackTwoFloatToColor(float m00, float m01)
	{
		byte f1, b1, f2, b2;
		m00.float_to_2byte(out f1, out b1);
		m01.float_to_2byte(out f2, out b2);
		return new Color32(f1, b1, f2, b2);
	}

	public static void float_to_2byte(this float raw, out byte f, out byte b)
	{
		int rawi = (int)(raw * 10000) + 32767;
		if (rawi < 0 || rawi > 65535)
		{
			Debug.LogError("Over precision:" + raw);
		}
		rawi = Mathf.Clamp(rawi, 0, 65535);
		f = (byte)(rawi >> 8);
		b = (byte)(rawi & 0xFF);
	}

	public static float pack_2byte_to_float(float f, float b)
	{
		float ret = (f * 256 + b) * 0.0255f - 3.2767f;
		return ret;
	}

	public static void MarkAllScenesDirty()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying)
		{
			UnityEditor.EditorApplication.CallbackFunction DelayCall = null;
			DelayCall = () =>
			{
				UnityEditor.EditorApplication.delayCall -= DelayCall;
				UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
			};
			UnityEditor.EditorApplication.delayCall += DelayCall;
		}
#endif
	}

	public static Texture2D CreateTexture2D(TextAsset textureRawData, GPUSkinningAnimation anim)
	{
		if (textureRawData == null || anim == null)
		{
			return null;
		}

		Texture2D texture = new Texture2D(anim.textureWidth, anim.textureHeight, TextureFormat.RGBAHalf, false, true);
		texture.name = "GPUSkinningTextureMatrix";
		texture.filterMode = FilterMode.Point;
		texture.LoadRawTextureData(textureRawData.bytes);
		texture.Apply(false, true);

		return texture;
	}

	public static string BonesHierarchyTree(GPUSkinningAnimation gpuSkinningAnimation)
	{
		if (gpuSkinningAnimation == null || gpuSkinningAnimation.bones == null)
		{
			return null;
		}

		string str = string.Empty;
		BonesHierarchy_Internal(gpuSkinningAnimation, gpuSkinningAnimation.bones[gpuSkinningAnimation.rootBoneIndex], string.Empty, ref str);
		return str;
	}

	public static void BonesHierarchy_Internal(GPUSkinningAnimation gpuSkinningAnimation, GPUSkinningBone bone, string tabs, ref string str)
	{
		str += tabs + bone.name + "\n";

		int numChildren = bone.childrenBonesIndices == null ? 0 : bone.childrenBonesIndices.Length;
		for (int i = 0; i < numChildren; ++i)
		{
			BonesHierarchy_Internal(gpuSkinningAnimation, gpuSkinningAnimation.bones[bone.childrenBonesIndices[i]], tabs + "    ", ref str);
		}
	}

	public static string BoneHierarchyPath(GPUSkinningBone[] bones, int boneIndex)
	{
		if (bones == null || boneIndex < 0 || boneIndex >= bones.Length)
		{
			return null;
		}

		GPUSkinningBone bone = bones[boneIndex];
		string path = bone.name;
		while (bone.parentBoneIndex != -1)
		{
			bone = bones[bone.parentBoneIndex];
			path = bone.name + "/" + path;
		}
		return path;
	}

	public static string BoneHierarchyPath(GPUSkinningAnimation gpuSkinningAnimation, int boneIndex)
	{
		if (gpuSkinningAnimation == null)
		{
			return null;
		}

		return BoneHierarchyPath(gpuSkinningAnimation.bones, boneIndex);
	}

	public static string MD5(string input)
	{
		MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
		byte[] bytValue, bytHash;
		bytValue = System.Text.Encoding.UTF8.GetBytes(input);
		bytHash = md5.ComputeHash(bytValue);
		md5.Clear();
		string sTemp = string.Empty;
		for (int i = 0; i < bytHash.Length; i++)
		{
			sTemp += bytHash[i].ToString("X").PadLeft(2, '0');
		}
		return sTemp.ToLower();
	}

	public static int NormalizeTimeToFrameIndex(GPUSkinningClip clip, float normalizedTime)
	{
		if (clip == null)
		{
			return 0;
		}

		normalizedTime = Mathf.Clamp01(normalizedTime);
		return (int)(normalizedTime * (clip.length * clip.fps - 1));
	}

	public static float FrameIndexToNormalizedTime(GPUSkinningClip clip, int frameIndex)
	{
		if (clip == null)
		{
			return 0;
		}

		int totalFrams = (int)(clip.fps * clip.length);
		frameIndex = Mathf.Clamp(frameIndex, 0, totalFrams - 1);
		return (float)frameIndex / (float)(totalFrams - 1);
	}
}

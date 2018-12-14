using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class GPUSkinningSampler : MonoBehaviour
{
#if UNITY_EDITOR
	[HideInInspector]
	[SerializeField]
	public string assetName = null;

	[HideInInspector]
	[System.NonSerialized]
	public AnimationClip curSampleAnimClip = null;

	[HideInInspector]
	[SerializeField]
	public AnimationClip[] animClips = null;

	[HideInInspector]
	[SerializeField]
	public GPUSkinningWrapMode[] wrapModes = null;

	[HideInInspector]
	[SerializeField]
	public int[] fpsList = null;

	[HideInInspector]
	[SerializeField]
	public bool[] rootMotionEnabled = null;

	[HideInInspector]
	[SerializeField]
	public bool[] individualDifferenceEnabled = null;

	[HideInInspector]
	[SerializeField]
	public Mesh[] lodMeshes = null;

	[HideInInspector]
	[SerializeField]
	public float[] lodDistances = null;

	[HideInInspector]
	[System.NonSerialized]
	public int samplingClipIndex = -1;

	[HideInInspector]
	[SerializeField]
	public Texture2D boneTexture = null;

	[HideInInspector]
	[SerializeField]
	public GPUSkinningQuality skinQuality = GPUSkinningQuality.BONE_4;

	[HideInInspector]
	[SerializeField]
	public Transform rootBoneTransform = null;

	[HideInInspector]
	[SerializeField]
	public GPUSkinningAnimation anim = null;

	[HideInInspector]
	[System.NonSerialized]
	public bool isSampling = false;

	[HideInInspector]
	[SerializeField]
	public Mesh savedMesh = null;

	[HideInInspector]
	[SerializeField]
	public Material savedMtrl = null;

	[HideInInspector]
	[SerializeField]
	public Shader savedShader = null;

	[HideInInspector]
	[SerializeField]
	public bool updateOrNew = true;

	private Animation legacyAnimation = null;

	private Animator animator = null;
	private RuntimeAnimatorController runtimeAnimatorController = null;

	private SkinnedMeshRenderer smr = null;

	private GPUSkinningAnimation _tmpAnimData = null;
	private GPUSkinningClip[] _oldSampleClips;

	private GPUSkinningClip _gpuSkinningClip = null;

	private Vector3 rootMotionPosition;

	private Quaternion rootMotionRotation;

	[HideInInspector]
	[System.NonSerialized]
	public int samplingTotalFrams = 0;

	[HideInInspector]
	[System.NonSerialized]
	public int samplingFrameIndex = 0;

	public const string TEMP_SAVED_ANIM_PATH = "GPUSkinning_Temp_Save_Anim_Path";
	public const string TEMP_SAVED_MTRL_PATH = "GPUSkinning_Temp_Save_Mtrl_Path";
	public const string TEMP_SAVED_MESH_PATH = "GPUSkinning_Temp_Save_Mesh_Path";
	public const string TEMP_SAVED_SHADER_PATH = "GPUSkinning_Temp_Save_Shader_Path";
	public const string TEMP_SAVED_TEXTURE_PATH = "GPUSkinning_Temp_Save_Texture_Path";

	public bool BeginSample()
	{
		if (string.IsNullOrEmpty(assetName.Trim()))
		{
			ShowDialog("Animation name is empty.");
			return false;
		}

		if (animClips == null || animClips.Length == 0)
		{
			ShowDialog("Please set Anim Clips.");
			return false;
		}

		smr = GetComponentInChildren<SkinnedMeshRenderer>();
		if (smr == null)
		{
			ShowDialog("Cannot find SkinnedMeshRenderer.");
			return false;
		}
		if (smr.sharedMesh == null)
		{
			ShowDialog("Cannot find SkinnedMeshRenderer.mesh.");
			return false;
		}

		Mesh mesh = smr.sharedMesh;
		if (mesh == null)
		{
			ShowDialog("Missing Mesh");
			return false;
		}

		if (rootBoneTransform == null)
		{
			rootBoneTransform = this.transform;
		}

		samplingClipIndex = 0;
		//First create animData
		if (anim != null)
		{
			_tmpAnimData = anim;
			_oldSampleClips = anim.clips;
		}
		else
		{
			_tmpAnimData = ScriptableObject.CreateInstance<GPUSkinningAnimation>();
			_tmpAnimData.guid = System.Guid.NewGuid().ToString();
			_oldSampleClips = null;
		}
		_tmpAnimData.assetName = assetName;

		//Reset bone info
		List<GPUSkinningBone> bonesResult = new List<GPUSkinningBone>();
		CollectBones(bonesResult, smr.bones, mesh.bindposes, null, rootBoneTransform, 0);
		GPUSkinningBone[] newBones = bonesResult.ToArray();
		GenerateBonesGUID(newBones);
		if (anim != null && anim.bones != null)
		{
			RestoreCustomBoneData(anim.bones, newBones);
		}

		_tmpAnimData.bones = newBones;
		_tmpAnimData.rootBoneIndex = 0;
		_tmpAnimData.exposeCount = GetExposeBonesCount(newBones);
		_tmpAnimData.skinQuality = skinQuality;

		//Reset clip info
		_tmpAnimData.clips = new GPUSkinningClip[animClips.Length];

		return true;
	}

	public void EndSample()
	{
		samplingClipIndex = -1;
	}

	public Bounds CalculateBoundsAuto(float boundsAutoExt = 0.1f)
	{
		Matrix4x4[] matrices = anim.clips[0].frames[0].matrices;
		Matrix4x4 rootMotionInv = anim.clips[0].rootMotionEnabled ? matrices[anim.rootBoneIndex].inverse : Matrix4x4.identity;
		GPUSkinningBone[] bones = anim.bones;
		Vector3 min = Vector3.one * 9999;
		Vector3 max = min * -1;
		for (int i = 0; i < bones.Length; ++i)
		{
			Vector4 pos = (rootMotionInv * matrices[i] * bones[i].bindpose.inverse) * new Vector4(0, 0, 0, 1);
			min.x = Mathf.Min(min.x, pos.x);
			min.y = Mathf.Min(min.y, pos.y);
			min.z = Mathf.Min(min.z, pos.z);
			max.x = Mathf.Max(max.x, pos.x);
			max.y = Mathf.Max(max.y, pos.y);
			max.z = Mathf.Max(max.z, pos.z);
		}
		min -= Vector3.one * boundsAutoExt;
		max += Vector3.one * boundsAutoExt;
		var ret = new Bounds();
		ret.SetMinMax(min, max);
		return ret;
	}


	public void SaveAsset()
	{
		string savePath = null;
		if (anim == null)
		{
			savePath = EditorUtility.SaveFolderPanel("GPUSkinning Sampler Save", GetUserPreferDir(), assetName);
		}
		else
		{
			string animPath = AssetDatabase.GetAssetPath(anim);
			savePath = new FileInfo(animPath).Directory.FullName.Replace('\\', '/');
		}

		if (!string.IsNullOrEmpty(savePath))
		{
			if (!savePath.Contains(Application.dataPath.Replace('\\', '/')))
			{
				ShowDialog("Must select a directory in the project's Asset folder.");
			}
			else
			{
				SaveUserPreferDir(savePath);
				string dir = "Assets" + savePath.Substring(Application.dataPath.Length);

				//Restore AnimClip Event
				if (_oldSampleClips != null)
				{
					for (int i = 0; i < _tmpAnimData.clips.Length; i++)
					{
						var newClip = _tmpAnimData.clips[i];
						foreach (var oldClip in _oldSampleClips)
						{
							if (oldClip.name == newClip.name)
							{
								RestoreCustomClipData(oldClip, newClip);
								break;
							}
						}
					}
				}
				SetSthAboutTexture(_tmpAnimData);
				EditorUtility.SetDirty(_tmpAnimData);
				if (anim != _tmpAnimData)
				{
					string savedAnimPath = dir + "/" + assetName + "_AnimData.asset";
					AssetDatabase.CreateAsset(_tmpAnimData, savedAnimPath);
					WriteTempData(TEMP_SAVED_ANIM_PATH, savedAnimPath);
				}

				//AnimData
				anim = _tmpAnimData;

				//Texture
				boneTexture = CreateTextureMatrix(dir, anim);
				anim.boneTexture = boneTexture;

				//Mesh
				Mesh newMesh = CreateNewMesh(smr.sharedMesh, smr.sharedMesh.name);
				if (savedMesh != null)
				{
					newMesh.bounds = savedMesh.bounds;
				}
				else
				{
					newMesh.bounds = CalculateBoundsAuto();
				}

				string savedMeshPath = dir + "/" + assetName + "_Mesh.asset";
				AssetDatabase.CreateAsset(newMesh, savedMeshPath);
				WriteTempData(TEMP_SAVED_MESH_PATH, savedMeshPath);
				savedMesh = newMesh;
				anim.defaultMesh = savedMesh;
				CreateLODMeshes(anim, newMesh.bounds, dir);

				//Material
				savedMtrl = CreateShaderAndMaterial(dir, anim);

				EditorUtility.SetDirty(anim);
			}
		}

		Debug.Log("Sample Success:" + savePath);
		AssetDatabase.Refresh();
		AssetDatabase.SaveAssets();
	}

	public bool IsSamplingProgress()
	{
		return samplingClipIndex != -1;
	}

	public bool IsAnimatorOrAnimation()
	{
		return animator != null;
	}

	public void StartSample()
	{
		if (isSampling)
		{
			return;
		}

		curSampleAnimClip = animClips[samplingClipIndex];
		if (curSampleAnimClip == null)
		{
			isSampling = false;
			return;
		}

		int numFrames = (int)(GetClipFPS(curSampleAnimClip, samplingClipIndex) * curSampleAnimClip.length);
		if (numFrames == 0)
		{
			isSampling = false;
			return;
		}

		samplingFrameIndex = 0;

		_gpuSkinningClip = new GPUSkinningClip();
		_gpuSkinningClip.name = curSampleAnimClip.name;
		_gpuSkinningClip.fps = GetClipFPS(curSampleAnimClip, samplingClipIndex);
		_gpuSkinningClip.length = curSampleAnimClip.length;
		_gpuSkinningClip.wrapMode = wrapModes[samplingClipIndex];
		_gpuSkinningClip.frames = new GPUSkinningFrame[numFrames];
		_gpuSkinningClip.rootMotionEnabled = rootMotionEnabled[samplingClipIndex];
		_gpuSkinningClip.individualDifferenceEnabled = individualDifferenceEnabled[samplingClipIndex];
		_tmpAnimData.clips[samplingClipIndex] = _gpuSkinningClip;

		SetCurrentAnimationClip();
		PrepareRecordAnimator();

		isSampling = true;
	}

	private int GetClipFPS(AnimationClip clip, int clipIndex)
	{
		return fpsList[clipIndex] == 0 ? (int)clip.frameRate : fpsList[clipIndex];
	}

	private void RestoreCustomClipData(GPUSkinningClip src, GPUSkinningClip dest)
	{
		if (src.events != null)
		{
			int totalFrames = (int)(dest.length * dest.fps);
			dest.events = new GPUSkinningAnimEvent[src.events.Length];
			for (int i = 0; i < dest.events.Length; ++i)
			{
				GPUSkinningAnimEvent evt = new GPUSkinningAnimEvent();
				evt.eventId = src.events[i].eventId;
				evt.frameIndex = Mathf.Clamp(src.events[i].frameIndex, 0, totalFrames - 1);
				dest.events[i] = evt;
			}
		}
	}

	private void RestoreCustomBoneData(GPUSkinningBone[] bonesOrig, GPUSkinningBone[] bonesNew)
	{
		for (int i = 0; i < bonesNew.Length; ++i)
		{
			for (int j = 0; j < bonesOrig.Length; ++j)
			{
				if (bonesNew[i].guid == bonesOrig[j].guid)
				{
					bonesNew[i].isExposed = bonesOrig[j].isExposed;
					break;
				}
			}
		}
	}

	private void GenerateBonesGUID(GPUSkinningBone[] bones)
	{
		int numBones = bones == null ? 0 : bones.Length;
		for (int i = 0; i < numBones; ++i)
		{
			string boneHierarchyPath = GPUSkinningUtil.BoneHierarchyPath(bones, i);
			string guid = GPUSkinningUtil.MD5(boneHierarchyPath);
			bones[i].guid = guid;
		}
	}

	private int GetExposeBonesCount(GPUSkinningBone[] bones)
	{
		int exposeCount = 0;
		int numBones = bones == null ? 0 : bones.Length;
		for (int i = 0; i < numBones; ++i)
		{
			if (bones[i].isExposed)
			{
				exposeCount++;
			}
		}

		return exposeCount;
	}

	private void PrepareRecordAnimator()
	{
		if (animator != null)
		{
			int numFrames = (int)(_gpuSkinningClip.fps * _gpuSkinningClip.length);

			animator.applyRootMotion = _gpuSkinningClip.rootMotionEnabled;
			animator.Rebind();
			animator.recorderStartTime = 0;
			animator.StartRecording(numFrames);
			for (int i = 0; i < numFrames; ++i)
			{
				animator.Update(1.0f / _gpuSkinningClip.fps);
			}
			animator.StopRecording();
			animator.StartPlayback();
		}
	}

	private void SetCurrentAnimationClip()
	{
		if (legacyAnimation == null)
		{
			AnimatorOverrideController animatorOverrideController = new AnimatorOverrideController();
			AnimationClip[] clips = runtimeAnimatorController.animationClips;
			List<KeyValuePair<AnimationClip, AnimationClip>> overrideClips = new List<KeyValuePair<AnimationClip, AnimationClip>>(clips.Length);
			for (int i = 0; i < clips.Length; ++i)
			{
				overrideClips.Add(new KeyValuePair<AnimationClip, AnimationClip>(clips[i], curSampleAnimClip));
			}
			animatorOverrideController.runtimeAnimatorController = runtimeAnimatorController;
			animatorOverrideController.ApplyOverrides(overrideClips);
			animator.runtimeAnimatorController = animatorOverrideController;
		}
	}

	private void CreateLODMeshes(GPUSkinningAnimation animData, Bounds bounds, string dir)
	{
		animData.lodMeshes = null;
		animData.lodDistances = null;

		if (lodMeshes != null)
		{
			List<Mesh> newMeshes = new List<Mesh>();
			List<float> newLodDistances = new List<float>();
			for (int i = 0; i < lodMeshes.Length; ++i)
			{
				Mesh lodMesh = lodMeshes[i];
				if (lodMesh != null)
				{
					Mesh newMesh = CreateNewMesh(lodMesh, "GPUSkinning_Mesh_LOD" + (i + 1));
					newMesh.bounds = bounds;
					string savedMeshPath = dir + "/" + assetName + "_Mesh_LOD" + (i + 1) + ".asset";
					AssetDatabase.CreateAsset(newMesh, savedMeshPath);
					newMeshes.Add(newMesh);
					newLodDistances.Add(lodDistances[i]);
				}
			}
			animData.lodMeshes = newMeshes.ToArray();

			newLodDistances.Add(9999);
			animData.lodDistances = newLodDistances.ToArray();
		}
	}

	private Mesh CreateNewMesh(Mesh mesh, string meshName)
	{
		Vector3[] normals = mesh.normals;
		Vector4[] tangents = mesh.tangents;
		Color[] colors = mesh.colors;
		Vector2[] uv = mesh.uv;

		Mesh newMesh = new Mesh();
		newMesh.name = meshName;
		newMesh.vertices = mesh.vertices;
		if (normals != null && normals.Length > 0) { newMesh.normals = normals; }
		if (tangents != null && tangents.Length > 0) { newMesh.tangents = tangents; }
		if (colors != null && colors.Length > 0) { newMesh.colors = colors; }
		if (uv != null && uv.Length > 0) { newMesh.uv = uv; }

		int numVertices = mesh.vertexCount;
		BoneWeight[] boneWeights = mesh.boneWeights;
		Vector4[] uv2 = new Vector4[numVertices];
		Vector4[] uv3 = new Vector4[numVertices];
		Transform[] smrBones = smr.bones;
		for (int i = 0; i < numVertices; ++i)
		{
			BoneWeight boneWeight = boneWeights[i];

			BoneWeightSortData[] weights = new BoneWeightSortData[4];
			weights[0] = new BoneWeightSortData() { index = boneWeight.boneIndex0, weight = boneWeight.weight0 };
			weights[1] = new BoneWeightSortData() { index = boneWeight.boneIndex1, weight = boneWeight.weight1 };
			weights[2] = new BoneWeightSortData() { index = boneWeight.boneIndex2, weight = boneWeight.weight2 };
			weights[3] = new BoneWeightSortData() { index = boneWeight.boneIndex3, weight = boneWeight.weight3 };
			System.Array.Sort(weights);

			GPUSkinningBone bone0 = GetBoneByTransform(smrBones[weights[0].index]);
			GPUSkinningBone bone1 = GetBoneByTransform(smrBones[weights[1].index]);
			GPUSkinningBone bone2 = GetBoneByTransform(smrBones[weights[2].index]);
			GPUSkinningBone bone3 = GetBoneByTransform(smrBones[weights[3].index]);

			Vector4 boneIndexData = new Vector4();
			boneIndexData.x = GetBoneIndex(bone0);
			boneIndexData.y = GetBoneIndex(bone1);
			boneIndexData.z = GetBoneIndex(bone2);
			boneIndexData.w = GetBoneIndex(bone3);
			uv2[i] = boneIndexData;

			Vector4 boneWeightData = new Vector4();
			boneWeightData.x = weights[0].weight;
			boneWeightData.y = weights[1].weight;
			boneWeightData.z = weights[2].weight;
			boneWeightData.w = weights[3].weight;
			uv3[i] = boneWeightData;
		}
		newMesh.SetUVs(1, new List<Vector4>(uv2));
		newMesh.SetUVs(2, new List<Vector4>(uv3));

		newMesh.triangles = mesh.triangles;
		return newMesh;
	}

	private class BoneWeightSortData : System.IComparable<BoneWeightSortData>
	{
		public int index = 0;

		public float weight = 0;

		public int CompareTo(BoneWeightSortData b)
		{
			return weight > b.weight ? -1 : 1;
		}
	}

	private void CollectBones(List<GPUSkinningBone> bones_result, Transform[] bones_smr, Matrix4x4[] bindposes, GPUSkinningBone parentBone, Transform currentBoneTransform, int currentBoneIndex)
	{
		GPUSkinningBone currentBone = new GPUSkinningBone();
		bones_result.Add(currentBone);

		int indexOfSmrBones = System.Array.IndexOf(bones_smr, currentBoneTransform);
		currentBone.transform = currentBoneTransform;
		currentBone.name = currentBone.transform.gameObject.name;
		currentBone.bindpose = indexOfSmrBones == -1 ? Matrix4x4.identity : bindposes[indexOfSmrBones];
		currentBone.parentBoneIndex = parentBone == null ? -1 : bones_result.IndexOf(parentBone);

		if (parentBone != null)
		{
			parentBone.childrenBonesIndices[currentBoneIndex] = bones_result.IndexOf(currentBone);
		}

		int numChildren = currentBone.transform.childCount;
		if (numChildren > 0)
		{
			currentBone.childrenBonesIndices = new int[numChildren];
			for (int i = 0; i < numChildren; ++i)
			{
				CollectBones(bones_result, bones_smr, bindposes, currentBone, currentBone.transform.GetChild(i), i);
			}
		}
	}

	private void SetSthAboutTexture(GPUSkinningAnimation gpuSkinningAnim)
	{
		int numPixels = 0;

		GPUSkinningClip[] clips = gpuSkinningAnim.clips;
		int numClips = clips.Length;
		for (int clipIndex = 0; clipIndex < numClips; ++clipIndex)
		{
			GPUSkinningClip clip = clips[clipIndex];
			clip.pixelSegmentation = numPixels;

			GPUSkinningFrame[] frames = clip.frames;
			int numFrames = frames.Length;
			numPixels += gpuSkinningAnim.bones.Length * 3/*treat 3 pixels as a float3x4*/ * numFrames * 2;
		}

		CalculateTextureSize(numPixels, out gpuSkinningAnim.textureWidth, out gpuSkinningAnim.textureHeight);
	}

	private Texture2D CreateTextureMatrix(string dir, GPUSkinningAnimation gpuSkinningAnim)
	{
		Texture2D texture = new Texture2D(gpuSkinningAnim.textureWidth, gpuSkinningAnim.textureHeight, TextureFormat.RGBA32, false, true);
		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.anisoLevel = 0;
		Color[] pixels = texture.GetPixels();
		int pixelIndex = 0;

		float max = 0;
		float min = float.MaxValue;

		for (int clipIndex = 0; clipIndex < gpuSkinningAnim.clips.Length; ++clipIndex)
		{
			GPUSkinningClip clip = gpuSkinningAnim.clips[clipIndex];
			GPUSkinningFrame[] frames = clip.frames;
			int numFrames = frames.Length;
			for (int frameIndex = 0; frameIndex < numFrames; ++frameIndex)
			{
				GPUSkinningFrame frame = frames[frameIndex];
				Matrix4x4[] matrices = frame.matrices;
				int numMatrices = matrices.Length;
				for (int matrixIndex = 0; matrixIndex < numMatrices; ++matrixIndex)
				{
					Matrix4x4 matrix = matrices[matrixIndex];

					for (int i = 0; i < 16; i++)
					{
						max = Mathf.Max(matrix[i], max);
						min = Mathf.Min(matrix[i], min);
					}
					pixels[pixelIndex++] = GPUSkinningUtil.PackTwoFloatToColor(matrix.m00, matrix.m01);
					pixels[pixelIndex++] = GPUSkinningUtil.PackTwoFloatToColor(matrix.m02, matrix.m03);

					pixels[pixelIndex++] = GPUSkinningUtil.PackTwoFloatToColor(matrix.m10, matrix.m11);
					pixels[pixelIndex++] = GPUSkinningUtil.PackTwoFloatToColor(matrix.m12, matrix.m13);

					pixels[pixelIndex++] = GPUSkinningUtil.PackTwoFloatToColor(matrix.m20, matrix.m21);
					pixels[pixelIndex++] = GPUSkinningUtil.PackTwoFloatToColor(matrix.m22, matrix.m23);

					//pixels[pixelIndex++] = new Color(matrix.m00, matrix.m01, matrix.m02, matrix.m03);
					//pixels[pixelIndex++] = new Color(matrix.m10, matrix.m11, matrix.m12, matrix.m13);
					//pixels[pixelIndex++] = new Color(matrix.m20, matrix.m21, matrix.m22, matrix.m23);
				}
			}
		}
		texture.SetPixels(pixels);
		texture.Apply();

		string savedPath = dir + "/" + assetName + "_BoneMap.asset";
		AssetDatabase.CreateAsset(texture, savedPath);
		WriteTempData(TEMP_SAVED_TEXTURE_PATH, savedPath);
		return texture;
	}

	private void CalculateTextureSize(int numPixels, out int texWidth, out int texHeight)
	{
		texWidth = 1;
		texHeight = 1;
		while (true)
		{
			if (texWidth * texHeight >= numPixels) break;
			texWidth *= 2;
			if (texWidth * texHeight >= numPixels) break;
			texHeight *= 2;
		}
	}

	public void MappingAnimationClips()
	{
		if (legacyAnimation == null)
		{
			return;
		}

		List<AnimationClip> newClips = null;
		AnimationClip[] clips = AnimationUtility.GetAnimationClips(gameObject);
		if (clips != null)
		{
			for (int i = 0; i < clips.Length; ++i)
			{
				AnimationClip clip = clips[i];
				if (clip != null)
				{
					if (animClips == null || System.Array.IndexOf(animClips, clip) == -1)
					{
						if (newClips == null)
						{
							newClips = new List<AnimationClip>();
						}
						newClips.Clear();
						if (animClips != null) newClips.AddRange(animClips);
						newClips.Add(clip);
						animClips = newClips.ToArray();
					}
				}
			}
		}

		if (animClips != null && clips != null)
		{
			for (int i = 0; i < animClips.Length; ++i)
			{
				AnimationClip clip = animClips[i];
				if (clip != null)
				{
					if (System.Array.IndexOf(clips, clip) == -1)
					{
						if (newClips == null)
						{
							newClips = new List<AnimationClip>();
						}
						newClips.Clear();
						newClips.AddRange(animClips);
						newClips.RemoveAt(i);
						animClips = newClips.ToArray();
						--i;
					}
				}
			}
		}
	}

	private void InitTransform()
	{
		transform.parent = null;
		transform.position = Vector3.zero;
		transform.eulerAngles = Vector3.zero;
	}

	private void Awake()
	{
		legacyAnimation = GetComponent<Animation>();
		animator = GetComponent<Animator>();
		if (animator == null && legacyAnimation == null)
		{
			DestroyImmediate(this);
			ShowDialog("Cannot find Animator Or Animation Component");
			return;
		}
		if (animator != null && legacyAnimation != null)
		{
			DestroyImmediate(this);
			ShowDialog("Animation is not coexisting with Animator");
			return;
		}
		if (animator != null)
		{
			if (animator.runtimeAnimatorController == null)
			{
				DestroyImmediate(this);
				ShowDialog("Missing RuntimeAnimatorController");
				return;
			}
			if (animator.runtimeAnimatorController is AnimatorOverrideController)
			{
				DestroyImmediate(this);
				ShowDialog("RuntimeAnimatorController could not be a AnimatorOverrideController");
				return;
			}
			runtimeAnimatorController = animator.runtimeAnimatorController;
			animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
			InitTransform();
			return;
		}
		if (legacyAnimation != null)
		{
			MappingAnimationClips();
			legacyAnimation.Stop();
			legacyAnimation.cullingType = AnimationCullingType.AlwaysAnimate;
			InitTransform();
			return;
		}
	}

	private void Update()
	{
		if (!isSampling)
		{
			return;
		}

		int totalFrams = (int)(_gpuSkinningClip.length * _gpuSkinningClip.fps);
		samplingTotalFrams = totalFrams;

		if (samplingFrameIndex >= totalFrams)
		{
			if (animator != null)
			{
				animator.StopPlayback();
			}

			isSampling = false;
			return;
		}

		float time = _gpuSkinningClip.length * ((float)samplingFrameIndex / totalFrams);
		GPUSkinningFrame frame = new GPUSkinningFrame();
		_gpuSkinningClip.frames[samplingFrameIndex] = frame;
		frame.matrices = new Matrix4x4[_tmpAnimData.bones.Length];
		frame.jointMatrices = _tmpAnimData.exposeCount > 0 ? new Matrix4x4[_tmpAnimData.exposeCount] : null;

		if (legacyAnimation == null)
		{
			animator.playbackTime = time;
			animator.Update(0);
		}
		else
		{
			legacyAnimation.Stop();
			AnimationState animState = legacyAnimation[curSampleAnimClip.name];
			if (animState != null)
			{
				animState.time = time;
				legacyAnimation.Sample();
				legacyAnimation.Play();
			}
		}
		StartCoroutine(SamplingCoroutine(frame, totalFrams));
	}

	private IEnumerator SamplingCoroutine(GPUSkinningFrame frame, int totalFrames)
	{
		yield return new WaitForEndOfFrame();

		GPUSkinningBone[] bones = _tmpAnimData.bones;
		int numBones = bones.Length;
		for (int i = 0; i < numBones; ++i)
		{
			Transform boneTransform = bones[i].transform;
			GPUSkinningBone currentBone = GetBoneByTransform(boneTransform);
			frame.matrices[i] = currentBone.bindpose;
			do
			{
				Matrix4x4 mat = Matrix4x4.TRS(currentBone.transform.localPosition, currentBone.transform.localRotation, currentBone.transform.localScale);
				frame.matrices[i] = mat * frame.matrices[i];
				if (currentBone.parentBoneIndex == -1)
				{
					break;
				}
				else
				{
					currentBone = bones[currentBone.parentBoneIndex];
				}
			}
			while (true);
		}

		//Bake Exposed Joint Matrix
		//frame.rootMotionInv = frame.matrices[gpuSkinningAnimation.rootBoneIndex].inverse;
		if (frame.jointMatrices != null && frame.jointMatrices.Length > 0)
		{
			int jointIndex = 0;
			for (int i = 0; i < numBones; ++i)
			{
				if (bones[i].isExposed && jointIndex < frame.jointMatrices.Length)
				{
					Matrix4x4 jointMatrix = frame.matrices[i] * bones[i].BindposeInv;
					frame.jointMatrices[jointIndex++] = jointMatrix;
				}
			}
		}

		if (samplingFrameIndex == 0)
		{
			rootMotionPosition = bones[_tmpAnimData.rootBoneIndex].transform.localPosition;
			rootMotionRotation = bones[_tmpAnimData.rootBoneIndex].transform.localRotation;
		}
		else
		{
			Vector3 newPosition = bones[_tmpAnimData.rootBoneIndex].transform.localPosition;
			Quaternion newRotation = bones[_tmpAnimData.rootBoneIndex].transform.localRotation;
			Vector3 deltaPosition = newPosition - rootMotionPosition;
			frame.rootMotionDeltaPositionQ = Quaternion.Inverse(Quaternion.Euler(transform.forward.normalized)) * Quaternion.Euler(deltaPosition.normalized);
			frame.rootMotionDeltaPositionL = deltaPosition.magnitude;
			frame.rootMotionDeltaRotation = Quaternion.Inverse(rootMotionRotation) * newRotation;
			rootMotionPosition = newPosition;
			rootMotionRotation = newRotation;

			if (samplingFrameIndex == 1)
			{
				_gpuSkinningClip.frames[0].rootMotionDeltaPositionQ = _gpuSkinningClip.frames[1].rootMotionDeltaPositionQ;
				_gpuSkinningClip.frames[0].rootMotionDeltaPositionL = _gpuSkinningClip.frames[1].rootMotionDeltaPositionL;
				_gpuSkinningClip.frames[0].rootMotionDeltaRotation = _gpuSkinningClip.frames[1].rootMotionDeltaRotation;
			}
		}

		++samplingFrameIndex;
	}

	private Material CreateShaderAndMaterial(string dir, GPUSkinningAnimation animData)
	{
		if (savedShader == null)
		{
			string shaderPath = PlayerPrefs.GetString(TEMP_SAVED_SHADER_PATH,
				"Assets/GPUSkinning/Shaders/GPUSkinning_Unlit.shader");
			savedShader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
		}
		WriteTempData(TEMP_SAVED_SHADER_PATH, AssetDatabase.GetAssetPath(savedShader));

		string savedMtrlPath = dir + "/" + assetName + "_Mat.mat";
		Material mat = animData.material;
		if (mat == null)
		{
			mat = new Material(savedShader);
			AssetDatabase.CreateAsset(mat, savedMtrlPath);
			animData.material = mat;
		}

		if (smr.sharedMaterial != null)
		{
			mat.CopyPropertiesFromMaterial(smr.sharedMaterial);
		}

		EditorUtility.SetDirty(mat);
		WriteTempData(TEMP_SAVED_MTRL_PATH, savedMtrlPath);

		return mat;
	}

	private GPUSkinningBone GetBoneByTransform(Transform transform)
	{
		GPUSkinningBone[] bones = _tmpAnimData.bones;
		int numBones = bones.Length;
		for (int i = 0; i < numBones; ++i)
		{
			if (bones[i].transform == transform)
			{
				return bones[i];
			}
		}
		return null;
	}

	private int GetBoneIndex(GPUSkinningBone bone)
	{
		return System.Array.IndexOf(_tmpAnimData.bones, bone);
	}

	public static void ShowDialog(string msg)
	{
		EditorUtility.DisplayDialog("GPUSkinning", msg, "OK");
	}

	private void SaveUserPreferDir(string dirPath)
	{
		PlayerPrefs.SetString("GPUSkinning_UserPreferDir", dirPath);
	}

	private string GetUserPreferDir()
	{
		return PlayerPrefs.GetString("GPUSkinning_UserPreferDir", Application.dataPath);
	}

	public static void WriteTempData(string key, string value)
	{
		PlayerPrefs.SetString(key, value);
	}

	public static string ReadTempData(string key)
	{
		return PlayerPrefs.GetString(key, string.Empty);
	}

	public static void DeleteTempData(string key)
	{
		PlayerPrefs.DeleteKey(key);
	}
#endif
}

﻿using UnityEngine;
using System.Collections;

public class GPUSkinningAnimation : ScriptableObject
{
	public string guid = null;

	public string assetName = null;

	public GPUSkinningBone[] bones = null;

	public int exposeCount = 0;

	public int rootBoneIndex = 0;

	public GPUSkinningClip[] clips = null;

	public int textureWidth = 0;

	public int textureHeight = 0;

	public float[] lodDistances = null;

	public Mesh defaultMesh;

	public Mesh[] lodMeshes = null;

	public GPUSkinningQuality skinQuality = GPUSkinningQuality.BONE_4;

	public Texture2D boneTexture;

	public Material material;
}

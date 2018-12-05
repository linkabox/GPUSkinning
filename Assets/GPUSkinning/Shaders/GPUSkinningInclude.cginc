
#ifndef GPUSKINNING_INCLUDE
#define GPUSKINNING_INCLUDE

uniform sampler2D _boneTexture;
uniform float3 _boneTextureParams; //x-textureWidth, y-textureHeight, z- bonePixelCount

#if (SHADER_TARGET < 30 || SHADER_API_GLES)
	uniform float2 _frameInfo;
	uniform float3 _blendInfo;
#else
	UNITY_INSTANCING_BUFFER_START(Props)
		UNITY_DEFINE_INSTANCED_PROP(float2, _frameInfo) //x-frameIndex, y-pixelSegment
#define _frameInfo_arr Props
#if defined(BLEND_ON)
		UNITY_DEFINE_INSTANCED_PROP(float3, _blendInfo) //x-frameIndex_crossFade, y-pixelSegment, z- crossFadeFactor
#define _blendInfo_arr Props
#endif
	UNITY_INSTANCING_BUFFER_END(Props)
#endif

inline float4 indexToUV(float3 boneTexParams, float index)
{
	int row = (int)(index / boneTexParams.x);
	float col = index - row * boneTexParams.x;
	return float4(col / boneTexParams.x, row / boneTexParams.y, 0, 0);
}

inline half4x4 getMatrix(sampler2D boneTex, float3 boneTexParams, int frameStartIndex, float boneIndex)
{
	float matStartIndex = frameStartIndex + boneIndex * 3;
	half4 row0 = tex2Dlod(boneTex, indexToUV(boneTexParams, matStartIndex));
	half4 row1 = tex2Dlod(boneTex, indexToUV(boneTexParams, matStartIndex + 1));
	half4 row2 = tex2Dlod(boneTex, indexToUV(boneTexParams, matStartIndex + 2));
	half4 row3 = half4(0, 0, 0, 1);
	half4x4 mat = half4x4(row0, row1, row2, row3);
	return mat;
}

inline half3x3 getMatrix3x3(sampler2D boneTex, float3 boneTexParams, int frameStartIndex, float boneIndex)
{
	float matStartIndex = frameStartIndex + boneIndex * 3;
	half3 row0 = tex2Dlod(boneTex, indexToUV(boneTexParams, matStartIndex));
	half3 row1 = tex2Dlod(boneTex, indexToUV(boneTexParams, matStartIndex + 1));
	half3 row2 = tex2Dlod(boneTex, indexToUV(boneTexParams, matStartIndex + 2));
	half3x3 mat = half3x3(row0, row1, row2);
	return mat;
}

inline float4 skin_blend(float4 pos0, float4 pos1, float crossFadeBlend)
{
	return float4(pos1.xyz + (pos0.xyz - pos1.xyz) * crossFadeBlend, 1);
}

inline float3 skin_blend_normal(float3 pos0, float3 pos1, float crossFadeBlend)
{
	return float3(pos1 + (pos0 - pos1) * crossFadeBlend);
}

inline float4 skinToPos(float4 vertex, float4 boneIndex, float4 boneWeight, float frameIndex, float segment)
{
	float frameStartIndex = segment + frameIndex * _boneTextureParams.z;
	half4x4 mat0 = getMatrix(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.x);
#if BONE_2 || BONE_4
	half4x4 mat1 = getMatrix(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.y);
#endif

#if BONE_4
	half4x4 mat2 = getMatrix(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.z);
	half4x4 mat3 = getMatrix(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.w);
#endif

	float4 pos = mul(mat0, vertex) * boneWeight.x;
#if BONE_2 || BONE_4
	pos = pos + mul(mat1, vertex) * boneWeight.y;
#endif

#if BONE_4
	pos = pos + mul(mat2, vertex) * boneWeight.z;
	pos = pos + mul(mat3, vertex) * boneWeight.w;
#endif
	return pos;
}

inline float4 skinning(float4 vertex, float4 boneIndex, float4 boneWeight)
{
#if (SHADER_TARGET < 30 || SHADER_API_GLES)
	float2 frameInfo = _frameInfo;
#else
	float2 frameInfo = UNITY_ACCESS_INSTANCED_PROP(_frameInfo_arr, _frameInfo);
#endif

	float4 pos0 = skinToPos(vertex, boneIndex, boneWeight, frameInfo.x, frameInfo.y);

#if BLEND_ON
#if (SHADER_TARGET < 30 || SHADER_API_GLES)
	float3 blendInfo = _blendInfo;
#else
	float3 blendInfo = UNITY_ACCESS_INSTANCED_PROP(_blendInfo_arr, _blendInfo);
#endif
	float4 pos1 = skinToPos(vertex, boneIndex, boneWeight, blendInfo.x, blendInfo.y);

	return skin_blend(pos0, pos1, blendInfo.z);
#else
	return pos0;
#endif
}

inline float3 skinToNormal(float3 normal, float4 boneIndex, float4 boneWeight, float frameIndex, float segment)
{
	float frameStartIndex = segment + frameIndex * _boneTextureParams.z;
	half3x3 mat0 = getMatrix3x3(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.x);
#if BONE_2 || BONE_4
	half3x3 mat1 = getMatrix3x3(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.y);
#endif

#if BONE_4
	half3x3 mat2 = getMatrix3x3(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.z);
	half3x3 mat3 = getMatrix3x3(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.w);
#endif

	float3 pos = mul(mat0, normal) * boneWeight.x;
#if BONE_2 || BONE_4
	pos = pos + mul(mat1, normal) * boneWeight.y;
#endif

#if BONE_4
	pos = pos + mul(mat2, normal) * boneWeight.z;
	pos = pos + mul(mat3, normal) * boneWeight.w;
#endif
	return pos;
}

inline float3 skinning_normal(float3 normal, float4 boneIndex, float4 boneWeight)
{
#if (SHADER_TARGET < 30 || SHADER_API_GLES)
	float2 frameInfo = _frameInfo;
#else
	float2 frameInfo = UNITY_ACCESS_INSTANCED_PROP(_frameInfo_arr, _frameInfo);
#endif
	float3 pos0 = skinToNormal(normal, boneIndex, boneWeight, frameInfo.x, frameInfo.y);

#if BLEND_ON
#if (SHADER_TARGET < 30 || SHADER_API_GLES)
	float3 blendInfo = _blendInfo;
#else
	float3 blendInfo = UNITY_ACCESS_INSTANCED_PROP(_blendInfo_arr, _blendInfo);
#endif
	float3 pos1 = skinToNormal(normal, boneIndex, boneWeight, blendInfo.x, blendInfo.y);

	return skin_blend_normal(pos0, pos1, blendInfo.z);
#else
	return pos0;
#endif
}

#endif
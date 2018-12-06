
#ifndef GPUSKINNING_INCLUDE
#define GPUSKINNING_INCLUDE

uniform sampler2D _boneTexture;
//x-textureWidth, y-textureHeight, z- bonePixelCount
uniform float3 _boneTextureParams;

//frameInfo: x-frameIndex, y-pixelSegment
//blendINfo: x-frameIndex_crossFade, y-pixelSegment, z- crossFadeFactor
#if (SHADER_TARGET < 30 || SHADER_API_GLES)
	uniform float2 _frameInfo;
	uniform float3 _blendInfo;
#else
	UNITY_INSTANCING_BUFFER_START(Props)
		UNITY_DEFINE_INSTANCED_PROP(float2, _frameInfo)
#define _frameInfo_arr Props
#if defined(BLEND_ON)
		UNITY_DEFINE_INSTANCED_PROP(float3, _blendInfo)
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

inline float4 skin_blend(float4 pos0, float4 pos1, float t)
{
	return lerp(pos1, pos0, t);
}

inline void skinning(inout float4 vertex, float4 boneIndex, float4 boneWeight)
{
#if (SHADER_TARGET < 30 || SHADER_API_GLES)
	float2 frameInfo = _frameInfo;
#else
	float2 frameInfo = UNITY_ACCESS_INSTANCED_PROP(_frameInfo_arr, _frameInfo);
#endif

	float frameStartIndex = frameInfo.y + frameInfo.x * _boneTextureParams.z;
	half4x4 mat0 = getMatrix(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.x);
	half4x4 mat1 = getMatrix(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.y);
	half4x4 mat2 = getMatrix(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.z);
	half4x4 mat3 = getMatrix(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.w);

	float4 pos0 = mul(mat0, vertex) * boneWeight.x;
	pos0 = pos0 + mul(mat1, vertex) * boneWeight.y;
	pos0 = pos0 + mul(mat2, vertex) * boneWeight.z;
	pos0 = pos0 + mul(mat3, vertex) * boneWeight.w;

#if BLEND_ON
#if (SHADER_TARGET < 30 || SHADER_API_GLES)
	float3 blendInfo = _blendInfo;
#else
	float3 blendInfo = UNITY_ACCESS_INSTANCED_PROP(_blendInfo_arr, _blendInfo);
#endif

	frameStartIndex = blendInfo.y + blendInfo.x * _boneTextureParams.z;
	mat0 = getMatrix(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.x);
	mat1 = getMatrix(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.y);
	mat2 = getMatrix(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.z);
	mat3 = getMatrix(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.w);

	float4 pos1 = mul(mat0, vertex) * boneWeight.x;
	pos1 = pos1 + mul(mat1, vertex) * boneWeight.y;
	pos1 = pos1 + mul(mat2, vertex) * boneWeight.z;
	pos1 = pos1 + mul(mat3, vertex) * boneWeight.w;

	vertex = skin_blend(pos0, pos1, blendInfo.z);
#else
	vertex = pos0;
#endif
}

inline void skinning_normal(inout float4 vertex,inout float3 normal, float4 boneIndex, float4 boneWeight)
{
#if (SHADER_TARGET < 30 || SHADER_API_GLES)
	float2 frameInfo = _frameInfo;
#else
	float2 frameInfo = UNITY_ACCESS_INSTANCED_PROP(_frameInfo_arr, _frameInfo);
#endif

	float frameStartIndex = frameInfo.y + frameInfo.x * _boneTextureParams.z;
	half4x4 mat0 = getMatrix(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.x);
	half4x4 mat1 = getMatrix(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.y);
	half4x4 mat2 = getMatrix(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.z);
	half4x4 mat3 = getMatrix(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.w);

	float4 pos0 = mul(mat0, vertex) * boneWeight.x;
	pos0 = pos0 + mul(mat1, vertex) * boneWeight.y;
	pos0 = pos0 + mul(mat2, vertex) * boneWeight.z;
	pos0 = pos0 + mul(mat3, vertex) * boneWeight.w;

	float4 n0 = mul(mat0, normal) * boneWeight.x;
	n0 = n0 + mul(mat1, normal) * boneWeight.y;
	n0 = n0 + mul(mat2, normal) * boneWeight.z;
	n0 = n0 + mul(mat3, normal) * boneWeight.w;

#if BLEND_ON
#if (SHADER_TARGET < 30 || SHADER_API_GLES)
	float3 blendInfo = _blendInfo;
#else
	float3 blendInfo = UNITY_ACCESS_INSTANCED_PROP(_blendInfo_arr, _blendInfo);
#endif

	frameStartIndex = blendInfo.y + blendInfo.x * _boneTextureParams.z;
	mat0 = getMatrix(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.x);
	mat1 = getMatrix(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.y);
	mat2 = getMatrix(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.z);
	mat3 = getMatrix(_boneTexture, _boneTextureParams, frameStartIndex, boneIndex.w);

	float4 pos1 = mul(mat0, vertex) * boneWeight.x;
	pos1 = pos1 + mul(mat1, vertex) * boneWeight.y;
	pos1 = pos1 + mul(mat2, vertex) * boneWeight.z;
	pos1 = pos1 + mul(mat3, vertex) * boneWeight.w;

	float4 n1 = mul(mat0, normal) * boneWeight.x;
	n1 = n1 + mul(mat1, normal) * boneWeight.y;
	n1 = n1 + mul(mat2, normal) * boneWeight.z;
	n1 = n1 + mul(mat3, normal) * boneWeight.w;

	vertex = skin_blend(pos0, pos1, blendInfo.z);
	normal = skin_blend(n0, n1, blendInfo.z);
#else
	vertex = pos0;
	normal = n0;
#endif
}
#endif

#ifndef GPUSKINNING_INCLUDE
#define GPUSKINNING_INCLUDE

//#pragma multi_compile ROOTON_BLENDOFF ROOTON_BLENDON_CROSSFADEROOTON ROOTON_BLENDON_CROSSFADEROOTOFF ROOTOFF_BLENDOFF ROOTOFF_BLENDON_CROSSFADEROOTON ROOTOFF_BLENDON_CROSSFADEROOTOFF
uniform sampler2D _boneTexture;
uniform float3 _boneTextureParams; //x-textureWidth, y-textureHeight, z- bonePixelCount

UNITY_INSTANCING_BUFFER_START(Props)
UNITY_DEFINE_INSTANCED_PROP(float2, _frameInfo) //x-frameIndex, y-pixelSegment
#define _frameInfo_arr Props
UNITY_DEFINE_INSTANCED_PROP(float3, _blendInfo) //x-frameIndex_crossFade, y-pixelSegment, z- crossFadeFactor
#define _blendInfo_arr Props
UNITY_INSTANCING_BUFFER_END(Props)

inline float4 indexToUV(float index)
{
	int row = (int)(index / _boneTextureParams.x);
	float col = index - row * _boneTextureParams.x;
	return float4(col / _boneTextureParams.x, row / _boneTextureParams.y, 0, 0);
}

inline float4x4 getMatrix(int frameStartIndex, float boneIndex)
{
	float matStartIndex = frameStartIndex + boneIndex * 3;
	half4 row0 = tex2Dlod(_boneTexture, indexToUV(matStartIndex));
	half4 row1 = tex2Dlod(_boneTexture, indexToUV(matStartIndex + 1));
	half4 row2 = tex2Dlod(_boneTexture, indexToUV(matStartIndex + 2));
	half4 row3 = half4(0, 0, 0, 1);
	half4x4 mat = half4x4(row0, row1, row2, row3);
	return mat;
}

inline float4 skinToPos(float4 vertex, float4 boneIndex, float4 boneWeight, float frameIndex, float segment)
{
	float frameStartIndex = segment + frameIndex * _boneTextureParams.z;
	half4x4 mat0 = getMatrix(frameStartIndex, boneIndex.x);
	half4x4 mat1 = getMatrix(frameStartIndex, boneIndex.y);
	half4x4 mat2 = getMatrix(frameStartIndex, boneIndex.z);
	half4x4 mat3 = getMatrix(frameStartIndex, boneIndex.w);

	float4 pos = mul(mat0, vertex) * boneWeight.x +
		mul(mat1, vertex) * boneWeight.y +
		mul(mat2, vertex) * boneWeight.z +
		mul(mat3, vertex) * boneWeight.w;

	return pos;
}

inline float4 skin_blend(float4 pos0, float4 pos1, float crossFadeBlend)
{
	return float4(pos1.xyz + (pos0.xyz - pos1.xyz) * crossFadeBlend, 1);
}

inline float4 skinning(float4 vertex, float4 boneIndex, float4 boneWeight)
{
	float2 frameInfo = UNITY_ACCESS_INSTANCED_PROP(_frameInfo_arr, _frameInfo);
	float4 pos0 = skinToPos(vertex, boneIndex, boneWeight, frameInfo.x, frameInfo.y);

	float3 blendInfo = UNITY_ACCESS_INSTANCED_PROP(_blendInfo_arr, _blendInfo);
	float4 pos1 = skinToPos(vertex, boneIndex, boneWeight, blendInfo.x, blendInfo.y);

	return skin_blend(pos0, pos1, blendInfo.z);
}

#endif
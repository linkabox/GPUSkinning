
#ifndef PLANARSHADOW_INCLUDE
#define PLANARSHADOW_INCLUDE

float3 ShadowProjectPos(float4 vertex, float floor, float4 worldLightPos)
{
	float3 shadowPos;

	//得到顶点的世界空间坐标
	float3 worldPos = mul(unity_ObjectToWorld , vertex).xyz;

	//灯光方向
	float3 lightDir = normalize(worldLightPos.xyz);

	//阴影的世界空间坐标（低于地面的部分不做改变）
	shadowPos.y = min(worldPos.y , floor);
	shadowPos.xz = worldPos.xz - lightDir.xz * max(0 , worldPos.y - floor) / lightDir.y;

	return shadowPos;
}

float4 ShadowFalloffColor(float4 shadowColor,float3 shadowPos,float falloff,float floor)
{
	//得到中心点世界坐标
	float3 center = float3(unity_ObjectToWorld[0].w , floor , unity_ObjectToWorld[2].w);
	//计算阴影衰减
	float f = 1 - saturate(distance(shadowPos , center) * falloff);

	//阴影颜色
	shadowColor.a *= f;
	return shadowColor;
}

#endif //PLANARSHADOW_INCLUDE
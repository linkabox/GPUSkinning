Shader "GPUSkinning/Cartoon"
{
	Properties
	{
		_Color ("Color", color) = (1,1,1,1)
		_MainTex ("Texture", 2D) = "white" {}
		[HideInInspector]_boneTexture ("boneTexture", 2D) = "white" {}
		[HideInInspector]_boneTextureParams ("boneTextureParams", Vector) = (0,0,0,0)

		_RampBias("Ramp Bias", Range(0, 1)) = 0.5
        _AmbientBlend("Ambient Blend", Range(0, 1)) = 0.5
		_Edge("Cell Edge", Range(0, 1)) = 0.1

		_ShadowFalloff("_ShadowFalloff", Float) = 0
		_ShadowColor("_ShadowColor", Color) = (0,0,0,0.5)
		_Floor("_Floor", float) = 0
	}

	CGINCLUDE
	#include "UnityCG.cginc"
	#include "Lighting.cginc"
	#include "AutoLight.cginc"
	#include "GPUSkinningInclude.cginc"

	struct appdata
	{
		float4 vertex : POSITION;
		float3 normal : NORMAL;
		float2 uv : TEXCOORD0;
		float4 uv2 : TEXCOORD1;
		float4 uv3 : TEXCOORD2;
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct v2f
	{
		float4 pos : SV_POSITION;
		float4 posWorld : TEXCOORD0;
		float3 normalDir : TEXCOORD1;
		float3 lightDir : TEXCOORD2;
		float3 viewDir : TEXCOORD3;
		float3 vertexLighting : TEXCOORD4;
		LIGHTING_COORDS(5, 6)
		float2 uv : TEXCOORD7;
	};

	fixed4 _Color;
	sampler2D _MainTex;
	float4 _MainTex_ST;
	half _RampBias;
	half _AmbientBlend;
	half _Edge;

	v2f vert(appdata v)
	{
		UNITY_SETUP_INSTANCE_ID(v);

		skinning_normal(v.vertex, v.normal, v.uv2, v.uv3);

		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);

		o.posWorld = mul(unity_ObjectToWorld, v.vertex);
		o.normalDir = UnityObjectToWorldNormal(v.normal);
		o.lightDir = UnityWorldSpaceLightDir(o.posWorld);
		o.viewDir = UnityWorldSpaceViewDir(o.posWorld);
		o.vertexLighting = float3(0, 0, 0);

	// SH/ambient and vertex lights  
#ifdef LIGHTMAP_OFF  
		o.vertexLighting = ShadeSH9(float4(o.normalDir, 1.0));;
#ifdef VERTEXLIGHT_ON  
		float3 vertexLight = Shade4PointLights(
			unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
			unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
			unity_4LightAtten0, o.posWorld, o.normalDir);
		o.vertexLighting += vertexLight;
#endif // VERTEXLIGHT_ON
#endif // LIGHTMAP_OFF

		// pass lighting information to pixel shader  
		TRANSFER_VERTEX_TO_FRAGMENT(o);

		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		fixed4 col = tex2D(_MainTex, i.uv) * _Color;

		float diff = dot(i.normalDir, i.lightDir) * LIGHT_ATTENUATION(i);
		float LightToon = smoothstep(_RampBias, _RampBias + _Edge, diff);
		fixed4 diff2 = LightToon * _LightColor0 * 0.8 + (1 - LightToon) * UNITY_LIGHTMODEL_AMBIENT * _AmbientBlend;
		col.rgb *= diff2.rgb + i.vertexLighting;

		return col;
	}
	ENDCG
	
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		Pass
		{
			Name "CelShade"
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_instancing
			#pragma multi_compile BLEND_OFF BLEND_ON
			//#pragma multi_compile BONE_1 BONE_2 BONE_4
			
			ENDCG
		}

		UsePass "GPUSkinning/PlanarShadow/SHADOW"
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			Name "CelShade"
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_instancing
			#pragma multi_compile BLEND_OFF BLEND_ON
			//#pragma multi_compile BONE_1 BONE_2 BONE_4
			
			ENDCG
		}
	}

	CustomEditor "GPUSkinningShaderEditor"
}

Shader "GPUSkinning/PlanarShadow"
{
	Properties
	{
		_ShadowFalloff("_ShadowFalloff", Float) = 0
		_ShadowColor("_ShadowColor", Color) = (0,0,0,1)
		_Floor("_Floor", float) = 0

		[HideInInspector]_boneTexture ("boneTexture", 2D) = "white" {}
		[HideInInspector]_boneTextureParams ("boneTextureParams", Vector) = (0,0,0,0)
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		Pass
		{
			Name "Shadow"
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite OFF
			Offset -1,0 //深度稍微偏移防止阴影与地面穿插

			Stencil
			{
				Ref 0
				Comp equal
				Pass incrWrap
				Fail keep
				ZFail keep
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			#include "UnityCG.cginc"
			#include "GPUSkinningInclude.cginc"
			#include "PlanarShadowInclude.cginc"
			
			struct appdata
			{
				float4 vertex : POSITION;
				float4 uv2 : TEXCOORD1;
				float4 uv3 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
			};

			float _Floor;
			float4 _ShadowColor;
			float _ShadowFalloff;

			v2f vert(appdata v)
			{
				UNITY_SETUP_INSTANCE_ID(v);

				v2f o;
				skinning(v.vertex, v.uv2, v.uv3);
				//得到阴影的世界空间坐标
				float3 shadowPos = ShadowProjectPos(v.vertex, _Floor, _WorldSpaceLightPos0);

				//转换到裁切空间
				o.vertex = UnityWorldToClipPos(shadowPos);
				o.color = ShadowFalloffColor(_ShadowColor, shadowPos, _ShadowFalloff, _Floor);

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return i.color;
			}
			ENDCG
		}
	}

	CustomEditor "GPUSkinningShaderEditor"
}

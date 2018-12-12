Shader "GPUSkinning/Unlit"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		[HideInInspector]_boneTexture ("boneTexture", 2D) = "white" {}
		[HideInInspector]_boneTextureParams ("boneTextureParams", Vector) = (0,0,0,0)
	}

		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile BLEND_OFF BLEND_ON
			//#pragma multi_compile BONE_1 BONE_2 BONE_4
			#include "UnityCG.cginc"
			#include "GPUSkinningInclude.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 uv2 : TEXCOORD1;
				float4 uv3 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			v2f vert(appdata v)
			{
				UNITY_SETUP_INSTANCE_ID(v);

				v2f o;

				skinning(v.vertex, v.uv2, v.uv3);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				return col;
			}
			ENDCG
		}
	}

	CustomEditor "GPUSkinningShaderEditor"
}

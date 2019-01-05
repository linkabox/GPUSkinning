Shader "GPUSkinning/ShadowCaster"
{
	Properties
	{
		[HideInInspector]_boneTexture ("boneTexture", 2D) = "white" {}
		[HideInInspector]_boneTextureParams ("boneTextureParams", Vector) = (0,0,0,0)
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			#include "GPUSkinningInclude.cginc"

			struct appdata_shadow
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 texcoord : TEXCOORD0;
				float4 uv2 : TEXCOORD1;
				float4 uv3 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				V2F_SHADOW_CASTER;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert( appdata_shadow v )
			{
				skinning(v.vertex, v.uv2, v.uv3);

				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float4 frag( v2f i ) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}

	CustomEditor "GPUSkinningShaderEditor"
}

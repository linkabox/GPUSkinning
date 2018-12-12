Shader "GPUSkinning/Diffuse" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		[HideInInspector]_boneTexture ("boneTexture", 2D) = "white" {}
		[HideInInspector]_boneTextureParams ("boneTextureParams", Vector) = (0,0,0,0)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Lambert addshadow
		#pragma vertex vert
		#pragma multi_compile BLEND_OFF BLEND_ON
		#include "GPUSkinningInclude.cginc"

		sampler2D _MainTex;
		fixed4 _Color;

		struct Input {
			float2 uv_MainTex;
		};

		void vert(inout appdata_full v)
		{
			skinning_normal(v.vertex, v.normal, v.texcoord1, v.texcoord2);
			//skinning(v.vertex, v.texcoord1, v.texcoord2);
		}

		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
	CustomEditor "GPUSkinningShaderEditor"
}

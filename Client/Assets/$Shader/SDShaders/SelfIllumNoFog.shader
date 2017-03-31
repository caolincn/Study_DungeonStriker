Shader "SDShader/SelfIllumNoFog_Object" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Color ("Main Color", Color) = (1,1,1,1)
	}
	SubShader {
		Tags { "Queue"="Geometry+55" "RenderType"="Opaque" }
		LOD 200
		
		Fog { Mode Off }
		pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			//#define SHADER_API_GLES
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;
		
			struct v2f{
				float4 pos 		: SV_POSITION;
				float2 uv 		: TEXCOORD0;
				float4 retColor 	: TEXCOORD2;
			};
		
			v2f vert(appdata_full v)
			{
				v2f o;
				o.pos = mul(UNITY_MATRIX_MVP,v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord,_MainTex);
				o.retColor		=	(_Color*2.0f);
				return o;
			}
		
			half4 frag(v2f i) : COLOR
			{
				//float emis = 2-saturate(dot(i.flength,i.flength));//1.0f - saturate(i.flength/MajorLightColor.w);
				return tex2D(_MainTex,i.uv)*i.retColor;//*emis;//tex2D(_MainTex,i.uv)*lm*2.0f*_Color*(1+emis*2.0f);
			}
			ENDCG
		}
	} 
	FallBack "Diffuse"
}

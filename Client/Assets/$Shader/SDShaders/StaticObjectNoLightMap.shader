// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "SDShader/Static_Object_NoLightMap" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Color ("Main Color", Color) = (1,1,1,1)
	}
	SubShader {
		Tags { "Queue"="Geometry+5" "RenderType"="Opaque" }
		LOD 200
		
		
		pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			//#define SHADER_API_GLES
			#include "UnityCG.cginc"


			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 MajorPos;
			float4 MajorLightColor;
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

				float4 worldPos = mul(unity_ObjectToWorld,v.vertex);
				worldPos /= worldPos.w;
				
				float3 vTemp	=	(worldPos.xyz- MajorPos.xyz)/MajorLightColor.w;
				float f	=	2.0020f-saturate(dot(vTemp,vTemp));
				o.retColor		=	_Color*MajorLightColor*f;

				return o;
			}
		
			half4 frag(v2f i) : COLOR
			{

				return tex2D(_MainTex,i.uv)*i.retColor;//*emis;//tex2D(_MainTex,i.uv)*lm*2.0f*_Color*(1+emis*2.0f);
			}
			ENDCG
		}
	} 
	FallBack "Diffuse"
}

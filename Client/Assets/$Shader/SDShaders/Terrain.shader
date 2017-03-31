// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: commented out 'half4 unity_LightmapST', a built-in variable
// Upgrade NOTE: commented out 'sampler2D unity_Lightmap', a built-in variable
// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D

Shader "SDShader/Static_Terrain" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Color ("Main Color", Color) = (1,1,1,1)
	}
	SubShader {
		Tags { "Queue"="Geometry+40" "RenderType"="Opaque" }
		LOD 200
		
		
		pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			//#define SHADER_API_GLES
			#include "UnityCG.cginc"
			#pragma multi_compile	LIGHTMAP_ON LIGHTMAP_OFF
			#define LIGHTMAP_ON
			#ifdef LIGHTMAP_ON
			// half4		unity_LightmapST;
			// sampler2D	unity_Lightmap;
			#endif

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 MajorPos;
			float4 MajorLightColor;
			float4 _Color;
		
			struct v2f{
				float4 pos 		: SV_POSITION;
				float2 uv 		: TEXCOORD0;
				#ifdef LIGHTMAP_ON
				float2 lmuv		: TEXCOORD1;
				#endif
				float4 retColor 	: TEXCOORD2;
			};
		
			v2f vert(appdata_full v)
			{
				v2f o;
				o.pos = mul(UNITY_MATRIX_MVP,v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord,_MainTex);
				#ifdef LIGHTMAP_ON
				o.lmuv	=	v.texcoord1.xy*unity_LightmapST.xy+unity_LightmapST.zw;
				#endif
				float4 worldPos = mul(unity_ObjectToWorld,v.vertex);
				worldPos /= worldPos.w;
				
				float3 vTemp	=	(worldPos.xyz- MajorPos.xyz)/MajorLightColor.w;
				float f	=	2.0040f-saturate(dot(vTemp,vTemp));
				o.retColor		=	_Color*MajorLightColor*2.0f*f;
				#if (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)) && defined(SHADER_API_MOBILE)
					
				#else
					o.retColor		*=	4.0f;
				#endif
				return o;
			}
		
			half4 frag(v2f i) : COLOR
			{
				float4 lightmap	=	UNITY_SAMPLE_TEX2D(unity_Lightmap,i.lmuv);
				
				#if (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)) && defined(SHADER_API_MOBILE)

				#else
					lightmap = float4(lightmap.xyz,1)*lightmap.w;
				#endif
				return tex2D(_MainTex,i.uv)*i.retColor*lightmap;//*emis;//tex2D(_MainTex,i.uv)*lm*2.0f*_Color*(1+emis*2.0f);
			}
			ENDCG
		}
	} 
	FallBack "Diffuse"
}

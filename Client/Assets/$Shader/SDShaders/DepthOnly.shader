Shader "SDShader/DepthOnly" {

	SubShader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		LOD 200
		
		Blend One One
		Cull Off 
		Lighting Off 
		ZTest Always
		Fog { Color (0,0,0,0) }
		pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			//#define SHADER_API_GLES
			#include "UnityCG.cginc"

		
			struct v2f{
				float4 pos 		: SV_POSITION;
				float2 uv 		: TEXCOORD0;
				float4 retColor 	: TEXCOORD2;
			};
		
			v2f vert(appdata_full v)
			{
				v2f o;
				o.pos = mul(UNITY_MATRIX_MVP,v.vertex);
				o.uv = v.texcoord;
				o.retColor		=	0;//(_Color*2.0f);
				return o;
			}
		
			half4 frag(v2f i) : COLOR
			{
				//float emis = 2-saturate(dot(i.flength,i.flength));//1.0f - saturate(i.flength/MajorLightColor.w);
				return 0;//tex2D(_MainTex,i.uv)*i.retColor;//*emis;//tex2D(_MainTex,i.uv)*lm*2.0f*_Color*(1+emis*2.0f);
			}
			ENDCG
		}
	} 
	FallBack "Diffuse"
}

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "SDShader/Transparent/ShaderCmBlendNoLightDouble" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Color ("Main Color", Color) = (1,1,1,1)
	}
	SubShader {
		Tags {"Queue" = "Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		LOD 200
		
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		cull Off
		
		pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 MajorPos;
			float4 MajorLightColor;
			fixed4 _Color;
		
			struct v2f{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
			};
		
			v2f vert(appdata_base v)
			{
				v2f o;
				o.pos = mul(UNITY_MATRIX_MVP,v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord,_MainTex);
				
				float4 worldPos = mul(unity_ObjectToWorld,v.vertex);
				worldPos /= worldPos.w;
				o.worldPos = worldPos.xyz;
				return o;
			}
		
			half4 frag(v2f i) : COLOR
			{
				half3 vec = i.worldPos.xyz - MajorPos.xyz;
				float len = length(vec);
				half emis = 1.0f - saturate(len/MajorLightColor.w);
				emis = emis*emis;
				half4 c = tex2D(_MainTex,i.uv)*(_Color+float4(MajorLightColor.xyz*emis,0.0f));
				
				return c;
			}
		ENDCG
		}
	} 
	FallBack "Diffuse"
}

Shader "SDShader/ShaderPlayerBlend" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Color ("Main Color", Color) = (1,1,1,1)
		_AddColor("Add Color", Color) = (0,0,0,1)
	}
	SubShader {
		Tags {"Queue" = "Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		LOD 200
		
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		
		CGPROGRAM
		#pragma surface surf Lambert noforwardadd

		sampler2D _MainTex;
		float4 MajorPos;
		float4 MajorLightColor;
		fixed4 _Color;
		
		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			half3 vec = IN.worldPos.xyz - MajorPos.xyz;
			float len = length(vec);
			half emis = 1.0f - saturate(len/MajorLightColor.w);
			emis = emis*emis;
			half4 c = tex2D (_MainTex, IN.uv_MainTex)*_Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
			
			o.Emission = c.rgb*MajorLightColor.xyz*emis;
		}
		ENDCG
	}
	FallBack "Diffuse"
}

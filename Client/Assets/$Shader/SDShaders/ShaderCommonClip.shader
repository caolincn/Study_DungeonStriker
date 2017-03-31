Shader "SDShader/Transparent/ShaderCommonClip" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_cutOff ("CutOff",Range(0.0,1.0)) = 0.5
		_Color ("Main Color", Color) = (1,1,1,1)
	}
	SubShader {
		Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert noforwardadd alphatest:_cutoff

		sampler2D _MainTex;
		float _cutOff;
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
			//saturate((vec.x*vec.x+vec.y*vec.y+vec.z*vec.z)/(MajorLightColor.w*MajorLightColor.w));
			//emis *= IN.color.xyz;
			half4 c = tex2D (_MainTex, IN.uv_MainTex)*_Color;
			clip(c.a - _cutOff);
			o.Albedo = c.rgb;
			o.Alpha = c.a;
			
			o.Emission = c.rgb*MajorLightColor.xyz*emis;
		}
		ENDCG
	} 
	Fallback "Transparent/Cutout/VertexLit"

}

Shader "SDShader/ShaderPlayerDouble" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_cutOff ("CutOff",Range(0.0,1.0)) = 0.5
		_Color ("Main Color", Color) = (1,1,1,1)
		_AddColor ("AddColor", Color) = (0,0,0,1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		Cull off
		
		CGPROGRAM
		#pragma surface surf Lambert noforwardadd

		sampler2D _MainTex;
		float4 _AddColor;
		float4 MajorPos;
		float4 MajorLightColor;
		float4 cameraDir;
		float _cutOff;
		fixed4 _Color;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
			float3 worldNormal;
		};
		
		/*void vert(inout appdata_full v)
		{
			float4 worldPos = mul(_Object2World,v.vertex);
			worldPos /= worldPos.w;
			half3 vec = worldPos.xyz - MajorPos.xyz;
			float len = length(vec);
			half emis = 1.0f - saturate(len/MajorLightColor.w);
			emis = emis*emis;
			//half emis = 1.0f - saturate((vec.x*vec.x+vec.y*vec.y+vec.z*vec.z)/(MajorLightColor.w*MajorLightColor.w));
			float3 wNormal = mul(_Object2World,float4(v.normal,0)).xyz;
			normalize(wNormal);
			emis *= saturate(dot(wNormal,cameraDir.xyz));
			v.color.xyz = MajorLightColor.xyz*emis;
		}*/

		void surf (Input IN, inout SurfaceOutput o) {
			
			
			
			float fDot	=	saturate(dot(normalize(float3(-1,1,-1)),IN.worldNormal)*0.8f)+0.2f;
			float3 vCameraDir	=	normalize(cameraDir.xyz-IN.worldPos.xyz);
			float frenel	=	pow(saturate(1.0f-dot(IN.worldNormal,vCameraDir)),5.0f)*0.5f;
			//float len = pow(length(vec),MajorPos.w);
			//half emis = saturate(len/MajorLightColor.w);
			//emis = cos(emis*3.1415926f)*0.5f+0.5f;
			//half emis = 1.0f - saturate((vec.x*vec.x+vec.y*vec.y+vec.z*vec.z)/(MajorLightColor.w*MajorLightColor.w));
			//emis *= IN.color.xyz;
			half4 c = tex2D (_MainTex, IN.uv_MainTex)*_Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
			clip(c.a - _cutOff);
			
			o.Emission = (MajorLightColor.xyz*c.rgb*(fDot+frenel)+_AddColor);//*emis;
		}
		ENDCG
	} 
	
	FallBack "Diffuse"
}

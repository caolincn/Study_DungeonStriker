Shader "SDShader/ShaderPlayerDiffuse" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Color ("Main Color", Color) = (1,1,1,1)
		_AddColor("Add Color", Color) = (0,0,0,1)
	}
	 
	SubShader {
		Tags { "Queue"="Geometry+25" "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert noforwardadd

		sampler2D _MainTex;
		float4 MajorPos;
		float4 MajorLightColor;
		float4 cameraDir;
		fixed4 _Color;
		float4 _AddColor;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
			float3 worldNormal;
			//float4 color : COLOR;
		};
		
		/*void vert(inout appdata_full v)
		{
			float3 wNormal = mul(_Object2World,float4(v.normal,0)).xyz;
			//normalize(wNormal);
			float emis = saturate(dot(wNormal,cameraDir.xyz));
			v.color.xyz = emis;
		}*/

		void surf (Input IN, inout SurfaceOutput o) {
			//half3 vec = IN.worldPos.xyz - MajorPos.xyz;
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
			
			
			o.Emission = (MajorLightColor.xyz*c.rgb*(fDot+frenel)+_AddColor);//*emis;
		}
		ENDCG
	} 
	
	FallBack "Diffuse"
}

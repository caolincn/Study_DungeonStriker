Shader "FX PACK 1/Energy Ball"
{
	Properties 
	{
		_MainTex("_MainTex", 2D) = "white" {}
		_Color("Main_Texture_Color", Color) = (1,1,1,1)
		_Blend_Texture("Blend_Texture", 2D) = "white" {}
		_Color02("Blend_Texture_Color", Color) = (1,1,1,1)
		_Blend_Texture01("Blend_Texture01", 2D) = "black" {}
		_Speed("Main_Texutre_Speed", Float) = 1
		_Speed01("Blend_Texture_Speed", Float) = 1
		_Lighten("Lighten", Float) = 1

	}
	
	SubShader 
	{
		Tags
		{
			"Queue"="Transparent"
			"IgnoreProjector"="False"
			"RenderType"="Transparent"

		}

		
		Cull Back
		ZWrite Off
		ZTest LEqual
		Blend One	One
		ColorMask RGBA
		FOG{Color(0,0,0,0)}


		pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			//#define SHADER_API_GLES
			#include "UnityCG.cginc"
			
			
			
			
			

			sampler2D _MainTex;
			float4 _Color;
			float4 _MainTex_ST;
			sampler2D	_Blend_Texture;
			float4 _Blend_Texture_ST;
			float4 _Color02;
			sampler2D _Blend_Texture01;
			float4 _Blend_Texture01_ST;
			float _Speed;
			float _Speed01;
			float _Speed02;
			float	_Lighten;
			
			struct v2f{
				float4 pos 		: SV_POSITION;
				float2 uv 		: TEXCOORD0;
				float2 lmuv		: TEXCOORD1;
				float2 bluv 	: TEXCOORD2;
				float4 retColor	: TEXCOORD3;
			};
		
			v2f vert(appdata_full v)
			{
				v2f o;
				o.pos = mul(UNITY_MATRIX_MVP,v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord,_MainTex);
				o.uv.y	-=	_Speed*_Time.x;
				o.lmuv	=	TRANSFORM_TEX(v.texcoord,_Blend_Texture);
				o.lmuv.y	-=	_Speed01*_Time.x;
				o.bluv		=	TRANSFORM_TEX(v.texcoord,_Blend_Texture01);
				o.retColor	=	_Lighten*_Color;
				return o;
			}
		
			half4 frag(v2f i) : COLOR
			{
				float4 	main		=	tex2D(_MainTex,i.uv);
				float4	blendcolor	=	tex2D(_Blend_Texture,i.lmuv);
				float4 	bcolor1		=	tex2D(_Blend_Texture01,i.bluv);
				float4 	c	=	(main*_Color*blendcolor*_Color02*bcolor1)*_Lighten;//+bcolor1*i.retColor;
				return	c;//*emis;//tex2D(_MainTex,i.uv)*lm*2.0f*_Color*(1+emis*2.0f);
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
}
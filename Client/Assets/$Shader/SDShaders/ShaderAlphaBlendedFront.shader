Shader "SDShader/AlphaBlended02Front" {
Properties {
	_TintColor ("Tint Color", Color) = (1,1,1,1)
	_MainTex ("Particle Texture", 2D) = "white" {}
}

/*Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Blend SrcAlpha OneMinusSrcAlpha
	Ztest Off
	AlphaTest Off
	ColorMask RGBA
	Cull Back Lighting Off ZWrite Off Fog { Mode Off }
	BindChannels {
		Bind "Color", color
		Bind "Vertex", vertex
		Bind "TexCoord", texcoord
	}
	
	// ---- Dual texture cards
	SubShader {
		Pass {
			SetTexture [_MainTex] {
				constantColor [_TintColor]
				combine constant * primary double
			}
			SetTexture [_MainTex] {
				combine texture * previous
			}
		}
	}
	
	// ---- Single texture cards (does not do color tint)
	SubShader {
		Pass {
			SetTexture [_MainTex] {
				combine texture * primary
			}
		}
	}
}
}*/

CGINCLUDE

	#include "UnityCG.cginc"
	
	uniform sampler2D _MainTex;
	fixed4 _TintColor;
	uniform fixed4 _MainTex_ST;

	struct appdata {
		half4 vertex : POSITION;
		half2 texcoord : TEXCOORD0;	
		fixed4 color : COLOR0;
	};

	struct v2f {
		half4 pos : SV_POSITION;
		half2	uv : TEXCOORD0;
		fixed4	color : COLOR;
	};	

	v2f vert (appdata v)
	{
		v2f o;
		o.pos = mul (UNITY_MATRIX_MVP, v.vertex);		
		o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
		o.color = v.color;
		return o;
	}
	
	fixed4 frag (v2f i) : COLOR
	{
		fixed4 Velcol = i.color * _TintColor * 2;
		fixed4 texcol = tex2D( _MainTex, i.uv ) * Velcol;
		return texcol;
	}

	ENDCG
	
SubShader {
	Fog { Mode Off }
	Tags { "Queue" = "Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Lighting Off	
	ZWrite Off
	Ztest Off
	
    Pass {	
	AlphaTest Off
	Blend SrcAlpha OneMinusSrcAlpha 

	CGPROGRAM
	#pragma vertex vert
	#pragma fragment frag
	
	ENDCG
    }
}
Fallback "iMonster/VertexColored/FX_SingleVertexColor"
} 
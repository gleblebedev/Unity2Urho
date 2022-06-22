Shader "Urho3D/Water" {
	Properties{
		_Color("Water Tint", Color) = (0.8,0.8,1,1)
		_SpecularColor("Specular", Color) = (1,1,1,1)
		_SpecularPower("Specular Power", Float) = 1.0
		_BumpMap("Normalmap", 2D) = "bump" {}
		_BumpScale("Normalmap Scale", Range(0,1)) = 0.1
		_NoiseSpeedX("Noise Speed X", Range(0,1)) = 0.05
		_NoiseSpeedY("Noise Speed Y", Range(0,1)) = 0.05
		_NoiseStrength("Noise Strength", Range(0,1)) = 0.02
		_FadeOffset("Fade Offset", Float) = 0.0
		_FadeScale("Fade Scale", Float) = 25.0
	}
	SubShader {
		// We must be transparent, so other objects are drawn before this one.
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Opaque" }

		GrabPass {
		}
		Pass {
			CGPROGRAM
			#pragma debug
			//#include "UnityPBSLighting.cginc"
			//#pragma surface frag Standard
			#pragma fragment frag 
			#pragma vertex vert
			#pragma target 3.0

			sampler2D _BumpMap;
			// builtin variable to get Grabbed Texture if GrabPass has no name
			sampler2D _GrabTexture;
			float _BumpScale;
			fixed4 _Color;

			struct v2f {
				float4 position : POSITION;
				float4 screenPos : TEXCOORD0;
				float2 uv_BumpMap: TEXCOORD1;
			};

			struct data {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv_BumpMap: TEXCOORD0;
			};

			//struct Input {
			//	half2 uv_MainTex;
			//	half2 uv_BumpMap;
			//	half3 viewDir;
			//	INTERNAL_DATA
			//};

			v2f vert(data i) {
				v2f o;
				o.position = UnityObjectToClipPos(i.vertex);
				o.screenPos = o.position;
				o.uv_BumpMap = i.uv_BumpMap;
				return o;
			}


			half4  frag(v2f i) : COLOR {
				float4 n = tex2D(_GrabTexture, float2(i.uv_BumpMap));

				float2 screenPos = i.screenPos.xy / i.screenPos.w;
				screenPos.x = (screenPos.x + 1) * 0.5;
				screenPos.y = 1 - (screenPos.y + 1) * 0.5;
				screenPos += n.xy * _BumpScale;

				return _Color * tex2D(_GrabTexture, float2(screenPos.x, screenPos.y));
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}


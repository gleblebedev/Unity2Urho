Shader "Urho3D/Water" {
	Properties{
		_Color("Water Tint", Color) = (0.8,0.8,1,1)
		_BumpMap("Normalmap", 2D) = "bump" {}
		_NoiseSpeedX("Noise Speed X", Range(0,1)) = 0.05
		_NoiseSpeedY("Noise Speed Y", Range(0,1)) = 0.05
		_NoiseStrength("Noise Strength", Range(0,1)) = 0.02
		_NoiseTiling("Noise Tiling", Float) = 50
		[PowerSlider(4)] _FresnelPower("Fresnel Exponent", Range(0.25, 8)) = 4
	}

		CGINCLUDE
#define _GLOSSYENV 1
#define UNITY_SETUP_BRDF_INPUT MetallicSetup
			ENDCG

			SubShader{
				Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
				LOD 200

				CGPROGRAM
				#include "UnityPBSLighting.cginc"
				#pragma surface surf Standard vertex:vert
				#pragma target 3.0

			sampler2D _BumpMap;
			fixed4 _Color;

			struct Input {
				half2 uv_MainTex;
				half2 uv_BumpMap;
				half3 viewDir;
				INTERNAL_DATA
			};

			void vert(inout appdata_full v) {
			}


			void surf(Input IN, inout SurfaceOutputStandard o) {

				half3 finalnormal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
				o.Albedo = _Color;
				o.Smoothness = 0;
				o.Metallic = 0;
				o.Normal = normalize(finalnormal);
			}
			ENDCG
		}
		FallBack "Diffuse"
}

Shader "Urho3D/Skybox" {
	Properties{
		[NoScaleOffset] _Tex("Cubemap   (HDR)", Cube) = "grey" {}
	}

		SubShader{
			Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
			Cull Off ZWrite Off

			Pass {

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"

				samplerCUBE _Tex;

				float4 RotateAroundYInDegrees(float4 vertex, float degrees)
				{
					float alpha = degrees * UNITY_PI / 180.0;
					float sina, cosa;
					sincos(alpha, sina, cosa);
					float2x2 m = float2x2(cosa, -sina, sina, cosa);
					return float4(mul(m, vertex.xz), vertex.yw).xzyw;
				}

				struct appdata_t {
					float4 vertex : POSITION;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float3 texcoord : TEXCOORD0;
				};

				v2f vert(appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.texcoord = v.vertex.xyz;
					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					half4 tex = texCUBE(_Tex, i.texcoord);
					return tex;
				}
				ENDCG
			}
	}

	Fallback Off
}

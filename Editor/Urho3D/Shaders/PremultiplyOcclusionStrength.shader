Shader "Hidden/UnityToCustomEngineExporter/Urho3D/PremultiplyOcclusionStrength"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1
    }
        SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            inline float3 ColorLinearToGamma(float3 value)
            {
                return float3(LinearToGammaSpaceExact(value.r), LinearToGammaSpaceExact(value.g), LinearToGammaSpaceExact(value.b));
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _OcclusionStrength;

            fixed4 frag(v2f i) : SV_Target
            {
                float4 rgba = tex2D(_MainTex, i.uv);
                float brightness = (rgba.r * 0.3 + rgba.g * 0.59 + rgba.b * 0.11) * _OcclusionStrength + (1.0 - _OcclusionStrength);
                return float4(ColorLinearToGamma(float3(brightness, brightness, brightness)), rgba.a);
            }
            ENDCG
        }
    }
}


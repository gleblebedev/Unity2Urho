Shader "Hidden/UnityToCustomEngineExporter/Urho3D/CombineMetallicRoughnessOcclusion"
{
    Properties
    {
        _RoughnessMap("Roughness Texture", 2D) = "black" {}
        _RoughnessScale("Roughness Scale", Float) = 1.0
        _RoughnessOffset("Roughness Offset", Float) = 0.0
        _RoughnessMask("Roughness Mask", Vector) = (1,1,1,0)

        _MetallicMap("Metallic Texture", 2D) = "black" {}
        _MetallicScale("Metallic Scale", Float) = 1.0
        _MetallicOffset("Metallic Offset", Float) = 0.0
        _MetallicMask("Metallic Mask", Vector) = (1,1,1,0)

        _OcclusionMap("Occlusion Texture", 2D) = "white" {}
        _OcclusionScale("Occlusion Scale", Float) = 1.0
        _OcclusionOffset("Occlusion Offset", Float) = 0.0
        _OcclusionMask("Occlusion Mask", Vector) = (1,1,1,0)
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _RoughnessMap;
            float _RoughnessScale;
            float _RoughnessOffset;
            float4 _RoughnessMask;

            sampler2D _MetallicMap;
            float _MetallicScale;
            float _MetallicOffset;
            float4 _MetallicMask;

            sampler2D _OcclusionMap;
            float _OcclusionScale;
            float _OcclusionOffset;
            float4 _OcclusionMask;


            inline float3 ColorLinearToGamma(float3 value)
            {
                return float3(LinearToGammaSpaceExact(value.r), LinearToGammaSpaceExact(value.g), LinearToGammaSpaceExact(value.b));
            }
            inline float3 ColorGammaToLinear(float3 value)
            {
                return float3(GammaToLinearSpaceExact(value.r), GammaToLinearSpaceExact(value.g), GammaToLinearSpaceExact(value.b));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float roughness = dot(tex2D(_RoughnessMap, i.uv), _RoughnessMask) / dot(_RoughnessMask, float4(1,1,1,1)) * _RoughnessScale + _RoughnessOffset;
                float metallic = dot(tex2D(_MetallicMap, i.uv), _MetallicMask) / dot(_MetallicMask, float4(1, 1, 1, 1)) * _MetallicScale + _MetallicOffset;
                float occlusion = dot(tex2D(_OcclusionMap, i.uv), _OcclusionMask) / dot(_OcclusionMask, float4(1, 1, 1, 1)) * _OcclusionScale + _OcclusionOffset;
                return fixed4(roughness, metallic, 0, occlusion);
            }
            ENDCG
        }
    }
}

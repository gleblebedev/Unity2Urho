Shader "Hidden/UnityToCustomEngineExporter/Urho3D/Copy"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [Toggle] _GammaInput("GammaInput", Float) = 1
        [Toggle] _GammaOutput("GammaOutput", Float) = 1
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
            inline float3 ColorGammaToLinear(float3 value)
            {
                return float3(GammaToLinearSpaceExact(value.r), GammaToLinearSpaceExact(value.g), GammaToLinearSpaceExact(value.b));
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _GammaInput;
            float _GammaOutput;

            fixed4 frag(v2f i) : SV_Target
            {
                float4 rgba = tex2D(_MainTex, i.uv);
                if (_GammaInput == _GammaOutput)
                    return rgba;
                if (_GammaInput < _GammaOutput)
                    return float4(ColorLinearToGamma(rgba.rgb), rgba.a);
                return float4(ColorGammaToLinear(rgba.rgb), rgba.a);
            }
            ENDCG
        }
    }
}

Shader "Hidden/UnityToCustomEngineExporter/Urho3D/ConvertToMetallicRoughness"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Smoothness("Smoothness", 2D) = "white" {}
        _MetallicScale("MetallicScale", Float) = 1.0
        _SmoothnessRemapMin("SmoothnessRemapMin", Float) = 0.0
        _SmoothnessRemapMax("SmoothnessRemapMax", Float) = 1.0
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

            sampler2D _MainTex;
            sampler2D _Smoothness;
            float _SmoothnessRemapMin;
            float _SmoothnessRemapMax;
            float _MetallicScale;

            fixed4 frag (v2f i) : SV_Target
            {
                float4 metGloss = tex2D(_MainTex, i.uv);
                float smoothness = tex2D(_Smoothness, i.uv).a;
                float r = 1.0 - (_SmoothnessRemapMin + smoothness * (_SmoothnessRemapMax - _SmoothnessRemapMin));
                return fixed4(r, metGloss.r * _MetallicScale, 0, 1);

            }
            ENDCG
        }
    }
}

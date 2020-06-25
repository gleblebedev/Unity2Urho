Shader "Hidden/UnityToCustomEngineExporter/Urho3D/ConvertToMetallicRoughness"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Smoothness("Smoothness", 2D) = "white" {}
        _SmoothnessScale("Smoothness Factor", Range(0.0, 1.0)) = 1 }
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
            float _SmoothnessScale;

            fixed4 frag (v2f i) : SV_Target
            {
                float4 metGloss = tex2D(_MainTex, i.uv);
                float r = 1.0 - tex2D(_Smoothness, i.uv).a * _SmoothnessScale;
                return fixed4(r, metGloss.r, 0, 1);

            }
            ENDCG
        }
    }
}

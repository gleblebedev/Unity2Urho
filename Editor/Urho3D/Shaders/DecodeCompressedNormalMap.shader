Shader "Hidden/UnityToCustomEngineExporter/Urho3D/DecodeCompressedNormalMap"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BumpScale("Normal Intensity", Range(0, 1)) = 1
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
            #include "Lighting.cginc"

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
            half _BumpScale;

            fixed4 frag (v2f i) : SV_Target
            {
                //half3 tnormal = UnpackScaleNormal(tex2D(_MainTex, i.uv), _BumpScale);
                half3 tnormal = UnpackNormal(tex2D(_MainTex, i.uv));
                tnormal = normalize(lerp(half3(0, 0, 1), tnormal, _BumpScale));
                half x = tnormal.x * 0.5 + 0.5;
                half y = tnormal.y * 0.5 + 0.5;
                return fixed4(y,y,y,x);
            }
            ENDCG
        }
    }
}

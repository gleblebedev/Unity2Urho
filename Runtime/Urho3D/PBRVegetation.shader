Shader "Urho3D/PBR/PBRVegetation"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Cutoff("Alpha cutoff", Range(0,1)) = 0.5

        _WindHeightFactor("WindHeightFactor", Range(0,1)) = 0.0
        _WindHeightPivot("WindHeightPivot", Float) = 0.0
        _WindStemAxisX("WindStemAxis X",  Float) = 0.0
        _WindStemAxisY("WindStemAxis Y",  Float) = 1.0
        _WindStemAxisZ("WindStemAxis Z",  Float) = 0.0
        _WindPeriod("WindPeriod", Range(0,1)) = 0.0
        _WindWorldSpacingX("WindWorldSpacing X", Range(0,1)) = 0.0
        _WindWorldSpacingY("WindWorldSpacing Y", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "Queue" = "AlphaTest" "RenderType"="TransparentCutout" }
        LOD 200
        //Cull Off

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard addshadow fullforwardshadows alphatest:_Cutoff vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        half _WindHeightFactor;
        half _WindHeightPivot;
        half _WindStemAxisX;
        half _WindStemAxisY;
        half _WindStemAxisZ;
        half _WindPeriod;
        half _WindWorldSpacingX;
        half _WindWorldSpacingY;

        void vert(inout appdata_full v) {

            half stemDistance = dot(v.vertex, half3(_WindStemAxisX, _WindStemAxisY, _WindStemAxisZ));
            float windStrength = max((stemDistance - _WindHeightPivot), 0.0) * _WindHeightFactor;
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
            float windPeriod = _Time.y * _WindPeriod + dot(worldPos.xz, float2(_WindWorldSpacingX, _WindWorldSpacingY));
            v.vertex.xyz += mul(unity_WorldToObject, float3(windStrength * sin(windPeriod), 0, windStrength * cos(windPeriod)));
        }

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)



        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

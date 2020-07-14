Shader "Urho3D/PBR/PBRWater"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) ="black" {}
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Intensity", Range(0, 1)) = 1
        _WaterMetallic("Water Metallic", Range(0,1)) = 1
        _WaterSmoothness("Water Smoothness", Range(0,1)) = 1
        _Metallic("Metallic", Range(0,1)) = 1
        _Smoothness("Smoothness", Range(0,1)) = 1
        _FlowSpeed("Flow speed", Range(0,1)) = 0.2
        [PowerSlider(4)] _TimeScale("Time scale", Range(0.1,8)) = 1
        [PowerSlider(4)] _FresnelPower("Fresnel Exponent", Range(0.25, 8)) = 4
    }
        SubShader
        {
            Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
            LOD 200

            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            // Physically based Standard lighting model, and enable shadows on all light types
            #pragma surface surf Standard alpha:blend
                //fullforwardshadows

            // Use shader model 3.0 target, to get nicer looking lighting
            #pragma target 3.0

            sampler2D _MainTex;
            sampler2D _BumpMap;

            struct Input
            {
                float2 uv_MainTex;
                float3 worldNormal;
                float3 viewDir;
                half4 color : COLOR;
                INTERNAL_DATA
            };

            fixed4 _Color;
            half _BumpScale;
            half _FresnelPower;
            half _Metallic;
            half _Smoothness;
            half _WaterMetallic;
            half _WaterSmoothness;
            half _FlowSpeed;
            half _TimeScale;

            // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
            // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
            // #pragma instancing_options assumeuniformscaling
            UNITY_INSTANCING_BUFFER_START(Props)
                // put more per-instance properties here
            UNITY_INSTANCING_BUFFER_END(Props)

            void surf(Input IN, inout SurfaceOutputStandard o)
            {
                float2 flowDir = (IN.color.xy - float2(0.5, 0.5)) * _FlowSpeed;
                float2 baseUV = IN.uv_MainTex;
                float t = _Time.y * _TimeScale - floor(_Time.y * _TimeScale);
                float2 uv0 = baseUV + (flowDir * t);
                float2 uv1 = baseUV + (flowDir * (t - 1));

                //float fresnelBase = saturate(1.0 - dot(IN.worldNormal, IN.viewDir));
                //float fresnel = pow(fresnelBase, _FresnelPower);
                float fresnel = 0;

                //float normalScale = length(flowDir) * 2.0;
                half3 n0 = UnpackScaleNormal(tex2D(_BumpMap, uv0), _BumpScale);
                half3 n1 = UnpackScaleNormal(tex2D(_BumpMap, uv1), _BumpScale);
                half3 n = normalize((n0 + (n1 - n0) * t));// *half3(normalScale, normalScale, 1.0));
                o.Normal = n;

                half4 overlay0 = tex2D(_MainTex, uv0);
                half4 overlay1 = tex2D(_MainTex, uv1);
                half4 overlay = lerp(overlay0, overlay1, t);

                half4 overlayFactor = overlay.a * IN.color.z;

                fixed4 c = lerp(_Color, lerp(_Color, overlay, overlay.a), IN.color.z);
                //fixed4 c = lerp(_Color, overlay, overlay.a);

                o.Albedo = c;// fixed3(fresnelBase, 1, 1);// lerp(c, float3(1, 1, 1), fresnel);// float3(fresnel, fresnel, fresnel);// c.rgb;

                // Metallic and smoothness come from slider variables
                o.Metallic = lerp(_WaterMetallic, _Metallic, overlayFactor);// *saturate(fresnel);
                o.Smoothness = lerp(_WaterSmoothness, _Smoothness, overlayFactor);
                o.Alpha = lerp(max(_Color.a, c.a), 1.0, fresnel) * IN.color.a;
            }
            ENDCG
        }
            FallBack "Diffuse"
}

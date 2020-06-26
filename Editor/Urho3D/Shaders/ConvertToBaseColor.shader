Shader "Hidden/UnityToCustomEngineExporter/Urho3D/ConvertToBaseColor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SpecGlossMap ("Specular", 2D) = "black" {}
        _Smoothness("Smoothness", 2D) = "white" {}
        _SmoothnessScale("Smoothness Factor", Range(0.0, 1.0)) = 1
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

            struct MetallicRoughness
            {
                float3 baseColor;
                float opacity;
                float metallic;
                float roughness;
            };

            struct SpecularGlossiness
            {
                float3 diffuse;
                float opacity;
                float3 specular;
                float glossiness;
            };

            float GetPerceivedBrightness(float3 color)
            {
                return dot(float3(0.299, 0.587, 0.114), color.rgb * color.rgb);
            }

            float SolveMetallic(float diffuse, float specular, float oneMinusSpecularStrength)
            {
                half oneMinusDielectricSpec = 1.0 - 0.22;// unity_ColorSpaceDielectricSpec.a;
                float dielectricSpecular = 1.0 - oneMinusDielectricSpec;
                if (specular < dielectricSpecular) return 0;

                float a = dielectricSpecular;
                float b = diffuse * oneMinusSpecularStrength / (1.0 - dielectricSpecular) + specular - 2.0 * dielectricSpecular;
                float c = dielectricSpecular - specular;
                float D = b * b - 4.0 * a * c;
                return clamp((-b + sqrt(D)) / (2.0 * a), 0.0, 1.0);
            }


            float maxColorComponent(float3 rgb)
            {
                return max(max(rgb.r, rgb.g), rgb.b);
            }

            MetallicRoughness ConvertToMetallicRoughness(SpecularGlossiness specularGlossiness)
            {
                half oneMinusDielectricSpec = 1.0-0.22;// unity_ColorSpaceDielectricSpec.a;
                float dielectricSpecular = 1.0 - oneMinusDielectricSpec;
                float epsilon = 1e-6;
                float3 diffuse = specularGlossiness.diffuse;
                float opacity = specularGlossiness.opacity;
                float3 specular = specularGlossiness.specular;
                float glossiness = specularGlossiness.glossiness;

                float oneMinusSpecularStrength = 1.0 - maxColorComponent(specular);
                float metallic = SolveMetallic(maxColorComponent(diffuse), maxColorComponent(specular), oneMinusSpecularStrength);
                //float metallic = SolveMetallic(GetPerceivedBrightness(diffuse), GetPerceivedBrightness(specular), oneMinusSpecularStrength);

                float3 baseColorFromDiffuse = diffuse * (oneMinusSpecularStrength / (oneMinusDielectricSpec) / max(1.0 - metallic, epsilon));
                float specAdj = dielectricSpecular * (1.0 - metallic);
                float3 baseColorFromSpecular = (specular - float3(specAdj, specAdj, specAdj)) * (1.0 / max(metallic, epsilon));
                float3 baseColor = clamp(lerp(baseColorFromDiffuse, baseColorFromSpecular, metallic * metallic), 0, 1);

                MetallicRoughness result;
                result.baseColor = baseColor;
                result.opacity = opacity;
                result.metallic = metallic;
                result.roughness = 1 - glossiness;
                return result;
            }

            float3 SRGBtoLinear(float3 sRGB)
            {
                return sRGB* (sRGB * (sRGB * 0.305306011 + 0.682171111) + 0.012522878);
            }

            float3 LinearToSRGB(float3 RGB)
            {
                float3 S1 = sqrt(RGB);
                float3 S2 = sqrt(S1);
                float3 S3 = sqrt(S2);
                return 0.662002687 * S1 + 0.684122060 * S2 - 0.323583601 * S3 - 0.0225411470 * RGB;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _SpecGlossMap;
            sampler2D _Smoothness;
            float _SmoothnessScale;

            fixed4 frag (v2f i) : SV_Target
            {
                SpecularGlossiness specularGlossiness;
                float4 diffSample = tex2D(_MainTex, i.uv);
                specularGlossiness.diffuse = diffSample.rgb;
                specularGlossiness.specular = tex2D(_SpecGlossMap, i.uv).rgb;
                specularGlossiness.opacity = diffSample.a;
                specularGlossiness.glossiness = tex2D(_Smoothness, i.uv).a * _SmoothnessScale;
                MetallicRoughness metallicRoughness = ConvertToMetallicRoughness(specularGlossiness);
                return fixed4(metallicRoughness.baseColor.rgb, diffSample.a);
                //return fixed4(specularGlossiness.specular, diffSample.a);
            }
            ENDCG
        }
    }
}


using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public static class PBRUtils
    {
        private const float epsilon = 1e-6f;

        private const float dielectricSpecular = 0.04f;

        /// <summary>
        ///     Linear space dielectric specular value is 0.04f
        /// </summary>
        private static readonly Color dielectricSpecularColor = new Color(dielectricSpecular, dielectricSpecular, dielectricSpecular, 0.0f);

        public static SpecularGlossiness ConvertToSpecularGlossinessSRGB(MetallicRoughness metallicRoughness)
        {
            return ConvertToSpecularGlossiness(metallicRoughness.linear()).gamma();
        }

        public static SpecularGlossiness ConvertToSpecularGlossiness(MetallicRoughness metallicRoughness)
        {
            var baseColor = metallicRoughness.baseColor;
            var opacity = metallicRoughness.opacity;
            var metallic = metallicRoughness.metallic;
            var roughness = metallicRoughness.roughness;
            var specular = Color.Lerp(dielectricSpecularColor, baseColor, metallic);

            var oneMinusSpecularStrength = 1.0f - specular.maxColorComponent;
            var diffuse = oneMinusSpecularStrength < epsilon
                ? Color.black
                : baseColor * ((1 - dielectricSpecular) * (1 - metallic) / oneMinusSpecularStrength);

            var glossiness = 1 - roughness;

            return new SpecularGlossiness
            {
                specular = new Color(specular.r, specular.g, specular.b, 1),
                opacity = opacity,
                diffuse = new Color(diffuse.r, diffuse.g, diffuse.b, metallicRoughness.baseColor.a),
                glossiness = glossiness
            };
        }

        public static float GetPerceivedBrightness(this Color color)
        {
            return Mathf.Sqrt(0.299f * color.r * color.r + 0.587f * color.g * color.g + 0.114f * color.b * color.b);
        }

        public static Color Clamp(this Color color)
        {
            return new Color(Mathf.Clamp(color.r, 0, 1),
                Mathf.Clamp(color.g, 0, 1),
                Mathf.Clamp(color.b, 0, 1),
                Mathf.Clamp(color.a, 0, 1));
        }

        public static MetallicRoughness ConvertToMetallicRoughnessSRGB(SpecularGlossiness specularGlossiness)
        {
            return ConvertToMetallicRoughness(specularGlossiness.linear()).gamma();
        }

        public static MetallicRoughness ConvertToMetallicRoughness(SpecularGlossiness specularGlossiness)
        {
            var diffuse = specularGlossiness.diffuse;
            var opacity = specularGlossiness.opacity;
            var specular = specularGlossiness.specular;
            var glossiness = specularGlossiness.glossiness;

            var oneMinusSpecularStrength = 1 - specular.maxColorComponent;
            //var metallic = SolveMetallic(diffuse.GetPerceivedBrightness(), specular.GetPerceivedBrightness(), oneMinusSpecularStrength);
            var metallic = SolveMetallic(diffuse.maxColorComponent, specular.maxColorComponent, oneMinusSpecularStrength, dielectricSpecular);

            var baseColorFromDiffuse =
                diffuse * (oneMinusSpecularStrength / (1 - dielectricSpecular) / Mathf.Max(1 - metallic, epsilon));
            var baseColorFromSpecular =
                (specular - dielectricSpecularColor * (1 - metallic)) * (1 / Mathf.Max(metallic, epsilon));
            var baseColor = Color.Lerp(baseColorFromDiffuse, baseColorFromSpecular, metallic * metallic).Clamp();

            return new MetallicRoughness
            {
                baseColor = new Color(baseColor.r, baseColor.g, baseColor.b, specularGlossiness.opacity),
                opacity = opacity,
                metallic = metallic,
                roughness = 1 - glossiness
            };
        }

        private static float SolveMetallic(float diffuse, float specular, float oneMinusSpecularStrength,
            float dielectricSpecular)
        {
            if (specular < dielectricSpecular) return 0;

            var a = dielectricSpecular;
            var b = diffuse * oneMinusSpecularStrength / (1 - dielectricSpecular) + specular -
                    2 * dielectricSpecular;
            var c = dielectricSpecular - specular;
            var D = b * b - 4 * a * c;
            return Mathf.Clamp((-b + Mathf.Sqrt(D)) / (2 * a), 0, 1);
        }

        public struct MetallicRoughness
        {
            public Color baseColor;
            public float opacity;
            public float metallic;
            public float roughness;

            public override string ToString()
            {
                return string.Format("BaseColor {0}, Opacity {1}, Metallic {2}, Roughness {3}", baseColor, opacity,
                    metallic, roughness);
            }

            public MetallicRoughness linear()
            {
                return new MetallicRoughness()
                {
                    baseColor = baseColor.linear,
                    opacity = opacity,
                    metallic = new Color(metallic,0,0).linear.r,
                    roughness = roughness
                };
            }
            public MetallicRoughness gamma()
            {
                return new MetallicRoughness()
                {
                    baseColor = baseColor.gamma,
                    opacity = opacity,
                    metallic = new Color(metallic, 0, 0).gamma.r,
                    roughness = roughness
                };
            }
        }

        public struct SpecularGlossiness
        {
            public Color specular;
            public float opacity;
            public Color diffuse;
            public float glossiness;

            public override string ToString()
            {
                return string.Format("Diffuse {0}, Opacity {1}, Specular {2}, Glossiness {3}", diffuse, opacity,
                    specular, glossiness);
            }

            public SpecularGlossiness linear()
            {
                return new SpecularGlossiness()
                {
                    specular = specular.linear,
                    opacity = opacity,
                    diffuse = diffuse.linear,
                    glossiness = glossiness
                };
            }
            public SpecularGlossiness gamma()
            {
                return new SpecularGlossiness()
                {
                    specular = specular.gamma,
                    opacity = opacity,
                    diffuse = diffuse.gamma,
                    glossiness = glossiness
                };
            }
        }
    }
}
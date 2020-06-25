using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public static class PBRUtils
    {
        private const float epsilon = 1e-6f;

        private static readonly Color dielectricSpecular = new Color(0.04f, 0.04f, 0.04f, 0.0f);

        public static SpecularGlossiness ConvertToSpecularGlossiness(MetallicRoughness metallicRoughness)
        {
            var baseColor = metallicRoughness.baseColor;
            var opacity = metallicRoughness.opacity;
            var metallic = metallicRoughness.metallic;
            var roughness = metallicRoughness.roughness;

            var specular = Color.Lerp(dielectricSpecular, baseColor, metallic);

            var oneMinusSpecularStrength = 1.0f - specular.maxColorComponent;
            var diffuse = oneMinusSpecularStrength < epsilon
                ? Color.black
                : baseColor * ((1 - dielectricSpecular.r) * (1 - metallic) / oneMinusSpecularStrength);

            var glossiness = 1 - roughness;

            return new SpecularGlossiness
            {
                specular = specular,
                opacity = opacity,
                diffuse = diffuse,
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

        public static MetallicRoughness ConvertToMetallicRoughness(SpecularGlossiness specularGlossiness)
        {
            var diffuse = specularGlossiness.diffuse;
            var opacity = specularGlossiness.opacity;
            var specular = specularGlossiness.specular;
            var glossiness = specularGlossiness.glossiness;

            var oneMinusSpecularStrength = 1 - specular.maxColorComponent;
            var metallic = SolveMetallic(diffuse.GetPerceivedBrightness(), specular.GetPerceivedBrightness(),
                oneMinusSpecularStrength);

            var baseColorFromDiffuse =
                diffuse * (oneMinusSpecularStrength / (1 - dielectricSpecular.r) / Mathf.Max(1 - metallic, epsilon));
            var baseColorFromSpecular =
                (specular - dielectricSpecular * (1 - metallic)) * (1 / Mathf.Max(metallic, epsilon));
            var baseColor = Color.Lerp(baseColorFromDiffuse, baseColorFromSpecular, metallic * metallic).Clamp();

            return new MetallicRoughness
            {
                baseColor = baseColor,
                opacity = opacity,
                metallic = metallic,
                roughness = 1 - glossiness
            };
        }

        private static float SolveMetallic(float diffuse, float specular, float oneMinusSpecularStrength)
        {
            if (specular < dielectricSpecular.r) return 0;

            var a = dielectricSpecular.r;
            var b = diffuse * oneMinusSpecularStrength / (1 - dielectricSpecular.r) + specular -
                    2 * dielectricSpecular.r;
            var c = dielectricSpecular.r - specular;
            var D = b * b - 4 * a * c;
            return Mathf.Clamp((-b + Mathf.Sqrt(D)) / (2 * a), 0, 1);
        }

        public struct MetallicRoughness
        {
            public Color baseColor;
            public float opacity;
            public float metallic;
            public float roughness;
        }

        public struct SpecularGlossiness
        {
            public Color specular { get; set; }
            public float opacity { get; set; }
            public Color diffuse { get; set; }
            public float glossiness { get; set; }
        }
    }
}
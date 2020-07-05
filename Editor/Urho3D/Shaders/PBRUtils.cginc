// Start of PBRUtils ---------------------------------------------------------------
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
    half oneMinusDielectricSpec = unity_ColorSpaceDielectricSpec.a;
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
    half oneMinusDielectricSpec = unity_ColorSpaceDielectricSpec.a;
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
// End of PBRUtils ---------------------------------------------------------------

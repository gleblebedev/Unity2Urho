using System.Collections.Generic;
using System.Linq;
using Assets.Urho3DExporter.Scripts.Editor;
using UnityEditor;
using UnityEngine;

namespace Urho3DExporter
{
    public class TextureMetadataCollection
    {
        public Dictionary<Texture, TextureMetadata> _textures = new Dictionary<Texture, TextureMetadata>();

        public TextureMetadataCollection()
        {

        }
        public IEnumerable<ProgressBarReport> Populate(DestinationFolder urhoDataFolder)
        {
            var allMaterials = AssetDatabase.FindAssets("").Select(_ => AssetContext.Create(_, urhoDataFolder))
                .Where(_ => _.Type == typeof(Material));
            foreach (var asset in allMaterials)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(asset.AssetPath);
                yield return new ProgressBarReport("Load "+ asset.AssetPath);
                var description = new MaterialDescription(material);
                if (description.MetallicRoughness != null)
                {
                    var metallicRoughness = description.MetallicRoughness;
                    if (metallicRoughness.MetallicGloss != null)
                    {
                        var meta = AddTexture(metallicRoughness.MetallicGloss, new PBRMetallicGlossinessTextureReference(
                            metallicRoughness.GlossinessTextureScale,
                            metallicRoughness.Smoothness));
                    }

                    AddTexture(metallicRoughness.BaseColor, new PBRBaseColorTextureReference(TryGetOpacityMask(metallicRoughness.BaseColor)));
                    AddTexture(metallicRoughness.DetailBaseColor, new TextureReference(TextureSemantic.MainTextureDetail));
                }
                else if (description.SpecularGlossiness != null)
                {
                    var specularGlossiness = description.SpecularGlossiness;
                    var smoothness = specularGlossiness.SmoothnessTextureChannel ==
                                     SmoothnessTextureChannel.MetallicOrSpecularAlpha
                        ? specularGlossiness.PBRSpecular
                        : specularGlossiness.Diffuse;
                    if (specularGlossiness.PBRSpecular != null)
                    {
                        AddTexture(specularGlossiness.PBRSpecular, new PBRSpecularGlossinessTextureReference(specularGlossiness.GlossinessTextureScale, smoothness, specularGlossiness.PBRSpecular));
                        AddTexture(specularGlossiness.Diffuse, new PBRDiffuseTextureReference(specularGlossiness.PBRSpecular, smoothness, specularGlossiness.GlossinessTextureScale, TryGetOpacityMask(specularGlossiness.Diffuse)));
                    }
                    else
                    {
                        AddTexture(specularGlossiness.Diffuse, new PBRDiffuseTextureReference(specularGlossiness.PBRSpecular, smoothness, specularGlossiness.GlossinessTextureScale, TryGetOpacityMask(specularGlossiness.Diffuse)));
                    }

                    AddTexture(specularGlossiness.DetailDiffuse,
                        new TextureReference(TextureSemantic.MainTextureDetail));
                }
                else if (description.Skybox != null)
                {
                    var legacy = description.Skybox;
                    AddTexture(legacy.Skybox, new TextureReference(TextureSemantic.MainTexture));
                    AddCommonTextures(legacy);
                }
                else
                {
                    var legacy = description.Legacy;
                    AddTexture(legacy.Diffuse, new TextureReference(TextureSemantic.MainTexture));
                    AddTexture(legacy.Specular, new TextureReference(TextureSemantic.Specular));
                    AddCommonTextures(legacy);
                }
            }
        }

        private Texture TryGetOpacityMask(Texture metallicRoughnessBaseColor)
        {
            return null;
        }

        public IEnumerable<TextureReference> ResolveReferences(Texture texture)
        {
            if (_textures.TryGetValue(texture, out var references))
                foreach (var reference in references.References)
                    yield return reference;
            else
                yield return new TextureReference(TextureSemantic.Other);
        }

        private void AddCommonTextures(ShaderArguments legacy)
        {
            AddTexture(legacy.Occlusion, new TextureReference(TextureSemantic.Occlusion));
            AddTexture(legacy.Bump, new TextureScaleReference(TextureSemantic.Bump, legacy.BumpScale));
            AddTexture(legacy.Detail, new TextureReference(TextureSemantic.Detail));
            AddTexture(legacy.DetailNormal, new TextureReference(TextureSemantic.DetailNormal));
            AddTexture(legacy.Emission, new TextureReference(TextureSemantic.Emission));
            AddTexture(legacy.Parallax, new TextureReference(TextureSemantic.Parallax));
        }

        private TextureMetadata EnsureTexture(Texture tex)
        {
            if (tex == null)
                return null;
            if (_textures.TryGetValue(tex, out var meta))
                return meta;
            meta = new TextureMetadata {Texture = tex};
            _textures.Add(tex, meta);
            return meta;
        }

        private bool AddTexture(Texture tex, TextureReference reference)
        {
            var meta = EnsureTexture(tex);
            if (meta == null)
                return false;
            return meta.References.Add(reference);
        }
    }
}
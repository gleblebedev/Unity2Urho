
using UnityEngine;

namespace Assets.Urho3DExporter.Scripts.Editor
{
    public struct TextureReferences
    {
        public TextureSemantic Semantic;
        public float Scale;
        public Texture SmoothnessSource;
        public SmoothnessTextureChannel SmoothnessTextureChannel;

        public TextureReferences(TextureSemantic semantic)
        {
            Semantic = semantic;
            Scale = 1.0f;
            SmoothnessSource = null;
            SmoothnessTextureChannel = (SmoothnessTextureChannel)0;
        }
        public TextureReferences(TextureSemantic semantic, float scale)
        {
            Semantic = semantic;
            Scale = scale;
            SmoothnessSource = null;
            SmoothnessTextureChannel = (SmoothnessTextureChannel)0;
        }
        public TextureReferences(TextureSemantic semantic, float scale, Texture source, SmoothnessTextureChannel smoothnessTextureChannel)
        {
            Semantic = semantic;
            Scale = scale;
            SmoothnessSource = source;
            SmoothnessTextureChannel = smoothnessTextureChannel;
        }
    }
}
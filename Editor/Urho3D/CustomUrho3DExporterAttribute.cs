using System;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class CustomUrho3DExporterAttribute : Attribute
    {
        public CustomUrho3DExporterAttribute(Type contentType)
        {
            ContentType = contentType;
        }

        public Type ContentType { get; }
    }
}
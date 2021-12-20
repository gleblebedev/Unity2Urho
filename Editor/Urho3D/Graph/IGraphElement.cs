using System.Xml;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public interface IGraphElement
    {
        void Write(XmlWriter writer);
    }
}
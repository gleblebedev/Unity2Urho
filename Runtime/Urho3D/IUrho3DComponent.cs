using System.Collections.Generic;

namespace UnityToCustomEngineExporter.Urho3D
{
    public interface IUrho3DComponent
    {
        string GetUrho3DComponentName();

        IEnumerable<Urho3DAttribute> GetUrho3DComponentAttributes();
        bool IsUrho3DComponentEnabled { get; }
    }
}

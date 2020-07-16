using System.IO;
using System.Text;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class AbstractBinaryExpoerter
    {
        protected void Write(BinaryWriter writer, Quaternion v)
        {
            writer.Write(v.w);
            writer.Write(v.x);
            writer.Write(v.y);
            writer.Write(v.z);
        }

        protected void Write(BinaryWriter writer, Vector3 v)
        {
            writer.Write(v.x);
            writer.Write(v.y);
            writer.Write(v.z);
        }

        protected void WriteStringSZ(BinaryWriter writer, string boneName)
        {
            var a = new UTF8Encoding(false).GetBytes(boneName + '\0');
            writer.Write(a);
        }

    }
}
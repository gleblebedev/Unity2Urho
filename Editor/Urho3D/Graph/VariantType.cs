using System;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public enum VariantType
    {
        None,
        Int,
        Bool,
        Float,
        Vector2,
        Vector3,
        Vector4,
        Quaternion,
        Color,
        String,
        Buffer,
        VoidPtr,
        ResourceRef,
        ResourceRefList,
        VariantVector,
        VariantMap,
        IntRect,
        IntVector2,
        Ptr,
        Matrix3,
        Matrix3x4,
        Matrix4,
        Double,
        StringVector,
        Rect,
        IntVector3,
        Int64,
        Custom,
        VariantCurve,
    }
    public static class VariantTypeExtensionMethods
    {
        public static VariantType ToVariantType(this Type type)
        {
            if (type == typeof(bool))
                return VariantType.Bool;
            if (type == typeof(int))
                return VariantType.Int;
            if (type == typeof(float))
                return VariantType.Float;
            if (type == typeof(double))
                return VariantType.Double;
            if (type == typeof(uint))
                return VariantType.Int;
            if (type == typeof(long))
                return VariantType.Int64;
            if (type == typeof(ulong))
                return VariantType.Int64;
            if (type == typeof(Vector2))
                return VariantType.Vector2;
            if (type == typeof(Vector3))
                return VariantType.Vector3;
            if (type == typeof(Vector4))
                return VariantType.Vector4;
            if (type == typeof(Quaternion))
                return VariantType.Quaternion;
            throw new NotImplementedException(type.Name);
        }

    }
}
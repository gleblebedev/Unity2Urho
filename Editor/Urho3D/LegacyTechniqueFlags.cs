namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class LegacyTechniqueFlags
    {
        public bool hasAlpha;
        public bool hasDiffuse;
        public bool hasEmissive;
        public bool hasNormal;
        public bool hasSpecular;

        public static int operator -(LegacyTechniqueFlags a, LegacyTechniqueFlags b)
        {
            return GetDistance(a.hasDiffuse, b.hasDiffuse) + GetDistance(a.hasSpecular, b.hasSpecular) +
                   GetDistance(a.hasNormal, b.hasNormal) + GetDistance(a.hasEmissive, b.hasEmissive) +
                   GetDistance(a.hasAlpha, b.hasAlpha);
        }

        private static int GetDistance(bool a, bool b)
        {
            return a != b ? 1 : 0;
        }

        public bool Fits(LegacyTechniqueFlags b)
        {
            return (!hasDiffuse || b.hasDiffuse)
                   && (!hasSpecular || b.hasSpecular)
                   && (!hasEmissive || b.hasEmissive)
                   && (!hasNormal || b.hasNormal)
                   && hasAlpha == b.hasAlpha;
        }
    }
}
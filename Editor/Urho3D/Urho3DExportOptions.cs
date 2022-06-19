namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class Urho3DExportOptions
    {
        public bool RBFX { get; set; } = true;

        public Urho3DExportOptions(string subfolder,
            bool exportUpdatedOnly,
            bool exportSceneAsPrefab, bool skipDisabled, bool usePhysicalValues)
        {
            Subfolder = (subfolder ?? "").FixAssetSeparator().Trim('/');
            if (!string.IsNullOrWhiteSpace(Subfolder)) Subfolder += "/";
            ExportUpdatedOnly = exportUpdatedOnly;
            UsePhysicalValues = usePhysicalValues;
            SkipDisabled = skipDisabled;
            ExportSceneAsPrefab = exportSceneAsPrefab;
        }

        public bool ExportUpdatedOnly { get; set; }

        public bool ExportSceneAsPrefab { get; set; }

        public string Subfolder { get; }

        public bool UsePhysicalValues { get; }
        public bool SkipDisabled { get; set; }
        public bool ExportCameras { get; set; } = true;
        public bool ExportLights { get; set; } = true;
        public bool ExportTextures { get; set; } = true;
        public bool ExportShadersAndTechniques { get; set; } = true;
        public bool ExportAnimations { get; set; } = true;
        public bool ExportMeshes { get; set; } = true;
        public bool ExportPrefabReferences { get; set; }

        public bool PackedNormal { get; set; } = false;

        /// <summary>
        ///     Replace all non-ASCII characters in file and node names.
        /// </summary>
        public bool ASCIIOnly { get; set; } = false;

        public bool ExportLODs { get; set; } = false;
        public bool ExportParticles { get; set; } = true;
        public bool EliminateRootMotion { get; set; } = true;
    }
}
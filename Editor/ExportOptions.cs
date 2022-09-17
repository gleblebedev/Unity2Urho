using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using UnityEditor;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
# endif
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.SceneManagement;
using UnityToCustomEngineExporter.Editor.Urho3D;

namespace UnityToCustomEngineExporter.Editor
{
    public class ExportOptions : EditorWindow
    {
        private static readonly string _dataPathKey = "UnityToCustomEngineExporter.DataPath";
        private static readonly string _subfolderKey = "UnityToCustomEngineExporter.Subfolder";
        private static readonly string _exportUpdatedOnlyKey = "UnityToCustomEngineExporter.UpdatedOnly";
        private static readonly string _exportSceneAsPrefabKey = "UnityToCustomEngineExporter.SceneAsPrefab";
        private static readonly string _skipDisabledKey = "UnityToCustomEngineExporter.SkipDisabled";
        private static readonly string _usePhysicalValuesKey = "UnityToCustomEngineExporter.UsePhysicalValues";

        private readonly BoolEditorProperty _exportShadersAndTechniques =
            new BoolEditorProperty("UnityToCustomEngineExporter.ExportShadersAndTechniques",
                "Export Shaders and Techniques", false);

        private readonly BoolEditorProperty _exportCameras =
            new BoolEditorProperty("UnityToCustomEngineExporter.ExportCameras", "Export Cameras", true);

        private readonly BoolEditorProperty _exportLights =
            new BoolEditorProperty("UnityToCustomEngineExporter.ExportLights", "Export Lights", true);

        private readonly BoolEditorProperty _exportTextures =
            new BoolEditorProperty("UnityToCustomEngineExporter.ExportTextures", "Export Textures", true);

        private readonly BoolEditorProperty _packedNormal =
            new BoolEditorProperty("UnityToCustomEngineExporter.PackedNormal", "Packed Normals", false);

        private readonly BoolEditorProperty _rbfx =
            new BoolEditorProperty("UnityToCustomEngineExporter.RBFX", "RBFX", false);

        private readonly BoolEditorProperty _exportAnimations =
            new BoolEditorProperty("UnityToCustomEngineExporter.ExportAnimations", "Export Animations", true);

        private readonly BoolEditorProperty _exportMeshes =
            new BoolEditorProperty("UnityToCustomEngineExporter.ExportMeshes", "Export Meshes", true);

        private readonly BoolEditorProperty _exportVertexColor =
            new BoolEditorProperty("UnityToCustomEngineExporter.ExportVertexColor", "Export Vertex Color", true);

        private readonly BoolEditorProperty _exportParticles =
            new BoolEditorProperty("UnityToCustomEngineExporter.ExportParticles", "Export Particles", true);
        
        private readonly BoolEditorProperty _eliminateRootMotion =
            new BoolEditorProperty("UnityToCustomEngineExporter.EliminateRootMotion", "Eliminate Root Motion", true);
        
        private readonly BoolEditorProperty _exportAscii =
            new BoolEditorProperty("UnityToCustomEngineExporter.ExportAscii", "Replace non-ASCII chars", false);

        private readonly BoolEditorProperty _exportLods =
            new BoolEditorProperty("UnityToCustomEngineExporter.ExportLODs", "Merge LOD Group into single model",
                false);

        private readonly BoolEditorProperty _exportPrefabReferences =
            new BoolEditorProperty("UnityToCustomEngineExporter.ExportPrefabReferences", "Export Prefab References", false);

        private readonly BoolEditorProperty _mergeStaticGeometry =
            new BoolEditorProperty("UnityToCustomEngineExporter.MergeStaticGeometry", "Merge static geometry", false);

        private readonly IList<BoolEditorProperty> _extraFlags;
        private string _exportFolder = "";
        private string _subfolder = "";
        private bool _exportUpdatedOnly;
        private bool _exportSceneAsPrefab;
        private bool _skipDisabled;
        private bool _usePhysicalValues;

        private IDestinationEngine _engine;
        private CancellationTokenSource _cancellationTokenSource;
        private GUIStyle _myCustomStyle;
        private bool _showExtraOptions;
        private bool _exporting;

        public ExportOptions()
        {
            _extraFlags = new[]
            {
                _exportShadersAndTechniques,
                _exportCameras,
                _exportLights,
                _exportTextures,
                _packedNormal,
                _exportAnimations,
                _exportMeshes,
                _exportVertexColor,
                _exportParticles,
                _exportAscii,
                _exportLods,
                _exportPrefabReferences,
                _eliminateRootMotion,
                _rbfx
            };
        }

        [MenuItem("Tools/Export To Custom Engine/Export Assets or Scene")]
        [MenuItem("Assets/Export To Custom Engine")]
        public static void Init()
        {
            var window = GetWindow<ExportOptions>("Export Assets or Scene");
            window.Show();
        }


        private static bool ValidateExportPath(string exportFolder)
        {
            var normalizedFolder = exportFolder.FixDirectorySeparator();
            var dataPath = Application.dataPath.FixDirectorySeparator();
            return !string.IsNullOrWhiteSpace(exportFolder) &&
                   !normalizedFolder.StartsWith(dataPath, StringComparison.InvariantCultureIgnoreCase);
        }

        public void OnGUI()
        {
            _myCustomStyle = _myCustomStyle ?? new GUIStyle(GUI.skin.GetStyle("label"))
            {
                wordWrap = true
            };

            // if (string.IsNullOrWhiteSpace(_exportFolder)) {
            //    PickFolder();
            // }

            _exportFolder = EditorGUILayout.TextField("Export Folder", _exportFolder);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Pick")) PickFolder();
                if (GUILayout.Button("Open")) EditorUtility.RevealInFinder(_exportFolder);
            }

            _subfolder = EditorGUILayout.TextField("Subfolder", _subfolder);

            _exportUpdatedOnly = EditorGUILayout.Toggle("Export only updated resources", _exportUpdatedOnly);

            _exportSceneAsPrefab = EditorGUILayout.Toggle("Export scene as prefab", _exportSceneAsPrefab);

            _usePhysicalValues = EditorGUILayout.Toggle("Use physical values", _usePhysicalValues);

            _showExtraOptions = EditorGUILayout.Foldout(_showExtraOptions, "Content filter");

            if (_showExtraOptions)
            {
                _skipDisabled = EditorGUILayout.Toggle("Skip disabled entities", _skipDisabled);
                foreach (var flag in _extraFlags) flag.Toggle();
            }

            //GUILayout.Label(EditorTaskScheduler.Default.CurrentReport.Message);

            {
                GUI.enabled = _engine == null && ValidateExportPath(_exportFolder);
                if (Selection.assetGUIDs.Length > 0)
                {
                    if (GUILayout.Button("Export selected (" + Selection.assetGUIDs.Length + ") assets"))
                        StartExportAssets(Selection.assetGUIDs, null);
                }
                else
                {
                    if (GUILayout.Button("Export all assets")) StartExportAssets(AssetDatabase.FindAssets(""), null);
                }

                if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                {
                    if (GUILayout.Button("Export active prefab")) StartExportPrefab();
                }
                else
                {
                    if (GUILayout.Button("Export active scene")) StartExportScene();
                }

                GUI.enabled = true;
            }
            {
                if (EditorTaskScheduler.Default.IsRunning)
                {
                    if (_cancellationTokenSource != null)
                        if (GUILayout.Button("Cancel"))
                            _cancellationTokenSource.Cancel();
                }
                else
                {
                    if (_engine != null)
                    {
                        _engine.Dispose();
                        _engine = null;
                        if (!_cancellationTokenSource.IsCancellationRequested)
                            _cancellationTokenSource.Cancel();
                        _cancellationTokenSource = null;
                    }
                }
            }


            EditorGUILayout.Separator();
            EditorGUILayout.LabelField(
                "Before exporting content with the tool please check that you have an appropriate license for the assets you are exporting. You can find Asset Store Terms of Service and EULA at https://unity3d.com/legal/as_terms",
                _myCustomStyle);
        }

        protected void Update()
        {
            if (_exporting) _exporting = EditorTaskScheduler.Default.DisplayProgressBar();
        }
        private void StartExportPrefab()
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                _exporting = true;
                _engine = CreateEngine();
                
                EditorTaskScheduler.Default.ScheduleForegroundTask(() => { _engine.ExportPrefab(prefabStage); },
                    prefabStage.assetPath);
            }
        }
        private void StartExportScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene != null)
            {
                _exporting = true;
                _engine = CreateEngine();
                EditorTaskScheduler.Default.ScheduleForegroundTask(() => { _engine.ExportScene(activeScene); },
                    activeScene.path);
            }
        }

        private void StartExportAssets(string[] assetGuiDs, PrefabContext prefabContext)
        {
            _exporting = true;
            _engine = CreateEngine();
            EditorTaskScheduler.Default.ScheduleForegroundTask(() => _engine.ExportAssets(assetGuiDs, prefabContext));
        }

        private IDestinationEngine CreateEngine()
        {
            if (_engine != null) return null;
            _cancellationTokenSource = new CancellationTokenSource();
            var options = new Urho3DExportOptions(_subfolder, _exportUpdatedOnly,
                _exportSceneAsPrefab, _skipDisabled, _usePhysicalValues);
            options.ExportShadersAndTechniques = _exportShadersAndTechniques.Value;
            options.ExportCameras = _exportCameras.Value;
            options.ExportLights = _exportLights.Value;
            options.ExportTextures = _exportTextures.Value;
            options.PackedNormal = _packedNormal.Value;
            options.RBFX = _rbfx.Value;
            options.ExportAnimations = _exportAnimations.Value;
            options.ExportMeshes = _exportMeshes.Value;
            options.ExportVertexColor = _exportVertexColor.Value;
            options.ExportPrefabReferences = _exportPrefabReferences.Value;
            options.ASCIIOnly = _exportAscii.Value;
            options.ExportLODs = _exportLods.Value;
            options.ExportParticles = _exportParticles.Value;
            options.EliminateRootMotion = _eliminateRootMotion.Value;
            return new Urho3DEngine(_exportFolder, _cancellationTokenSource.Token, options);
        }

        private void PickFolder()
        {
            retry:
            var exportFolder = EditorUtility.SaveFolderPanel("Save assets to Data folder", _exportFolder, "");
            if (string.IsNullOrWhiteSpace(exportFolder))
            {
                if (string.IsNullOrWhiteSpace(_exportFolder))
                    Close();
            }
            else
            {
                if (!ValidateExportPath(exportFolder))
                {
                    EditorUtility.DisplayDialog("Error",
                        "Selected path is inside Unity folder. Please select a different folder.", "Ok");
                    goto retry;
                }

                _exportFolder = exportFolder;
                SaveConfig();
            }
        }

        private void OnFocus()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (EditorPrefs.HasKey(_dataPathKey))
                _exportFolder = EditorPrefs.GetString(_dataPathKey);
            else
                _exportFolder = Application.dataPath;
            if (EditorPrefs.HasKey(_subfolderKey))
                _subfolder = EditorPrefs.GetString(_subfolderKey);
            if (EditorPrefs.HasKey(_exportUpdatedOnlyKey))
                _exportUpdatedOnly = EditorPrefs.GetBool(_exportUpdatedOnlyKey);
            if (EditorPrefs.HasKey(_exportSceneAsPrefabKey))
                _exportSceneAsPrefab = EditorPrefs.GetBool(_exportSceneAsPrefabKey);
            if (EditorPrefs.HasKey(_skipDisabledKey))
                _skipDisabled = EditorPrefs.GetBool(_skipDisabledKey);
            if (EditorPrefs.HasKey(_usePhysicalValuesKey))
                _usePhysicalValues = EditorPrefs.GetBool(_usePhysicalValuesKey);

            foreach (var flag in _extraFlags) flag.Load();
        }

        private void OnLostFocus()
        {
            SaveConfig();
        }

        private void SaveConfig()
        {
            EditorPrefs.SetString(_dataPathKey, _exportFolder);
            EditorPrefs.SetString(_subfolderKey, _subfolder);
            EditorPrefs.SetBool(_exportUpdatedOnlyKey, _exportUpdatedOnly);
            EditorPrefs.SetBool(_exportSceneAsPrefabKey, _exportSceneAsPrefab);
            EditorPrefs.SetBool(_skipDisabledKey, _skipDisabled);
            EditorPrefs.SetBool(_usePhysicalValuesKey, _usePhysicalValues);
            foreach (var flag in _extraFlags) flag.Save();
        }

        private void OnDestroy()
        {
            SaveConfig();
        }
    }
}
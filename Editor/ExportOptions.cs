using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
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
        private BoolEditorProperty _exportShadersAndTechniques = new BoolEditorProperty("UnityToCustomEngineExporter.ExportShadersAndTechniques", "Export Shaders and Techniques", true);
        private BoolEditorProperty _exportCameras = new BoolEditorProperty("UnityToCustomEngineExporter.ExportCameras", "Export Cameras", true);
        private BoolEditorProperty _exportLights = new BoolEditorProperty("UnityToCustomEngineExporter.ExportLights", "Export Lights", true);
        private BoolEditorProperty _exportTextures = new BoolEditorProperty("UnityToCustomEngineExporter.ExportTextures", "Export Textures", true);
        private BoolEditorProperty _exportAnimations = new BoolEditorProperty("UnityToCustomEngineExporter.ExportAnimations", "Export Animations", true);
        private BoolEditorProperty _exportMeshes = new BoolEditorProperty("UnityToCustomEngineExporter.ExportMeshes", "Export Meshes", true);
        private BoolEditorProperty _exportAscii = new BoolEditorProperty("UnityToCustomEngineExporter.ExportAscii", "Replace non-ASCII chars", false);
        private IList<BoolEditorProperty> _extraFlags;
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

        public ExportOptions()
        {
            _extraFlags = new[]
            {
                _exportShadersAndTechniques,
                _exportCameras,
                _exportLights,
                _exportTextures,
                _exportAnimations,
                _exportMeshes,
                _exportAscii,
            };
        }

        [MenuItem("Assets/Export/Export Assets and Scene")]
        public static void Init()
        {
            var window = (ExportOptions) GetWindow(typeof(ExportOptions));
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
            if (GUILayout.Button("Pick")) PickFolder();

            _subfolder = EditorGUILayout.TextField("Subfolder", _subfolder);

            _exportUpdatedOnly = EditorGUILayout.Toggle("Export only updated resources", _exportUpdatedOnly);

            _exportSceneAsPrefab = EditorGUILayout.Toggle("Export scene as prefab", _exportSceneAsPrefab);

            _usePhysicalValues = EditorGUILayout.Toggle("Use physical values", _usePhysicalValues);

            _showExtraOptions = EditorGUILayout.Foldout(_showExtraOptions, "Content filter");

            if (_showExtraOptions)
            {
                _skipDisabled = EditorGUILayout.Toggle("Skip disabled entities", _skipDisabled);
                foreach (var flag in _extraFlags)
                {
                    flag.Toggle();
                }
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

                if (GUILayout.Button("Export active scene ")) StartExportScene();

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
            EditorGUILayout.LabelField("Before exporting content with the tool please check that you have an appropriate license for the assets you are exporting. You can find Asset Store Terms of Service and EULA at https://unity3d.com/legal/as_terms", _myCustomStyle);

        }

        private void StartExportScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene != null)
            {
                _engine = CreateEngine();
                EditorTaskScheduler.Default.ScheduleForegroundTask(() => { _engine.ExportScene(activeScene); },
                    activeScene.path);
            }
        }

        protected void Update()
        {
            EditorTaskScheduler.Default.DisplayProgressBar();
        }

        private void StartExportAssets(string[] assetGuiDs, PrefabContext prefabContext)
        {
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
            options.ExportAnimations = _exportAnimations.Value;
            options.ExportMeshes = _exportMeshes.Value;
            options.ASCIIOnly = _exportAscii.Value;
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

            foreach (var flag in _extraFlags)
            {
                flag.Load();
            }
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
            foreach (var flag in _extraFlags)
            {
                flag.Save();
            }
        }

        private void OnDestroy()
        {
            SaveConfig();
        }
    }
}
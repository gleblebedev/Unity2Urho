using System;
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
        private static readonly string _exportCamerasKey = "UnityToCustomEngineExporter.ExportCameras";
        private static readonly string _exportLightsKey = "UnityToCustomEngineExporter.ExportLights";
        private static readonly string _exportTexturesKey = "UnityToCustomEngineExporter.ExportTextures";
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
        private bool _exportCameras = true;
        private bool _exportLights = true;
        private bool _exportTextures = true;

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

                _exportCameras = EditorGUILayout.Toggle("Export cameras", _exportCameras);

                _exportLights = EditorGUILayout.Toggle("Export lights", _exportLights);

                _exportTextures = EditorGUILayout.Toggle("Export textures", _exportTextures);
            }

            //GUILayout.Label(EditorTaskScheduler.Default.CurrentReport.Message);

            EditorTaskScheduler.Default.DisplayProgressBar();

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
            EditorGUILayout.LabelField("Before exporting the tool please check that you have an appropriate license for the assets you are exporting. You can find Asset Store Terms of Service and EULA at https://unity3d.com/legal/as_terms", _myCustomStyle);

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
            options.ExportCameras = _exportCameras;
            options.ExportLights = _exportLights;
            options.ExportTextures = _exportTextures;
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

            if (EditorPrefs.HasKey(_exportCamerasKey))
                _exportCameras = EditorPrefs.GetBool(_exportCamerasKey);
            if (EditorPrefs.HasKey(_exportLightsKey))
                _exportLights = EditorPrefs.GetBool(_exportLightsKey);
            if (EditorPrefs.HasKey(_exportTexturesKey))
                _exportTextures = EditorPrefs.GetBool(_exportTexturesKey);
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
            EditorPrefs.SetBool(_exportCamerasKey, _exportCameras);
            EditorPrefs.SetBool(_exportLightsKey, _exportLights);
            EditorPrefs.SetBool(_exportTexturesKey, _exportTextures);
        }

        private void OnDestroy()
        {
            SaveConfig();
        }
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Xsl;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityToCustomEngineExporter.Editor.Urho3D;

namespace UnityToCustomEngineExporter.Editor
{
    public class ExportOptions : EditorWindow
    {
        private static readonly string _dataPathKey = "UnityToCustomEngineExporter.DataPath";
        private static readonly string _exportUpdatedOnlyKey = "UnityToCustomEngineExporter.UpdatedOnly";
        private static readonly string _exportSceneAsPrefabKey = "UnityToCustomEngineExporter.SceneAsPrefab";
        private static readonly string _skipDisabledKey = "UnityToCustomEngineExporter.SkipDisabled";
        private string _exportFolder = "";
        private bool _exportUpdatedOnly;
        private bool _exportSceneAsPrefab;
        private bool _skipDisabled;
        private Urho3DEngine _destinationEngine;

        private IDestinationEngine _engine;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _exportTask;

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
            // if (string.IsNullOrWhiteSpace(_exportFolder)) {
            //    PickFolder();
            // }

            _exportFolder = EditorGUILayout.TextField("Export Folder", _exportFolder);
            if (GUILayout.Button("Pick")) PickFolder();
            _exportUpdatedOnly = EditorGUILayout.Toggle("Export only updated resources", _exportUpdatedOnly);

            _exportSceneAsPrefab = EditorGUILayout.Toggle("Export scene as prefab", _exportSceneAsPrefab);

            _skipDisabled = EditorGUILayout.Toggle("Skip disabled entities", _skipDisabled);

            //GUILayout.Label(EditorTaskScheduler.Default.CurrentReport.Message);

            EditorTaskScheduler.Default.DisplayProgressBar();

            {
                GUI.enabled = _engine == null && ValidateExportPath(_exportFolder);
                if (Selection.assetGUIDs.Length > 0)
                {
                    if (GUILayout.Button("Export selected (" + Selection.assetGUIDs.Length + ") assets"))
                        StartExportAssets(Selection.assetGUIDs);
                }
                else
                {
                    if (GUILayout.Button("Export all assets")) StartExportAssets(AssetDatabase.FindAssets(""));
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
            //EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.EndHorizontal();
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

        private void StartExportAssets(string[] assetGuiDs)
        {
            _engine = CreateEngine();
            EditorTaskScheduler.Default.ScheduleForegroundTask(() => _engine.ExportAssets(assetGuiDs));
        }

        private IDestinationEngine CreateEngine()
        {
            if (_engine != null) return null;
            _cancellationTokenSource = new CancellationTokenSource();
            return new Urho3DEngine(_exportFolder, _cancellationTokenSource.Token, _exportUpdatedOnly,
                _exportSceneAsPrefab, _skipDisabled);
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
            if (EditorPrefs.HasKey(_exportUpdatedOnlyKey))
                _exportUpdatedOnly = EditorPrefs.GetBool(_exportUpdatedOnlyKey);
            if (EditorPrefs.HasKey(_exportSceneAsPrefabKey))
                _exportSceneAsPrefab = EditorPrefs.GetBool(_exportSceneAsPrefabKey);
            if (EditorPrefs.HasKey(_skipDisabledKey))
                _skipDisabled = EditorPrefs.GetBool(_skipDisabledKey);
        }

        private void OnLostFocus()
        {
            SaveConfig();
        }

        private void SaveConfig()
        {
            EditorPrefs.SetString(_dataPathKey, _exportFolder);
            EditorPrefs.SetBool(_exportUpdatedOnlyKey, _exportUpdatedOnly);
            EditorPrefs.SetBool(_exportSceneAsPrefabKey, _exportSceneAsPrefab);
            EditorPrefs.SetBool(_skipDisabledKey, _skipDisabled);
        }

        private void OnDestroy()
        {
            SaveConfig();
        }
    }
}
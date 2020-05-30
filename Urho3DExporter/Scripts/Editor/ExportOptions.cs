using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Urho3DExporter
{
    public class ExportOptions: EditorWindow
    {
        private static readonly string _dataPathKey = "Urho3DExporter.DataPath";
        private static readonly string _overrideKey = "Urho3DExporter.Override";
        string _exportFolder = "";
        private bool _override = true;

        [MenuItem("Assets/Export Assets and Scene To Urho3D")]
        static void Init()
        {
            ExportOptions window = (ExportOptions)EditorWindow.GetWindow(typeof(ExportOptions));
            window.Show();
        }

        void OnGUI()
        {
            if (string.IsNullOrWhiteSpace(_exportFolder))
            {
                PickFolder();
            }
            _exportFolder = EditorGUILayout.TextField("Export Folder", _exportFolder);
            if (GUILayout.Button("Pick"))
            {
                PickFolder();
            }
            _override = EditorGUILayout.Toggle("Override existing files", _override);

            if (!string.IsNullOrWhiteSpace(_exportFolder))
            {
                if (GUILayout.Button("Export"))
                {
                    ExportAssets.ExportToUrho(_exportFolder, _override);
                    this.Close();
                }
            }

            //EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.EndHorizontal();
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
                if (exportFolder.StartsWith(Path.GetDirectoryName(Application.dataPath).FixDirectorySeparator(), StringComparison.InvariantCultureIgnoreCase))
                {
                    EditorUtility.DisplayDialog("Error", "Selected path is inside Unity folder. Please select a different folder.", "Ok");
                    goto retry;
                }

                _exportFolder = exportFolder;
                SaveConfig();
            }
        }

        void OnFocus()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (EditorPrefs.HasKey(_dataPathKey))
                _exportFolder = EditorPrefs.GetString(_dataPathKey);
            if (EditorPrefs.HasKey(_overrideKey))
                _override = EditorPrefs.GetBool(_overrideKey);
        }

        void OnLostFocus()
        {
            SaveConfig();
        }

        private void SaveConfig()
        {
            EditorPrefs.SetString(_dataPathKey, _exportFolder);
        }

        void OnDestroy()
        {
            SaveConfig();
        }
    }
}
using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class FindNameCollisions: EditorWindow
    {
        //[MenuItem("Assets/Export/Find and fix Urho3D asset name collisions")]
        public static void FindAndFixNameCollisions()
        {
            var window = (FixTexureImportOptions)GetWindow(typeof(FixTexureImportOptions));
            window.Show();
        }

        public void OnGUI()
        {
            GUILayout.Label("Under construction...");
        }
    }
}
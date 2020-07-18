using UnityEditor;

namespace UnityToCustomEngineExporter.Editor
{
    public class BoolEditorProperty
    {
        private readonly string _name;
        private readonly string _key;

        public BoolEditorProperty(string key, string name, bool value)
        {
            _key = key;
            _name = name;
            Value = value;
        }

        public bool Value { get; set; }

        public void Toggle()
        {
            Value = EditorGUILayout.Toggle(_name, Value);
        }

        public void Load()
        {
            if (EditorPrefs.HasKey(_key))
                Value = EditorPrefs.GetBool(_key);
        }

        public void Save()
        {
            EditorPrefs.SetBool(_key, Value);
        }
    }
}
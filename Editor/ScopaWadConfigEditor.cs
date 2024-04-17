#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Scopa.Editor {

    [CustomPropertyDrawer(typeof(ScopaWadConfig))]
    public class ScopaWadConfigDrawer : PropertyDrawer
    {
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            // Draw label
            // position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // Don't make child fields be indented
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            SerializedProperty prop = property.FindPropertyRelative("linearColorspace");
            do {
                EditorGUILayout.PropertyField(prop, true);
            }
            while (prop.NextVisible(false));

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }
}
#endif
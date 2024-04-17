#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Scopa;

// we need this or else Unity complains when we use GUILayout in the ScriptableObject property drawer
// https://answers.unity.com/questions/1667834/how-do-i-prevent-argument-exception-getting-contro.html
namespace Scopa.Editor {
    [CustomEditor(typeof(ScopaMapConfigAsset), true)]
    public class ScopaMapConfigAssetEditor : UnityEditor.Editor { }
}
#endif
using UnityEngine;
using UnityEditor;
using Elfenlabs.Scripting;

[CustomEditor(typeof(ScriptTester))]
public class ScriptTesterEditor : Editor
{
    public string Script;
    
    public override void OnInspectorGUI()
    {
        Script = EditorGUILayout.TextArea(Script);
        if (GUILayout.Button("Run Script"))
        {
            CompilerUtility.Debug(Script);
        }
    }
}
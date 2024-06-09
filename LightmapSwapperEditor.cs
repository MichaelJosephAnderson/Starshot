using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(LightmapSwapper), true)]
public class LightmapSwapperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        LightmapSwapper targetScript = (LightmapSwapper)target;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Room Dark"))
        {
            targetScript.SetRoomDark();
              
            EditorUtility.SetDirty(targetScript);
            EditorSceneManager.MarkSceneDirty(targetScript.gameObject.scene);
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Room Bright"))
        {
            targetScript.SetRoomBright();
              
            EditorUtility.SetDirty(targetScript);
            EditorSceneManager.MarkSceneDirty(targetScript.gameObject.scene);
        }
        GUILayout.EndHorizontal();
        
        base.OnInspectorGUI();
    }
}
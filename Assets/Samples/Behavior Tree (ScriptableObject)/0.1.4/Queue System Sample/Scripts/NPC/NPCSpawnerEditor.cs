using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NPCSpawner))]
[CanEditMultipleObjects] // Enable multi-object editing
public class NPCSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Spawn NPC"))
            {
                // Run for all selected spawners
                foreach (var t in targets)
                {
                    var spawner = t as NPCSpawner;
                    if (spawner != null)
                        spawner.Spawn();
                }
            }
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("This button is only active in Play Mode.", MessageType.Info);
        }
    }
}

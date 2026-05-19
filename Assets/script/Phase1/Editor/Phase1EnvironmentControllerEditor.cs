using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Phase1EnvironmentController))]
public sealed class Phase1EnvironmentControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);

        Phase1EnvironmentController controller = (Phase1EnvironmentController)target;
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Play Phase 2 Transition"))
                controller.PlayPhase2Transition();

            if (GUILayout.Button("Reset Phase 1 Sky"))
                controller.ResetPhase1Sky();
        }

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Transition buttons are available in Play Mode.", MessageType.Info);
    }
}

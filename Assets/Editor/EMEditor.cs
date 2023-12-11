using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(SimulateEM))]
public class EMEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (Application.isPlaying)
        {
            SimulateEM tgt = (SimulateEM)target;

            EditorGUILayout.LabelField("");

            if (GUILayout.Button("Take Snapshot"))
                tgt.SaveScreen();

            EditorGUILayout.LabelField("");
            EditorGUILayout.LabelField("Simulation Time: ", tgt.simTime.ToString("f2") + " s");
            EditorGUILayout.LabelField("Simulation Frame: ", tgt.simulationFrameIndex.ToString());
            EditorGUILayout.LabelField("Rendered Frame: ", tgt.frameIndex.ToString());
            EditorGUILayout.LabelField("Number of Voxels: ", (tgt.elements * 1E-6d).ToString("f3") + " M");
        }
    }
}

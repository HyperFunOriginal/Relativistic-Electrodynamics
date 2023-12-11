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
            EditorGUILayout.LabelField("Domain Size: ", "x: " + (-2f * tgt.CoordinateTransform(0, 0)).ToString("f3") + ", y: " + (-2f * tgt.CoordinateTransform(0, 1)).ToString("f3") + ", z: " + (-2f * tgt.CoordinateTransform(0, 2)).ToString("f3"));
        }
    }
}

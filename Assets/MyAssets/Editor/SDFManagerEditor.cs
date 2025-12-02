using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SDFManager))]
public class SDFManagerEditor : Editor
{
    SerializedProperty maxSteps;
    SerializedProperty maxDistance;
    SerializedProperty surfaceDistance;
    SerializedProperty smoothBlend;

    void OnEnable()
    {
        maxSteps = serializedObject.FindProperty("maxSteps");
        maxDistance = serializedObject.FindProperty("maxDistance");
        surfaceDistance = serializedObject.FindProperty("surfaceDistance");
        smoothBlend = serializedObject.FindProperty("smoothBlend");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox("This is the central SDF Manager. It renders all SDF shapes in the scene using a single shader instance.", MessageType.Info);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Raymarching Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(maxSteps);
        EditorGUILayout.PropertyField(maxDistance);
        EditorGUILayout.PropertyField(surfaceDistance);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Blend Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(smoothBlend, new GUIContent("Smooth Blend (Global)"));
        
        EditorGUILayout.Space();
        
        // Información de debug
        SDFManager manager = (SDFManager)target;
        int shapeCount = FindObjectsOfType<SDFShape>().Length;
        EditorGUILayout.LabelField($"Active Shapes: {shapeCount}", EditorStyles.helpBox);

        serializedObject.ApplyModifiedProperties();
    }
}

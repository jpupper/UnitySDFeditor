using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SDFShape))]
public class SDFShapeEditor : Editor
{
    SerializedProperty shapeType;
    SerializedProperty shapeColor;
    SerializedProperty blendOperation;
    SerializedProperty blendSmoothness;
    
    SerializedProperty sphereRadius;
    SerializedProperty boxSize;
    SerializedProperty torusRadius1;
    SerializedProperty torusRadius2;
    SerializedProperty capsulePointA;
    SerializedProperty capsulePointB;
    SerializedProperty capsuleRadius;
    SerializedProperty pyramidHeight;

    void OnEnable()
    {
        shapeType = serializedObject.FindProperty("shapeType");
        shapeColor = serializedObject.FindProperty("shapeColor");
        blendOperation = serializedObject.FindProperty("blendOperation");
        blendSmoothness = serializedObject.FindProperty("blendSmoothness");
        
        sphereRadius = serializedObject.FindProperty("sphereRadius");
        boxSize = serializedObject.FindProperty("boxSize");
        torusRadius1 = serializedObject.FindProperty("torusRadius1");
        torusRadius2 = serializedObject.FindProperty("torusRadius2");
        capsulePointA = serializedObject.FindProperty("capsulePointA");
        capsulePointB = serializedObject.FindProperty("capsulePointB");
        capsuleRadius = serializedObject.FindProperty("capsuleRadius");
        pyramidHeight = serializedObject.FindProperty("pyramidHeight");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Shape Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(shapeType);
        EditorGUILayout.PropertyField(shapeColor);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Blend Operation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(blendOperation);
        EditorGUILayout.PropertyField(blendSmoothness, new GUIContent("Smoothness"));
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shape Parameters", EditorStyles.boldLabel);
        
        SDFShapeType currentType = (SDFShapeType)shapeType.enumValueIndex;
        
        switch (currentType)
        {
            case SDFShapeType.Sphere:
                EditorGUILayout.PropertyField(sphereRadius, new GUIContent("Radius"));
                break;
                
            case SDFShapeType.Box:
                EditorGUILayout.PropertyField(boxSize, new GUIContent("Size"));
                break;
                
            case SDFShapeType.Torus:
                EditorGUILayout.PropertyField(torusRadius1, new GUIContent("Major Radius"));
                EditorGUILayout.PropertyField(torusRadius2, new GUIContent("Minor Radius"));
                break;
                
            case SDFShapeType.Capsule:
                EditorGUILayout.PropertyField(capsulePointA, new GUIContent("Point A"));
                EditorGUILayout.PropertyField(capsulePointB, new GUIContent("Point B"));
                EditorGUILayout.PropertyField(capsuleRadius, new GUIContent("Radius"));
                break;
                
            case SDFShapeType.Pyramid:
                EditorGUILayout.PropertyField(pyramidHeight, new GUIContent("Height"));
                break;
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This shape is managed by the SDF Manager. No need to add any renderer components.", MessageType.Info);
        
        serializedObject.ApplyModifiedProperties();
    }
}

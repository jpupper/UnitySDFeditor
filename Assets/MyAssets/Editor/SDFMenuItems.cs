using UnityEngine;
using UnityEditor;

public class SDFMenuItems
{
    [MenuItem("GameObject/SDF/SDF Manager", false, 10)]
    static void CreateSDFManager(MenuCommand menuCommand)
    {
        // Verificar si ya existe un manager
        SDFManager existingManager = Object.FindObjectOfType<SDFManager>();
        if (existingManager != null)
        {
            Selection.activeGameObject = existingManager.gameObject;
            EditorGUIUtility.PingObject(existingManager.gameObject);
            Debug.Log("SDF Manager already exists in the scene.");
            return;
        }
        
        // Crear el manager
        GameObject go = new GameObject("SDF Manager");
        go.AddComponent<SDFManager>();
        
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create SDF Manager");
        Selection.activeObject = go;
    }
    
    [MenuItem("GameObject/SDF/SDF Shape", false, 10)]
    static void CreateSDFShape(MenuCommand menuCommand)
    {
        // Crear forma
        GameObject go = new GameObject("SDF Shape");
        go.AddComponent<SDFShape>();
        
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create SDF Shape");
        Selection.activeObject = go;
    }
    
    [MenuItem("GameObject/SDF/SDF Shape - Sphere", false, 11)]
    static void CreateSDFSphere(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("SDF Sphere");
        SDFShape shape = go.AddComponent<SDFShape>();
        
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create SDF Sphere");
        Selection.activeObject = go;
    }
    
    [MenuItem("GameObject/SDF/SDF Shape - Box", false, 11)]
    static void CreateSDFBox(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("SDF Box");
        SDFShape shape = go.AddComponent<SDFShape>();
        // Box es el índice 1 en el enum
        SerializedObject so = new SerializedObject(shape);
        so.FindProperty("shapeType").enumValueIndex = 1;
        so.ApplyModifiedProperties();
        
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create SDF Box");
        Selection.activeObject = go;
    }
    
    [MenuItem("GameObject/SDF/SDF Shape - Torus", false, 11)]
    static void CreateSDFTorus(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("SDF Torus");
        SDFShape shape = go.AddComponent<SDFShape>();
        SerializedObject so = new SerializedObject(shape);
        so.FindProperty("shapeType").enumValueIndex = 2;
        so.ApplyModifiedProperties();
        
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create SDF Torus");
        Selection.activeObject = go;
    }
}

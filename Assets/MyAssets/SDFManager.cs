using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class SDFManager : MonoBehaviour
{
    private static SDFManager instance;
    
    [Header("Raymarching Settings")]
    [SerializeField] private int maxSteps = 100;
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private float surfaceDistance = 0.001f;
    
    [Header("Blend Settings")]
    [SerializeField] [Range(0.0f, 1.0f)] private float smoothBlend = 0.5f;
    
    private Material raymarchMaterial;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    
    private const int MAX_SHAPES = 32;
    
    // Lista de todas las formas registradas
    private static List<SDFShape> registeredShapes = new List<SDFShape>();
    
    public static SDFManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SDFManager>();
                
                if (instance == null)
                {
                    GameObject go = new GameObject("SDF Manager");
                    instance = go.AddComponent<SDFManager>();
                }
            }
            return instance;
        }
    }
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            DestroyImmediate(gameObject);
            return;
        }
        
        InitializeRenderer();
    }
    
    void OnEnable()
    {
        if (instance == null)
        {
            instance = this;
        }
        InitializeRenderer();
    }
    
    void LateUpdate()
    {
        UpdateShaderData();
    }
    
    private void InitializeRenderer()
    {
        // Configurar componentes
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }
        
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        
        // Crear mesh fullscreen
        if (meshFilter.sharedMesh == null)
        {
            meshFilter.sharedMesh = CreateFullscreenQuad();
        }
        
        // Crear material
        if (raymarchMaterial == null)
        {
            Shader raymarchShader = Shader.Find("Custom/SDFRaymarching");
            if (raymarchShader != null)
            {
                raymarchMaterial = new Material(raymarchShader);
                meshRenderer.sharedMaterial = raymarchMaterial;
            }
            else
            {
                Debug.LogError("No se encontró el shader 'Custom/SDFRaymarching'");
            }
        }
        
        UpdateShaderProperties();
    }
    
    private Mesh CreateFullscreenQuad()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Fullscreen Raymarching Quad";
        
        // Crear un quad muy grande que cubra toda la escena
        float size = 1000f;
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-size, -size, 0),
            new Vector3(size, -size, 0),
            new Vector3(-size, size, 0),
            new Vector3(size, size, 0)
        };
        
        Vector2[] uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        int[] triangles = new int[]
        {
            0, 2, 1,
            2, 3, 1
        };
        
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    private void UpdateShaderProperties()
    {
        if (raymarchMaterial == null) return;
        
        raymarchMaterial.SetInt("_MaxSteps", maxSteps);
        raymarchMaterial.SetFloat("_MaxDistance", maxDistance);
        raymarchMaterial.SetFloat("_SurfaceDistance", surfaceDistance);
        raymarchMaterial.SetFloat("_SmoothBlend", smoothBlend);
    }
    
    private void UpdateShaderData()
    {
        if (raymarchMaterial == null) return;
        
        // Limpiar formas nulas
        registeredShapes.RemoveAll(s => s == null);
        
        int count = Mathf.Min(registeredShapes.Count, MAX_SHAPES);
        
        Vector4[] shapePositions = new Vector4[MAX_SHAPES];
        Vector4[] shapeRotations = new Vector4[MAX_SHAPES]; // quaternions (x, y, z, w)
        Vector4[] shapeScales = new Vector4[MAX_SHAPES]; // scale (x, y, z)
        Vector4[] shapeParams1 = new Vector4[MAX_SHAPES];
        Vector4[] shapeParams2 = new Vector4[MAX_SHAPES];
        Vector4[] shapeParams3 = new Vector4[MAX_SHAPES];
        Vector4[] shapeParams4 = new Vector4[MAX_SHAPES];
        Vector4[] shapeColors = new Vector4[MAX_SHAPES];
        float[] shapeTypes = new float[MAX_SHAPES];
        float[] blendOperations = new float[MAX_SHAPES];
        float[] blendSmoothness = new float[MAX_SHAPES];
        
        for (int i = 0; i < count; i++)
        {
            SDFShape shape = registeredShapes[i];
            if (shape != null && shape.enabled)
            {
                Vector3 pos = shape.transform.position;
                shapePositions[i] = new Vector4(pos.x, pos.y, pos.z, 1.0f);
                
                // Capturar rotación como quaternion
                Quaternion rot = shape.transform.rotation;
                shapeRotations[i] = new Vector4(rot.x, rot.y, rot.z, rot.w);
                
                // Capturar escala
                Vector3 scale = shape.transform.lossyScale;
                shapeScales[i] = new Vector4(scale.x, scale.y, scale.z, 1.0f);
                
                Color col = shape.ShapeColor;
                shapeColors[i] = new Vector4(col.r, col.g, col.b, col.a);
                
                shapeTypes[i] = (float)shape.ShapeType;
                blendOperations[i] = (float)shape.BlendOperation;
                blendSmoothness[i] = shape.BlendSmoothness;
                
                // Empaquetar parámetros
                switch (shape.ShapeType)
                {
                    case SDFShapeType.Sphere:
                        shapeParams1[i] = new Vector4(shape.SphereRadius, 0, 0, 0);
                        break;
                    case SDFShapeType.Box:
                        shapeParams1[i] = new Vector4(shape.BoxSize.x, shape.BoxSize.y, shape.BoxSize.z, 0);
                        break;
                    case SDFShapeType.Torus:
                        shapeParams1[i] = new Vector4(shape.TorusRadius1, shape.TorusRadius2, 0, 0);
                        break;
                    case SDFShapeType.Capsule:
                        shapeParams1[i] = new Vector4(shape.CapsuleRadius, 0, 0, 0);
                        shapeParams3[i] = new Vector4(shape.CapsulePointA.x, shape.CapsulePointA.y, shape.CapsulePointA.z, 0);
                        shapeParams4[i] = new Vector4(shape.CapsulePointB.x, shape.CapsulePointB.y, shape.CapsulePointB.z, 0);
                        break;
                    case SDFShapeType.Pyramid:
                        shapeParams1[i] = new Vector4(shape.PyramidHeight, 0, 0, 0);
                        break;
                }
            }
        }
        
        // Enviar al shader
        raymarchMaterial.SetInt("_ShapeCount", count);
        raymarchMaterial.SetVectorArray("_ShapePositions", shapePositions);
        raymarchMaterial.SetVectorArray("_ShapeRotations", shapeRotations);
        raymarchMaterial.SetVectorArray("_ShapeScales", shapeScales);
        raymarchMaterial.SetVectorArray("_ShapeParams1", shapeParams1);
        raymarchMaterial.SetVectorArray("_ShapeParams2", shapeParams2);
        raymarchMaterial.SetVectorArray("_ShapeParams3", shapeParams3);
        raymarchMaterial.SetVectorArray("_ShapeParams4", shapeParams4);
        raymarchMaterial.SetVectorArray("_ShapeColors", shapeColors);
        raymarchMaterial.SetFloatArray("_ShapeTypes", shapeTypes);
        raymarchMaterial.SetFloatArray("_BlendOperations", blendOperations);
        raymarchMaterial.SetFloatArray("_BlendSmoothness", blendSmoothness);
    }
    
    // Sistema de registro de formas
    public static void RegisterShape(SDFShape shape)
    {
        if (!registeredShapes.Contains(shape))
        {
            registeredShapes.Add(shape);
        }
        
        // Asegurar que el manager existe
        var manager = Instance;
    }
    
    public static void UnregisterShape(SDFShape shape)
    {
        registeredShapes.Remove(shape);
    }
    
    void OnDestroy()
    {
        if (raymarchMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(raymarchMaterial);
            else
                DestroyImmediate(raymarchMaterial);
        }
        
        if (instance == this)
        {
            instance = null;
        }
    }
    
    void OnValidate()
    {
        UpdateShaderProperties();
    }
    
    // Propiedades públicas
    public float SmoothBlend
    {
        get { return smoothBlend; }
        set
        {
            smoothBlend = Mathf.Clamp01(value);
            UpdateShaderProperties();
        }
    }
}

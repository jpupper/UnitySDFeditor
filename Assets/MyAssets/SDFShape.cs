using UnityEngine;

public enum SDFShapeType
{
    Sphere,
    Box,
    Torus,
    Capsule,
    Pyramid
}

public enum SDFBlendOperation
{
    Union,
    Subtraction,
    Intersection
}

[ExecuteAlways]
public class SDFShape : MonoBehaviour
{
    [Header("Shape Settings")]
    [SerializeField] private SDFShapeType shapeType = SDFShapeType.Sphere;
    [SerializeField] private Color shapeColor = Color.white;
    
    [Header("Blend Operation")]
    [SerializeField] private SDFBlendOperation blendOperation = SDFBlendOperation.Union;
    [SerializeField] [Range(0.0f, 1.0f)] private float blendSmoothness = 0.5f;
    
    [Header("Shape Parameters")]
    // Sphere
    [SerializeField] [Range(0.1f, 2.0f)] private float sphereRadius = 0.5f;
    
    // Box
    [SerializeField] private Vector3 boxSize = new Vector3(0.5f, 0.5f, 0.5f);
    
    // Torus
    [SerializeField] [Range(0.1f, 2.0f)] private float torusRadius1 = 0.5f;
    [SerializeField] [Range(0.05f, 1.0f)] private float torusRadius2 = 0.2f;
    
    // Capsule
    [SerializeField] private Vector3 capsulePointA = new Vector3(0, -0.5f, 0);
    [SerializeField] private Vector3 capsulePointB = new Vector3(0, 0.5f, 0);
    [SerializeField] [Range(0.05f, 1.0f)] private float capsuleRadius = 0.2f;
    
    // Pyramid
    [SerializeField] [Range(0.1f, 2.0f)] private float pyramidHeight = 1.0f;
    
    // Propiedades públicas para acceso externo
    public SDFShapeType ShapeType => shapeType;
    public Color ShapeColor => shapeColor;
    public SDFBlendOperation BlendOperation => blendOperation;
    public float BlendSmoothness => blendSmoothness;
    public float SphereRadius => sphereRadius;
    public Vector3 BoxSize => boxSize;
    public float TorusRadius1 => torusRadius1;
    public float TorusRadius2 => torusRadius2;
    public Vector3 CapsulePointA => capsulePointA;
    public Vector3 CapsulePointB => capsulePointB;
    public float CapsuleRadius => capsuleRadius;
    public float PyramidHeight => pyramidHeight;
    
    void OnEnable()
    {
        SDFManager.RegisterShape(this);
    }
    
    void OnDisable()
    {
        SDFManager.UnregisterShape(this);
    }
    
    void OnDestroy()
    {
        SDFManager.UnregisterShape(this);
    }
    
    // Visualización en la escena
    void OnDrawGizmos()
    {
        Gizmos.color = shapeColor;
        
        switch (shapeType)
        {
            case SDFShapeType.Sphere:
                Gizmos.DrawWireSphere(transform.position, sphereRadius);
                break;
                
            case SDFShapeType.Box:
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(Vector3.zero, boxSize * 2);
                Gizmos.matrix = Matrix4x4.identity;
                break;
                
            case SDFShapeType.Torus:
                // Aproximación simple del torus
                Gizmos.DrawWireSphere(transform.position, torusRadius1 + torusRadius2);
                break;
                
            case SDFShapeType.Capsule:
                Gizmos.DrawWireSphere(transform.position + capsulePointA, capsuleRadius);
                Gizmos.DrawWireSphere(transform.position + capsulePointB, capsuleRadius);
                Gizmos.DrawLine(transform.position + capsulePointA, transform.position + capsulePointB);
                break;
                
            case SDFShapeType.Pyramid:
                // Aproximación simple de la pirámide
                Vector3 top = transform.position + Vector3.up * pyramidHeight;
                Vector3 base1 = transform.position + new Vector3(-0.5f, 0, -0.5f);
                Vector3 base2 = transform.position + new Vector3(0.5f, 0, -0.5f);
                Vector3 base3 = transform.position + new Vector3(0.5f, 0, 0.5f);
                Vector3 base4 = transform.position + new Vector3(-0.5f, 0, 0.5f);
                
                // Base
                Gizmos.DrawLine(base1, base2);
                Gizmos.DrawLine(base2, base3);
                Gizmos.DrawLine(base3, base4);
                Gizmos.DrawLine(base4, base1);
                
                // Lados
                Gizmos.DrawLine(base1, top);
                Gizmos.DrawLine(base2, top);
                Gizmos.DrawLine(base3, top);
                Gizmos.DrawLine(base4, top);
                break;
        }
    }
}

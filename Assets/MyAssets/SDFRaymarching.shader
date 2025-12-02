Shader "Custom/SDFRaymarching"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Radius ("Sphere Radius", Range(0.1, 2.0)) = 0.5
        _MaxSteps ("Max Steps", Int) = 100
        _MaxDistance ("Max Distance", Float) = 100.0
        _SurfaceDistance ("Surface Distance", Float) = 0.001
        _SmoothBlend ("Smooth Blend", Range(0.0, 1.0)) = 0.5
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 ro : TEXCOORD1; // ray origin
                float3 hitPos : TEXCOORD2; // world position
            };

            float4 _Color;
            int _ShapeType;
            int _MaxSteps;
            float _MaxDistance;
            float _SurfaceDistance;
            float _SmoothBlend;
            
            // Parámetros de formas individuales (para el objeto actual)
            float _SphereRadius;
            float3 _BoxSize;
            float2 _TorusParams;
            float3 _CapsuleA;
            float3 _CapsuleB;
            float _CapsuleRadius;
            float _PyramidHeight;
            
            // Arrays para múltiples formas
            #define MAX_SPHERES 32
            int _ShapeCount;
            float4 _ShapePositions[MAX_SPHERES];
            float4 _ShapeRotations[MAX_SPHERES]; // quaternions (x, y, z, w)
            float4 _ShapeScales[MAX_SPHERES]; // scales (x, y, z)
            float4 _ShapeParams1[MAX_SPHERES];
            float4 _ShapeParams2[MAX_SPHERES];
            float4 _ShapeParams3[MAX_SPHERES];
            float4 _ShapeParams4[MAX_SPHERES];
            float4 _ShapeColors[MAX_SPHERES];
            float _ShapeTypes[MAX_SPHERES];
            float _BlendOperations[MAX_SPHERES];
            float _BlendSmoothness[MAX_SPHERES];
            
            // Variable global para almacenar el color mezclado
            float4 _BlendedColor;
            
            // ============= FUNCIONES DE ROTACIÓN =============
            
            // Rotar un punto usando un quaternion
            float3 RotatePointByQuaternion(float3 p, float4 q)
            {
                // q = (x, y, z, w)
                float3 u = float3(q.x, q.y, q.z);
                float s = q.w;
                
                return 2.0 * dot(u, p) * u
                     + (s * s - dot(u, u)) * p
                     + 2.0 * s * cross(u, p);
            }
            
            // Conjugado de un quaternion (rotación inversa)
            float4 QuaternionConjugate(float4 q)
            {
                return float4(-q.x, -q.y, -q.z, q.w);
            }
            
            // ============= FUNCIONES DE ESCALADO =============
            
            // Escalar un SDF de forma no uniforme (distorsiona pero permite escala independiente en cada eje)
            float ScaleSDF(float dist, float3 scale)
            {
                // Usar el componente mínimo de escala para mantener distancias correctas
                float minScale = min(min(scale.x, scale.y), scale.z);
                return dist * minScale;
            }

            // ============= FUNCIONES SDF =============
            
            // Esfera
            float sdSphere(float3 p, float3 center, float r)
            {
                return length(p - center) - r;
            }
            
            // Box (Cubo)
            float sdBox(float3 p, float3 center, float3 b)
            {
                float3 q = abs(p - center) - b;
                return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
            }
            
            // Torus
            float sdTorus(float3 p, float3 center, float2 t)
            {
                float3 localP = p - center;
                float2 q = float2(length(localP.xz) - t.x, localP.y);
                return length(q) - t.y;
            }
            
            // Capsule
            float sdCapsule(float3 p, float3 center, float3 a, float3 b, float r)
            {
                float3 localP = p - center;
                float3 pa = localP - a;
                float3 ba = b - a;
                float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
                return length(pa - ba * h) - r;
            }
            
            // Pyramid
            float sdPyramid(float3 p, float3 center, float h)
            {
                float3 localP = p - center;
                float m2 = h * h + 0.25;
                
                localP.xz = abs(localP.xz);
                localP.xz = (localP.z > localP.x) ? localP.zx : localP.xz;
                localP.xz -= 0.5;
                
                float3 q = float3(localP.z, h * localP.y - 0.5 * localP.x, h * localP.x + 0.5 * localP.y);
                
                float s = max(-q.x, 0.0);
                float t = clamp((q.y - 0.5 * localP.z) / (m2 + 0.25), 0.0, 1.0);
                
                float a = m2 * (q.x + s) * (q.x + s) + q.y * q.y;
                float b = m2 * (q.x + 0.5 * t) * (q.x + 0.5 * t) + (q.y - m2 * t) * (q.y - m2 * t);
                
                float d2 = min(q.y, -q.x * m2 - q.y * 0.5) > 0.0 ? 0.0 : min(a, b);
                
                return sqrt((d2 + q.z * q.z) / m2) * sign(max(q.z, -localP.y));
            }
            
            // Evaluar SDF según el tipo (con rotación y escala)
            float EvaluateShape(float shapeType, float3 p, float3 center, float4 rotation, float3 scale, float4 params1, float4 params2, float4 params3, float4 params4)
            {
                int type = int(shapeType);
                
                // Convertir el punto a espacio local de la forma
                float3 localP = p - center;
                
                // Aplicar rotación inversa al punto
                float4 invRot = QuaternionConjugate(rotation);
                localP = RotatePointByQuaternion(localP, invRot);
                
                // Aplicar escala inversa al punto (escalar el espacio)
                float3 scaledP = localP / scale;
                
                // Evaluar SDF en espacio transformado
                float dist = 0.0;
                
                if (type == 0) // Sphere
                {
                    dist = sdSphere(center + scaledP, center, params1.x);
                }
                else if (type == 1) // Box
                {
                    dist = sdBox(center + scaledP, center, params1.xyz);
                }
                else if (type == 2) // Torus
                {
                    dist = sdTorus(center + scaledP, center, params1.xy);
                }
                else if (type == 3) // Capsule
                {
                    dist = sdCapsule(center + scaledP, center, params3.xyz, params4.xyz, params1.x);
                }
                else if (type == 4) // Pyramid
                {
                    dist = sdPyramid(center + scaledP, center, params1.x);
                }
                else
                {
                    return _MaxDistance;
                }
                
                // Aplicar corrección de escala a la distancia resultante
                return ScaleSDF(dist, scale);
            }
            
            // ============= OPERACIONES DE BLEND =============
            
            // Smooth Union operation
            float opSmoothUnion(float d1, float d2, float k)
            {
                k *= 4.0;
                float h = max(k - abs(d1 - d2), 0.0);
                return min(d1, d2) - h * h * 0.25 / k;
            }
            
            // Smooth Subtraction
            float opSmoothSubtraction(float d1, float d2, float k)
            {
                return -opSmoothUnion(d1, -d2, k);
            }
            
            // Smooth Intersection
            float opSmoothIntersection(float d1, float d2, float k)
            {
                return -opSmoothUnion(-d1, -d2, k);
            }
            
            // Smooth Union con mezcla de colores
            float opSmoothUnionColor(float d1, float d2, float4 col1, float4 col2, float k, out float4 outColor)
            {
                k *= 4.0;
                float h = max(k - abs(d1 - d2), 0.0);
                float blend = h * h * 0.25 / k;
                
                // Mezclar colores basado en las distancias
                float t = saturate((d2 - d1 + k) / (2.0 * k));
                outColor = lerp(col2, col1, t);
                
                return min(d1, d2) - blend;
            }
            
            // Smooth Subtraction con mezcla de colores
            float opSmoothSubtractionColor(float d1, float d2, float4 col1, float4 col2, float k, out float4 outColor)
            {
                k *= 4.0;
                float h = max(k - abs(-d1 - d2), 0.0);
                
                // En subtraction, el color dominante es el de la forma que se mantiene (d1)
                outColor = col1;
                
                return max(-d1, d2) + h * h * 0.25 / k;
            }
            
            // Smooth Intersection con mezcla de colores
            float opSmoothIntersectionColor(float d1, float d2, float4 col1, float4 col2, float k, out float4 outColor)
            {
                k *= 4.0;
                float h = max(k - abs(d1 - d2), 0.0);
                
                // En intersection, mezclar colores de ambas formas
                float t = saturate((d2 - d1 + k) / (2.0 * k));
                outColor = lerp(col2, col1, t);
                
                return max(d1, d2) + h * h * 0.25 / k;
            }
            
            // Aplicar operación de blend según el tipo
            float ApplyBlendOperation(float d1, float d2, float4 col1, float4 col2, int operation, float k, out float4 outColor)
            {
                if (operation == 0) // Union
                {
                    return opSmoothUnionColor(d1, d2, col1, col2, k, outColor);
                }
                else if (operation == 1) // Subtraction
                {
                    return opSmoothSubtractionColor(d1, d2, col1, col2, k, outColor);
                }
                else if (operation == 2) // Intersection
                {
                    return opSmoothIntersectionColor(d1, d2, col1, col2, k, outColor);
                }
                
                // Por defecto, union
                return opSmoothUnionColor(d1, d2, col1, col2, k, outColor);
            }

            // Función de distancia de la escena con smooth union
            float GetDist(float3 p)
            {
                if (_ShapeCount == 0)
                {
                    _BlendedColor = _Color;
                    return _MaxDistance;
                }
                
                // Primera forma
                float d = EvaluateShape(
                    _ShapeTypes[0],
                    p,
                    _ShapePositions[0].xyz,
                    _ShapeRotations[0],
                    _ShapeScales[0].xyz,
                    _ShapeParams1[0],
                    _ShapeParams2[0],
                    _ShapeParams3[0],
                    _ShapeParams4[0]
                );
                float4 currentColor = _ShapeColors[0];
                
                // Combinar con el resto de formas usando la operación específica de cada una
                for (int i = 1; i < _ShapeCount; i++)
                {
                    float d2 = EvaluateShape(
                        _ShapeTypes[i],
                        p,
                        _ShapePositions[i].xyz,
                        _ShapeRotations[i],
                        _ShapeScales[i].xyz,
                        _ShapeParams1[i],
                        _ShapeParams2[i],
                        _ShapeParams3[i],
                        _ShapeParams4[i]
                    );
                    float4 col2 = _ShapeColors[i];
                    
                    // Usar la operación y smoothness específica de esta forma
                    int operation = int(_BlendOperations[i]);
                    float k = _BlendSmoothness[i];
                    
                    d = ApplyBlendOperation(d, d2, currentColor, col2, operation, k, currentColor);
                }
                
                _BlendedColor = currentColor;
                return d;
            }

            // Raymarching
            float Raymarch(float3 ro, float3 rd)
            {
                float dO = 0.0; // distancia desde el origen
                
                for(int i = 0; i < _MaxSteps; i++)
                {
                    float3 p = ro + rd * dO;
                    float dS = GetDist(p);
                    dO += dS;
                    
                    if(dO > _MaxDistance || dS < _SurfaceDistance)
                        break;
                }
                
                return dO;
            }

            // Calcular la normal usando el gradiente
            float3 GetNormal(float3 p)
            {
                float d = GetDist(p);
                float2 e = float2(0.001, 0);
                
                float3 n = d - float3(
                    GetDist(p - e.xyy),
                    GetDist(p - e.yxy),
                    GetDist(p - e.yyx)
                );
                
                return normalize(n);
            }

            v2f vert (appdata v)
            {
                v2f o;
                
                // Billboard: hacer que el quad mire a la cámara
                float3 worldPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float3 camRight = UNITY_MATRIX_V[0].xyz;
                float3 camUp = UNITY_MATRIX_V[1].xyz;
                float3 camForward = -UNITY_MATRIX_V[2].xyz;
                
                // Construir posición billboard
                float3 billboardPos = worldPos + camRight * v.vertex.x + camUp * v.vertex.y;
                
                o.vertex = mul(UNITY_MATRIX_VP, float4(billboardPos, 1.0));
                o.uv = v.uv;
                o.hitPos = billboardPos;
                o.ro = _WorldSpaceCameraPos;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 ro = i.ro; // ray origin (cámara)
                float3 rd = normalize(i.hitPos - ro); // ray direction
                
                // Realizar raymarching
                float d = Raymarch(ro, rd);
                
                // Si no golpeamos nada, retornar transparente
                if(d >= _MaxDistance)
                {
                    return fixed4(0, 0, 0, 0);
                }
                
                // Calcular la posición de impacto
                float3 p = ro + rd * d;
                
                // Calcular la normal
                float3 n = GetNormal(p);
                
                // Iluminación simple (difusa)
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float diff = max(dot(n, lightDir), 0.0);
                
                // Luz ambiente
                float ambient = 0.3;
                
                // Color final usando el color mezclado de las esferas con alpha
                float3 col = _BlendedColor.rgb * (diff + ambient);
                float alpha = _BlendedColor.a;
                
                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}

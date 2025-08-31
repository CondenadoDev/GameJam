using UnityEngine;

/// <summary>
/// Sistema de cono de visión adaptativo que se ajusta a obstáculos.
/// Genera un mesh dinámico que representa el área visible del guardia.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VisionCone : MonoBehaviour
{
    // ====== CONFIGURACIÓN VISUAL ======
    [Header("Configuración Visual")]
    [Tooltip("Material para el cono de visión (debe soportar transparencia)")]
    [SerializeField] private Material visionConeMaterial;
    [Tooltip("Color cuando está patrullando")]
    [SerializeField] private Color patrolColor = new Color(0, 1, 0, 0.25f);
    [Tooltip("Color cuando está investigando")]
    [SerializeField] private Color investigateColor = new Color(1, 1, 0, 0.35f);
    [Tooltip("Color cuando está persiguiendo")]
    [SerializeField] private Color chaseColor = new Color(1, 0, 0, 0.45f);
    [Tooltip("Color cuando está rotando")]
    [SerializeField] private Color rotateColor = new Color(0, 1, 1, 0.3f);
    
    // ====== CONFIGURACIÓN TÉCNICA ======
    [Header("Configuración Técnica")]
    [Tooltip("Resolución del mesh (número de rayos)")]
    [SerializeField] private int meshResolution = 80;
    [Tooltip("Distancia extra para detectar bordes")]
    [SerializeField] private float edgeDistanceThreshold = 0.05f;
    [Tooltip("Resolución para detectar bordes")]
    [SerializeField] private int edgeResolveIterations = 8;
    [Tooltip("Orden de renderizado")]
    [SerializeField] private int sortingOrder = 5;
    
    // ====== REFERENCIAS ======
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh visionMesh;
    private GuardController guardController;
    private LayerMask obstacleLayer;
    
    private bool isInitialized = false;
    private float currentVisionRange;
    private float currentVisionAngle;
    
    // ====== INICIALIZACIÓN ======
    public void Initialize(GuardController controller, LayerMask obstacles)
    {
        guardController = controller;
        obstacleLayer = obstacles;
        
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        visionMesh = new Mesh();
        visionMesh.name = "Vision_Cone_Mesh";
        meshFilter.mesh = visionMesh;
        
        if (visionConeMaterial == null)
        {
            // Crear un material por defecto si no hay uno asignado
            visionConeMaterial = new Material(Shader.Find("Sprites/Default"));
            Debug.LogWarning("⚠ No hay material asignado al cono de visión, usando material por defecto");
        }
        
        meshRenderer.material = visionConeMaterial;
        ConfigureMaterialForTransparency();
        
        meshRenderer.sortingOrder = sortingOrder;
        meshRenderer.sortingLayerName = "Default";
        meshRenderer.enabled = true;
        
        isInitialized = true;
    }
    
    void Start()
    {
        if (!isInitialized)
        {
            guardController = GetComponentInParent<GuardController>();
            if (guardController != null)
            {
                obstacleLayer = LayerMask.GetMask("Obstacles", "Walls");
                Initialize(guardController, obstacleLayer);
            }
            else
            {
                Debug.LogError("No se encontró GuardController en el padre!");
                enabled = false;
            }
        }
    }
    
    void LateUpdate()
    {
        if (meshRenderer != null && meshRenderer.enabled && guardController != null)
        {
            DrawFieldOfView();
        }
    }
    
    // ====== GENERACIÓN DEL MESH ======
    private void DrawFieldOfView()
    {
        currentVisionRange = guardController.VisionRange;
        currentVisionAngle = guardController.VisionAngle;
        
        int stepCount = Mathf.RoundToInt(currentVisionAngle * meshResolution / 360f);
        float stepAngleSize = currentVisionAngle / stepCount;
        
        var viewPoints = new System.Collections.Generic.List<Vector3>();
        ViewCastInfo oldViewCast = new ViewCastInfo();
        
        Vector3 guardDirection = guardController.GetFacingDirection();
        float baseAngle = Mathf.Atan2(guardDirection.y, guardDirection.x) * Mathf.Rad2Deg;
        
        for (int i = 0; i <= stepCount; i++)
        {
            float angle = baseAngle - currentVisionAngle / 2 + stepAngleSize * i;
            ViewCastInfo newViewCast = ViewCast(angle);
            
            // Detectar bordes de obstáculos
            if (i > 0)
            {
                bool edgeDistanceThresholdExceeded = Mathf.Abs(oldViewCast.distance - newViewCast.distance) > edgeDistanceThreshold;
                if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDistanceThresholdExceeded))
                {
                    // Encontramos un borde, refinar la detección
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
                    if (edge.pointA != Vector3.zero) viewPoints.Add(edge.pointA);
                    if (edge.pointB != Vector3.zero) viewPoints.Add(edge.pointB);
                }
            }
            
            viewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }
        
        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];
        
        vertices[0] = Vector3.zero;
        
        for (int i = 0; i < viewPoints.Count; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);
        }
        
        for (int i = 0; i < vertexCount - 2; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }
        
        visionMesh.Clear();
        visionMesh.vertices = vertices;
        visionMesh.triangles = triangles;
        visionMesh.RecalculateNormals();
    }
    
    // ====== SISTEMA DE RAYCASTING ======
    private ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        RaycastHit2D hit = Physics2D.Raycast(transform.parent.position, dir, currentVisionRange, obstacleLayer);
        
        if (hit.collider != null)
        {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }
        else
        {
            Vector3 endPoint = transform.parent.position + dir * currentVisionRange;
            return new ViewCastInfo(false, endPoint, currentVisionRange, globalAngle);
        }
    }
    
    private EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
    {
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;
        
        // Refinar la búsqueda del borde con iteraciones para mayor precisión
        for (int i = 0; i < edgeResolveIterations; i++)
        {
            float angle = (minAngle + maxAngle) / 2;
            ViewCastInfo newViewCast = ViewCast(angle);
            bool edgeDistanceThresholdExceeded = Mathf.Abs(minViewCast.distance - newViewCast.distance) > edgeDistanceThreshold;
            
            if (newViewCast.hit == minViewCast.hit && !edgeDistanceThresholdExceeded)
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            }
            else
            {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }
        
        return new EdgeInfo(minPoint, maxPoint);
    }
    
    private Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.z;
        }
        return new Vector3(Mathf.Cos(angleInDegrees * Mathf.Deg2Rad), Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0);
    }
    
    // ====== CONTROL VISUAL ======
    public void SetVisible(bool visible)
    {
        if (meshRenderer != null)
        {
            meshRenderer.enabled = visible;
        }
    }
    
    public void UpdateColor(GuardController.GuardState state)
    {
        if (meshRenderer == null || meshRenderer.material == null) return;
        
        Color targetColor = patrolColor;
        switch (state)
        {
            case GuardController.GuardState.Patrolling: targetColor = patrolColor; break;
            case GuardController.GuardState.Chasing: targetColor = chaseColor; break;
            case GuardController.GuardState.Investigating: targetColor = investigateColor; break;
            case GuardController.GuardState.Rotating: targetColor = rotateColor; break;
        }
        meshRenderer.material.color = targetColor;
    }
    
    // ====== CONFIGURACIÓN DE MATERIAL ======
    private void ConfigureMaterialForTransparency()
    {
        if (visionConeMaterial == null) return;
        
        // Configurar el material para transparencia (Modo Fade)
        visionConeMaterial.SetFloat("_Mode", 2);
        visionConeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        visionConeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        visionConeMaterial.SetInt("_ZWrite", 0);
        visionConeMaterial.DisableKeyword("_ALPHATEST_ON");
        visionConeMaterial.EnableKeyword("_ALPHABLEND_ON");
        visionConeMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        visionConeMaterial.renderQueue = 3000;
        
        // Asegurar que el color inicial tenga transparencia
        Color currentColor = visionConeMaterial.color;
        currentColor.a = Mathf.Min(currentColor.a, 0.4f); // Máximo 40% de opacidad
        visionConeMaterial.color = currentColor;
    }
    
    // ====== ESTRUCTURAS DE DATOS ======
    private struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float distance;
        public float angle;
        
        public ViewCastInfo(bool _hit, Vector3 _point, float _distance, float _angle)
        {
            hit = _hit;
            point = _point;
            distance = _distance;
            angle = _angle;
        }
    }
    
    private struct EdgeInfo
    {
        public Vector3 pointA;
        public Vector3 pointB;
        
        public EdgeInfo(Vector3 _pointA, Vector3 _pointB)
        {
            pointA = _pointA;
            pointB = _pointB;
        }
    }
    
    // ====== DEBUG ======
    void OnDrawGizmosSelected()
    {
        if (guardController == null) return;
        
        // Dibuja el rango y ángulo de visión en el editor para facilitar el debug
        Vector3 guardDirection = guardController.GetFacingDirection();
        float baseAngle = Mathf.Atan2(guardDirection.y, guardDirection.x) * Mathf.Rad2Deg;
        Vector3 leftBoundary = DirFromAngle(baseAngle - guardController.VisionAngle / 2, true);
        Vector3 rightBoundary = DirFromAngle(baseAngle + guardController.VisionAngle / 2, true);
        
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.parent.position, leftBoundary * guardController.VisionRange);
        Gizmos.DrawRay(transform.parent.position, rightBoundary * guardController.VisionRange);
    }
}
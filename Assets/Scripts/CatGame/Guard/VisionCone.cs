using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VisionCone : MonoBehaviour
{
    // ====== CONFIGURACIÓN VISUAL ======
    [Header("Configuración Visual")]
    [Tooltip("Material para el cono de visión (debe soportar transparencia)")]
    [SerializeField] private Material visionConeMaterial;
    
    [Header("Colores para Guardias")]
    [Tooltip("Color cuando está patrullando o pasivo")]
    [SerializeField] private Color guardPassiveColor = new Color(1f, 1f, 1f, 0.25f);
    [Tooltip("Color cuando está investigando o alerta")]
    [SerializeField] private Color guardAlertColor = new Color(1f, 0.5f, 0f, 0.35f);
    [Tooltip("Color cuando está atacando o persiguiendo")]
    [SerializeField] private Color guardAggressiveColor = new Color(1f, 0f, 0f, 0.45f);
    
    [Header("Colores para Cámaras")]
    [Tooltip("Color normal de cámara")]
    [SerializeField] private Color cameraNormalColor = new Color(0f, 1f, 0f, 0.3f); 
    [Tooltip("Color de cámara detectando")]
    [SerializeField] private Color cameraDetectionColor = new Color(1f, 0.5f, 0f, 0.4f); 
    [Tooltip("Color de cámara en alerta")]
    [SerializeField] private Color cameraAlertColor = new Color(1f, 0f, 0f, 0.5f); 
    
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
    private SecurityCameraController cameraController;
    private LayerMask obstacleLayer;
    
    private bool isInitialized = false;
    private float currentVisionRange;
    private float currentVisionAngle;
    private bool isCamera = false;
    
    public void Initialize(GuardController controller, LayerMask obstacles)
    {
        guardController = controller;
        obstacleLayer = obstacles;
        isCamera = false;
        CommonInitialize();
    }
    
    public void Initialize(SecurityCameraController controller, LayerMask obstacles)
    {
        cameraController = controller;
        obstacleLayer = obstacles;
        isCamera = true;
        CommonInitialize();
    }
    
    private void CommonInitialize()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        visionMesh = new Mesh();
        visionMesh.name = isCamera ? "Camera_Vision_Cone_Mesh" : "Guard_Vision_Cone_Mesh";
        meshFilter.mesh = visionMesh;
        
        if (visionConeMaterial == null)
        {
            visionConeMaterial = new Material(Shader.Find("Sprites/Default"));
            Debug.LogWarning("⚠️ No hay material asignado al cono de visión, usando material por defecto");
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
            cameraController = GetComponentInParent<SecurityCameraController>();
            
            if (guardController != null)
            {
                isCamera = false;
                obstacleLayer = LayerMask.GetMask("Obstacles", "Walls");
                Initialize(guardController, obstacleLayer);
            }
            else if (cameraController != null)
            {
                isCamera = true;
                obstacleLayer = LayerMask.GetMask("Obstacles", "Walls");
                Initialize(cameraController, obstacleLayer);
            }
            else
            {
                Debug.LogError("No se encontró GuardController ni SecurityCameraController en el padre!");
                enabled = false;
            }
        }
    }
    
    void LateUpdate()
    {
        if (meshRenderer != null && meshRenderer.enabled && isInitialized)
        {
            DrawFieldOfView();
        }
    }
    
    private void DrawFieldOfView()
    {
        if (isCamera && cameraController != null)
        {
            currentVisionRange = cameraController.VisionRange;
            currentVisionAngle = cameraController.VisionAngle;
        }
        else if (!isCamera && guardController != null)
        {
            currentVisionRange = guardController.VisionRange;
            currentVisionAngle = guardController.VisionAngle;
        }
        else
        {
            return; 
        }
        
        int stepCount = Mathf.RoundToInt(currentVisionAngle * meshResolution / 360f);
        float stepAngleSize = currentVisionAngle / stepCount;
        
        var viewPoints = new System.Collections.Generic.List<Vector3>();
        ViewCastInfo oldViewCast = new ViewCastInfo();
        
        Vector3 facingDirection;
        if (isCamera)
        {
            facingDirection = cameraController.GetForwardDirection();
        }
        else
        {
            facingDirection = guardController.GetFacingDirection();
        }
        
        float baseAngle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;
        
        for (int i = 0; i <= stepCount; i++)
        {
            float angle = baseAngle - currentVisionAngle / 2 + stepAngleSize * i;
            ViewCastInfo newViewCast = ViewCast(angle);
            
            if (i > 0)
            {
                bool edgeDistanceThresholdExceeded = Mathf.Abs(oldViewCast.distance - newViewCast.distance) > edgeDistanceThreshold;
                if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDistanceThresholdExceeded))
                {
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
    
    private ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        Vector3 startPosition = transform.parent.position;
        RaycastHit2D hit = Physics2D.Raycast(startPosition, dir, currentVisionRange, obstacleLayer);
        
        if (hit.collider != null)
        {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }
        else
        {
            Vector3 endPoint = startPosition + dir * currentVisionRange;
            return new ViewCastInfo(false, endPoint, currentVisionRange, globalAngle);
        }
    }
    
    private EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
    {
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;
        
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
    
    public void SetVisible(bool visible)
    {
        if (meshRenderer != null)
        {
            meshRenderer.enabled = visible;
        }
    }
    
    public void UpdateColor(GuardController.GuardState state)
    {
        if (meshRenderer == null || meshRenderer.material == null || isCamera) return;
        
        Color targetColor;
        
        switch (state)
        {
            case GuardController.GuardState.Chasing:
            case GuardController.GuardState.Flanking:
                targetColor = guardAggressiveColor; 
                break;
                
            case GuardController.GuardState.Investigating:
            case GuardController.GuardState.Searching:
            case GuardController.GuardState.Ambushing:
                targetColor = guardAlertColor; 
                break;
                
            case GuardController.GuardState.Patrolling:
            case GuardController.GuardState.ReturningToSpawn:
            case GuardController.GuardState.Stunned:
            default:
                targetColor = guardPassiveColor; 
                break;
        }
        
        meshRenderer.material.color = targetColor;
    }
    
    public void UpdateCameraColor(Color cameraColor)
    {
        if (meshRenderer == null || meshRenderer.material == null || !isCamera) return;
        
        Color targetColor;
        
        if (IsColorSimilar(cameraColor, Color.green))
        {
            targetColor = cameraNormalColor;
        }
        else if (IsColorSimilar(cameraColor, new Color(1f, 0.5f, 0f))) 
        {
            targetColor = cameraDetectionColor;
        }
        else if (IsColorSimilar(cameraColor, Color.red))
        {
            targetColor = cameraAlertColor;
        }
        else
        {
            targetColor = new Color(cameraColor.r, cameraColor.g, cameraColor.b, 0.3f);
        }
        
        meshRenderer.material.color = targetColor;
    }
    
    private bool IsColorSimilar(Color color1, Color color2, float threshold = 0.1f)
    {
        return Vector3.Distance(new Vector3(color1.r, color1.g, color1.b), 
                               new Vector3(color2.r, color2.g, color2.b)) < threshold;
    }
    
    private void ConfigureMaterialForTransparency()
    {
        if (visionConeMaterial == null) return;
        
        visionConeMaterial.SetFloat("_Mode", 2);
        visionConeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        visionConeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        visionConeMaterial.SetInt("_ZWrite", 0);
        visionConeMaterial.DisableKeyword("_ALPHATEST_ON");
        visionConeMaterial.EnableKeyword("_ALPHABLEND_ON");
        visionConeMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        visionConeMaterial.renderQueue = 3000;
        
        Color currentColor = visionConeMaterial.color;
        currentColor.a = Mathf.Min(currentColor.a, 0.4f);
        visionConeMaterial.color = currentColor;
    }
    
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
    
    void OnDrawGizmosSelected()
    {
        if (!isInitialized) return;
        
        Vector3 facingDirection;
        if (isCamera && cameraController != null)
        {
            facingDirection = cameraController.GetForwardDirection();
        }
        else if (!isCamera && guardController != null)
        {
            facingDirection = guardController.GetFacingDirection();
        }
        else
        {
            return;
        }
        
        float baseAngle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;
        Vector3 leftBoundary = DirFromAngle(baseAngle - currentVisionAngle / 2, true);
        Vector3 rightBoundary = DirFromAngle(baseAngle + currentVisionAngle / 2, true);
        
        Gizmos.color = isCamera ? Color.green : Color.red;
        Gizmos.DrawRay(transform.parent.position, leftBoundary * currentVisionRange);
        Gizmos.DrawRay(transform.parent.position, rightBoundary * currentVisionRange);
    }
}
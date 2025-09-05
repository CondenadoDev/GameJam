using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class SecurityCameraController : MonoBehaviour
{
    [Header("Configuración de Cámara")]
    [SerializeField] private float visionRange = 6f;
    [SerializeField] private float visionAngle = 60f;
    [SerializeField] private float rotationSpeed = 30f; 
    [SerializeField] private float maxRotationAngle = 90f; 
    
    [Header("Dirección del Cono")]
    [SerializeField] private Vector2 initialDirection = Vector2.right; 
    [SerializeField] private bool useTransformRotation = false; 
    
    [Header("Configuración de Rotación")]
    [SerializeField] private bool rotatesAutomatically = true;
    [SerializeField] private float pauseTimeAtEnd = 1f; 
    
    [Header("Detección")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float detectionDelay = 0.5f; 
    
    [Header("Estados Visuales")]
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color alertColor = Color.red;
    [SerializeField] private Color detectionColor = Color.black;
    
    [Header("Comportamiento de Alerta")]
    [SerializeField] private bool arrestImmediately = false; 
    [SerializeField] private float alertRadius = 20f; 
    
    private SpriteRenderer spriteRenderer;
    private VisionCone cameraVisionCone;
    private Transform player;
    private GameManager gameManager;
    
    private float initialRotation;
    private float currentRotation;
    private bool rotatingClockwise = true;
    private bool isPaused = false;
    private float pauseTimer = 0f;
    
    private Vector2 _initialDirection;
    
    private bool isDetectingPlayer = false;
    private float detectionTimer = 0f;
    private bool hasAlertedPlayer = false;
    
    private bool isActive = true;
    private bool isDisabled = false;
    
    public float VisionRange => visionRange;
    public float VisionAngle => visionAngle;
    public bool IsActive => isActive && !isDisabled;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
        
        _initialDirection = initialDirection.normalized;
        if (useTransformRotation)
        {
            initialRotation = transform.eulerAngles.z;
            float angleInRadians = initialRotation * Mathf.Deg2Rad;
            _initialDirection = new Vector2(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians));
        }
        else
        {
            initialRotation = Mathf.Atan2(_initialDirection.y, _initialDirection.x) * Mathf.Rad2Deg;
        }
        currentRotation = initialRotation;
        
        CreateVisionCone();
    }
    
    void Start()
    {
        UpdateVisuals();
    }
    
    void Update()
    {
        if (!IsActive) return;
        
        if (rotatesAutomatically)
        {
            HandleAutomaticRotation();
        }
        
        bool canSeePlayer = CanSeePlayer();
        HandlePlayerDetection(canSeePlayer);
        UpdateVisuals();
    }
    
    private void CreateVisionCone()
    {
        cameraVisionCone = GetComponentInChildren<VisionCone>();
        
        if (cameraVisionCone == null)
        {
            GameObject visionConeObj = new GameObject("Camera_VisionCone");
            visionConeObj.transform.SetParent(transform);
            visionConeObj.transform.localPosition = Vector3.zero;
            visionConeObj.transform.localRotation = Quaternion.identity;
            
            visionConeObj.AddComponent<MeshFilter>();
            visionConeObj.AddComponent<MeshRenderer>();
            cameraVisionCone = visionConeObj.AddComponent<VisionCone>();
            
            cameraVisionCone.Initialize(this, obstacleLayer);
        }
    }
    
    private void HandleAutomaticRotation()
    {
        if (isPaused)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                isPaused = false;
                rotatingClockwise = !rotatingClockwise;
            }
            return;
        }
        
        float rotationDirection = rotatingClockwise ? 1f : -1f;
        float targetRotation = currentRotation + (rotationSpeed * rotationDirection * Time.deltaTime);
        
        float minRotation = initialRotation - maxRotationAngle / 2f;
        float maxRotation = initialRotation + maxRotationAngle / 2f;
        
        if (targetRotation >= maxRotation && rotatingClockwise)
        {
            targetRotation = maxRotation;
            isPaused = true;
            pauseTimer = pauseTimeAtEnd;
        }
        else if (targetRotation <= minRotation && !rotatingClockwise)
        {
            targetRotation = minRotation;
            isPaused = true;
            pauseTimer = pauseTimeAtEnd;
        }
        
        currentRotation = targetRotation;
    }
    
    private bool CanSeePlayer()
    {
        if (player == null || !IsActive) return false;
        
        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;
        
        if (distanceToPlayer > visionRange) return false;
        
        Vector3 forwardDirection = GetForwardDirection();
        float angleToPlayer = Vector3.Angle(forwardDirection, directionToPlayer);
        if (angleToPlayer > visionAngle / 2f) return false;
        
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer.normalized, 
                                           distanceToPlayer, obstacleLayer | playerLayer);
        
        return hit.collider != null && hit.collider.CompareTag("Player");
    }
    
    private void HandlePlayerDetection(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            if (!isDetectingPlayer)
            {
                isDetectingPlayer = true;
                detectionTimer = 0f;
                // FMOD: Reproducir sonido de cámara detectando
            }
            
            detectionTimer += Time.deltaTime;
            
            if (detectionTimer >= detectionDelay && !hasAlertedPlayer)
            {
                AlertPlayer();
            }
        }
        else
        {
            if (isDetectingPlayer)
            {
                isDetectingPlayer = false;
                detectionTimer = 0f;
                hasAlertedPlayer = false;
                // FMOD: Detener sonido de detección
            }
        }
    }
    
    private void AlertPlayer()
    {
        hasAlertedPlayer = true;
        Debug.Log($"¡Cámara {name} ha detectado al jugador!");
        
        // FMOD: Reproducir sonido de alarma de cámara
        
        AlertNearestGuard();
        
        if (arrestImmediately && gameManager != null)
        {
            gameManager.OnPlayerArrested();
        }
    }
    
    private void AlertNearestGuard()
    {
        GuardController[] allGuards = FindObjectsByType<GuardController>(FindObjectsSortMode.None);
        GuardController nearestGuard = null;
        float shortestDistance = float.MaxValue;
        
        foreach (GuardController guard in allGuards)
        {
            if (guard != null && !guard.IsStunned())
            {
                float distance = Vector3.Distance(transform.position, guard.transform.position);
                if (distance < shortestDistance && distance <= alertRadius)
                {
                    shortestDistance = distance;
                    nearestGuard = guard;
                }
            }
        }
        
        if (nearestGuard != null && player != null)
        {
            Debug.Log($"Cámara {name} alertando al guardia {nearestGuard.name} (distancia: {shortestDistance:F1}m)");
            nearestGuard.OnCameraAlert(player.position, transform.position);
        }
        else
        {
            Debug.Log($"Cámara {name}: No hay guardias disponibles para alertar dentro del radio de {alertRadius}m");
        }
    }
    
    private void UpdateVisuals()
    {
        if (spriteRenderer == null) return;
        
        Color currentColor;
        
        if (!IsActive)
        {
            currentColor = Color.gray;
        }
        else if (isDetectingPlayer)
        {
            float progress = detectionTimer / detectionDelay;
            currentColor = Color.Lerp(detectionColor, alertColor, progress);
        }
        else
        {
            currentColor = normalColor;
        }
        
        spriteRenderer.color = currentColor;
        
        if (cameraVisionCone != null)
        {
            cameraVisionCone.UpdateCameraColor(currentColor);
        }
    }
    
    public Vector3 GetForwardDirection()
    {
        float angleInRadians = currentRotation * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians), 0);
    }
    
    public void SetInitialDirection(Vector2 direction)
    {
        _initialDirection = direction.normalized;
        initialDirection = _initialDirection; 
        if (!useTransformRotation)
        {
            initialRotation = Mathf.Atan2(_initialDirection.y, _initialDirection.x) * Mathf.Rad2Deg;
            currentRotation = initialRotation;
        }
    }
    
    public void SetInitialDirection(float angleInDegrees)
    {
        float angleInRadians = angleInDegrees * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians));
        SetInitialDirection(direction);
    }
    
    public Vector2 GetInitialDirection()
    {
        return _initialDirection;
    }
    
    public void DisableCamera(float duration = 0f)
    {
        if (duration > 0f)
        {
            StartCoroutine(DisableCameraTemporarily(duration));
        }
        else
        {
            isDisabled = true;
            isDetectingPlayer = false;
            hasAlertedPlayer = false;
            UpdateVisuals();
        }
    }
    
    public void EnableCamera()
    {
        isDisabled = false;
        UpdateVisuals();
    }
    
    private IEnumerator DisableCameraTemporarily(float duration)
    {
        isDisabled = true;
        isDetectingPlayer = false;
        hasAlertedPlayer = false;
        UpdateVisuals();
        
        // FMOD: Reproducir sonido de cámara desactivándose
        
        yield return new WaitForSeconds(duration);
        
        isDisabled = false;
        UpdateVisuals();
        
        // FMOD: Reproducir sonido de cámara reactivándose
    }
    
    void OnDrawGizmos()
    {
        Vector3 forwardDir = GetForwardDirection();
        
        Gizmos.color = IsActive ? Color.red : Color.gray;
        Gizmos.DrawWireSphere(transform.position, visionRange);
        
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, alertRadius);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, forwardDir * visionRange);
        
        Vector3 leftBoundary = Quaternion.Euler(0, 0, visionAngle / 2f) * forwardDir;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, -visionAngle / 2f) * forwardDir;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, leftBoundary * visionRange);
        Gizmos.DrawRay(transform.position, rightBoundary * visionRange);
        
        Vector3 initialDir = new Vector3(_initialDirection.x, _initialDirection.y, 0);
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(transform.position, initialDir * visionRange * 0.7f);
        
        if (rotatesAutomatically)
        {
            Vector3 minRotDir = Quaternion.Euler(0, 0, initialRotation - maxRotationAngle / 2f) * Vector3.right;
            Vector3 maxRotDir = Quaternion.Euler(0, 0, initialRotation + maxRotationAngle / 2f) * Vector3.right;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, minRotDir * visionRange * 0.8f);
            Gizmos.DrawRay(transform.position, maxRotDir * visionRange * 0.8f);
            
            DrawGizmoArc(transform.position, initialRotation - maxRotationAngle / 2f, 
                        initialRotation + maxRotationAngle / 2f, visionRange * 0.8f);
        }
    }
    
    private void DrawGizmoArc(Vector3 center, float startAngle, float endAngle, float radius)
    {
        int segments = 20;
        float angleStep = (endAngle - startAngle) / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (startAngle + angleStep * i) * Mathf.Deg2Rad;
            float angle2 = (startAngle + angleStep * (i + 1)) * Mathf.Deg2Rad;
            
            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1), Mathf.Sin(angle1), 0) * radius;
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2), 0) * radius;
            
            Gizmos.DrawLine(point1, point2);
        }
    }
}
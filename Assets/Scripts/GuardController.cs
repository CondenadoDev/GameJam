using UnityEngine;
using System.Collections;

public class GuardController : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float waitTime = 1f;
    
    [Header("Vision Settings")]
    [SerializeField] private float visionRange = 8f;
    [SerializeField] private float visionAngle = 90f;
    [SerializeField] private LayerMask playerLayer = 1;
    [SerializeField] private LayerMask obstacleLayer = 1;
    
    [Header("Distraction Settings")]
    [SerializeField] private float distractionDuration = 3f;
    [SerializeField] private Color normalColor = Color.red;
    [SerializeField] private Color distractedColor = Color.yellow;
    
    [Header("Vision Visualization")]
    [SerializeField] private bool showVisionCone = true;
    [SerializeField] private int visionRays = 20;
    [SerializeField] private LineRenderer visionConeRenderer;
    [SerializeField] private Color normalVisionColor = new Color(1f, 0.2f, 0.2f, 0.3f);
    [SerializeField] private Color distractedVisionColor = new Color(1f, 1f, 0f, 0.2f);
    
    private int currentPatrolIndex = 0;
    private bool isWaiting = false;
    private bool isDistracted = false;
    private Vector3 distractionPoint;
    private Vector3 originalPosition;
    
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private Vector3 currentDirection = Vector3.right;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        spriteRenderer.color = normalColor;
        
        if (patrolPoints.Length == 0)
        {
            CreateDefaultPatrolPoints();
        }
        
        originalPosition = transform.position;
        CreateVisionConeVisualization();
    }
    
    void Update()
    {
        if (isDistracted)
        {
            HandleDistractedBehavior();
        }
        else
        {
            HandlePatrolBehavior();
        }
        
        CheckForPlayer();
        
        if (showVisionCone && visionConeRenderer != null)
        {
            UpdateVisionConeVisualization();
        }
    }
    
    void HandlePatrolBehavior()
    {
        if (isWaiting || patrolPoints.Length == 0) return;
        
        Transform targetPoint = patrolPoints[currentPatrolIndex];
        Vector3 direction = (targetPoint.position - transform.position).normalized;
        
        rb.linearVelocity = direction * moveSpeed;
        
        if (direction != Vector3.zero)
        {
            currentDirection = direction;
        }
        
        if (Vector3.Distance(transform.position, targetPoint.position) < 0.2f)
        {
            StartCoroutine(WaitAtPatrolPoint());
        }
    }
    
    void HandleDistractedBehavior()
    {
        Vector3 targetPosition = distractionPoint;
        
        // Pathfinding básico hacia el sonido
        Vector3 direction = GetPathToTarget(targetPosition);
        
        if (Vector3.Distance(transform.position, distractionPoint) > 0.5f)
        {
            if (direction != Vector3.zero)
            {
                rb.linearVelocity = direction * moveSpeed * 1.5f;
                currentDirection = direction;
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            currentDirection = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 3) * 45) * Vector3.right;
        }
    }
    
    Vector3 GetPathToTarget(Vector3 target)
    {
        Vector3 directDirection = (target - transform.position).normalized;
        
        // Raycast directo al objetivo
        RaycastHit2D directHit = Physics2D.Raycast(transform.position, directDirection, 
            Vector3.Distance(transform.position, target), obstacleLayer);
        
        if (directHit.collider == null)
        {
            return directDirection;
        }
        
        // Si hay obstáculo, buscar rutas alternativas
        Vector3[] directions = {
            Quaternion.Euler(0, 0, 45) * directDirection,
            Quaternion.Euler(0, 0, -45) * directDirection,
            Quaternion.Euler(0, 0, 90) * directDirection,
            Quaternion.Euler(0, 0, -90) * directDirection,
            Quaternion.Euler(0, 0, 135) * directDirection,
            Quaternion.Euler(0, 0, -135) * directDirection
        };
        
        float rayDistance = 2f;
        
        foreach (Vector3 testDirection in directions)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, testDirection, rayDistance, obstacleLayer);
            
            if (hit.collider == null)
            {
                return testDirection;
            }
        }
        
        return Vector3.zero;
    }
    
    IEnumerator WaitAtPatrolPoint()
    {
        isWaiting = true;
        rb.linearVelocity = Vector2.zero;
        
        yield return new WaitForSeconds(waitTime);
        
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        isWaiting = false;
    }
    
    void CheckForPlayer()
    {
        if (player == null) return;
        
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= visionRange)
        {
            float angleToPlayer = Vector3.Angle(currentDirection, directionToPlayer);
            
            if (angleToPlayer <= visionAngle / 2f)
            {
                // Raycast más preciso con múltiples puntos
                bool canSeePlayer = CanSeePlayer(player.position);
                
                if (canSeePlayer)
                {
                    DetectPlayer();
                }
            }
        }
    }
    
    bool CanSeePlayer(Vector3 playerPosition)
    {
        Vector3 guardPosition = transform.position;
        
        // Verificar línea de visión central
        RaycastHit2D centerHit = Physics2D.Linecast(guardPosition, playerPosition, obstacleLayer);
        
        if (centerHit.collider == null)
        {
            return true;
        }
        
        // Verificar líneas de visión laterales para más precisión
        Vector3 guardToPlayer = (playerPosition - guardPosition);
        Vector3 perpendicular = Vector3.Cross(guardToPlayer, Vector3.forward).normalized * 0.2f;
        
        Vector3 leftRayStart = guardPosition + new Vector3(perpendicular.x, perpendicular.y, 0);
        Vector3 rightRayStart = guardPosition - new Vector3(perpendicular.x, perpendicular.y, 0);
        
        RaycastHit2D leftHit = Physics2D.Linecast(leftRayStart, playerPosition, obstacleLayer);
        RaycastHit2D rightHit = Physics2D.Linecast(rightRayStart, playerPosition, obstacleLayer);
        
        return (leftHit.collider == null || rightHit.collider == null);
    }
    
    void DetectPlayer()
    {
        Debug.Log("Guardia detectó al jugador");
        
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnPlayerDetected();
        }
        
        StartCoroutine(DetectionFlash());
    }
    
    IEnumerator DetectionFlash()
    {
        Color originalColor = spriteRenderer.color;
        
        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    public void GetDistracted(Vector3 soundPosition)
    {
        if (isDistracted) return;
        
        Debug.Log("Guardia distraído por sonido");
        
        isDistracted = true;
        
        // En lugar de ir exactamente al sonido, ir a una posición cercana accesible
        distractionPoint = FindAccessiblePointNear(soundPosition);
        
        spriteRenderer.color = distractedColor;
        
        StopAllCoroutines();
        isWaiting = false;
        
        StartCoroutine(ReturnToNormalAfterDistraction());
    }
    
    Vector3 FindAccessiblePointNear(Vector3 targetPosition)
    {
        Vector3 guardPosition = transform.position;
        Vector3 directDirection = (targetPosition - guardPosition).normalized;
        
        // Probar diferentes distancias desde el sonido
        float[] testDistances = { 1f, 1.5f, 2f, 2.5f, 0.5f };
        
        foreach (float distance in testDistances)
        {
            // Probar en diferentes ángulos alrededor del sonido
            for (int angle = 0; angle < 360; angle += 45)
            {
                Vector3 offset = Quaternion.Euler(0, 0, angle) * Vector3.right * distance;
                Vector3 testPosition = targetPosition + offset;
                
                // Verificar si la posición es accesible
                RaycastHit2D hit = Physics2D.Linecast(guardPosition, testPosition, obstacleLayer);
                
                if (hit.collider == null)
                {
                    // Verificar que no esté dentro de una pared
                    Collider2D overlapCheck = Physics2D.OverlapPoint(testPosition, obstacleLayer);
                    if (overlapCheck == null)
                    {
                        return testPosition;
                    }
                }
            }
        }
        
        // Si no encuentra ninguna posición accesible, mantenerse en su lugar
        return guardPosition;
    }
    
    IEnumerator ReturnToNormalAfterDistraction()
    {
        yield return new WaitForSeconds(distractionDuration);
        
        isDistracted = false;
        spriteRenderer.color = normalColor;
        
        Debug.Log("Guardia ya no está distraído");
        
        FindClosestPatrolPoint();
    }
    
    void FindClosestPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;
        
        float closestDistance = float.MaxValue;
        int closestIndex = 0;
        
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, patrolPoints[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        
        currentPatrolIndex = closestIndex;
    }
    
    void CreateDefaultPatrolPoints()
    {
        GameObject patrolParent = new GameObject($"{gameObject.name}_PatrolPoints");
        
        Vector3[] defaultPositions = {
            transform.position,
            transform.position + Vector3.right * 3,
            transform.position + Vector3.up * 2,
            transform.position + Vector3.left * 2
        };
        
        patrolPoints = new Transform[defaultPositions.Length];
        
        for (int i = 0; i < defaultPositions.Length; i++)
        {
            GameObject point = new GameObject($"PatrolPoint_{i}");
            point.transform.position = defaultPositions[i];
            point.transform.SetParent(patrolParent.transform);
            patrolPoints[i] = point.transform;
        }
    }
    
    void CreateVisionConeVisualization()
    {
        GameObject visionConeObject = new GameObject("VisionCone");
        visionConeObject.transform.SetParent(transform);
        visionConeObject.transform.localPosition = Vector3.zero;
        
        visionConeRenderer = visionConeObject.AddComponent<LineRenderer>();
        visionConeRenderer.material = new Material(Shader.Find("Sprites/Default"));
        visionConeRenderer.material.color = normalVisionColor;
        visionConeRenderer.startWidth = 0.1f;
        visionConeRenderer.endWidth = 0.1f;
        visionConeRenderer.positionCount = visionRays + 2;
        visionConeRenderer.useWorldSpace = false;
        visionConeRenderer.sortingOrder = -1;
    }
    
    void UpdateVisionConeVisualization()
    {
        Color currentColor = isDistracted ? distractedVisionColor : normalVisionColor;
        visionConeRenderer.material.color = currentColor;
        
        Vector3[] visionPoints = new Vector3[visionRays + 2];
        visionPoints[0] = Vector3.zero;
        
        float startAngle = -visionAngle / 2f;
        float angleStep = visionAngle / (visionRays - 1);
        
        for (int i = 0; i < visionRays; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Vector3 rayDirection = Quaternion.Euler(0, 0, Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg + currentAngle) * Vector3.right;
            
            RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, visionRange, obstacleLayer);
            
            float rayDistance = hit.collider != null ? hit.distance : visionRange;
            Vector3 rayEndPoint = rayDirection * rayDistance;
            
            visionPoints[i + 1] = rayEndPoint;
        }
        
        visionPoints[visionRays + 1] = Vector3.zero;
        
        visionConeRenderer.positionCount = visionPoints.Length;
        visionConeRenderer.SetPositions(visionPoints);
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = isDistracted ? Color.yellow : Color.red;
        Gizmos.DrawWireSphere(transform.position, visionRange);
        
        Vector3 leftBoundary = Quaternion.Euler(0, 0, visionAngle / 2) * currentDirection * visionRange;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, -visionAngle / 2) * currentDirection * visionRange;
        
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, leftBoundary);
        Gizmos.DrawRay(transform.position, rightBoundary);
        
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawWireSphere(patrolPoints[i].position, 0.3f);
                    
                    int nextIndex = (i + 1) % patrolPoints.Length;
                    if (patrolPoints[nextIndex] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
                    }
                }
            }
        }
        
        if (patrolPoints != null && patrolPoints.Length > 0 && patrolPoints[currentPatrolIndex] != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(patrolPoints[currentPatrolIndex].position, 0.5f);
        }
    }
}
using UnityEngine;
using System.Collections;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(SpriteRenderer))]
public class GuardController : MonoBehaviour
{
    public enum GuardState 
    { 
        Patrolling,
        Chasing,
        Investigating,
        Rotating,
        ReturningToSpawn
    }
    
    [Header("Estado Actual (Debug)")]
    [SerializeField] private GuardState currentState = GuardState.Patrolling;
    
    [Header("Configuración de Patrullaje")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitTimeAtPoint = 2f;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float destinationThreshold = 0.5f;
    
    [Header("Configuración de Visión")]
    [SerializeField] private float visionRange = 8f;
    [SerializeField] private float visionAngle = 90f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask playerLayer;
    
    [Header("Configuración de Persecución")]
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float arrestDistance = 1.5f;
    [SerializeField] private float maxChaseTime = 3f;
    
    [Header("Configuración de Investigación")]
    [SerializeField] private float investigateSpeed = 1.5f;
    [SerializeField] private float investigateTime = 5f;
    
    [Header("Configuración Visual")]
    [SerializeField] private Color patrolColor = Color.green;
    [SerializeField] private Color chaseColor = Color.red;
    [SerializeField] private Color investigateColor = Color.yellow;
    [SerializeField] private Color rotateColor = Color.cyan;
    [SerializeField] private Color returnColor = Color.blue;
    
    private NavMeshAgent navAgent;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private GameManager gameManager;
    private VisionCone visionCone;
    
    private int currentPatrolIndex = 0;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Vector3 originalScale;
    private float timeSinceLastSawPlayer = 0f;
    private bool isWaitingAtPoint = false;
    private bool hasReachedDestination = false;
    private Coroutine currentStateCoroutine;
    private bool hasPatrolPoints;
    private bool isDisabled = false;
    
    public float VisionRange => visionRange;
    public float VisionAngle => visionAngle;
    public GuardState CurrentState => currentState;
    
    void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        visionCone = GetComponentInChildren<VisionCone>();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        originalScale = transform.localScale;
        hasPatrolPoints = patrolPoints != null && patrolPoints.Length > 0;
        
        ConfigureNavMeshFor2D();
    }
    
    void Start()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
        
        if (visionCone != null)
        {
            visionCone.Initialize(this, obstacleLayer);
        }
        
        InitializeState();
    }
    
    void InitializeState()
    {
        if (hasPatrolPoints)
        {
            ChangeState(GuardState.Patrolling);
        }
        else
        {
            transform.rotation = spawnRotation;
            ChangeState(GuardState.Rotating);
        }
    }
    
    void Update()
    {
        if (isDisabled) return;
        
        UpdateFacingDirection();
        
        bool canSeePlayer = CanSeePlayer();
        
        if (canSeePlayer && currentState != GuardState.Chasing)
        {
            lastKnownPlayerPosition = player.position;
            ChangeState(GuardState.Chasing);
        }
        
        switch (currentState)
        {
            case GuardState.Patrolling:
                UpdatePatrolling();
                break;
            case GuardState.Chasing:
                UpdateChasing(canSeePlayer);
                break;
            case GuardState.Investigating:
                UpdateInvestigating();
                break;
            case GuardState.Rotating:
                UpdateRotating();
                break;
            case GuardState.ReturningToSpawn:
                UpdateReturningToSpawn();
                break;
        }
    }
    
    private void ConfigureNavMeshFor2D()
    {
        navAgent.updateRotation = false;
        navAgent.updateUpAxis = false;
        navAgent.speed = patrolSpeed;
        navAgent.angularSpeed = rotationSpeed;
        navAgent.acceleration = 8f;
    }
    
    private void ChangeState(GuardState newState)
    {
        if (currentState == newState || isDisabled) return;
        
        StopCurrentStateCoroutine();
        isWaitingAtPoint = false;
        
        if (newState != GuardState.Rotating)
        {
            hasReachedDestination = false;
        }
        
        currentState = newState;
        
        switch (newState)
        {
            case GuardState.Patrolling:
                StartPatrolling();
                break;
            case GuardState.Chasing:
                StartChasing();
                break;
            case GuardState.Investigating:
                StartInvestigating();
                break;
            case GuardState.Rotating:
                StartRotating();
                break;
            case GuardState.ReturningToSpawn:
                StartReturningToSpawn();
                break;
        }
        
        UpdateVisuals();
    }
    
    private void StopCurrentStateCoroutine()
    {
        if (currentStateCoroutine != null)
        {
            StopCoroutine(currentStateCoroutine);
            currentStateCoroutine = null;
        }
    }
    
    private void StartPatrolling()
    {
        if (!hasPatrolPoints)
        {
            ChangeState(GuardState.Rotating);
            return;
        }
        
        navAgent.speed = patrolSpeed;
        navAgent.isStopped = false;
        hasReachedDestination = false;

        if (currentPatrolIndex < 0 || currentPatrolIndex >= patrolPoints.Length)
        {
            FindClosestPatrolPoint();
        }

        MoveToPatrolPoint();
    }
    
    private void UpdatePatrolling()
    {
        if (isWaitingAtPoint) return;
        
        if (!navAgent.pathPending && navAgent.remainingDistance < destinationThreshold)
        {
            if (!hasReachedDestination)
            {
                hasReachedDestination = true;
                currentStateCoroutine = StartCoroutine(WaitAtPatrolPoint());
            }
        }
    }
    
    private IEnumerator WaitAtPatrolPoint()
    {
        isWaitingAtPoint = true;
        navAgent.isStopped = true;
        
        yield return new WaitForSeconds(waitTimeAtPoint);
        
        isWaitingAtPoint = false;
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        
        ChangeState(GuardState.Rotating);
    }
    
    private void MoveToPatrolPoint()
    {
        if (!hasPatrolPoints) return;
        
        Transform targetPoint = patrolPoints[currentPatrolIndex];
        if (targetPoint != null)
        {
            navAgent.SetDestination(targetPoint.position);
            navAgent.isStopped = false;
        }
    }
    
    private void FindClosestPatrolPoint()
    {
        if (!hasPatrolPoints) return;
        
        float minDistance = float.MaxValue;
        
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;
            
            float distance = Vector3.Distance(transform.position, patrolPoints[i].position);
            if (distance < minDistance)
            {
                minDistance = distance;
                currentPatrolIndex = i;
            }
        }
    }
    
    private void StartRotating()
    {
        navAgent.isStopped = true;
        navAgent.velocity = Vector3.zero;
        
        if (hasPatrolPoints && patrolPoints[currentPatrolIndex] != null)
        {
            currentStateCoroutine = StartCoroutine(RotateTowardsTarget(patrolPoints[currentPatrolIndex].position, GuardState.Patrolling));
        }
        else
        {
            currentStateCoroutine = StartCoroutine(StationaryGuard());
        }
    }
    
    private void UpdateRotating()
    {
    }
    
    private IEnumerator RotateTowardsTarget(Vector3 targetPosition, GuardState nextState)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        
        yield return StartCoroutine(RotateToAngle(targetAngle));
        
        ChangeState(nextState);
    }
    
    private IEnumerator StationaryGuard()
    {
        transform.rotation = spawnRotation;
        navAgent.isStopped = true;
        navAgent.velocity = Vector3.zero;
        
        while (!isDisabled)
        {
            yield return null;
        }
    }
    
    private IEnumerator RotateToAngle(float targetAngle)
    {
        float startAngle = NormalizeAngle(transform.eulerAngles.z);
        targetAngle = NormalizeAngle(targetAngle);
        float angleDifference = Mathf.DeltaAngle(startAngle, targetAngle);
        
        if (Mathf.Abs(angleDifference) < 5f) yield break;
        
        float rotationTime = Mathf.Abs(angleDifference) / rotationSpeed;
        float elapsedTime = 0f;
        
        while (elapsedTime < rotationTime && !isDisabled)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / rotationTime;
            float currentAngle = Mathf.LerpAngle(startAngle, targetAngle, t);
            transform.rotation = Quaternion.Euler(0, 0, currentAngle);
            yield return null;
        }
        
        transform.rotation = Quaternion.Euler(0, 0, targetAngle);
    }
    
    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
    
    private void StartChasing()
    {
        navAgent.speed = chaseSpeed;
        navAgent.isStopped = false;
        timeSinceLastSawPlayer = 0f;
    }
    
    private void UpdateChasing(bool canSeePlayer)
    {
        if (player == null)
        {
            ReturnToNormalState();
            return;
        }
        
        if (canSeePlayer)
        {
            lastKnownPlayerPosition = player.position;
            navAgent.SetDestination(player.position);
            timeSinceLastSawPlayer = 0f;
        }
        else
        {
            timeSinceLastSawPlayer += Time.deltaTime;
            
            if (timeSinceLastSawPlayer >= maxChaseTime)
            {
                ChangeState(GuardState.Investigating);
            }
            else
            {
                navAgent.SetDestination(lastKnownPlayerPosition);
            }
        }
    }
    
    private void ArrestPlayer()
    {
        if (gameManager != null)
        {
            gameManager.OnPlayerArrested();
        }
    }
    
    private void StartInvestigating()
    {
        navAgent.speed = investigateSpeed;
        navAgent.isStopped = false;
        navAgent.SetDestination(lastKnownPlayerPosition);
        currentStateCoroutine = StartCoroutine(InvestigateArea());
    }
    
    private void UpdateInvestigating()
    {
    }
    
    private IEnumerator InvestigateArea()
    {
        while ((navAgent.pathPending || navAgent.remainingDistance > destinationThreshold) && !isDisabled)
        {
            yield return null;
        }
        
        float investigateTimer = 0f;
        float lookAroundInterval = 1f;
        int lookDirection = 0;
        
        while (investigateTimer < investigateTime && !isDisabled)
        {
            investigateTimer += Time.deltaTime;
            
            if ((int)(investigateTimer / lookAroundInterval) > lookDirection)
            {
                lookDirection++;
                float randomAngle = Random.Range(0f, 360f);
                transform.rotation = Quaternion.Euler(0, 0, randomAngle);
            }
            
            if (CanSeePlayer())
            {
                lastKnownPlayerPosition = player.position;
                ChangeState(GuardState.Chasing);
                yield break;
            }
            
            yield return null;
        }
        
        ChangeState(GuardState.ReturningToSpawn);
    }
    
    private void StartReturningToSpawn()
    {
        navAgent.speed = patrolSpeed;
        navAgent.isStopped = false;
        navAgent.SetDestination(spawnPosition);
    }
    
    private void UpdateReturningToSpawn()
    {
        if (!navAgent.pathPending && navAgent.remainingDistance < destinationThreshold)
        {
            ReturnToNormalState();
        }
    }
    
    private void ReturnToNormalState()
    {
        if (hasPatrolPoints)
        {
            FindClosestPatrolPoint();
            ChangeState(GuardState.Patrolling);
        }
        else
        {
            ChangeState(GuardState.Rotating);
        }
    }
    
    public void ResetToSpawn()
    {
        isDisabled = false;
        StopCurrentStateCoroutine();
        
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        transform.localScale = originalScale;
        navAgent.Warp(spawnPosition);
        
        currentPatrolIndex = 0;
        lastKnownPlayerPosition = Vector3.zero;
        timeSinceLastSawPlayer = 0f;
        isWaitingAtPoint = false;
        hasReachedDestination = false;
        
        InitializeState();
    }
    
    public void DisableGuard()
    {
        isDisabled = true;
        StopCurrentStateCoroutine();
        navAgent.isStopped = true;
        navAgent.velocity = Vector3.zero;
    }
    
    private bool CanSeePlayer()
    {
        if (player == null) return false;
        
        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;
        
        if (distanceToPlayer > visionRange) return false;
        
        if (Vector3.Angle(GetFacingDirection(), directionToPlayer) > visionAngle / 2f) return false;
        
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer.normalized, distanceToPlayer, obstacleLayer | playerLayer);
        
        if (hit.collider != null)
        {
            return hit.collider.CompareTag("Player");
        }
        
        return false;
    }
    
    public Vector3 GetFacingDirection()
    {
        if (hasPatrolPoints && navAgent.velocity.magnitude > 0.1f)
        {
            return navAgent.velocity.normalized;
        }
        float angle = (transform.eulerAngles.z + 90) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
    }
    
    private void UpdateFacingDirection()
    {
        if (hasPatrolPoints && navAgent.velocity.magnitude > 0.1f && currentState != GuardState.Rotating)
        {
            Vector3 direction = navAgent.velocity.normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        else if (!hasPatrolPoints && currentState == GuardState.Rotating)
        {
            transform.rotation = spawnRotation;
        }
    }
    
    public void OnSoundHeard(Vector3 soundPosition)
    {
        if (currentState == GuardState.Chasing || isDisabled) return;
        
        lastKnownPlayerPosition = soundPosition;
        ChangeState(GuardState.Investigating);
    }
    
    private void UpdateVisuals()
    {
        Color targetColor = patrolColor;
        switch (currentState)
        {
            case GuardState.Patrolling: targetColor = patrolColor; break;
            case GuardState.Chasing: targetColor = chaseColor; break;
            case GuardState.Investigating: targetColor = investigateColor; break;
            case GuardState.Rotating: targetColor = rotateColor; break;
            case GuardState.ReturningToSpawn: targetColor = returnColor; break;
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = targetColor;
        }
        
        if (visionCone != null)
        {
            visionCone.SetVisible(true);
            visionCone.UpdateColor(currentState);
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isDisabled)
        {
            DisableGuard();
            ArrestPlayer();
        }
    }
    
    void OnDrawGizmos()
    {
        if (hasPatrolPoints && patrolPoints.Length > 0)
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
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(spawnPosition, 0.5f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, arrestDistance);
    }
}
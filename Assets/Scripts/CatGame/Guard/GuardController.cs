using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class GuardController : MonoBehaviour
{
    public enum GuardState 
    { 
        Patrolling,
        Chasing,
        Investigating,
        ReturningToSpawn,
        Stunned,
        Searching,
        Flanking,
        Ambushing
    }
    
    [Header("Estado Actual (Debug)")]
    [SerializeField] private GuardState currentState = GuardState.Patrolling;
    
    [Header("Configuración de Patrullaje")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitTimeAtPoint = 2f;
    [SerializeField] private float destinationThreshold = 0.5f;
    
    [Header("Configuración de Visión")]
    [SerializeField] private float visionRange = 8f;
    [SerializeField] private float visionAngle = 90f;
    [SerializeField] private float visionTurnSpeed = 10f; 
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask playerLayer;
    
    [Header("Configuración de Persecución")]
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float arrestDistance = 1.5f;
    [SerializeField] private float maxChaseTime = 8f;
    
    [Header("Configuración de Investigación")]
    [SerializeField] private float investigateSpeed = 1.5f;
    [SerializeField] private float investigateTime = 10f;
    
    [Header("IA Avanzada")]
    [SerializeField] private float memoryDuration = 30f;
    [SerializeField] private float searchRadius = 5f;
    [SerializeField] private float flankingDistance = 4f;
    [SerializeField] private float predictionTime = 2f;
    [SerializeField] private float losePursuitDistance = 15f;
    [SerializeField] private int maxSearchPoints = 8;
    [SerializeField] private float ambushWaitTime = 5f;

    [Header("IA Avanzada - NavMesh Tuning")]
    [SerializeField] private float patrolAcceleration = 8f;
    [SerializeField] private float chaseAcceleration = 20f;
    [SerializeField] private float angularSpeed = 720f;
    
    private NavMeshAgent navAgent;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private GameManager gameManager;
    private VisionCone visionCone;
    private Animator animator;
    
    private Vector3 lastFacingDirection = Vector3.down;
    private int currentPatrolIndex = 0;
    private Vector3 spawnPosition;
    private Vector3 originalScale;
    private float timeSinceLastSawPlayer = 0f;
    private bool isWaitingAtPoint = false;
    private bool hasReachedDestination = false;
    private Coroutine currentStateCoroutine;
    private bool hasPatrolPoints;
    private bool isDisabled = false;
    private bool isStunned = false;
    private float stunEndTime = 0f;
    private Coroutine stunCoroutine;
    
    private Queue<Vector3> playerPositionHistory = new Queue<Vector3>();
    private List<Vector3> searchPoints = new List<Vector3>();
    private Vector3 lastKnownPlayerPosition;
    private Vector3 predictedPlayerPosition;
    private float lastPlayerSightTime = 0f;
    private bool isPlayerInSight = false;
    private Vector3 playerLastDirection;
    private float playerSpeed;
    private int currentSearchIndex = 0;
    private bool hasSearchedAllPoints = false;
    private Vector3 flankingTarget;
    private bool isExecutingFlanking = false;
    
    public float VisionRange => visionRange;
    public float VisionAngle => visionAngle;
    public GuardState CurrentState => currentState;
    
    void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        visionCone = GetComponentInChildren<VisionCone>();
        animator = GetComponent<Animator>();
        
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
        
        spawnPosition = transform.position;
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
        ChangeState(GuardState.Patrolling);
    }
    
    void Update()
    {
        if (isDisabled || isStunned) 
        {
            UpdateAnimator();
            return;
        }
        
        UpdatePlayerTracking();
        bool canSeePlayer = CanSeePlayer();
        
        UpdateVisionDirection(canSeePlayer);
        UpdateAnimator();
        
        if (canSeePlayer && currentState != GuardState.Chasing && currentState != GuardState.Flanking)
        {
            StartChaseSequence();
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
                break;
            case GuardState.Searching:
                UpdateSearching();
                break;
            case GuardState.Flanking:
                UpdateFlanking(canSeePlayer);
                break;
            case GuardState.Ambushing:
                UpdateAmbushing(canSeePlayer);
                break;
            case GuardState.ReturningToSpawn:
                UpdateReturningToSpawn();
                break;
            case GuardState.Stunned:
                break;
        }
    }
    
    private void ConfigureNavMeshFor2D()
    {
        navAgent.updateRotation = false;
        navAgent.updateUpAxis = false;
        navAgent.angularSpeed = angularSpeed;
    }
    
    private void UpdateVisionDirection(bool canSeePlayer)
    {
        if ((currentState == GuardState.Chasing || currentState == GuardState.Flanking) && canSeePlayer && player != null)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            lastFacingDirection = Vector3.Slerp(lastFacingDirection, directionToPlayer, Time.deltaTime * visionTurnSpeed);
        }
        else if (navAgent.velocity.magnitude > 0.1f)
        {
            lastFacingDirection = navAgent.velocity.normalized;
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        Vector3 velocity = navAgent.velocity;
        float currentSpeed = velocity.magnitude;
        float speedValue = 0f; 

        if (currentSpeed > 0.1f)
        {
            if (currentState == GuardState.Patrolling || 
                currentState == GuardState.Investigating || 
                currentState == GuardState.ReturningToSpawn ||
                currentState == GuardState.Searching)
            {
                speedValue = 1f; 
            }
            else if (currentState == GuardState.Chasing ||
                     currentState == GuardState.Flanking)
            {
                speedValue = 2f; 
            }
        }
        
        animator.SetFloat("Speed", speedValue);

        Vector3 animationDirection;
        if (currentSpeed > 0.1f)
        {
            animationDirection = velocity.normalized;
        }
        else
        {
            animationDirection = lastFacingDirection;
        }
        
        if (Mathf.Abs(animationDirection.x) > Mathf.Abs(animationDirection.y))
        {
            animator.SetFloat("MoveX", Mathf.Sign(animationDirection.x));
            animator.SetFloat("MoveY", 0);
        }
        else
        {
            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveY", Mathf.Sign(animationDirection.y));
        }
    }
    
    private void UpdatePlayerTracking()
    {
        if (player == null) return;
        
        if (isPlayerInSight)
        {
            playerPositionHistory.Enqueue(player.position);
            if (playerPositionHistory.Count > 10)
            {
                playerPositionHistory.Dequeue();
            }
            
            if (playerPositionHistory.Count >= 2)
            {
                Vector3[] positions = new Vector3[playerPositionHistory.Count];
                playerPositionHistory.CopyTo(positions, 0);
                
                Vector3 oldPos = positions[positions.Length - 2];
                Vector3 newPos = positions[positions.Length - 1];
                
                playerLastDirection = (newPos - oldPos).normalized;
                playerSpeed = Vector3.Distance(oldPos, newPos) / Time.deltaTime;
                
                predictedPlayerPosition = newPos + (playerLastDirection * playerSpeed * predictionTime);
            }
            
            lastKnownPlayerPosition = player.position;
            lastPlayerSightTime = Time.time;
        }
        
        timeSinceLastSawPlayer = Time.time - lastPlayerSightTime;
    }
    
    private void StartChaseSequence()
    {
        lastKnownPlayerPosition = player.position;
        
        // FMOD: Reproducir sonido de alerta al detectar jugador
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer > flankingDistance && CanFlankPlayer())
        {
            ChangeState(GuardState.Flanking);
        }
        else
        {
            ChangeState(GuardState.Chasing);
        }
    }
    
    private bool CanFlankPlayer()
    {
        if (player == null) return false;
        
        Vector3 toPlayer = (player.position - transform.position).normalized;
        Vector3 leftFlank = Vector3.Cross(Vector3.forward, toPlayer).normalized * flankingDistance;
        Vector3 rightFlank = -leftFlank;
        
        Vector3 leftFlankPos = player.position + leftFlank;
        Vector3 rightFlankPos = player.position + rightFlank;
        
        NavMeshPath leftPath = new NavMeshPath();
        NavMeshPath rightPath = new NavMeshPath();
        
        navAgent.CalculatePath(leftFlankPos, leftPath);
        navAgent.CalculatePath(rightFlankPos, rightPath);
        
        return leftPath.status == NavMeshPathStatus.PathComplete || 
               rightPath.status == NavMeshPathStatus.PathComplete;
    }
    
    private void UpdateChasing(bool canSeePlayer)
    {
        if (player == null)
        {
            ReturnToNormalState();
            return;
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer > losePursuitDistance)
        {
            GenerateSearchPoints();
            ChangeState(GuardState.Searching);
            return;
        }
        
        if (canSeePlayer)
        {
            Vector3 targetPosition = predictedPlayerPosition != Vector3.zero ? 
                                   predictedPlayerPosition : player.position;
            navAgent.SetDestination(targetPosition);
            timeSinceLastSawPlayer = 0f;
        }
        else
        {
            if (timeSinceLastSawPlayer >= maxChaseTime)
            {
                GenerateSearchPoints();
                ChangeState(GuardState.Searching);
            }
            else
            {
                navAgent.SetDestination(lastKnownPlayerPosition);
                
                if (Vector3.Distance(transform.position, lastKnownPlayerPosition) < 2f)
                {
                    if (CanFlankPlayer())
                    {
                        ChangeState(GuardState.Flanking);
                    }
                }
            }
        }
    }
    
    private void GenerateSearchPoints()
    {
        searchPoints.Clear();
        currentSearchIndex = 0;
        hasSearchedAllPoints = false;
        
        for (int i = 0; i < maxSearchPoints; i++)
        {
            float angle = (360f / maxSearchPoints) * i * Mathf.Deg2Rad;
            Vector3 searchPoint = lastKnownPlayerPosition + new Vector3(
                Mathf.Cos(angle) * searchRadius,
                Mathf.Sin(angle) * searchRadius,
                0
            );
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(searchPoint, out hit, searchRadius, NavMesh.AllAreas))
            {
                searchPoints.Add(hit.position);
            }
        }
        
        searchPoints.Sort((a, b) => 
            Vector3.Distance(transform.position, a).CompareTo(Vector3.Distance(transform.position, b)));
    }
    
    private void UpdateSearching()
    {
        if (searchPoints.Count == 0 || hasSearchedAllPoints)
        {
            ChangeState(GuardState.Investigating);
            return;
        }
        
        if (!navAgent.pathPending && navAgent.remainingDistance < destinationThreshold)
        {
            currentSearchIndex++;
            if (currentSearchIndex >= searchPoints.Count)
            {
                hasSearchedAllPoints = true;
                return;
            }
            
            navAgent.SetDestination(searchPoints[currentSearchIndex]);
        }
        
        if (CanSeePlayer())
        {
            StartChaseSequence();
        }
    }
    
    private void UpdateFlanking(bool canSeePlayer)
    {
        if (player == null)
        {
            ChangeState(GuardState.Searching);
            return;
        }
        
        if (canSeePlayer)
        {
            ChangeState(GuardState.Chasing);
            return;
        }
        
        if (!isExecutingFlanking)
        {
            Vector3 toPlayer = (lastKnownPlayerPosition - transform.position).normalized;
            Vector3 flankDirection = Vector3.Cross(Vector3.forward, toPlayer).normalized;
            
            if (Random.Range(0f, 1f) > 0.5f)
                flankDirection = -flankDirection;
            
            flankingTarget = lastKnownPlayerPosition + (flankDirection * flankingDistance);
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(flankingTarget, out hit, 2f, NavMesh.AllAreas))
            {
                flankingTarget = hit.position;
                navAgent.SetDestination(flankingTarget);
                isExecutingFlanking = true;
            }
            else
            {
                ChangeState(GuardState.Chasing);
            }
        }
        
        if (isExecutingFlanking && !navAgent.pathPending && navAgent.remainingDistance < destinationThreshold)
        {
            ChangeState(GuardState.Ambushing);
        }
        
        if (timeSinceLastSawPlayer > 5f)
        {
            GenerateSearchPoints();
            ChangeState(GuardState.Searching);
        }
    }
    
    private void UpdateAmbushing(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            ChangeState(GuardState.Chasing);
            return;
        }
        
        if (timeSinceLastSawPlayer > ambushWaitTime)
        {
            GenerateSearchPoints();
            ChangeState(GuardState.Searching);
        }
    }
    
    private void ChangeState(GuardState newState)
    {
        if (currentState == newState || isDisabled) return;
        
        StopCurrentStateCoroutine();
        isWaitingAtPoint = false;
        hasReachedDestination = false;
        isExecutingFlanking = false;
        
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
            case GuardState.Searching:
                StartSearching();
                break;
            case GuardState.Flanking:
                StartFlanking();
                break;
            case GuardState.Ambushing:
                StartAmbushing();
                break;
            case GuardState.ReturningToSpawn:
                StartReturningToSpawn();
                break;
            case GuardState.Stunned:
                StartStunned();
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
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;
            return;
        }
        
        navAgent.speed = patrolSpeed;
        navAgent.acceleration = patrolAcceleration;
        navAgent.autoBraking = true;
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
        if (isWaitingAtPoint || !hasPatrolPoints) return;
        
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
        
        ChangeState(GuardState.Patrolling);
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
    
    private void StartChasing()
    {
        navAgent.speed = chaseSpeed;
        navAgent.acceleration = chaseAcceleration;
        navAgent.autoBraking = false;
        navAgent.isStopped = false;
        timeSinceLastSawPlayer = 0f;
    }
    
    private void StartSearching()
    {
        navAgent.speed = investigateSpeed * 1.2f;
        navAgent.acceleration = patrolAcceleration;
        navAgent.autoBraking = true;
        navAgent.isStopped = false;
        
        if (searchPoints.Count > 0)
        {
            navAgent.SetDestination(searchPoints[0]);
        }
    }
    
    private void StartFlanking()
    {
        navAgent.speed = chaseSpeed * 0.8f;
        navAgent.acceleration = chaseAcceleration;
        navAgent.autoBraking = false;
        navAgent.isStopped = false;
    }
    
    private void StartAmbushing()
    {
        navAgent.isStopped = true;
        navAgent.velocity = Vector3.zero;
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
        navAgent.acceleration = patrolAcceleration;
        navAgent.autoBraking = true;
        navAgent.isStopped = false;
        navAgent.SetDestination(lastKnownPlayerPosition);
        currentStateCoroutine = StartCoroutine(InvestigateArea());
    }
    
    private IEnumerator InvestigateArea()
    {
        while ((navAgent.pathPending || navAgent.remainingDistance > destinationThreshold) && !isDisabled)
        {
            yield return null;
        }
        
        navAgent.isStopped = true;
        navAgent.velocity = Vector3.zero;

        float investigateTimer = 0f;
        float lookAroundInterval = 1.5f;
        int lookDirection = 0;
        
        Vector3 initialLookDirection = lastFacingDirection;

        while (investigateTimer < investigateTime && !isDisabled)
        {
            investigateTimer += Time.deltaTime;
            
            if ((int)(investigateTimer / lookAroundInterval) > lookDirection)
            {
                lookDirection++;
                float scanAngle = 0f;
                switch (lookDirection % 3)
                {
                    case 1: 
                        scanAngle = visionAngle; 
                        break;
                    case 2: 
                        scanAngle = -visionAngle;
                        break;
                    case 0: 
                        scanAngle = 0f;
                        break;
                }
                lastFacingDirection = Quaternion.Euler(0, 0, scanAngle) * initialLookDirection;
            }
            
            if (CanSeePlayer())
            {
                lastKnownPlayerPosition = player.position;
                ChangeState(GuardState.Chasing);
                yield break;
            }
            
            yield return null;
        }
        
        lastFacingDirection = initialLookDirection;
        ChangeState(GuardState.ReturningToSpawn);
    }
    
    private void StartReturningToSpawn()
    {
        navAgent.speed = patrolSpeed;
        navAgent.acceleration = patrolAcceleration;
        navAgent.autoBraking = true;
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
    
    private void StartStunned()
    {
        navAgent.isStopped = true;
        navAgent.velocity = Vector3.zero;
    }
    
    private void ReturnToNormalState()
    {
        ChangeState(GuardState.Patrolling);
    }
    
    public void ResetToSpawn()
    {
        isDisabled = false;
    
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
            stunCoroutine = null;
        }
        isStunned = false;
        stunEndTime = 0f;
        if(animator != null) animator.SetBool("IsStunned", false);

        StopCurrentStateCoroutine();
    
        transform.position = spawnPosition;
        transform.rotation = Quaternion.identity;
        transform.localScale = originalScale;
        navAgent.Warp(spawnPosition);
    
        currentPatrolIndex = 0;
        lastKnownPlayerPosition = Vector3.zero;
        timeSinceLastSawPlayer = 0f;
        isWaitingAtPoint = false;
        hasReachedDestination = false;
        
        playerPositionHistory.Clear();
        searchPoints.Clear();
        predictedPlayerPosition = Vector3.zero;
        isPlayerInSight = false;
        isExecutingFlanking = false;;
    
        InitializeState();
    }
    
    public void DisableGuard()
    {
        isDisabled = true;
    
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
            stunCoroutine = null;
        }
        isStunned = false;
        if(animator != null) animator.SetBool("IsStunned", false);

        StopCurrentStateCoroutine();
        navAgent.isStopped = true;
        navAgent.velocity = Vector3.zero;
    }
    
    private bool CanSeePlayer()
    {
        if (player == null) return false;
        
        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;
        
        if (distanceToPlayer > visionRange) 
        {
            isPlayerInSight = false;
            return false;
        }
        
        if (Vector3.Angle(GetFacingDirection(), directionToPlayer) > visionAngle / 2f) 
        {
            isPlayerInSight = false;
            return false;
        }
        
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer.normalized, distanceToPlayer, obstacleLayer | playerLayer);
        
        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            isPlayerInSight = true;
            return true;
        }
        
        isPlayerInSight = false;
        return false;
    }
    
    public Vector3 GetFacingDirection()
    {
        return lastFacingDirection;
    }
    
    public void OnSoundHeard(Vector3 soundPosition)
    {
        if (currentState == GuardState.Chasing || isDisabled) return;
        
        // FMOD: Reproducir sonido de "¿Qué fue eso?"
        
        lastKnownPlayerPosition = soundPosition;
        ChangeState(GuardState.Investigating);
    }
    
    private void UpdateVisuals()
    {
        if (isStunned) return;
    
        if (visionCone != null)
        {
            visionCone.SetVisible(!isStunned);
            if (!isStunned)
            {
                visionCone.UpdateColor(currentState);
            }
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
        
        if (searchPoints != null && searchPoints.Count > 0)
        {
            Gizmos.color = Color.black;
            foreach (Vector3 searchPoint in searchPoints)
            {
                Gizmos.DrawWireSphere(searchPoint, 0.2f);
            }
        }
        
        if (predictedPlayerPosition != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(predictedPlayerPosition, 0.3f);
        }
        
        if (isExecutingFlanking)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(flankingTarget, 0.4f);
            Gizmos.DrawLine(transform.position, flankingTarget);
        }
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(spawnPosition, 0.5f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, arrestDistance);
    }

    public bool IsStunned()
    {
        return isStunned;
    }

    public void ApplyStun(float duration)
    {
        if (isDisabled || isStunned) return;
        
        Debug.Log($"Guardia {name} stuneado por {duration} segundos");
        
        // FMOD: Reproducir sonido de stun/aturdimiento
        
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
        }
        
        stunCoroutine = StartCoroutine(StunCoroutine(duration));
    }

    private IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;
        if(animator != null) animator.SetBool("IsStunned", true); 
        stunEndTime = Time.time + duration;
        
        GuardState previousState = currentState;
        ChangeState(GuardState.Stunned);
        
        navAgent.isStopped = true;
        navAgent.velocity = Vector3.zero;
        
        StartCoroutine(StunVisualEffect());
        
        yield return new WaitForSeconds(duration);
        
        isStunned = false;
        if(animator != null) animator.SetBool("IsStunned", false); 
        
        ReturnToNormalState();
        
        Debug.Log($"Guardia {name} se ha recuperado del stun");
    }

    private IEnumerator StunVisualEffect()
    {
        float blinkInterval = 0.3f;
        float elapsed = 0f;
        float stunDuration = stunEndTime - Time.time;
        
        while (elapsed < stunDuration && isStunned)
        {
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }
    }
}
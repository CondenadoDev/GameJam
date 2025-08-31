using UnityEngine;
using System.Collections;
using UnityEngine.AI;

/// <summary>
/// Controlador principal del comportamiento del guardia.
/// Maneja patrullaje, persecución, investigación y detección del jugador.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(SpriteRenderer))]
public class GuardController : MonoBehaviour
{
    // ====== ENUMS Y ESTADOS ======
    public enum GuardState 
    { 
        Patrolling,     // Patrullando entre puntos
        Chasing,        // Persiguiendo al jugador
        Investigating,  // Investigando un sonido o última posición conocida
        Rotating        // Rotando hacia el siguiente punto de patrulla
    }
    
    [Header("Estado Actual (Debug)")]
    [SerializeField] private GuardState currentState = GuardState.Patrolling;
    
    // ====== CONFIGURACIÓN DE PATRULLAJE ======
    [Header(" Configuración de Patrullaje")]
    [Tooltip("Puntos de patrullaje del guardia")]
    [SerializeField] private Transform[] patrolPoints;
    [Tooltip("Velocidad de movimiento normal")]
    [SerializeField] private float patrolSpeed = 2f;
    [Tooltip("Tiempo de espera en cada punto")]
    [SerializeField] private float waitTimeAtPoint = 2f;
    [Tooltip("Velocidad de rotación (grados/segundo)")]
    [SerializeField] private float rotationSpeed = 90f;
    [Tooltip("Umbral para considerar que llegó al destino")]
    [SerializeField] private float destinationThreshold = 0.5f;
    
    // ====== CONFIGURACIÓN DE VISIÓN ======
    [Header("Configuración de Visión")]
    [Tooltip("Distancia máxima de visión")]
    [SerializeField] private float visionRange = 8f;
    [Tooltip("Ángulo del cono de visión")]
    [SerializeField] private float visionAngle = 90f;
    [Tooltip("Capas que bloquean la visión")]
    [SerializeField] private LayerMask obstacleLayer;
    [Tooltip("Capa del jugador")]
    [SerializeField] private LayerMask playerLayer;
    
    // ====== CONFIGURACIÓN DE PERSECUCIÓN ======
    [Header("Configuración de Persecución")]
    [Tooltip("Velocidad al perseguir")]
    [SerializeField] private float chaseSpeed = 4f;
    [Tooltip("Distancia para arrestar al jugador")]
    [SerializeField] private float arrestDistance = 1.5f;
    [Tooltip("Tiempo máximo persiguiendo sin ver al jugador")]
    [SerializeField] private float maxChaseTime = 3f;
    
    // ====== CONFIGURACIÓN DE INVESTIGACIÓN ======
    [Header("Configuración de Investigación")]
    [Tooltip("Velocidad al investigar")]
    [SerializeField] private float investigateSpeed = 1.5f;
    [Tooltip("Tiempo investigando una posición")]
    [SerializeField] private float investigateTime = 5f;
    
    // ====== CONFIGURACIÓN VISUAL ======
    [Header("Configuración Visual")]
    [SerializeField] private Color patrolColor = Color.green;
    [SerializeField] private Color chaseColor = Color.red;
    [SerializeField] private Color investigateColor = Color.yellow;
    [SerializeField] private Color rotateColor = Color.cyan;
    
    // ====== REFERENCIAS ======
    private NavMeshAgent navAgent;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private GameManager gameManager;
    private VisionCone visionCone;
    
    // ====== VARIABLES DE CONTROL ======
    private int currentPatrolIndex = 0;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 initialPosition;
    private float timeSinceLastSawPlayer = 0f;
    private bool isWaitingAtPoint = false;
    private bool hasReachedDestination = false;
    private Coroutine currentStateCoroutine;
    
    // ====== PROPIEDADES PÚBLICAS ======
    public float VisionRange => visionRange;
    public float VisionAngle => visionAngle;
    public GuardState CurrentState => currentState;
    
    // ====== INICIALIZACIÓN ======
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
        else
        {
            Debug.LogWarning(" No se encontró el jugador con tag 'Player'");
        }
        
        gameManager = GameManager.Instance;
        initialPosition = transform.position;
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
        
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogError("No hay puntos de patrullaje asignados!");
            enabled = false;
            return;
        }
        
        ChangeState(GuardState.Patrolling);
    }
    
    void Update()
    {
        // >> SFX: Aquí se podrían gestionar sonidos de pasos continuos
        // basados en la velocidad (navAgent.velocity.magnitude).
        // Unos pasos para patrullar/investigar y otros más rápidos para perseguir.

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
        }
    }
    
    // ====== CONFIGURACIÓN INICIAL ======
    private void ConfigureNavMeshFor2D()
    {
        navAgent.updateRotation = false;
        navAgent.updateUpAxis = false;
        navAgent.speed = patrolSpeed;
        navAgent.angularSpeed = rotationSpeed;
        navAgent.acceleration = 8f;
    }
    
    // ====== SISTEMA DE ESTADOS ======
    private void ChangeState(GuardState newState)
    {
        if (currentState == newState) return;
        
        Debug.Log($"Guardia cambiando de {currentState} a {newState}");
        
        StopCurrentStateCoroutine();
        isWaitingAtPoint = false;
        
        // evitar un bucle infinito entre Patrolling -> Rotating -> Patrolling.
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
    
    // ====== ESTADO: PATRULLAJE ======
    private void StartPatrolling()
    {
        navAgent.speed = patrolSpeed;
        navAgent.isStopped = false;
        hasReachedDestination = false;

        // Solo busca el punto más cercano la primera vez que patrulla.
        if (currentPatrolIndex < 0 || currentPatrolIndex >= patrolPoints.Length)
        {
            FindClosestPatrolPoint();
        }

        // Asegura que el guardia siempre tenga un destino inicial.
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
        
        Debug.Log($" Esperando en punto de patrulla {currentPatrolIndex}");
        
        yield return new WaitForSeconds(waitTimeAtPoint);
        
        isWaitingAtPoint = false;
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        
        // Cambiar a estado de rotación antes de moverse al siguiente punto.
        ChangeState(GuardState.Rotating);
    }
    
    private void MoveToPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;
        
        Transform targetPoint = patrolPoints[currentPatrolIndex];
        if (targetPoint != null)
        {
            navAgent.SetDestination(targetPoint.position);
            navAgent.isStopped = false;
            Debug.Log($"Moviendo a punto de patrulla {currentPatrolIndex}");
        }
    }
    
    private void FindClosestPatrolPoint()
    {
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
    
    // ====== ESTADO: ROTACIÓN ======
    private void StartRotating()
    {
        navAgent.isStopped = true;
        navAgent.velocity = Vector3.zero;
        currentStateCoroutine = StartCoroutine(RotateTowardsNextPoint());
    }
    
    private void UpdateRotating()
    {
        // La rotación se maneja completamente en la corrutina.
    }
    
    private IEnumerator RotateTowardsNextPoint()
    {
        Transform targetPoint = patrolPoints[currentPatrolIndex];
        Vector3 direction = (targetPoint.position - transform.position).normalized;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        
        float startAngle = NormalizeAngle(transform.eulerAngles.z);
        targetAngle = NormalizeAngle(targetAngle);
        float angleDifference = Mathf.DeltaAngle(startAngle, targetAngle);
        
        if (Mathf.Abs(angleDifference) < 5f)
        {
            ChangeState(GuardState.Patrolling);
            yield break;
        }
        
        float rotationTime = Mathf.Abs(angleDifference) / rotationSpeed;
        float elapsedTime = 0f;
        
        while (elapsedTime < rotationTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / rotationTime;
            float currentAngle = Mathf.LerpAngle(startAngle, targetAngle, t);
            transform.rotation = Quaternion.Euler(0, 0, currentAngle);
            yield return null;
        }
        
        transform.rotation = Quaternion.Euler(0, 0, targetAngle);
        ChangeState(GuardState.Patrolling);
    }
    
    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
    
    // ====== ESTADO: PERSECUCIÓN ======
    private void StartChasing()
    {
        // >> SFX: ¡Sonido de ALERTA! Un sonido icónico tipo "!" para indicar que ha visto al jugador.
        navAgent.speed = chaseSpeed;
        navAgent.isStopped = false;
        timeSinceLastSawPlayer = 0f;
        Debug.Log("¡Persiguiendo al jugador!");
    }
    
    private void UpdateChasing(bool canSeePlayer)
    {
        if (player == null)
        {
            ChangeState(GuardState.Patrolling);
            return;
        }
        
        if (canSeePlayer)
        {
            lastKnownPlayerPosition = player.position;
            navAgent.SetDestination(player.position);
            timeSinceLastSawPlayer = 0f;
            
            if (Vector3.Distance(transform.position, player.position) <= arrestDistance)
            {
                ArrestPlayer();
            }
        }
        else
        {
            timeSinceLastSawPlayer += Time.deltaTime;
            
            // Si pierde al jugador, investiga su última posición.
            if (timeSinceLastSawPlayer >= maxChaseTime)
            {
                // >> SFX: Un sonido de "confusión" o "perder el rastro".
                Debug.Log("Perdí de vista al jugador, investigando...");
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
        // >> SFX: Sonido de "captura"
        Debug.Log("⚠¡Jugador arrestado!");
        
        if (gameManager != null)
        {
            gameManager.OnPlayerArrested();
        }
        
        ResetPosition();
        ChangeState(GuardState.Patrolling);
    }
    
    // ====== ESTADO: INVESTIGACIÓN ======
    private void StartInvestigating()
    {
        navAgent.speed = investigateSpeed;
        navAgent.isStopped = false;
        navAgent.SetDestination(lastKnownPlayerPosition);
        currentStateCoroutine = StartCoroutine(InvestigateArea());
        Debug.Log($"Investigando posición: {lastKnownPlayerPosition}");
    }
    
    private void UpdateInvestigating()
    {
        // La investigación se maneja en la corrutina.
    }
    
    private IEnumerator InvestigateArea()
    {
        // Esperar a llegar al punto de investigación.
        while (navAgent.pathPending || navAgent.remainingDistance > destinationThreshold)
        {
            yield return null;
        }
        
        float investigateTimer = 0f;
        float lookAroundInterval = 1f;
        int lookDirection = 0;
        
        while (investigateTimer < investigateTime)
        {
            investigateTimer += Time.deltaTime;
            
            if ((int)(investigateTimer / lookAroundInterval) > lookDirection)
            {
                // >> SFX: Sonido sutil de "mirar alrededor" o "búsqueda".
                lookDirection++;
                float randomAngle = Random.Range(0f, 360f);
                transform.rotation = Quaternion.Euler(0, 0, randomAngle);
            }
            
            // Si ve al jugador mientras investiga, vuelve a perseguirlo.
            if (CanSeePlayer())
            {
                lastKnownPlayerPosition = player.position;
                ChangeState(GuardState.Chasing);
                yield break;
            }
            
            yield return null;
        }
        
        // >> SFX: Sonido de "no encontrar nada".
        Debug.Log("No encontré nada, volviendo a patrullar");
        ChangeState(GuardState.Patrolling);
    }
    
    // ====== SISTEMA DE VISIÓN ======
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
        // La dirección es hacia donde se mueve el NavMeshAgent.
        if (navAgent.velocity.magnitude > 0.1f)
        {
            return navAgent.velocity.normalized;
        }
        // Si está quieto, usa la rotación del transform.
        float angle = (transform.eulerAngles.z + 90) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
    }
    
    private void UpdateFacingDirection()
    {
        // Rota el sprite en la dirección del movimiento, excepto cuando está en el estado de rotación manual.
        if (navAgent.velocity.magnitude > 0.1f && currentState != GuardState.Rotating)
        {
            Vector3 direction = navAgent.velocity.normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    // ====== EVENTOS EXTERNOS ======
    public void OnSoundHeard(Vector3 soundPosition)
    {
        // No reaccionar a sonidos si ya está persiguiendo al jugador.
        if (currentState == GuardState.Chasing) return;
        
        // >> SFX: Un sonido de "¿Qué fue eso?" o un "?" auditivo.
        Debug.Log($"Escuché un sonido en {soundPosition}");
        
        lastKnownPlayerPosition = soundPosition;
        ChangeState(GuardState.Investigating);
    }
    
    // ====== UTILIDADES ======
    private void UpdateVisuals()
    {
        Color targetColor = patrolColor;
        switch (currentState)
        {
            case GuardState.Patrolling: targetColor = patrolColor; break;
            case GuardState.Chasing: targetColor = chaseColor; break;
            case GuardState.Investigating: targetColor = investigateColor; break;
            case GuardState.Rotating: targetColor = rotateColor; break;
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = targetColor;
        }
        
        // El cono de visión siempre está activo para que el jugador pueda anticipar la detección.
        if (visionCone != null)
        {
            visionCone.SetVisible(true);
            visionCone.UpdateColor(currentState);
        }
    }
    
    private void ResetPosition()
    {
        navAgent.Warp(initialPosition);
        currentPatrolIndex = 0;
        lastKnownPlayerPosition = Vector3.zero;
        timeSinceLastSawPlayer = 0f;
        isWaitingAtPoint = false;
        hasReachedDestination = false;
    }
    
    // ====== GIZMOS PARA DEBUG ======
    void OnDrawGizmos()
    {
        // Dibujar puntos y rutas de patrullaje para facilitar el diseño de niveles.
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
        
        // Dibujar rango de arresto.
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, arrestDistance);
    }
}
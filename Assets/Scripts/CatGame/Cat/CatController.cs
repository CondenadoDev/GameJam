using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class CatController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float walkSpeedMultiplier = 0.5f;

    [Header("Habilidad de Maullido")]
    [SerializeField] private float minPurringRange = 2f;
    [SerializeField] private float maxPurringRange = 10f;
    [SerializeField] private float purrChargeTime = 1.5f;
    [SerializeField] private float purringCooldown = 3f;

    [Header("Habilidad de Concentración")]
    [SerializeField] private float concentrationRange = 15f;
    [SerializeField] private bool showEnemyIndicators = true;

    [Header("Habilidad de Bolas de Pelo")]
    [SerializeField] private GameObject hairballPrefab;
    [SerializeField] private Transform hairballSpawnPoint;
    [SerializeField] private float hairballSpeed = 8f;
    [SerializeField] private float hairballCooldown = 2f;
    [SerializeField] private int maxActiveHairballs = 3;

    [Header("Efectos Visuales")]
    [SerializeField] private SpriteRenderer purrVfxSprite;
    [SerializeField] private Color purrEffectColor = new Color(1f, 1f, 0f, 0.5f);
    [SerializeField] private float purrFadeOutDuration = 0.25f;

    [Header("Animaciones")]
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction purringAction;
    private InputAction concentrationAction;
    private InputAction hairballAction;
    
    private bool isChargingPurr = false;
    private bool isConcentrating = false;
    private bool isWalking = false;
    
    private float purrHoldTime = 0f;
    private float currentPurringRange = 0f;
    private float lastPurrTime = -999f;
    private float lastHairballTime = -999f;
    private Vector2 moveInput;
    
    private int totalMeowsUsed = 0;
    private int guardsDistracted = 0;
    private int activeHairballs = 0;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        
        if (animator == null)
            animator = GetComponent<Animator>();
        
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        
        try
        {
            moveAction = playerInput.actions["Move"];
            Debug.Log("Acción 'Move' encontrada");
        }
        catch (System.Exception)
        {
            Debug.LogError("No se encontró la acción 'Move'");
        }
        
        try
        {
            purringAction = playerInput.actions["Purring"];
            Debug.Log("Acción 'Purring' encontrada");
        }
        catch (System.Exception)
        {
            Debug.LogError("No se encontró la acción 'Purring'");
        }
        
        try
        {
            concentrationAction = playerInput.actions["Concentration"];
            Debug.Log("Acción 'Concentration' encontrada");
        }
        catch (System.Exception)
        {
            Debug.LogError("No se encontró la acción 'Concentration'");
        }
        
        try
        {
            hairballAction = playerInput.actions["Hairball"];
            Debug.Log("Acción 'Hairball' encontrada");
        }
        catch (System.Exception)
        {
            Debug.LogWarning("No se encontró la acción 'Hairball' - Las bolas de pelo estarán deshabilitadas");
            hairballAction = null;
        }

        if (purrVfxSprite != null)
        {
            purrVfxSprite.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("No hay sprite asignado para el efecto visual del maullido");
        }
    }
    
    void OnEnable()
    {
        if (purringAction != null)
        {
            purringAction.performed += OnPurringPerformed;
            purringAction.canceled += OnPurringCanceled;
        }
    
        if (concentrationAction != null)
        {
            concentrationAction.performed += OnConcentrationPerformed;
            concentrationAction.canceled += OnConcentrationCanceled;
        }
    
        if (hairballAction != null)
        {
            hairballAction.performed += OnHairballPerformed;
        }
    }
    
    void OnDisable()
    {
        if (purringAction != null)
        {
            purringAction.performed -= OnPurringPerformed;
            purringAction.canceled -= OnPurringCanceled;
        }
    
        if (concentrationAction != null)
        {
            concentrationAction.performed -= OnConcentrationPerformed;
            concentrationAction.canceled -= OnConcentrationCanceled;
        }
    
        if (hairballAction != null)
        {
            hairballAction.performed -= OnHairballPerformed;
        }
    }
    
    void Update()
    {
        HandleMovementInput();
        HandlePurrCharge();
        UpdateAnimations();
        
        if (isConcentrating)
        {
            DetectNearbyEnemies();
        }
    }

    void FixedUpdate()
    {
        HandleMovement();
    }
    
    void HandleMovementInput()
    {
        moveInput = moveAction.ReadValue<Vector2>();
    
        if (moveInput.magnitude > 1f)
        {
            moveInput = moveInput.normalized;
        }
    
        isWalking = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
    }
    
    void HandleMovement()
    {
        float currentSpeed = moveSpeed * (isWalking ? walkSpeedMultiplier : 1f);
        rb.linearVelocity = moveInput * currentSpeed;
    }

    void UpdateAnimations()
    {
        if (animator == null) return;
    
        bool isMoving = moveInput.magnitude > 0.1f;
    
        if (isMoving)
        {
            animator.SetFloat("Horizontal", moveInput.x);
            animator.SetFloat("Vertical", moveInput.y);
        }
        else
        {
            animator.SetFloat("Horizontal", 0f);
            animator.SetFloat("Vertical", 0f);
        }
    
        animator.SetBool("isMoving", isMoving);
    }
    
    private void HandlePurrCharge()
    {
        if (isChargingPurr)
        {
            purrHoldTime += Time.deltaTime;
            
            float chargeRatio = Mathf.Clamp01(purrHoldTime / purrChargeTime);
            currentPurringRange = Mathf.Lerp(minPurringRange, maxPurringRange, chargeRatio);

            if (purrVfxSprite != null)
            {
                purrVfxSprite.transform.localScale = Vector3.one * currentPurringRange * 2f;
                
                Color currentColor = purrEffectColor;
                currentColor.a = Mathf.Lerp(0.2f, 0.6f, chargeRatio);
                purrVfxSprite.color = currentColor;
            }
        }
    }
    
    void OnPurringPerformed(InputAction.CallbackContext context)
    {
        if (Time.time < lastPurrTime + purringCooldown)
        {
            Debug.Log("Maullido en cooldown!");
            return;
        }

        isChargingPurr = true;
        purrHoldTime = 0f;
        
        Debug.Log("Cargando maullido...");
        
        // FMOD: Reproducir sonido de carga de maullido (loop mientras se carga)
        
        if (purrVfxSprite != null)
        {
            purrVfxSprite.gameObject.SetActive(true);
            purrVfxSprite.color = purrEffectColor;
        }
    }
    
    void OnPurringCanceled(InputAction.CallbackContext context)
    {
        if (!isChargingPurr) return;

        isChargingPurr = false;
        
        // FMOD: Detener sonido de carga y reproducir maullido según la potencia
        
        if (animator != null)
        {
            animator.SetTrigger("Meow");
        }
        
        EmitPurr();
        
        totalMeowsUsed++;
        lastPurrTime = Time.time;
        
        if (purrVfxSprite != null)
        {
            StartCoroutine(FadeOutPurrVfx());
        }
    }
    
    private void EmitPurr()
    {
        Debug.Log($"¡MIAU! Rango: {currentPurringRange:F1}m");
        
        DistractNearbyGuards(currentPurringRange);
    }
    
    private void DistractNearbyGuards(float range)
    {
        int guardsAlerted = 0;
        GuardController[] guards = FindObjectsByType<GuardController>(FindObjectsSortMode.None);
        
        foreach (GuardController guard in guards)
        {
            float distance = Vector2.Distance(transform.position, guard.transform.position);
            
            if (distance <= range)
            {
                guard.OnSoundHeard(transform.position);
                guardsAlerted++;
                Debug.Log($"Guardia distraído: {guard.name} (Distancia: {distance:F1}m)");
            }
        }
        
        if (guardsAlerted > 0)
        {
            guardsDistracted += guardsAlerted;
            Debug.Log($"{guardsAlerted} guardia(s) distraído(s)");
        }
        else
        {
            Debug.Log("Ningún guardia en rango");
        }
    }
    
    private IEnumerator FadeOutPurrVfx()
    {
        if (purrVfxSprite == null) yield break;
        
        float elapsedTime = 0f;
        Color startColor = purrVfxSprite.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (elapsedTime < purrFadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / purrFadeOutDuration;
            
            purrVfxSprite.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        purrVfxSprite.gameObject.SetActive(false);
    }

    void OnHairballPerformed(InputAction.CallbackContext context)
    {
        if (Time.time < lastHairballTime + hairballCooldown)
        {
            Debug.Log("Bolas de pelo en cooldown!");
            return;
        }
        
        if (activeHairballs >= maxActiveHairballs)
        {
            Debug.Log("Demasiadas bolas de pelo activas!");
            return;
        }
        
        DropHairball();
    }

    private void DropHairball()
    {
        if (hairballPrefab == null)
        {
            Debug.LogError("No hay prefab de bola de pelo asignado!");
            return;
        }
        
        Vector3 spawnPosition = transform.position;
        
        // FMOD: Reproducir sonido de bola de pelo siendo dejada
        
        GameObject hairballObj = Instantiate(hairballPrefab, spawnPosition, Quaternion.identity);
        Hairball hairball = hairballObj.GetComponent<Hairball>();
        
        if (hairball != null)
        {
            hairball.InitializeAsStaticTrap();
        }
        else
        {
            Debug.LogError("El prefab no tiene el componente Hairball!");
            Destroy(hairballObj);
            return;
        }
        
        activeHairballs++;
        lastHairballTime = Time.time;
        
        StartCoroutine(TrackHairball(hairballObj));
        
        Debug.Log($"¡Bola de pelo dejada en el suelo! ({activeHairballs}/{maxActiveHairballs})");
    }

    private IEnumerator TrackHairball(GameObject hairballObj)
    {
        while (hairballObj != null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        activeHairballs = Mathf.Max(0, activeHairballs - 1);
        Debug.Log($"Bola de pelo destruida. Restantes: {activeHairballs}");
    }
    
    void OnConcentrationPerformed(InputAction.CallbackContext context)
    {
        isConcentrating = true;
        Debug.Log("Concentración activada");
        
        // FMOD: Reproducir sonido ambiente de concentración (loop)
    }
    
    void OnConcentrationCanceled(InputAction.CallbackContext context)
    {
        isConcentrating = false;
        Debug.Log("Concentración desactivada");
        
        // FMOD: Detener sonido ambiente de concentración
    }
    
    void DetectNearbyEnemies()
    {
        GuardController[] guards = FindObjectsByType<GuardController>(FindObjectsSortMode.None);
        
        foreach (GuardController guard in guards)
        {
            float distance = Vector2.Distance(transform.position, guard.transform.position);
            
            if (distance <= concentrationRange)
            {
                Color stateColor = GetGuardStateColor(guard.CurrentState);
                
                if (showEnemyIndicators)
                {
                    SpriteRenderer guardSprite = guard.GetComponent<SpriteRenderer>();
                    if (guardSprite != null)
                    {
                        guardSprite.color = Color.Lerp(guardSprite.color, stateColor * 1.5f, Time.deltaTime * 2f);
                    }
                }
            }
        }
    }
    
    Color GetGuardStateColor(GuardController.GuardState state)
    {
        switch (state)
        {
            case GuardController.GuardState.Patrolling:
                return Color.green;
            case GuardController.GuardState.Investigating:
                return Color.yellow;
            case GuardController.GuardState.Chasing:
                return Color.red;
            case GuardController.GuardState.Stunned:
                return Color.magenta;
            default:
                return Color.gray;
        }
    }
    
    void OnDrawGizmos()
    {
        if (isChargingPurr)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, currentPurringRange);
        }

        if (isConcentrating)
        {
            Gizmos.color = new Color(0, 0.5f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, concentrationRange);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, minPurringRange);
        
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, maxPurringRange);
    }
}
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Controlador del jugador (gato).
/// Maneja movimiento, maullido para distracción y habilidad de concentración.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CatController : MonoBehaviour
{
    // ====== CONFIGURACIÓN DE MOVIMIENTO ======
    [Header("Configuración de Movimiento")]
    [Tooltip("Velocidad de movimiento normal")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Tooltip("Multiplicador de velocidad al caminar sigiloso")]
    [SerializeField] private float walkSpeedMultiplier = 0.5f;

    // ====== HABILIDAD DE MAULLIDO ======
    [Header("🐱 Habilidad de Maullido (Distracción)")]
    [Tooltip("Rango mínimo del maullido")]
    [SerializeField] private float minPurringRange = 2f;
    
    [Tooltip("Rango máximo del maullido (con carga completa)")]
    [SerializeField] private float maxPurringRange = 10f;
    
    [Tooltip("Tiempo para cargar el maullido al máximo")]
    [SerializeField] private float purrChargeTime = 1.5f;
    
    [Tooltip("Tiempo de enfriamiento entre maullidos")]
    [SerializeField] private float purringCooldown = 3f;

    // ====== HABILIDAD DE CONCENTRACIÓN ======
    [Header(" Habilidad de Concentración")]
    [Tooltip("Rango de detección de enemigos")]
    [SerializeField] private float concentrationRange = 15f;
    
    [Tooltip("Mostrar indicadores visuales de enemigos detectados")]
    [SerializeField] private bool showEnemyIndicators = true;

    // ====== EFECTOS VISUALES ======
    [Header("Efectos Visuales")]
    [Tooltip("Sprite para el efecto visual del maullido")]
    [SerializeField] private SpriteRenderer purrVfxSprite;
    
    [Tooltip("Color del efecto de maullido")]
    [SerializeField] private Color purrEffectColor = new Color(1f, 1f, 0f, 0.5f);
    
    [Tooltip("Duración del fade out del efecto")]
    [SerializeField] private float purrFadeOutDuration = 0.25f;

    // ====== REFERENCIAS Y VARIABLES PRIVADAS ======
    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction purringAction;
    private InputAction concentrationAction;
    
    private bool isChargingPurr = false;
    private bool isConcentrating = false;
    private bool isWalking = false;
    
    private float purrHoldTime = 0f;
    private float currentPurringRange = 0f;
    private float lastPurrTime = -999f;
    private Vector2 moveInput;
    
    private int totalMeowsUsed = 0;
    private int guardsDistracted = 0;
    
    // ====== INICIALIZACIÓN ======
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        
        rb.gravityScale = 0f; // Sin gravedad para vista top-down
        rb.freezeRotation = true; // No rotar por físicas
        
        moveAction = playerInput.actions["Move"];
        purringAction = playerInput.actions["Purring"];
        concentrationAction = playerInput.actions["Concentration"];

        if (purrVfxSprite != null)
        {
            purrVfxSprite.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning(" No hay sprite asignado para el efecto visual del maullido");
        }
    }
    
    void OnEnable()
    {
        purringAction.performed += OnPurringPerformed;
        purringAction.canceled += OnPurringCanceled;
        concentrationAction.performed += OnConcentrationPerformed;
        concentrationAction.canceled += OnConcentrationCanceled;
    }
    
    void OnDisable()
    {
        purringAction.performed -= OnPurringPerformed;
        purringAction.canceled -= OnPurringCanceled;
        concentrationAction.performed -= OnConcentrationPerformed;
        concentrationAction.canceled -= OnConcentrationCanceled;
    }
    
    void Update()
    {
        HandleMovementInput();
        HandlePurrCharge();
        
        if (isConcentrating)
        {
            DetectNearbyEnemies();
        }
        
        if (Time.time < lastPurrTime + purringCooldown)
        {
            float remainingCooldown = (lastPurrTime + purringCooldown) - Time.time;
            Debug.Log($" Cooldown del maullido: {remainingCooldown:F1}s");
        }
    }

    void FixedUpdate()
    {
        HandleMovement();
    }
    
    // ====== SISTEMA DE MOVIMIENTO ======
    void HandleMovementInput()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        
        // Detectar si está caminando sigiloso (Shift)
        isWalking = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
    }
    
    void HandleMovement()
    {
        float currentSpeed = moveSpeed * (isWalking ? walkSpeedMultiplier : 1f);
        rb.linearVelocity = moveInput * currentSpeed;
        
        // Rotar el sprite según la dirección (opcional)
        if (moveInput.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    // ====== SISTEMA DE MAULLIDO ======
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
            // >> SFX: Reproducir un sonido de "habilidad no disponible" o "error".
            Debug.Log("Maullido en cooldown!");
            return;
        }

        isChargingPurr = true;
        purrHoldTime = 0f;
        
        // >> SFX: Iniciar un sonido de carga o ronroneo que aumente de intensidad.
        Debug.Log("Cargando maullido...");
        
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
        
        // >> SFX: Detener el sonido de carga que se inició en OnPurringPerformed.
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
        PlayPurrSound();
    }
    
    private void DistractNearbyGuards(float range)
    {
        int guardsAlerted = 0;
        GuardController[] guards = FindObjectsOfType<GuardController>();
        
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
            Debug.Log(" Ningún guardia en rango");
        }
    }
    
    private void PlayPurrSound()
    {
        // >> SFX: Este es el lugar ideal para reproducir el sonido principal del maullido.
        // La fuerza o el tipo de maullido podría depender de 'currentPurringRange'.
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
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
    
    // ====== SISTEMA DE CONCENTRACIÓN ======
    void OnConcentrationPerformed(InputAction.CallbackContext context)
    {
        // >> SFX: Reproducir un sonido para indicar el inicio de la concentración.
        isConcentrating = true;
        Debug.Log("Concentración activada");
    }
    
    void OnConcentrationCanceled(InputAction.CallbackContext context)
    {
        // >> SFX: Reproducir un sonido para indicar el final de la concentración.
        isConcentrating = false;
        Debug.Log("Concentración desactivada");
    }
    
    void DetectNearbyEnemies()
    {
        GuardController[] guards = FindObjectsOfType<GuardController>();
        
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
            case GuardController.GuardState.Rotating:
                return Color.cyan;
            default:
                return Color.gray;
        }
    }
    
    // ====== DEBUG Y GIZMOS ======
    void OnDrawGizmos()
    {
        // Mostrar rango del maullido mientras se carga
        if (isChargingPurr)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, currentPurringRange);
        }

        // Mostrar rango de concentración
        if (isConcentrating)
        {
            Gizmos.color = new Color(0, 0.5f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, concentrationRange);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Mostrar rangos mínimo y máximo del maullido
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, minPurringRange);
        
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, maxPurringRange);
    }
}
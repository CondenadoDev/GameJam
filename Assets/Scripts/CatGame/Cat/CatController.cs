using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class CatController : MonoBehaviour
{
    [Header("ConfiguraciÃ³n de Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float walkSpeedMultiplier = 0.5f;

    [Header("Habilidad de Maullido")]
    [SerializeField] private float minPurringRange = 2f;
    [SerializeField] private float maxPurringRange = 10f;
    [SerializeField] private float purrChargeTime = 1.5f;
    [SerializeField] private float purringCooldown = 3f;

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
    private InputAction hairballAction;
    
    private bool isChargingPurr = false;
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
            Debug.Log("AcciÃ³n 'Move' encontrada");
        }
        catch (System.Exception)
        {
            Debug.LogError("No se encontrÃ³ la acciÃ³n 'Move'");
        }
        
        try
        {
            purringAction = playerInput.actions["Purring"];
            Debug.Log("AcciÃ³n 'Purring' encontrada");
        }
        catch (System.Exception)
        {
            Debug.LogError("No se encontrÃ³ la acciÃ³n 'Purring'");
        }
        
        try
        {
            hairballAction = playerInput.actions["Hairball"];
            Debug.Log("AcciÃ³n 'Hairball' encontrada");
        }
        catch (System.Exception)
        {
            Debug.LogWarning("No se encontrÃ³ la acciÃ³n 'Hairball' - Las bolas de pelo estarÃ¡n deshabilitadas");
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
    
        if (animator != null)
        {
            animator.SetTrigger("Meow");
        }
    
        EmitPurr();
    
        HUDManager.Instance?.StartPurringCooldown();
    
        totalMeowsUsed++;
        lastPurrTime = Time.time;
    
        if (purrVfxSprite != null)
        {
            StartCoroutine(FadeOutPurrVfx());
        }
    }
    
    private void EmitPurr()
    {
        Debug.Log($"Â¡MIAU! Rango: {currentPurringRange:F1}m");
        
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
                Debug.Log($"Guardia distraÃ­do: {guard.name} (Distancia: {distance:F1}m)");
            }
        }
        
        if (guardsAlerted > 0)
        {
            guardsDistracted += guardsAlerted;
            Debug.Log($"{guardsAlerted} guardia(s) distraÃ­do(s)");
        }
        else
        {
            Debug.Log("NingÃºn guardia en rango");
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
        
        Debug.Log($"Â¡Bola de pelo dejada en el suelo! ({activeHairballs}/{maxActiveHairballs})");
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
    
    void OnDrawGizmos()
    {
        if (isChargingPurr)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, currentPurringRange);
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
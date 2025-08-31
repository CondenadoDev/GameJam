using UnityEngine;
using System.Collections;

public class ExitPointController : MonoBehaviour
{
    [Header("Exit Settings")]
    [SerializeField] private bool requiresObjectives = true;
    [SerializeField] private float activationRange = 1.5f;
    
    [Header("Visual Effects")]
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color inactiveColor = Color.red;
    [SerializeField] private float pulseSpeed = 3f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip exitSound;
    [SerializeField] private AudioSource audioSource;
    
    private SpriteRenderer spriteRenderer;
    private Transform playerTransform;
    private GameManager gameManager;
    
    private bool isActive = false;
    private float pulseTimer = 0f;
    
    void Start()
    {
        InitializeExit();
        FindReferences();
    }
    
    void InitializeExit()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (GetComponent<AudioSource>() == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            audioSource = GetComponent<AudioSource>();
        }
        
        if (spriteRenderer == null)
        {
            Debug.LogError("ExitPoint necesita un componente SpriteRenderer");
        }
    }
    
    void FindReferences()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
    }
    
    void Update()
    {
        UpdateExitStatus();
        HandleVisualEffects();
        CheckForPlayerExit();
    }
    
    void UpdateExitStatus()
    {
        if (gameManager == null) return;
        
        bool wasActive = isActive;
        isActive = !requiresObjectives || gameManager.GetObjectivesCollected() > 0;
        
        if (!wasActive && isActive)
        {
            // >> SFX: Reproducir un sonido de "activación" o "energía" para notificar
            // al jugador que la salida ya está disponible.
            Debug.Log("Punto de salida ACTIVO - el jugador puede escapar");
        }
    }
    
    void HandleVisualEffects()
    {
        if (spriteRenderer == null) return;
        
        pulseTimer += Time.deltaTime * pulseSpeed;
        Color baseColor = isActive ? activeColor : inactiveColor;
        float pulseFactor = (Mathf.Sin(pulseTimer) + 1f) * 0.5f;
        Color currentColor = Color.Lerp(baseColor, Color.white, pulseFactor * 0.3f);
        spriteRenderer.color = currentColor;
        
        if (isActive)
        {
            // >> SFX: Aquí podría haber un sonido de "zumbido" o "hum" sutil y en bucle
            // para indicar que la salida está activa y esperando.
            float scaleFactor = 1f + (pulseFactor * 0.1f);
            transform.localScale = Vector3.one * scaleFactor;
        }
    }
    
    void CheckForPlayerExit()
    {
        if (playerTransform == null || gameManager == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        if (distanceToPlayer <= activationRange)
        {
            if (isActive)
            {
                TriggerExit();
            }
            else
            {
                ShowInactiveMessage();
            }
        }
    }
    
    void TriggerExit()
    {
        Debug.Log("Jugador llegó al punto de salida");
        
        // >> SFX: Este es el lugar principal para el sonido de éxito.
        if (audioSource != null && exitSound != null)
        {
            audioSource.PlayOneShot(exitSound);
        }
        
        if (gameManager != null)
        {
            gameManager.OnReachExit();
        }
        
        // Desactiva el objeto para que no se pueda volver a activar.
        this.enabled = false;
        
        StartCoroutine(ExitEffect());
    }
    
    void ShowInactiveMessage()
    {
        // >> SFX: Reproducir un sonido de "error" o "acceso denegado"
        // para informar al jugador que aún no puede usar la salida.
        Debug.Log("No puedes escapar sin recolectar al menos un objetivo");
    }
    
    IEnumerator ExitEffect()
    {
        Color originalColor = spriteRenderer.color;
        
        for (int i = 0; i < 5; i++)
        {
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (isActive)
            {
                TriggerExit();
            }
            else
            {
                ShowInactiveMessage();
            }
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = isActive ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, activationRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.8f);
        
        Vector3 arrowStart = transform.position + Vector3.up * 2f;
        Vector3 arrowEnd = transform.position + Vector3.up * 1f;
        Gizmos.DrawLine(arrowStart, arrowEnd);
        Vector3 arrowHeadRight = arrowEnd + new Vector3(0.3f, 0.3f, 0);
        Vector3 arrowHeadLeft = arrowEnd + new Vector3(-0.3f, 0.3f, 0);
        Gizmos.DrawLine(arrowEnd, arrowHeadRight);
        Gizmos.DrawLine(arrowEnd, arrowHeadLeft);
    }
}
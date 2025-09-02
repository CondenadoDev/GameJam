using UnityEngine;
using System.Collections;

/// <summary>
/// Bola de pelo que puede stunear guardias al impactar.
/// Se desactiva automáticamente después de un tiempo o al colisionar.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Hairball : MonoBehaviour
{
    [Header("Configuración de Proyectil")]
    [Tooltip("Velocidad de la bola de pelo")]
    [SerializeField] private float projectileSpeed = 8f;
    
    [Tooltip("Tiempo de vida del proyectil")]
    [SerializeField] private float lifetime = 3f;
    
    [Tooltip("Radio de detección para stunear guardias")]
    [SerializeField] private float stunRadius = 1.5f;
    
    [Tooltip("Duración del stun en guardias")]
    [SerializeField] private float stunDuration = 2f;
    
    [Header("Efectos Visuales")]
    [Tooltip("Sprite de la bola de pelo")]
    [SerializeField] private SpriteRenderer hairballSprite;
    
    [Tooltip("Color de la bola de pelo")]
    [SerializeField] private Color hairballColor = new Color(0.8f, 0.6f, 0.4f, 1f);
    
    [Tooltip("Velocidad de rotación visual")]
    [SerializeField] private float rotationSpeed = 360f;
    
    [Header("Capas de Colisión")]
    [Tooltip("Capas que detienen el proyectil")]
    [SerializeField] private LayerMask obstacleLayer;
    
    [Tooltip("Capas de guardias")]
    [SerializeField] private LayerMask guardLayer;
    
    private Rigidbody2D rb;
    private Collider2D col;
    private Vector2 direction;
    private bool hasLanded = false;
    private float timer = 0f;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        
        // Configurar física para proyectil 2D
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        
        // Configurar collider como trigger inicialmente
        col.isTrigger = true;
        
        if (hairballSprite == null)
        {
            hairballSprite = GetComponent<SpriteRenderer>();
        }
        
        if (hairballSprite != null)
        {
            hairballSprite.color = hairballColor;
        }
    }
    
    void Start()
    {
        // Autodestruirse después del tiempo de vida
        Destroy(gameObject, lifetime);
    }
    
    void Update()
    {
        timer += Time.deltaTime;
        
        // Rotación visual
        if (hairballSprite != null)
        {
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
        
        // Si ha aterrizado, verificar guardias cercanos
        if (hasLanded)
        {
            CheckForNearbyGuards();
        }
    }
    
    /// <summary>
    /// Inicializa el proyectil con una dirección específica
    /// </summary>
    public void Initialize(Vector2 shootDirection, float speed = -1f)
    {
        direction = shootDirection.normalized;
        
        if (speed > 0)
        {
            projectileSpeed = speed;
        }
        
        // Aplicar velocidad inicial
        rb.linearVelocity = direction * projectileSpeed;
        
        Debug.Log($"Bola de pelo lanzada en dirección: {direction}");
    }
    
    /// <summary>
    /// Maneja las colisiones del proyectil
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        // Si colisiona con un obstáculo, aterrizar
        if (IsInLayerMask(other.gameObject.layer, obstacleLayer))
        {
            LandHairball();
            return;
        }
        
        // Si colisiona directamente con un guardia, stunearlo inmediatamente
        if (IsInLayerMask(other.gameObject.layer, guardLayer))
        {
            GuardController guard = other.GetComponent<GuardController>();
            if (guard != null)
            {
                StunGuard(guard);
            }
            LandHairball();
            return;
        }
    }
    
    /// <summary>
    /// Hace que la bola de pelo aterrice y se convierta en una trampa
    /// </summary>
    private void LandHairball()
    {
        if (hasLanded) return;
        
        hasLanded = true;
        
        // Detener movimiento
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        
        // Cambiar a collider sólido para que los guardias puedan pisarla
        col.isTrigger = true; // Mantener como trigger para detectar cuando pasan por encima
        
        Debug.Log($"Bola de pelo aterrizó en: {transform.position}");
        
        // Cambiar apariencia visual para indicar que es una trampa activa
        if (hairballSprite != null)
        {
            Color landedColor = hairballColor;
            landedColor.a = 0.8f; // Ligeramente más transparente
            hairballSprite.color = landedColor;
        }
    }
    
    /// <summary>
    /// Verifica si hay guardias cerca cuando está en modo trampa
    /// </summary>
    private void CheckForNearbyGuards()
    {
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, stunRadius);
        
        foreach (Collider2D collider in nearbyColliders)
        {
            if (IsInLayerMask(collider.gameObject.layer, guardLayer))
            {
                GuardController guard = collider.GetComponent<GuardController>();
                if (guard != null && !guard.IsStunned())
                {
                    StunGuard(guard);
                    DestroyHairball();
                    break;
                }
            }
        }
    }
    
    /// <summary>
    /// Aplica el efecto de stun a un guardia
    /// </summary>
    private void StunGuard(GuardController guard)
    {
        Debug.Log($"¡Guardia {guard.name} stuneado por bola de pelo!");
        
        guard.ApplyStun(stunDuration);
        
        // Efecto visual de impacto (opcional)
        StartCoroutine(ShowStunEffect());
    }
    
    /// <summary>
    /// Destruye la bola de pelo con efecto
    /// </summary>
    private void DestroyHairball()
    {
        // Aquí podrías añadir partículas o efectos de destrucción
        Debug.Log("Bola de pelo consumida");
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Efecto visual cuando stunea un guardia
    /// </summary>
    private IEnumerator ShowStunEffect()
    {
        if (hairballSprite != null)
        {
            // Efecto de parpadeo
            for (int i = 0; i < 3; i++)
            {
                hairballSprite.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                hairballSprite.color = hairballColor;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
    
    /// <summary>
    /// Verifica si un layer está en una LayerMask
    /// </summary>
    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) > 0;
    }
    
    /// <summary>
    /// Dibuja gizmos para debug
    /// </summary>
    void OnDrawGizmos()
    {
        if (hasLanded)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stunRadius);
        }
        
        // Mostrar dirección si se está moviendo
        if (!hasLanded && rb != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 2f);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Mostrar rango de stun siempre cuando está seleccionado
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, stunRadius);
    }
}
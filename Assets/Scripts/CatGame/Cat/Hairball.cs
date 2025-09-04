using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Hairball : MonoBehaviour
{
    [Header("Configuración de Proyectil")]
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float stunRadius = 1.5f;
    [SerializeField] private float stunDuration = 2f;
    
    [Header("Efectos Visuales")]
    [SerializeField] private SpriteRenderer hairballSprite;
    [SerializeField] private Color hairballColor = new Color(0.8f, 0.6f, 0.4f, 1f);
    [SerializeField] private float rotationSpeed = 360f;
    
    [Header("Capas de Colisión")]
    [SerializeField] private LayerMask obstacleLayer = -1;
    [SerializeField] private LayerMask guardLayer = -1;
    
    private Rigidbody2D rb;
    private Collider2D col;
    private Vector2 direction;
    private bool hasLanded = false;
    private bool isStaticTrap = false;
    private float timer = 0f;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        
        col.isTrigger = true;
        
        if (hairballSprite == null)
        {
            hairballSprite = GetComponent<SpriteRenderer>();
        }
        
        if (hairballSprite != null)
        {
            hairballSprite.color = hairballColor;
        }
        
        Debug.Log($"Hairball creada - GuardLayer: {guardLayer.value}, ObstacleLayer: {obstacleLayer.value}");
    }
    
    void Start()
    {
        Destroy(gameObject, lifetime);
    }
    
    void Update()
    {
        timer += Time.deltaTime;
        
        if (hasLanded)
        {
            CheckForNearbyGuards();
        }
    }
    
    public void Initialize(Vector2 shootDirection, float speed = -1f)
    {
        direction = shootDirection.normalized;
        isStaticTrap = false;
        
        if (speed > 0)
        {
            projectileSpeed = speed;
        }
        
        rb.linearVelocity = direction * projectileSpeed;
        
        Debug.Log($"Bola de pelo lanzada en dirección: {direction}");
    }
    
    public void InitializeAsStaticTrap()
    {
        isStaticTrap = true;
        
        rb.linearVelocity = Vector2.zero;
        
        LandHairball();
        
        Debug.Log($"Bola de pelo colocada como trampa estática en: {transform.position}");
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Hairball colisionó con: {other.name} (Layer: {other.gameObject.layer})");
        
        GuardController guard = other.GetComponent<GuardController>();
        if (guard != null)
        {
            Debug.Log($"¡Guardia detectado: {guard.name}!");
            
            if (!guard.IsStunned())
            {
                StunGuard(guard);
                DestroyHairball();
            }
            else
            {
                Debug.Log("El guardia ya está stuneado");
            }
            return;
        }
        
        if (isStaticTrap)
        {
            return;
        }
        
        if (IsInLayerMask(other.gameObject.layer, obstacleLayer))
        {
            Debug.Log("Bola de pelo colisionó con obstáculo");
            LandHairball();
            return;
        }
    }
    
    private void LandHairball()
    {
        if (hasLanded) return;
        
        hasLanded = true;
        
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        
        col.isTrigger = true;
        
        Debug.Log($"Bola de pelo aterrizó en: {transform.position}");
        
        if (hairballSprite != null)
        {
            Color landedColor = hairballColor;
            landedColor.a = 0.9f;
            hairballSprite.color = landedColor;
            
            StartCoroutine(PulseTrap());
        }
    }
    
    private IEnumerator PulseTrap()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 pulseScale = originalScale * 1.05f;
        
        while (hasLanded && gameObject != null)
        {
            float elapsedTime = 0f;
            float pulseDuration = 1f;
            
            while (elapsedTime < pulseDuration && gameObject != null)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / pulseDuration;
                float pulseValue = Mathf.Sin(t * Mathf.PI);
                
                transform.localScale = Vector3.Lerp(originalScale, pulseScale, pulseValue * 0.1f);
                yield return null;
            }
            
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    private void CheckForNearbyGuards()
    {
        if (isStaticTrap) return;
        
        GuardController[] allGuards = FindObjectsByType<GuardController>(FindObjectsSortMode.None);
        
        foreach (GuardController guard in allGuards)
        {
            if (guard != null && !guard.IsStunned())
            {
                float distance = Vector2.Distance(transform.position, guard.transform.position);
                
                if (distance <= stunRadius)
                {
                    Debug.Log($"Guardia {guard.name} detectado por proximidad (distancia: {distance:F1}m)");
                    StunGuard(guard);
                    DestroyHairball();
                    break;
                }
            }
        }
    }
    
    private void StunGuard(GuardController guard)
    {
        Debug.Log($"¡Guardia {guard.name} stuneado por bola de pelo!");
        
        // FMOD: Reproducir sonido de bola de pelo impactando/stuneando
        
        guard.ApplyStun(stunDuration);
        
        StartCoroutine(ShowStunEffect());
    }
    
    private void DestroyHairball()
    {
        Debug.Log("Bola de pelo consumida");
        
        Destroy(gameObject);
    }
    
    private IEnumerator ShowStunEffect()
    {
        if (hairballSprite != null)
        {
            Color originalColor = hairballSprite.color;
            
            for (int i = 0; i < 3; i++)
            {
                hairballSprite.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                hairballSprite.color = originalColor;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
    
    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) > 0;
    }
    
    void OnDrawGizmos()
    {
        if (hasLanded || isStaticTrap)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, stunRadius);
        }
        
        if (!hasLanded && rb != null && rb.linearVelocity.magnitude > 0.1f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 2f);
        }
        
        if (isStaticTrap)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, stunRadius);
        
        if (isStaticTrap)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);
        }
    }
}
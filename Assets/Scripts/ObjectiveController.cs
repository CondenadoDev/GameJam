using UnityEngine;
using System.Collections;

public class ObjectiveController : MonoBehaviour
{
    [Header("Objective Settings")]
    [SerializeField] private string objectiveName = "Tesoro Nacional";
    [SerializeField] private bool isCollected = false;
    [SerializeField] private float collectionRange = 1f;
    
    [Header("Visual Effects")]
    [SerializeField] private float glowSpeed = 2f;
    [SerializeField] private float glowIntensity = 0.3f;
    [SerializeField] private Color originalColor = Color.yellow;
    [SerializeField] private Color glowColor = Color.white;
    [SerializeField] private GameObject collectionEffect;
    
    [Header("Audio")]
    [SerializeField] private AudioClip collectionSound;
    [SerializeField] private AudioSource audioSource;
    
    private SpriteRenderer spriteRenderer;
    private Collider2D objectiveCollider;
    private Transform playerTransform;
    
    private float glowTimer = 0f;
    private Vector3 originalScale;
    private bool isGlowing = true;
    
    void Start()
    {
        InitializeObjective();
        FindPlayer();
    }
    
    void InitializeObjective()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        objectiveCollider = GetComponent<Collider2D>();
        audioSource = GetComponent<AudioSource>();
        
        originalColor = spriteRenderer.color;
        originalScale = transform.localScale;
        
        isCollected = false;
        gameObject.SetActive(true);
        
        Debug.Log("Objetivo '" + objectiveName + "' iniciado");
    }
    
    void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }
    
    void Update()
    {
        if (isCollected) return;
        
        HandleGlowEffect();
        CheckForCollection();
    }
    
    void HandleGlowEffect()
    {
        if (!isGlowing) return;
        
        glowTimer += Time.deltaTime * glowSpeed;
        
        float glowFactor = (Mathf.Sin(glowTimer) + 1f) * 0.5f;
        Color currentColor = Color.Lerp(originalColor, glowColor, glowFactor * glowIntensity);
        spriteRenderer.color = currentColor;
        
        float scaleFactor = 1f + (glowFactor * 0.1f);
        transform.localScale = originalScale * scaleFactor;
    }
    
    void CheckForCollection()
    {
        if (playerTransform == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        if (distanceToPlayer <= collectionRange)
        {
            CollectObjective();
        }
    }
    
    void CollectObjective()
    {
        if (isCollected) return;
        
        isCollected = true;
        
        Debug.Log("Objetivo '" + objectiveName + "' recolectado");
        
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.OnObjectiveCollected();
        }
        
        PlayCollectionEffects();
        StartCoroutine(CollectionSequence());
    }
    
    void PlayCollectionEffects()
    {
        if (audioSource != null && collectionSound != null)
        {
            audioSource.PlayOneShot(collectionSound);
        }
        
        if (collectionEffect != null)
        {
            GameObject effect = Instantiate(collectionEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
    }
    
    IEnumerator CollectionSequence()
    {
        isGlowing = false;
        
        float animationDuration = 0.5f;
        float elapsedTime = 0f;
        
        Vector3 startScale = transform.localScale;
        Color startColor = spriteRenderer.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);
            spriteRenderer.color = Color.Lerp(startColor, endColor, progress);
            
            yield return null;
        }
        
        gameObject.SetActive(false);
        
        Debug.Log("Objetivo '" + objectiveName + "' recolección completa");
    }
    
    public bool IsCollected() => isCollected;
    public string GetObjectiveName() => objectiveName;
    
    public void ResetObjective()
    {
        isCollected = false;
        isGlowing = true;
        glowTimer = 0f;
        
        transform.localScale = originalScale;
        spriteRenderer.color = originalColor;
        
        gameObject.SetActive(true);
        
        Debug.Log("Objetivo '" + objectiveName + "' reseteado");
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isCollected)
        {
            CollectObjective();
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = isCollected ? Color.gray : Color.green;
        Gizmos.DrawWireSphere(transform.position, collectionRange);
        
        if (!isCollected)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 1.5f, Vector3.one * 0.5f);
        }
    }
    
    [ContextMenu("Recolectar Este Objetivo")]
    void Debug_CollectObjective()
    {
        if (!isCollected)
        {
            CollectObjective();
        }
    }
    
    [ContextMenu("Resetear Este Objetivo")]
    void Debug_ResetObjective()
    {
        ResetObjective();
    }
}
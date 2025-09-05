using UnityEngine;
using System.Collections;

public class ObjectiveController : MonoBehaviour
{
    [Header("Objective Settings")]
    [SerializeField] private string objectiveName = "Tesoro Nacional";
    [SerializeField] private int objectiveIndex = 0; // 0=Acta, 1=Copihue, 2=Gaviota, 3=Moai
    [SerializeField] private bool isCollected = false;
    [SerializeField] private float collectionRange = 1f;
    
    [Header("Visual Effects")]
    [SerializeField] private float glowSpeed = 2f;
    [SerializeField] private float glowIntensity = 0.3f;
    [SerializeField] private Color originalColor = Color.yellow;
    [SerializeField] private Color glowColor = Color.white;
    [SerializeField] private GameObject collectionEffect;
    
    [Header("Collection Animation")]
    [SerializeField] private float collectionAnimationDuration = 1.5f;
    [SerializeField] private float storeAnimationDuration = 1.0f;
    
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
        
        originalColor = spriteRenderer.color;
        originalScale = transform.localScale;
        
        isCollected = false;
        gameObject.SetActive(true);
        
        Debug.Log("Objetivo '" + objectiveName + "' iniciado - Index: " + objectiveIndex);
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
        
        glowTimer += Time.unscaledDeltaTime * glowSpeed;
        
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
        
        Debug.Log("Objetivo '" + objectiveName + "' recolectado - Index: " + objectiveIndex + " - Iniciando secuencia animada");
        
        StartCoroutine(CollectionAnimationSequence());
    }
    
    IEnumerator CollectionAnimationSequence()
    {
        Time.timeScale = 0f;
        CatController catController = playerTransform.GetComponent<CatController>();
        Animator playerAnimator = null;
        if (catController != null)
        {
            catController.enabled = false;
            playerAnimator = catController.GetComponent<Animator>();
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("CollectItem");
        }
        PlayCollectionEffects();

        StartCoroutine(StoreItemAnimation());

        yield return new WaitForSecondsRealtime(collectionAnimationDuration);

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("StoreItem");
        }

        yield return new WaitForSecondsRealtime(storeAnimationDuration);

        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.OnSpecificObjectiveCollected(objectiveIndex);
        }
        Time.timeScale = 1f;
        if (catController != null)
        {
            catController.enabled = true;
        }
        gameObject.SetActive(false);

        Debug.Log("Objetivo '" + objectiveName + "' recolección completa - Gameplay reanudado");
    }
    
    IEnumerator StoreItemAnimation()
    {
        isGlowing = false;
        
        Vector3 startScale = transform.localScale;
        Vector3 startPosition = transform.position;
        
        Vector3 targetPosition = GetUIIconPosition();
        if (targetPosition == Vector3.zero)
        {
            targetPosition = playerTransform.position + Vector3.up * 2f;
        }
        
        Color startColor = spriteRenderer.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        float elapsedTime = 0f;
        
        while (elapsedTime < 0.8f)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = elapsedTime / 0.8f;
            
            transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);
            spriteRenderer.color = Color.Lerp(startColor, endColor, progress);
            
            yield return null;
        }
    }
    
    Vector3 GetUIIconPosition()
    {
        HUDManager hudManager = HUDManager.Instance;
        if (hudManager == null) return Vector3.zero;
        
        return hudManager.GetObjectiveIconWorldPosition(objectiveIndex);
    }
    
    void PlayCollectionEffects()
    {
        if (collectionEffect != null)
        {
            GameObject effect = Instantiate(collectionEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
    }
    
    public bool IsCollected() => isCollected;
    public string GetObjectiveName() => objectiveName;
    public int GetObjectiveIndex() => objectiveIndex;
    
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
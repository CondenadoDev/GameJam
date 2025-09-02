using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo a seguir")]
    public Transform target;
    
    [Header("Configuración de seguimiento")]
    [Range(0.01f, 1f)]
    public float smoothTime = 0.125f;
    public Vector2 offset = Vector2.zero;
    
    [Header("Límites de la cámara (opcional)")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;
    
    [Header("Mouse Peek Settings")]
    public bool enableMousePeek = true;
    [Range(0f, 100f)]
    public float peekDistance = 5f;
    [Range(0.01f, 1f)]
    public float peekSmoothTime = 0.1f;
    [Range(0f, 0.5f)]
    public float edgeThreshold = 0.1f; // Qué tan cerca del borde debe estar el mouse
    
    private Vector3 velocity = Vector3.zero;
    private Vector2 peekOffset = Vector2.zero;
    private Vector2 peekVelocity = Vector2.zero;
    private Camera cam;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        
        // Si no se asignó un target, buscar uno automáticamente
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // Calcular peek offset basado en la posición del mouse
        UpdateMousePeek();
        
        // Posición objetivo (target + offset + peekOffset)
        Vector3 targetPosition = target.position + (Vector3)offset + (Vector3)peekOffset;
        targetPosition.z = transform.position.z; // Mantener la Z de la cámara
        
        // Aplicar límites si están habilitados
        if (useBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
        }
        
        // Mover la cámara suavemente hacia la posición objetivo
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
    
    void UpdateMousePeek()
    {
        if (!enableMousePeek) 
        {
            peekOffset = Vector2.SmoothDamp(peekOffset, Vector2.zero, ref peekVelocity, peekSmoothTime);
            return;
        }
        
        Vector2 mouseScreenPos = new Vector2(
            Input.mousePosition.x / Screen.width,
            Input.mousePosition.y / Screen.height
        );
        
        Vector2 targetPeekOffset = Vector2.zero;
        
        if (mouseScreenPos.x < edgeThreshold)
        {
            float intensity = (edgeThreshold - mouseScreenPos.x) / edgeThreshold;
            targetPeekOffset.x = -peekDistance * intensity;
        }
        else if (mouseScreenPos.x > 1f - edgeThreshold)
        {
            float intensity = (mouseScreenPos.x - (1f - edgeThreshold)) / edgeThreshold;
            targetPeekOffset.x = peekDistance * intensity;
        }
        
        if (mouseScreenPos.y < edgeThreshold)
        {
            float intensity = (edgeThreshold - mouseScreenPos.y) / edgeThreshold;
            targetPeekOffset.y = -peekDistance * intensity;
        }
        else if (mouseScreenPos.y > 1f - edgeThreshold)
        {
            float intensity = (mouseScreenPos.y - (1f - edgeThreshold)) / edgeThreshold;
            targetPeekOffset.y = peekDistance * intensity;
        }
        
        peekOffset = Vector2.SmoothDamp(peekOffset, targetPeekOffset, ref peekVelocity, peekSmoothTime);
    }
    
    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
        useBounds = true;
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    void OnDrawGizmosSelected()
    {
        if (useBounds)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2f, (minBounds.y + maxBounds.y) / 2f, 0f);
            Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0f);
            Gizmos.DrawWireCube(center, size);
        }
        
        if (enableMousePeek && target != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 targetPos = target.position + (Vector3)offset;
            Gizmos.DrawWireSphere(targetPos + Vector3.right * peekDistance, 0.5f);
            Gizmos.DrawWireSphere(targetPos + Vector3.left * peekDistance, 0.5f);
            Gizmos.DrawWireSphere(targetPos + Vector3.up * peekDistance, 0.5f);
            Gizmos.DrawWireSphere(targetPos + Vector3.down * peekDistance, 0.5f);
        }
    }
}
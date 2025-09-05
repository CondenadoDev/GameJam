using UnityEngine;
using UnityEngine.U2D;

public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo a seguir")]
    public Transform target;
    
    [Header("Configuración de seguimiento")]
    [Tooltip("Esta variable ya no se usa en esta versión para evitar el judder. Se puede re-implementar un suavizado diferente si es necesario.")]
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
    public float edgeThreshold = 0.1f;
    
    private Vector2 peekOffset = Vector2.zero;
    private Vector2 peekVelocity = Vector2.zero;

    private PixelPerfectCamera pixelPerfectCamera;
    private float currentPixelsPerUnit;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        pixelPerfectCamera = GetComponent<PixelPerfectCamera>();
        if (pixelPerfectCamera != null)
        {
            currentPixelsPerUnit = pixelPerfectCamera.assetsPPU;
        }
        else
        {
            Debug.LogWarning("Componente PixelPerfectCamera no encontrado en la cámara. El ajuste a la cuadrícula podría no funcionar.");
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        UpdateMousePeek();
        
        Vector3 targetPosition = target.position + (Vector3)offset + (Vector3)peekOffset;
        targetPosition.z = transform.position.z;
        
        if (useBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
        }
        
        transform.position = SnapToPixelGrid(targetPosition);
    }

    private Vector3 SnapToPixelGrid(Vector3 position)
    {
        if (currentPixelsPerUnit <= 0)
        {
            return position;
        }

        float pixelGridSize = 1f / currentPixelsPerUnit;
        float snappedX = Mathf.Round(position.x / pixelGridSize) * pixelGridSize;
        float snappedY = Mathf.Round(position.y / pixelGridSize) * pixelGridSize;
        return new Vector3(snappedX, snappedY, position.z);
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
            float intensity = Mathf.Clamp01((edgeThreshold - mouseScreenPos.x) / edgeThreshold);
            targetPeekOffset.x = -peekDistance * intensity;
        }
        else if (mouseScreenPos.x > 1f - edgeThreshold)
        {
            float intensity = Mathf.Clamp01((mouseScreenPos.x - (1f - edgeThreshold)) / edgeThreshold);
            targetPeekOffset.x = peekDistance * intensity;
        }
        
        if (mouseScreenPos.y < edgeThreshold)
        {
            float intensity = Mathf.Clamp01((edgeThreshold - mouseScreenPos.y) / edgeThreshold);
            targetPeekOffset.y = -peekDistance * intensity;
        }
        else if (mouseScreenPos.y > 1f - edgeThreshold)
        {
            float intensity = Mathf.Clamp01((mouseScreenPos.y - (1f - edgeThreshold)) / edgeThreshold);
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


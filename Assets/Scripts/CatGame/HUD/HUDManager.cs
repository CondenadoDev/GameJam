using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HUDManager : MonoBehaviour
{
    [Header("Canvas Principal")]
    [SerializeField] private Canvas hudCanvas;
    
    [Header("Vidas (Esquina Superior Izquierda)")]
    [SerializeField] private Image life1Image;
    [SerializeField] private Image life2Image;
    [SerializeField] private Image life3Image;
    [SerializeField] private Image life4Image;
    [SerializeField] private Image life5Image;
    
    [Header("Sprites de Vidas")]
    [SerializeField] private Sprite lifeFullSprite;
    [SerializeField] private Sprite lifeEmptySprite;
    
    [Header("Objetivos (4 Sprites Verticales - Lado Derecho)")]
    [SerializeField] private Image objective1Image;
    [SerializeField] private Image objective2Image;
    [SerializeField] private Image objective3Image;
    [SerializeField] private Image objective4Image;
    
    [Header("Sprites de Objetivos")]
    [SerializeField] private Sprite objective1EmptySprite;
    [SerializeField] private Sprite objective1CompletedSprite;
    [SerializeField] private Sprite objective2EmptySprite;
    [SerializeField] private Sprite objective2CompletedSprite;
    [SerializeField] private Sprite objective3EmptySprite;
    [SerializeField] private Sprite objective3CompletedSprite;
    [SerializeField] private Sprite objective4EmptySprite;
    [SerializeField] private Sprite objective4CompletedSprite;
    
    [Header("Cooldown Maullido")]
    [SerializeField] private GameObject purringCooldownPanel;
    [SerializeField] private Image purringCooldownIcon;
    [SerializeField] private Image purringCooldownFill;
    [SerializeField] private TextMeshProUGUI purringCooldownText;
    
    [Header("Cooldown Bolas de Pelo")]
    [SerializeField] private GameObject hairballCooldownPanel;
    [SerializeField] private Image hairballCooldownIcon;
    [SerializeField] private Image hairballCooldownFill;
    [SerializeField] private TextMeshProUGUI hairballCooldownText;
    
    [Header("Configuración de Colores")]
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color warningTextColor = Color.yellow;
    [SerializeField] private Color criticalTextColor = Color.red;
    [SerializeField] private Color cooldownTextColor = Color.white;
    
    private GameManager gameManager;
    private CatController catController;
    
    private int currentLives = 5;
    private int currentObjectives = 0;
    private Image[] objectiveImages;
    private Image[] lifeImages;

    private bool[] objectivesCompleted;
    
    private bool isPurringOnCooldown = false;
    private bool isHairballOnCooldown = false;
    private float purringCooldownTime = 3f;
    private float hairballCooldownTime = 2f;
    
    public static HUDManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        objectiveImages = new Image[] { objective1Image, objective2Image, objective3Image, objective4Image };
        lifeImages = new Image[] { life1Image, life2Image, life3Image, life4Image, life5Image };
        
        objectivesCompleted = new bool[objectiveImages.Length];
    }
    
    void Start()
    {
        FindReferences();
        SubscribeToEvents();
        InitializeHUD();
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    private void FindReferences()
    {
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            catController = playerObj.GetComponent<CatController>();
        }
    }
    
    private void SubscribeToEvents()
    {
        if (gameManager != null)
        {
            gameManager.OnLivesChanged += UpdateLives;
            gameManager.OnObjectivesChanged += UpdateObjectives;
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        if (gameManager != null)
        {
            gameManager.OnLivesChanged -= UpdateLives;
            gameManager.OnObjectivesChanged -= UpdateObjectives;
        }
    }
    
    private void InitializeHUD()
    {
        UpdateObjectivesDisplay(0);
        UpdateLivesDisplay();
        
        if (purringCooldownPanel != null)
            purringCooldownPanel.SetActive(false);
        if (hairballCooldownPanel != null)
            hairballCooldownPanel.SetActive(false);
    }
    
    public void UpdateLives(int newLives)
    {
        currentLives = newLives;
        UpdateLivesDisplay();
    }
    
    public void UpdateObjectives(int current, int total)
    {
        currentObjectives = current;
        UpdateObjectivesDisplay(current);
    }
    
    private void UpdateLivesDisplay()
    {
        for (int i = 0; i < lifeImages.Length; i++)
        {
            if (lifeImages[i] != null)
            {
                if (i < currentLives)
                {
                    lifeImages[i].sprite = lifeFullSprite;
                }
                else
                {
                    lifeImages[i].sprite = lifeEmptySprite;
                }
            }
        }
    }
    
    private void UpdateObjectivesDisplay(int completedCount)
    {
        Sprite[] emptySprites = { objective1EmptySprite, objective2EmptySprite, objective3EmptySprite, objective4EmptySprite };
        Sprite[] completedSprites = { objective1CompletedSprite, objective2CompletedSprite, objective3CompletedSprite, objective4CompletedSprite };
        
        for (int i = 0; i < objectiveImages.Length; i++)
        {
            if (objectiveImages[i] != null)
            {
                if (i < objectivesCompleted.Length && objectivesCompleted[i])
                {
                    objectiveImages[i].sprite = completedSprites[i];
                }
                else
                {
                    objectiveImages[i].sprite = emptySprites[i];
                }
            }
        }
    }
    
    public void StartPurringCooldown()
    {
        if (!isPurringOnCooldown)
        {
            StartCoroutine(HandleCooldown(purringCooldownFill, purringCooldownTime, () => isPurringOnCooldown = false));
            isPurringOnCooldown = true;
        }
    }
    
    public void StartHairballCooldown()
    {
        if (!isHairballOnCooldown)
        {
            StartCoroutine(HandleCooldown(hairballCooldownFill, hairballCooldownTime, () => isHairballOnCooldown = false));
            isHairballOnCooldown = true;
        }
    }
    
    private IEnumerator HandleCooldown(Image fillImage, float duration, System.Action onComplete)
    {
        if (fillImage != null)
        {
            Color opaqueColor = fillImage.color;
            opaqueColor.a = 0.4f;
            fillImage.color = opaqueColor;
            fillImage.fillAmount = 0f;
        }
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            if (fillImage != null)
                fillImage.fillAmount = progress;
            
            yield return null;
        }
        
        if (fillImage != null)
        {
            fillImage.fillAmount = 1f;
            Color readyColor = fillImage.color;
            readyColor.a = 1f;
            fillImage.color = readyColor;
            
            StartCoroutine(ReadyPulseEffect(fillImage));
        }
        
        onComplete?.Invoke();
    }
    
    private IEnumerator ReadyPulseEffect(Image fillImage)
    {
        if (fillImage == null) yield break;
        
        Color originalColor = fillImage.color;
        
        for (int i = 0; i < 3; i++)
        {
            float elapsedTime = 0f;
            float pulseDuration = 0.3f;
            
            while (elapsedTime < pulseDuration)
            {
                elapsedTime += Time.deltaTime;
                float intensity = Mathf.Sin(elapsedTime / pulseDuration * Mathf.PI);
                
                Color pulseColor = Color.Lerp(originalColor, Color.white, intensity * 0.4f);
                fillImage.color = pulseColor;
                
                yield return null;
            }
            
            fillImage.color = originalColor;
            yield return new WaitForSeconds(0.2f);
        }
    }
    
    public bool IsPurringOnCooldown() => isPurringOnCooldown;
    public bool IsHairballOnCooldown() => isHairballOnCooldown;
    
    public void SetCooldownTimes(float purringTime, float hairballTime)
    {
        purringCooldownTime = purringTime;
        hairballCooldownTime = hairballTime;
    }

    public void UpdateSpecificObjective(int objectiveIndex)
    {
        if (objectiveIndex >= 0 && objectiveIndex < objectivesCompleted.Length)
        {
            objectivesCompleted[objectiveIndex] = true;
        }
    }

    public Vector3 GetObjectiveIconWorldPosition(int objectiveIndex)
    {
        if (objectiveIndex >= 0 && objectiveIndex < objectiveImages.Length && objectiveImages[objectiveIndex] != null)
        {
            RectTransform rectTransform = objectiveImages[objectiveIndex].GetComponent<RectTransform>();
            Vector3 worldPosition = rectTransform.TransformPoint(rectTransform.rect.center);
            return Camera.main.ScreenToWorldPoint(new Vector3(worldPosition.x, worldPosition.y, Camera.main.nearClipPlane));
        }
        return Vector3.zero;
    }
}
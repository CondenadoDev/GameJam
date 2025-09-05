using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private int maxLives = 5;
    [SerializeField] private int totalObjectives = 4;
    [SerializeField] private Vector3 catSpawnPoint = new Vector3(-8, 0, 0);
    
    [Header("Current Game State")]
    [SerializeField] private int currentLives;
    [SerializeField] private int objectivesCollected = 0;
    [SerializeField] private bool gameOver = false;
    [SerializeField] private bool missionComplete = false;
    
    [Header("References")]
    [SerializeField] private Transform catTransform;
    [SerializeField] private CatController catController;
    
    [Header("UI References")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private Canvas fadeCanvas;
    
    [Header("Detection System")]
    [SerializeField] private float arrestCooldown = 2f;
    private bool isProcessingArrest = false;
    private float lastArrestTime = -999f;
    
    public System.Action<int> OnLivesChanged;
    public System.Action<int, int> OnObjectivesChanged;
    public System.Action<GameEndType> OnGameEnd;
    
    public enum GameEndType { Victory_Complete, Victory_Partial, Defeat_NoLives }
    
    public static GameManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // FMOD: Inicializar audio del juego principal
        
        InitializeGame();
        FindReferences();
        CreateFadeUI();
        UpdateUI();
    }
    
    void InitializeGame()
    {
        currentLives = maxLives;
        objectivesCollected = 0;
        gameOver = false;
        missionComplete = false;
        isProcessingArrest = false;
        lastArrestTime = -999f;
    }
    
    void FindReferences()
    {
        GameObject catObject = GameObject.FindGameObjectWithTag("Player");
        if (catObject != null)
        {
            catTransform = catObject.transform;
            catController = catObject.GetComponent<CatController>();
            if (catSpawnPoint == Vector3.zero) catSpawnPoint = catTransform.position;
        }
    }
    
    void CreateFadeUI()
    {
        if (fadeCanvas == null)
        {
            GameObject canvasGO = new GameObject("Fade Canvas");
            fadeCanvas = canvasGO.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 1000;
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        if (fadeImage == null)
        {
            GameObject imageGO = new GameObject("Fade Image");
            imageGO.transform.SetParent(fadeCanvas.transform, false);
            
            fadeImage = imageGO.AddComponent<Image>();
            fadeImage.color = new Color(0, 0, 0, 0);
            
            RectTransform rectTransform = fadeImage.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    public void OnPlayerArrested()
    {
        if (gameOver || isProcessingArrest) return;
        
        if (Time.time < lastArrestTime + arrestCooldown) return;
        
        // FMOD: Reproducir sonido de arresto/captura
        
        lastArrestTime = Time.time;
        ResetAllGuards();
        StartCoroutine(ProcessArrest());
    }
    
    void ResetAllGuards()
    {
        GuardController[] allGuards = FindObjectsOfType<GuardController>();
        foreach (GuardController guard in allGuards)
        {
            guard.DisableGuard();
        }
    }
    
    void ResetAllGuardsToSpawn()
    {
        GuardController[] allGuards = FindObjectsOfType<GuardController>();
        foreach (GuardController guard in allGuards)
        {
            guard.ResetToSpawn();
        }
    }

    IEnumerator ProcessArrest()
    {
        isProcessingArrest = true;
        
        ResetAllGuards();
        
        if (catController != null) 
        {
            catController.enabled = false;
            
            Rigidbody2D catRb = catController.GetComponent<Rigidbody2D>();
            if (catRb != null)
            {
                catRb.linearVelocity = Vector2.zero;
                catRb.angularVelocity = 0f;
            }
        }
        
        yield return StartCoroutine(FadeToBlack(0.5f));
        
        currentLives--;
        OnLivesChanged?.Invoke(currentLives);
    
        if (currentLives <= 0)
        {
            EndGame(GameEndType.Defeat_NoLives);
            yield break;
        }
        
        ResetCatPosition();
        
        yield return new WaitForSeconds(0.3f);
        
        ResetAllGuardsToSpawn();
        
        yield return StartCoroutine(FadeFromBlack(0.5f));
        
        if (catController != null) 
        {
            catController.enabled = true;
        }
        
        isProcessingArrest = false;
    }
    
    IEnumerator FadeToBlack(float duration)
    {
        float elapsedTime = 0f;
        Color startColor = fadeImage.color;
        Color targetColor = new Color(0, 0, 0, 1);
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            fadeImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        fadeImage.color = targetColor;
    }
    
    IEnumerator FadeFromBlack(float duration)
    {
        float elapsedTime = 0f;
        Color startColor = fadeImage.color;
        Color targetColor = new Color(0, 0, 0, 0);
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            fadeImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        fadeImage.color = targetColor;
    }
    
    public void OnObjectiveCollected()
    {
        if (gameOver) return;
        
        objectivesCollected++;
        OnObjectivesChanged?.Invoke(objectivesCollected, totalObjectives);
        
        if (objectivesCollected >= totalObjectives)
        {
            missionComplete = true;
            
            // FMOD: Reproducir sonido de misión completada
        }
    }
    
    public void OnReachExit()
    {
        if (gameOver) return;
        
        if (objectivesCollected > 0)
        {
            EndGame(missionComplete ? GameEndType.Victory_Complete : GameEndType.Victory_Partial);
        }
    }
    
    void ResetCatPosition()
    {
        if (catTransform != null)
        {
            catTransform.position = catSpawnPoint;
            
            Rigidbody2D catRb = catTransform.GetComponent<Rigidbody2D>();
            if (catRb != null)
            {
                catRb.linearVelocity = Vector2.zero;
                catRb.angularVelocity = 0f;
            }
        }
    }
    
    void EndGame(GameEndType endType)
    {
        if (gameOver) return;
        gameOver = true;
        
        // FMOD: Reproducir música/sonido de final del juego según el tipo de victoria/derrota
        
        if (catController != null)
        {
            catController.enabled = false;
        }
        
        OnGameEnd?.Invoke(endType);
        
        StartCoroutine(DelayedRestart(3f));
    }
    
    IEnumerator DelayedRestart(float delay)
    {
        yield return new WaitForSeconds(delay);
        RestartGame();
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }
    
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    void UpdateUI()
    {
        OnLivesChanged?.Invoke(currentLives);
        OnObjectivesChanged?.Invoke(objectivesCollected, totalObjectives);
    }
    
    public int GetObjectivesCollected()
    {
        return objectivesCollected;
    }
    public void OnSpecificObjectiveCollected(int objectiveIndex)
    {
        if (gameOver) return;
    
        objectivesCollected++;
    
        HUDManager.Instance?.UpdateSpecificObjective(objectiveIndex);
    
        OnObjectivesChanged?.Invoke(objectivesCollected, totalObjectives);
    
        if (objectivesCollected >= totalObjectives)
        {
            missionComplete = true;
            // FMOD: Reproducir sonido de misión completada
        }
    }
}
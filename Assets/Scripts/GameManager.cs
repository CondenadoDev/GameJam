using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private int maxLives = 7;
    [SerializeField] private int totalObjectives = 3;
    [SerializeField] private Vector3 catSpawnPoint = new Vector3(-8, 0, 0);
    
    [Header("Current Game State")]
    [SerializeField] private int currentLives;
    [SerializeField] private int objectivesCollected = 0;
    [SerializeField] private bool gameOver = false;
    [SerializeField] private bool missionComplete = false;
    
    [Header("References")]
    [SerializeField] private Transform catTransform;
    [SerializeField] private CatController catController;
    
    [Header("Detection System")]
    [SerializeField] private float detectionCooldown = 1.5f;
    private float lastLifeLossTime = -999f;
    private bool isProcessingDetection = false;
    
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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        InitializeGame();
    }

    void Start()
    {
        FindReferences();
        UpdateUI();
    }
    
    void InitializeGame()
    {
        currentLives = maxLives;
        objectivesCollected = 0;
        gameOver = false;
        missionComplete = false;
        isProcessingDetection = false;
        lastLifeLossTime = -999f;
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

    public void OnPlayerArrested()
    {
        if (gameOver || isProcessingDetection) return;

        if (Time.time < lastLifeLossTime + detectionCooldown)
        {
            return;
        }

        StartCoroutine(ProcessArrest());
    }

    IEnumerator ProcessArrest()
    {
        isProcessingDetection = true;
        lastLifeLossTime = Time.time;
        
        currentLives--;
        OnLivesChanged?.Invoke(currentLives);
    
        if (currentLives <= 0)
        {
            EndGame(GameEndType.Defeat_NoLives);
        }
        else
        {
            if (catController != null) catController.enabled = false;
            
            yield return new WaitForSeconds(0.5f);
            
            ResetCatPosition();
        
            if (catController != null) catController.enabled = true;
        }
        
        yield return new WaitForSeconds(detectionCooldown);
        isProcessingDetection = false;
    }
    
    public void OnObjectiveCollected()
    {
        if (gameOver) return;
        
        objectivesCollected++;
        OnObjectivesChanged?.Invoke(objectivesCollected, totalObjectives);
        
        if (objectivesCollected >= totalObjectives)
        {
            missionComplete = true;
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
            }
        }
    }
    
    void EndGame(GameEndType endType)
    {
        if (gameOver) return;
        gameOver = true;
        
        if (catController != null)
        {
            catController.enabled = false;
        }
        
        OnGameEnd?.Invoke(endType);
        StartCoroutine(AutoRestartAfterDelay(5f));
    }
    
    IEnumerator AutoRestartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        RestartGame();
    }
    
    void Update()
    {
        if (gameOver && Input.GetKeyDown(KeyCode.R))
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
    // Este método permite que otros scripts pregunten cuántos objetivos se han recolectado.
    public int GetObjectivesCollected()
    {
        return objectivesCollected;
    }
}
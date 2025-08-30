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
    
    public System.Action<int> OnLivesChanged;
    public System.Action<int, int> OnObjectivesChanged;
    public System.Action<GameEndType> OnGameEnd;
    
    public enum GameEndType
    {
        Victory_Complete,
        Victory_Partial,
        Defeat_NoLives,
        Defeat_Expelled
    }
    
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
            return;
        }
        
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
        
        Debug.Log("Juego iniciado - Vidas: " + currentLives + " | Objetivos: " + objectivesCollected + "/" + totalObjectives);
    }
    
    void FindReferences()
    {
        if (catTransform == null)
        {
            GameObject catObject = GameObject.FindGameObjectWithTag("Player");
            if (catObject != null)
            {
                catTransform = catObject.transform;
                catController = catObject.GetComponent<CatController>();
            }
        }
        
        if (catTransform != null)
        {
            catSpawnPoint = catTransform.position;
        }
    }
    
    public void OnPlayerDetected()
    {
        if (gameOver) return;
        
        currentLives--;
        Debug.Log("Jugador detectado - Vidas restantes: " + currentLives);
        
        OnLivesChanged?.Invoke(currentLives);
        
        if (currentLives <= 0)
        {
            EndGame(GameEndType.Defeat_NoLives);
        }
        else
        {
            ResetCatPosition();
        }
    }
    
    public void OnObjectiveCollected()
    {
        if (gameOver) return;
        
        objectivesCollected++;
        Debug.Log("Objetivo recolectado - Progreso: " + objectivesCollected + "/" + totalObjectives);
        
        OnObjectivesChanged?.Invoke(objectivesCollected, totalObjectives);
        
        if (objectivesCollected >= totalObjectives)
        {
            missionComplete = true;
            Debug.Log("Todos los objetivos recolectados - Dirígete a la salida");
        }
    }
    
    public void OnReachExit()
    {
        if (gameOver) return;
        
        if (objectivesCollected > 0)
        {
            if (objectivesCollected >= totalObjectives)
            {
                EndGame(GameEndType.Victory_Complete);
            }
            else
            {
                EndGame(GameEndType.Victory_Partial);
            }
        }
        else
        {
            Debug.Log("No puedes escapar sin recolectar al menos un objetivo");
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
            
            Debug.Log("Gato regresado al punto de aparición");
        }
    }
    
    void EndGame(GameEndType endType)
    {
        gameOver = true;
        
        if (catController != null)
        {
            catController.enabled = false;
        }
        
        OnGameEnd?.Invoke(endType);
        
        ShowEndGameMessage(endType);
        
        StartCoroutine(AutoRestartAfterDelay(5f));
    }
    
    void ShowEndGameMessage(GameEndType endType)
    {
        string message = "";
        
        switch (endType)
        {
            case GameEndType.Victory_Complete:
                message = "MISION COMPLETADA\nTodos los tesoros han sido rescatados.\nEl gato será el próximo presidente";
                break;
                
            case GameEndType.Victory_Partial:
                message = $"MISION PARCIAL\n{objectivesCollected}/{totalObjectives} objetivos rescatados.\nFelicitaciones del jefe.";
                break;
                
            case GameEndType.Defeat_NoLives:
                message = "MISION FALLIDA\nEl gato fue expulsado de la isla.\nHa sido despedido de la agencia.";
                break;
                
            case GameEndType.Defeat_Expelled:
                message = "MISION FALLIDA\nDemasiadas detecciones.\nLa mansión está cerrada.";
                break;
        }
        
        Debug.Log("=== FIN DEL JUEGO ===");
        Debug.Log(message);
        Debug.Log("Presiona R para reiniciar o espera 5 segundos");
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
        Debug.Log("Reiniciando juego...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    void UpdateUI()
    {
        OnLivesChanged?.Invoke(currentLives);
        OnObjectivesChanged?.Invoke(objectivesCollected, totalObjectives);
    }
    
    public int GetCurrentLives() => currentLives;
    public int GetObjectivesCollected() => objectivesCollected;
    public int GetTotalObjectives() => totalObjectives;
    public bool IsGameOver() => gameOver;
    public bool IsMissionComplete() => missionComplete;
    
    [ContextMenu("Añadir Vida")]
    void Debug_AddLife()
    {
        currentLives++;
        OnLivesChanged?.Invoke(currentLives);
    }
    
    [ContextMenu("Quitar Vida")]
    void Debug_RemoveLife()
    {
        OnPlayerDetected();
    }
    
    [ContextMenu("Recolectar Objetivo")]
    void Debug_CollectObjective()
    {
        OnObjectiveCollected();
    }
    
    [ContextMenu("Resetear Posición del Gato")]
    void Debug_ResetCatPosition()
    {
        ResetCatPosition();
    }
}
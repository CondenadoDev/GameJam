using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [Header("UI Referencias")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Canvas pauseCanvas;
    
    private bool isPaused = false;
    private EventSystem eventSystem;
    
    public static PauseManager Instance { get; private set; }
    
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
        
        // Buscar EventSystem
        eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogWarning("No se encontró EventSystem. Creando uno automáticamente.");
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystem = eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
        }
        
        // Configurar Canvas para que funcione con Time.timeScale = 0
        if (pauseCanvas == null && pauseMenuUI != null)
        {
            pauseCanvas = pauseMenuUI.GetComponent<Canvas>();
            if (pauseCanvas == null)
            {
                pauseCanvas = pauseMenuUI.GetComponentInParent<Canvas>();
            }
        }
        
        ConfigureCanvasForPause();
        
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
    }
    
    void ConfigureCanvasForPause()
    {
        if (pauseCanvas != null)
        {
            // CRÍTICO: Asegurar que el Canvas de pausa esté ENCIMA del fade del GameManager
            pauseCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            pauseCanvas.sortingOrder = 2000; // MÁS ALTO que el fade (1000)
            
            // Configurar Canvas Scaler para timeScale = 0
            CanvasScaler scaler = pauseCanvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = pauseCanvas.gameObject.AddComponent<CanvasScaler>();
            }
            
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // Asegurar GraphicRaycaster
            GraphicRaycaster raycaster = pauseCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = pauseCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }
            
            Debug.Log($"Canvas de pausa configurado con sortingOrder: {pauseCanvas.sortingOrder}");
        }
    }
    
    public void TogglePause()
    {
        Debug.Log($"TogglePause llamado. Estado actual: isPaused = {isPaused}");
        
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }
    
    public void PauseGame()
    {
        Debug.Log("PauseGame() ejecutado");
        
        // CRÍTICO: Verificar que GameManager no esté procesando un arrest
        if (GameManager.Instance != null)
        {
            // Si hay un fade activo del GameManager, no pausar
            if (IsGameManagerFadeActive())
            {
                Debug.Log("No se puede pausar: GameManager está procesando un evento");
                return;
            }
        }
        
        isPaused = true;
        Time.timeScale = 0f;
        
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
            Debug.Log("Menú de pausa activado");
            
            // Forzar configuración del Canvas cada vez
            ConfigureCanvasForPause();
            
            // Esperar un frame antes de seleccionar botón
            StartCoroutine(SelectFirstButtonDelayed());
        }
        else
        {
            Debug.LogError("pauseMenuUI es null!");
        }
        
        AudioListener.pause = true;
        Debug.Log($"Juego pausado. Time.timeScale = {Time.timeScale}");
    }
    
    private System.Collections.IEnumerator SelectFirstButtonDelayed()
    {
        yield return null; // Esperar un frame
        
        Button firstButton = pauseMenuUI.GetComponentInChildren<Button>();
        if (firstButton != null && eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(null); // Limpiar primero
            eventSystem.SetSelectedGameObject(firstButton.gameObject);
            Debug.Log($"Botón seleccionado: {firstButton.name}");
        }
    }
    
    private bool IsGameManagerFadeActive()
    {
        // Verificar si el GameManager está procesando un arrest
        // (puedes usar reflection o hacer público el campo isProcessingArrest)
        return false; // Por ahora, siempre permitir pausa
    }
    
    public void ResumeGame()
    {
        Debug.Log("ResumeGame() ejecutado");
        
        isPaused = false;
        Time.timeScale = 1f;
        
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
            Debug.Log("Menú de pausa desactivado");
        }
        
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(null);
        }
        
        AudioListener.pause = false;
        Debug.Log($"Juego reanudado. Time.timeScale = {Time.timeScale}");
    }
    
    public void RestartLevel()
    {
        Debug.Log("RestartLevel() ejecutado");
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void ReturnToMainMenu()
    {
        Debug.Log("ReturnToMainMenu() ejecutado");
        Time.timeScale = 1f;
        AudioListener.pause = false;
        
        string mainMenuScene = "MainMenu";
        
        if (Application.CanStreamedLevelBeLoaded(mainMenuScene))
        {
            SceneManager.LoadScene(mainMenuScene);
        }
        else
        {
            Debug.LogWarning($"La escena '{mainMenuScene}' no existe. Reiniciando nivel actual.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
    
    public void QuitGame()
    {
        Debug.Log("QuitGame() ejecutado");
        Time.timeScale = 1f;
        AudioListener.pause = false;
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            Debug.Log("Juego terminado en Editor");
        #else
            Application.Quit();
            Debug.Log("Aplicación cerrada");
        #endif
    }
    
    public bool IsPaused()
    {
        return isPaused;
    }
    
    [ContextMenu("Test Pause")]
    public void TestPause()
    {
        Debug.Log("Test Pause ejecutado desde Inspector");
        TogglePause();
    }
    
    [ContextMenu("Debug Canvas Info")]
    public void DebugCanvasInfo()
    {
        if (pauseCanvas != null)
        {
            Debug.Log($"Pause Canvas - Sorting Order: {pauseCanvas.sortingOrder}");
            Debug.Log($"Pause Canvas - Active: {pauseCanvas.gameObject.activeInHierarchy}");
            Debug.Log($"Pause Canvas - Enabled: {pauseCanvas.enabled}");
        }
        
        Canvas[] allCanvas = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in allCanvas)
        {
            Debug.Log($"Canvas '{canvas.name}' - Sorting Order: {canvas.sortingOrder}, Active: {canvas.gameObject.activeInHierarchy}");
        }
    }
}
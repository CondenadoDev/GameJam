using UnityEngine;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Botones del Menú")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;
    
    void Start()
    {
        SetupButtons();
    }
    
    void SetupButtons()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(OnResumeClicked);
        }
        
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
    }
    
    public void OnResumeClicked()
    {
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.ResumeGame();
        }
    }
    
    public void OnRestartClicked()
    {
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.RestartLevel();
        }
    }
    
    public void OnMainMenuClicked()
    {
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.ReturnToMainMenu();
        }
    }
    
    public void OnQuitClicked()
    {
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.QuitGame();
        }
    }
    
    void OnEnable()
    {
        if (resumeButton != null)
        {
            resumeButton.Select();
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject creditsPanel;
    
    [Header("Menu Buttons - Main")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button exitButton;
    
    [Header("Menu Buttons - Credits")]
    [SerializeField] private Button backFromCreditsButton;
    [SerializeField] private TextMeshProUGUI creditsText;
    
    [Header("Game Settings")]
    [SerializeField] private string cinematicSceneName = "CinematicScene";
    [SerializeField] private string gameSceneName = "GameScene"; 
    [SerializeField] private float transitionDuration = 0.5f;
    
    [Header("Visual Effects")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float buttonAnimationDuration = 0.2f;
    [SerializeField] private AnimationCurve buttonScaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 1.1f);
    
    [Header("Title Animation")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private float titlePulseSpeed = 2f;
    [SerializeField] private float titlePulseIntensity = 0.1f;
    
    private MenuState currentMenuState = MenuState.MainMenu;
    private int currentSelectedButtonIndex = 0;
    private Button[] currentMenuButtons;
    
    public enum MenuState
    {
        MainMenu,
        Credits,
        Transitioning
    }
    
    void Start()
    {
        // FMOD: Reproducir música de menú principal
        
        InitializeMenu();
        SetupButtonListeners();
        SetupInitialSelection();
        StartTitleAnimation();
        
        if (fadeImage != null)
        {
            StartCoroutine(FadeIn());
        }
    }
    
    void Update()
    {
        HandleKeyboardNavigation();
        HandleGamepadNavigation();
    }
    
    void InitializeMenu()
    {
        ShowPanel(MenuState.MainMenu);
    }
    
    void SetupButtonListeners()
    {
        if (playButton != null) playButton.onClick.AddListener(StartGame);
        if (creditsButton != null) creditsButton.onClick.AddListener(ShowCredits);
        if (exitButton != null) exitButton.onClick.AddListener(ExitGame);
        if (backFromCreditsButton != null) backFromCreditsButton.onClick.AddListener(BackToMainMenu);
        
        SetupButtonHoverEffects();
    }
    
    void SetupButtonHoverEffects()
    {
        Button[] allButtons = { playButton, creditsButton, exitButton, backFromCreditsButton };
        
        foreach (Button button in allButtons)
        {
            if (button != null)
            {
                EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
                if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
                
                EventTrigger.Entry hoverEnter = new EventTrigger.Entry();
                hoverEnter.eventID = EventTriggerType.PointerEnter;
                hoverEnter.callback.AddListener((data) => { OnButtonHover(button); });
                trigger.triggers.Add(hoverEnter);
                
                EventTrigger.Entry hoverExit = new EventTrigger.Entry();
                hoverExit.eventID = EventTriggerType.PointerExit;
                hoverExit.callback.AddListener((data) => { OnButtonHoverExit(button); });
                trigger.triggers.Add(hoverExit);
            }
        }
    }
    
    void SetupInitialSelection()
    {
        currentSelectedButtonIndex = 0;
        UpdateCurrentMenuButtons();
        
        if (currentMenuButtons.Length > 0 && currentMenuButtons[0] != null)
        {
            EventSystem.current.SetSelectedGameObject(currentMenuButtons[0].gameObject);
        }
    }
    
    void ShowPanel(MenuState state)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        
        switch (state)
        {
            case MenuState.MainMenu:
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                break;
            case MenuState.Credits:
                if (creditsPanel != null) creditsPanel.SetActive(true);
                break;
        }
        
        currentMenuState = state;
        currentSelectedButtonIndex = 0;
        UpdateCurrentMenuButtons();
        
        if (currentMenuButtons.Length > 0 && currentMenuButtons[0] != null)
        {
            EventSystem.current.SetSelectedGameObject(currentMenuButtons[0].gameObject);
        }
    }
    
    void UpdateCurrentMenuButtons()
    {
        switch (currentMenuState)
        {
            case MenuState.MainMenu:
                currentMenuButtons = new Button[] { playButton, creditsButton, exitButton };
                break;
            case MenuState.Credits:
                currentMenuButtons = new Button[] { backFromCreditsButton };
                break;
            default:
                currentMenuButtons = new Button[0];
                break;
        }
        
        var validButtons = new System.Collections.Generic.List<Button>();
        foreach (Button btn in currentMenuButtons)
        {
            if (btn != null) validButtons.Add(btn);
        }
        currentMenuButtons = validButtons.ToArray();
    }
    
    void HandleKeyboardNavigation()
    {
        if (currentMenuState == MenuState.Transitioning) return;
        
        bool upPressed = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
        bool downPressed = Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
        bool enterPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
        bool backPressed = Input.GetKeyDown(KeyCode.Escape);
        
        if (upPressed)
        {
            NavigateUp();
        }
        else if (downPressed)
        {
            NavigateDown();
        }
        else if (enterPressed)
        {
            ActivateCurrentButton();
        }
        else if (backPressed)
        {
            HandleBackInput();
        }
    }
    
    void HandleGamepadNavigation()
    {
        if (currentMenuState == MenuState.Transitioning) return;
        
        float verticalInput = Input.GetAxis("Vertical");
        bool selectPressed = Input.GetButtonDown("Submit");
        bool cancelPressed = Input.GetButtonDown("Cancel");
        
        if (verticalInput > 0.5f && Time.unscaledTime > 0.2f)
        {
            NavigateUp();
        }
        else if (verticalInput < -0.5f && Time.unscaledTime > 0.2f)
        {
            NavigateDown();
        }
        
        if (selectPressed)
        {
            ActivateCurrentButton();
        }
        else if (cancelPressed)
        {
            HandleBackInput();
        }
    }
    
    void NavigateUp()
    {
        if (currentMenuButtons.Length > 0)
        {
            currentSelectedButtonIndex = (currentSelectedButtonIndex - 1 + currentMenuButtons.Length) % currentMenuButtons.Length;
            SelectCurrentButton();
        }
    }
    
    void NavigateDown()
    {
        if (currentMenuButtons.Length > 0)
        {
            currentSelectedButtonIndex = (currentSelectedButtonIndex + 1) % currentMenuButtons.Length;
            SelectCurrentButton();
        }
    }
    
    void SelectCurrentButton()
    {
        if (currentMenuButtons.Length > currentSelectedButtonIndex && currentMenuButtons[currentSelectedButtonIndex] != null)
        {
            // FMOD: Reproducir sonido de hover/navegación
            
            EventSystem.current.SetSelectedGameObject(currentMenuButtons[currentSelectedButtonIndex].gameObject);
            OnButtonHover(currentMenuButtons[currentSelectedButtonIndex]);
        }
    }
    
    void ActivateCurrentButton()
    {
        if (currentMenuButtons.Length > currentSelectedButtonIndex && currentMenuButtons[currentSelectedButtonIndex] != null)
        {
            // FMOD: Reproducir sonido de click/selección
            
            currentMenuButtons[currentSelectedButtonIndex].onClick.Invoke();
        }
    }
    
    void HandleBackInput()
    {
        switch (currentMenuState)
        {
            case MenuState.MainMenu:
                ExitGame();
                break;
            case MenuState.Credits:
                BackToMainMenu();
                break;
        }
    }
    
    public void StartGame()
    {
        if (currentMenuState == MenuState.Transitioning) return;
        
        // FMOD: Reproducir sonido de confirmación "empezar partida" 
        // FMOD: Detener música de menú con fade out
        
        StartCoroutine(TransitionToGame());
    }
    
    public void ShowCredits()
    {
        // FMOD: Reproducir sonido de click suave
        
        ShowPanel(MenuState.Credits);
    }
    
    public void BackToMainMenu()
    {
        // FMOD: Reproducir sonido de back/cancelar
        
        ShowPanel(MenuState.MainMenu);
    }
    
    public void ExitGame()
    {
        // FMOD: Reproducir sonido de confirmación de salida
        
        StartCoroutine(ExitGameCoroutine());
    }
    
    void OnButtonHover(Button button)
    {
        if (button != null)
        {
            StartCoroutine(AnimateButtonScale(button.transform, Vector3.one * 1.05f));
        }
    }
    
    void OnButtonHoverExit(Button button)
    {
        if (button != null)
        {
            StartCoroutine(AnimateButtonScale(button.transform, Vector3.one));
        }
    }
    
    IEnumerator AnimateButtonScale(Transform buttonTransform, Vector3 targetScale)
    {
        Vector3 startScale = buttonTransform.localScale;
        float elapsedTime = 0f;
        
        while (elapsedTime < buttonAnimationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = elapsedTime / buttonAnimationDuration;
            float curveValue = buttonScaleCurve.Evaluate(progress);
            
            buttonTransform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);
            yield return null;
        }
        
        buttonTransform.localScale = targetScale;
    }
    
    void StartTitleAnimation()
    {
        if (titleText != null)
        {
            StartCoroutine(AnimateTitle());
        }
    }
    
    IEnumerator AnimateTitle()
    {
        Vector3 originalScale = titleText.transform.localScale;
        
        while (titleText != null && titleText.gameObject.activeInHierarchy)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * titlePulseSpeed) * titlePulseIntensity;
            titleText.transform.localScale = originalScale * pulse;
            yield return null;
        }
    }
    
    IEnumerator FadeIn()
    {
        Color startColor = fadeImage.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        float elapsedTime = 0f;
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = elapsedTime / transitionDuration;
            fadeImage.color = Color.Lerp(startColor, targetColor, progress);
            yield return null;
        }
        
        fadeImage.color = targetColor;
    }
    
    IEnumerator FadeOut()
    {
        Color startColor = fadeImage.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 1f);
        
        float elapsedTime = 0f;
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = elapsedTime / transitionDuration;
            fadeImage.color = Color.Lerp(startColor, targetColor, progress);
            yield return null;
        }
        
        fadeImage.color = targetColor;
    }
    
    IEnumerator TransitionToGame()
    {
        currentMenuState = MenuState.Transitioning;
        
        if (fadeImage != null)
        {
            fadeImage.raycastTarget = true; // Bloquear input durante transición
            yield return StartCoroutine(FadeOut());
        }
        else
        {
            yield return new WaitForSecondsRealtime(transitionDuration);
        }
        
        SceneManager.LoadScene(cinematicSceneName);
    }
    
    IEnumerator ExitGameCoroutine()
    {
        currentMenuState = MenuState.Transitioning;
        
        if (fadeImage != null)
        {
            fadeImage.raycastTarget = true; // Bloquear input durante salida
            yield return StartCoroutine(FadeOut());
        }
        else
        {
            yield return new WaitForSecondsRealtime(transitionDuration);
        }
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #elif UNITY_WEBGL
            // En WebGL, mostrar mensaje de agradecimiento y opcional redirigir
            Debug.Log("Gracias por jugar!");
            Application.OpenURL("javascript:alert('¡Gracias por jugar!')");
        #else
            Application.Quit();
        #endif
    }
}
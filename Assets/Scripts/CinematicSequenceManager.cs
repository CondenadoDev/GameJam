using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class CinematicSequenceManager : MonoBehaviour
{
    [System.Serializable]
    public class CinematicStep
    {
        [Header("Content")]
        [TextArea(2, 4)]
        public string dialogueText;
        
        [Header("Visual Changes")]
        public bool changeImage = false;
        public Sprite newImage;
        public Vector2 imagePosition = Vector2.zero;
        public Vector2 imageSize = new Vector2(800, 600);
        
        [Header("Timing")]
        public float textTypewriterSpeed = 0.05f;
        public float imageTransitionDuration = 0.5f;
        
        [Header("Audio")]
        public bool playStepSound = false;
        public string stepSoundEvent = "";
    }
    
    [Header("Cinematic Settings")]
    [SerializeField] private CinematicStep[] cinematicSteps;
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private float finalTransitionDelay = 1f;
    
    [Header("UI References")]
    [SerializeField] private Image cinematicImage;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject continuePrompt;
    [SerializeField] private Image fadeImage;
    
    [Header("Input Settings")]
    [SerializeField] private KeyCode continueKey = KeyCode.Space;
    [SerializeField] private KeyCode skipKey = KeyCode.Escape;
    [SerializeField] private KeyCode alternativeContinueKey = KeyCode.Return;
    
    [Header("Visual Effects")]
    [SerializeField] private float fadeTransitionDuration = 0.5f;
    [SerializeField] private AnimationCurve imageTransitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Text Effects")]
    [SerializeField] private float textPulseDuration = 1f;
    [SerializeField] private float textPulseIntensity = 0.1f;
    
    private int currentStepIndex = 0;
    private bool isTypingText = false;
    private bool canContinue = false;
    private bool isTransitioning = false;
    private bool skipCinematic = false;
    
    private Coroutine currentTextCoroutine;
    
    void Start()
    {
        // FMOD: Inicializar música cinemática de apertura
        
        InitializeCinematic();
    }
    
    void Update()
    {
        HandleInput();
    }
    
    void InitializeCinematic()
    {
        if (cinematicSteps == null || cinematicSteps.Length == 0)
        {
            Debug.LogError("No hay pasos configurados para la cinemática");
            StartGame();
            return;
        }
        
        currentStepIndex = 0;
        isTypingText = false;
        canContinue = false;
        isTransitioning = false;
        skipCinematic = false;
        
        if (continuePrompt != null)
        {
            continuePrompt.SetActive(false);
        }
        
        if (cinematicImage != null)
        {
            cinematicImage.gameObject.SetActive(false);
        }
        
        if (fadeImage != null)
        {
            fadeImage.color = new Color(0, 0, 0, 1);
            StartCoroutine(FadeIn());
        }
        else
        {
            StartStep();
        }
    }
    
    void HandleInput()
    {
        if (Input.GetKeyDown(skipKey))
        {
            SkipCinematic();
            return;
        }
        
        if (Input.GetKeyDown(continueKey) || Input.GetKeyDown(alternativeContinueKey) || Input.GetMouseButtonDown(0))
        {
            if (isTypingText)
            {
                CompleteCurrentText();
            }
            else if (canContinue)
            {
                AdvanceCinematic();
            }
        }
    }
    
    void SkipCinematic()
    {
        if (skipCinematic) return;
        
        skipCinematic = true;
        
        // FMOD: Detener música cinemática con fade out
        
        if (currentTextCoroutine != null)
        {
            StopCoroutine(currentTextCoroutine);
        }
        
        StartCoroutine(SkipToGame());
    }
    
    IEnumerator SkipToGame()
    {
        if (fadeImage != null)
        {
            yield return StartCoroutine(FadeOut());
        }
        
        StartGame();
    }
    
    void AdvanceCinematic()
    {
        if (isTransitioning || skipCinematic) return;
        
        // FMOD: Reproducir sonido de avance de texto
        
        currentStepIndex++;
        
        if (currentStepIndex >= cinematicSteps.Length)
        {
            CompleteCinematic();
        }
        else
        {
            StartStep();
        }
    }
    
    void StartStep()
    {
        if (currentStepIndex >= cinematicSteps.Length) return;
        
        CinematicStep currentStep = cinematicSteps[currentStepIndex];
        
        if (currentStep.changeImage)
        {
            StartCoroutine(ChangeImageAndStartText(currentStep));
        }
        else
        {
            DisplayCurrentText(currentStep);
        }
    }
    
    IEnumerator ChangeImageAndStartText(CinematicStep step)
    {
        isTransitioning = true;
        canContinue = false;
        
        if (continuePrompt != null)
        {
            continuePrompt.SetActive(false);
        }
        
        if (cinematicImage != null && cinematicImage.gameObject.activeInHierarchy)
        {
            yield return StartCoroutine(FadeOutImage());
        }
        
        if (cinematicImage != null && step.newImage != null)
        {
            cinematicImage.sprite = step.newImage;
            
            RectTransform imageRect = cinematicImage.GetComponent<RectTransform>();
            if (imageRect != null)
            {
                imageRect.anchoredPosition = step.imagePosition;
                imageRect.sizeDelta = step.imageSize;
            }
            
            cinematicImage.gameObject.SetActive(true);
            
            yield return StartCoroutine(FadeInImage());
        }
        
        isTransitioning = false;
        
        DisplayCurrentText(step);
    }
    
    IEnumerator FadeOutImage()
    {
        if (cinematicImage == null) yield break;
        
        CanvasGroup canvasGroup = cinematicImage.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = cinematicImage.gameObject.AddComponent<CanvasGroup>();
        }
        
        float duration = cinematicSteps[currentStepIndex].imageTransitionDuration * 0.5f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = elapsedTime / duration;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        cinematicImage.gameObject.SetActive(false);
    }
    
    IEnumerator FadeInImage()
    {
        if (cinematicImage == null) yield break;
        
        CanvasGroup canvasGroup = cinematicImage.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = cinematicImage.gameObject.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = 0f;
        
        Vector3 originalScale = cinematicImage.transform.localScale;
        cinematicImage.transform.localScale = originalScale * 0.8f;
        
        float duration = cinematicSteps[currentStepIndex].imageTransitionDuration;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = elapsedTime / duration;
            float curveValue = imageTransitionCurve.Evaluate(progress);
            
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, curveValue);
            cinematicImage.transform.localScale = Vector3.Lerp(originalScale * 0.8f, originalScale, curveValue);
            
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        cinematicImage.transform.localScale = originalScale;
    }
    
    void DisplayCurrentText(CinematicStep step)
    {
        if (currentTextCoroutine != null)
        {
            StopCoroutine(currentTextCoroutine);
        }
        
        // FMOD: Reproducir sonido específico del step si está configurado
        if (step.playStepSound && !string.IsNullOrEmpty(step.stepSoundEvent))
        {
            // FMOD: Ejecutar evento step.stepSoundEvent
        }
        
        currentTextCoroutine = StartCoroutine(TypewriterEffect(step));
    }
    
    IEnumerator TypewriterEffect(CinematicStep step)
    {
        isTypingText = true;
        canContinue = false;
        
        if (continuePrompt != null)
        {
            continuePrompt.SetActive(false);
        }
        
        if (dialogueText != null)
        {
            dialogueText.text = "";
            
            for (int i = 0; i <= step.dialogueText.Length; i++)
            {
                if (skipCinematic) yield break;
                
                dialogueText.text = step.dialogueText.Substring(0, i);
                
                if (i < step.dialogueText.Length)
                {
                    // FMOD: Reproducir sonido sutil de escritura (pitch variable por carácter)
                }
                
                yield return new WaitForSecondsRealtime(step.textTypewriterSpeed);
            }
        }
        
        isTypingText = false;
        canContinue = true;
        
        if (continuePrompt != null)
        {
            continuePrompt.SetActive(true);
            StartCoroutine(PulseContinuePrompt());
        }
    }
    
    void CompleteCurrentText()
    {
        if (currentTextCoroutine != null)
        {
            StopCoroutine(currentTextCoroutine);
        }
        
        if (dialogueText != null && currentStepIndex < cinematicSteps.Length)
        {
            dialogueText.text = cinematicSteps[currentStepIndex].dialogueText;
        }
        
        isTypingText = false;
        canContinue = true;
        
        if (continuePrompt != null)
        {
            continuePrompt.SetActive(true);
            StartCoroutine(PulseContinuePrompt());
        }
    }
    
    IEnumerator PulseContinuePrompt()
    {
        if (continuePrompt == null) yield break;
        
        CanvasGroup canvasGroup = continuePrompt.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = continuePrompt.AddComponent<CanvasGroup>();
        }
        
        while (continuePrompt.activeInHierarchy && canContinue)
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < textPulseDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float pulseValue = 1f - (Mathf.Sin(elapsedTime / textPulseDuration * Mathf.PI) * textPulseIntensity);
                canvasGroup.alpha = pulseValue;
                yield return null;
            }
        }
    }
    
    void CompleteCinematic()
    {
        // FMOD: Detener música cinemática y preparar transición al juego
        
        StartCoroutine(FinishCinematicSequence());
    }
    
    IEnumerator FinishCinematicSequence()
    {
        canContinue = false;
        
        if (continuePrompt != null)
        {
            continuePrompt.SetActive(false);
        }
        
        yield return new WaitForSecondsRealtime(finalTransitionDelay);
        
        if (fadeImage != null)
        {
            yield return StartCoroutine(FadeOut());
        }
        
        StartGame();
    }
    
    IEnumerator FadeIn(float duration = -1f)
    {
        if (fadeImage == null) yield break;
        
        if (duration < 0) duration = fadeTransitionDuration;
        
        Color startColor = fadeImage.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = elapsedTime / duration;
            fadeImage.color = Color.Lerp(startColor, targetColor, progress);
            yield return null;
        }
        
        fadeImage.color = targetColor;
        
        if (currentStepIndex == 0)
        {
            StartStep();
        }
    }
    
    IEnumerator FadeOut(float duration = -1f)
    {
        if (fadeImage == null) yield break;
        
        if (duration < 0) duration = fadeTransitionDuration;
        
        Color startColor = fadeImage.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 1f);
        
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = elapsedTime / duration;
            fadeImage.color = Color.Lerp(startColor, targetColor, progress);
            yield return null;
        }
        
        fadeImage.color = targetColor;
    }
    
    void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}
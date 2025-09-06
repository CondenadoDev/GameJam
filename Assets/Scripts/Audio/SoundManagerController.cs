using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManagerController : MonoBehaviour
{
    [Header("Asignar el Sound Manager")]
    public GameObject soundManager;  // el objeto del Sound Manager

    [Header("Nombre de la escena donde se desactiva")]
    public string sceneToDisable;

    private void Awake()
    {
        if (soundManager != null)
        {
            DontDestroyOnLoad(soundManager);
        }

        // Suscribirse al evento de cambio de escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (soundManager == null) return;

        if (scene.name == sceneToDisable)
        {
            soundManager.SetActive(false); // desactiva el SoundManager
        }
        else
        {
            soundManager.SetActive(true); // lo vuelve a activar en otras escenas
        }
    }

    private void OnDestroy()
    {
        // Para evitar múltiples suscripciones al evento
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}


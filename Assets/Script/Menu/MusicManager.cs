using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [SerializeField] private AudioClip[] sceneMusic; // Assign tracks for each scene
    private AudioSource audioSource;

    private void Awake()
    {
        // Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
            audioSource = GetComponent<AudioSource>();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        UpdateMusicForScene();
    }

    private void OnEnable()
    {
        // Subscribe to scene change event
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Unsubscribe from scene change event
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        UpdateMusicForScene();
    }

    private void UpdateMusicForScene()
    {
        if (Instance != this || audioSource == null) return;

        // Get the current scene index
        int sceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;

        // Play the music corresponding to the scene
        if (sceneIndex >= 0 && sceneIndex < sceneMusic.Length)
        {
            AudioClip clip = sceneMusic[sceneIndex];
            if (clip != null && clip != audioSource.clip)
            {
                audioSource.clip = clip;
                audioSource.Play();
            }
        }
    }
}

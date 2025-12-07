// MainMenuManager.cs
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public Button newGameButton;
    public Button quitButton;
    public Text versionText;
    public Text highScoreText;

    [Header("Scene Names")]
    public string introSceneName = "Intro_Scene";
    public string gameSceneName = "Level_1";

    [Header("Intro")]
    [Tooltip("How long the intro scene should play (seconds) before automatically loading the game scene.")]
    public float introDuration = 20f;

    [Header("Audio")]
    public AudioSource buttonClickSound;
    public AudioSource backgroundMusic;

    void Start()
    {
        // Initialize UI
        versionText.text = "Version " + Application.version;

        // Load high score
        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        highScoreText.text = "High Score: " + highScore;

        // Setup button listeners
        newGameButton.onClick.AddListener(OnNewGameClicked);
        quitButton.onClick.AddListener(OnQuitClicked);

        // Play background music
        if (backgroundMusic != null)
            backgroundMusic.Play();

        // Editor-only validation: check that named scenes are present in Build Settings
#if UNITY_EDITOR
        ValidateScenesInBuildSettings();
#endif
    }

    void OnNewGameClicked()
    {
        PlayButtonSound();

        // Optional: Add loading screen or fade effect
        Debug.Log("Starting New Game and playing intro...");

        // Start the intro -> level sequence using a persistent controller so the coroutine survives scene loads
        StartIntroSequence();
    }

    void StartIntroSequence()
    {
        // Prevent creating multiple controllers if player presses New Game repeatedly
        if (FindObjectOfType<SceneSequenceController>() != null)
            return;

        GameObject controller = new GameObject("SceneSequenceController");
        DontDestroyOnLoad(controller);
        var seq = controller.AddComponent<SceneSequenceController>();
        seq.Begin(introSceneName, gameSceneName, introDuration);
    }

    void OnQuitClicked()
    {
        PlayButtonSound();
        Debug.Log("Quitting Game...");

        // Optional: Add confirmation dialog
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void PlayButtonSound()
    {
        if (buttonClickSound != null)
            buttonClickSound.Play();
    }

    // Async loading for smooth transition
    System.Collections.IEnumerator LoadGameAsync()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(gameSceneName);

        // Optional: Show loading progress
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            Debug.Log("Loading progress: " + (progress * 100) + "%");
            yield return null;
        }
    }

    void Update()
    {
        // ESC to quit
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnQuitClicked();
        }

        // Space/Enter to start game
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            OnNewGameClicked();
        }
    }

#if UNITY_EDITOR
    // Editor helper to warn when a referenced scene is not present in Build Settings
    void ValidateScenesInBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes;
        var availableNames = new System.Collections.Generic.List<string>(scenes.Length);
        foreach (var s in scenes)
        {
            if (string.IsNullOrEmpty(s.path))
                continue;
            string name = Path.GetFileNameWithoutExtension(s.path);
            availableNames.Add(name);
        }

        if (!string.IsNullOrEmpty(introSceneName) && !availableNames.Contains(introSceneName))
        {
            Debug.LogWarning($"Intro scene '{introSceneName}' is not in Build Settings. Available scenes: {string.Join(", ", availableNames)}");
        }

        if (!string.IsNullOrEmpty(gameSceneName) && !availableNames.Contains(gameSceneName))
        {
            Debug.LogWarning($"Game scene '{gameSceneName}' is not in Build Settings. Available scenes: {string.Join(", ", availableNames)}");
        }
    }
#endif

    // Persistent controller that sequences the intro scene then the game scene.
    private class SceneSequenceController : MonoBehaviour
    {
        string introScene;
        string gameScene;
        float waitSeconds;

        public void Begin(string intro, string game, float seconds)
        {
            introScene = intro;
            gameScene = game;
            waitSeconds = seconds;
            StartCoroutine(RunSequence());
        }

        IEnumerator RunSequence()
        {
            bool introLoaded = false;

            // Load intro scene if provided
            if (!string.IsNullOrEmpty(introScene))
            {
                AsyncOperation loadIntro = SceneManager.LoadSceneAsync(introScene);
                if (loadIntro != null)
                {
                    yield return loadIntro;
                    introLoaded = true;
                }
                else
                {
                    Debug.LogWarning("Intro scene not found: " + introScene + " — skipping to game scene.");
                }
            }

            // If intro was loaded, wait the specified introDuration using unscaled time.
            if (introLoaded)
            {
                float elapsed = 0f;
                while (elapsed < waitSeconds)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            // Load the game scene
            if (!string.IsNullOrEmpty(gameScene))
            {
                AsyncOperation loadGame = SceneManager.LoadSceneAsync(gameScene);
                if (loadGame != null)
                    yield return loadGame;
                else
                    Debug.LogError("Game scene not found: " + gameScene);
            }

            // Cleanup
            Destroy(gameObject);
        }
    }
}
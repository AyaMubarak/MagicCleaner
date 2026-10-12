using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Quest Guide, Progress Tracker & HUD System
/// - Top minimal Progress Bar.
/// - Big, clearly visible HINT button in the top-left (instead of old Quit).
/// - Big, clearly visible QUIT button in the top-right.
/// - Auto-hint feature: If the player stays idle/unsolved for 60 seconds, the hint bubble automatically appears!
/// - QUIT button returns directly to the LEVELS menu.
/// </summary>
public class LevelProgressGuide : MonoBehaviour
{
    public static LevelProgressGuide Instance { get; private set; }

    [System.Serializable]
    public class TaskInfo
    {
        public string id;            // Task ID (dust, trash, candle, chest, key, door)
        public string title;         // Task Title
        public string hint;          // Single Hint for this specific step
        public bool isCompleted;     // Completion state
    }

    [Header("Level Tasks (In Logical Sequential Order)")]
    public List<TaskInfo> tasks = new List<TaskInfo>();

    [Header("UI References (Auto-created if empty)")]
    public Canvas guideCanvas;
    public Image progressBarFill;
    public TextMeshProUGUI progressPercentageText;

    [Header("Hint Bubble UI (Side Popup)")]
    public GameObject hintBubblePanel;
    public TextMeshProUGUI hintBubbleText;

    [Header("Settings")]
    public bool playChimeOnComplete = true;
    public string mainMenuSceneName = "MainMenu";
    public float firstHintDelaySeconds = 60f;        // 1 minute before first hint
    public float subsequentHintDelaySeconds = 120f;   // 2 minutes for next hints

    private float targetProgress = 0f;
    private float currentProgress = 0f;
    private AudioSource audioSource;
    private string currentSceneName;
    private TaskInfo currentActiveTask = null;

    private float idleTimer = 0f;
    private bool isHintUnlocked = false;
    private Button hintButtonComponent;
    private TextMeshProUGUI hintButtonText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitializeOnSceneLoaded()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "MainMenu" || scene == "Introduction") return;

        if (FindFirstObjectByType<LevelProgressGuide>() == null)
        {
            GameObject autoGuide = new GameObject("LevelProgressGuide");
            autoGuide.AddComponent<LevelProgressGuide>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        currentSceneName = SceneManager.GetActiveScene().name;

        InitializeDefaultTasks();
        LoadSavedProgress();
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        EnsureUIExists();
        UpdateUIImmediate();
    }

    private float GetRequiredDelay()
    {
        // If no tasks completed yet, wait 1 min (60s), otherwise wait 2 mins (120s)
        int completedCount = 0;
        for (int i = 0; i < tasks.Count; i++)
        {
            if (tasks[i].isCompleted) completedCount++;
        }
        return (completedCount == 0) ? firstHintDelaySeconds : subsequentHintDelaySeconds;
    }

    private void Update()
    {
        // Smooth fill animation for the top bar
        if (Mathf.Abs(currentProgress - targetProgress) > 0.001f)
        {
            currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * 6f);
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = currentProgress;
            }
        }

        // ================= Strict Hint Timer =================
        // Hint strictly cannot appear unless the player waits the required duration without progress
        if (!isHintUnlocked && currentActiveTask != null)
        {
            idleTimer += Time.deltaTime;
            float reqDelay = GetRequiredDelay();

            // Update Hint button label with remaining countdown
            if (hintButtonText != null)
            {
                int remaining = Mathf.Max(0, Mathf.CeilToInt(reqDelay - idleTimer));
                int minutes = remaining / 60;
                int seconds = remaining % 60;
                hintButtonText.text = $"💡 HINT ({minutes}:{seconds:D2})";
                hintButtonText.color = new Color(0.75f, 0.85f, 1f, 0.7f);
            }

            if (idleTimer >= reqDelay)
            {
                isHintUnlocked = true;
                if (hintButtonText != null)
                {
                    hintButtonText.text = "💡 HINT";
                    hintButtonText.color = Color.white;
                }
                // Automatically display the hint when time is up
                ShowHintBubble();
            }
        }
    }

    public static void ReportTask(string taskId)
    {
        if (string.IsNullOrEmpty(taskId)) return;

        if (Instance == null)
        {
            Instance = Object.FindAnyObjectByType<LevelProgressGuide>();
            if (Instance == null)
            {
                GameObject go = new GameObject("LevelProgressGuide");
                Instance = go.AddComponent<LevelProgressGuide>();
            }
        }

        Instance.CompleteTask(taskId);
    }

    public void CompleteTask(string taskId)
    {
        bool found = false;
        for (int i = 0; i < tasks.Count; i++)
        {
            if (tasks[i].id.Equals(taskId, System.StringComparison.OrdinalIgnoreCase))
            {
                if (!tasks[i].isCompleted)
                {
                    tasks[i].isCompleted = true;
                    found = true;
                    Debug.Log($"<color=#00FFA3>[Quest Guide]:</color> Completed: {tasks[i].title}");

                    if (playChimeOnComplete)
                    {
                        PlaySuccessChime();
                    }
                }
                break;
            }
        }

        if (!found)
        {
            tasks.Add(new TaskInfo
            {
                id = taskId,
                title = taskId,
                hint = "Completed",
                isCompleted = true
            });
            if (playChimeOnComplete) PlaySuccessChime();
        }

        // As requested: Immediately hide the hint bubble when an item/task is found!
        CloseHintBubble();

        // Reset timer and lock hint for the next task (waiting 2 minutes)
        idleTimer = 0f;
        isHintUnlocked = false;

        SaveProgress();
        CalculateAndUpdateProgress();
        UpdateHintBubbleContent();
    }

    public bool IsTaskDone(string taskId)
    {
        foreach (var task in tasks)
        {
            if (task.id.Equals(taskId, System.StringComparison.OrdinalIgnoreCase))
                return task.isCompleted;
        }
        return false;
    }

    private void CalculateAndUpdateProgress()
    {
        if (tasks.Count == 0) return;

        int completedCount = 0;
        currentActiveTask = null;

        for (int i = 0; i < tasks.Count; i++)
        {
            if (tasks[i].isCompleted)
            {
                completedCount++;
            }
            else if (currentActiveTask == null)
            {
                currentActiveTask = tasks[i];
            }
        }

        targetProgress = (float)completedCount / tasks.Count;

        int percent = Mathf.RoundToInt(targetProgress * 100f);
        if (progressPercentageText != null)
        {
            progressPercentageText.text = $"{percent}%";
        }
    }

    private void UpdateUIImmediate()
    {
        CalculateAndUpdateProgress();
        currentProgress = targetProgress;
        if (progressBarFill != null)
        {
            progressBarFill.fillAmount = currentProgress;
        }
        UpdateHintBubbleContent();
    }

    private void UpdateHintBubbleContent()
    {
        if (hintBubbleText == null) return;

        if (currentActiveTask != null)
        {
            hintBubbleText.text = currentActiveTask.hint;
        }
        else
        {
            hintBubbleText.text = "All tasks completed! Unlock the door to exit.";
        }
    }

    public void ToggleHintBubble()
    {
        // STRICT RULE: If hint hasn't unlocked yet, do not allow viewing early
        if (!isHintUnlocked && currentActiveTask != null)
        {
            float reqDelay = GetRequiredDelay();
            int remaining = Mathf.Max(0, Mathf.CeilToInt(reqDelay - idleTimer));
            int m = remaining / 60;
            int s = remaining % 60;
            Debug.Log($"<color=yellow>[Hint Locked]</color> Hint unlocks in {m}:{s:D2}. Search around first!");
            return;
        }

        if (hintBubblePanel != null)
        {
            bool willOpen = !hintBubblePanel.activeSelf;
            if (willOpen)
            {
                UpdateHintBubbleContent();
            }
            hintBubblePanel.SetActive(willOpen);
        }
    }

    public void ShowHintBubble()
    {
        if (hintBubblePanel != null)
        {
            UpdateHintBubbleContent();
            hintBubblePanel.SetActive(true);
        }
    }

    public void CloseHintBubble()
    {
        if (hintBubblePanel != null)
        {
            hintBubblePanel.SetActive(false);
        }
    }

    public void SaveProgress()
    {
        if (string.IsNullOrEmpty(currentSceneName))
            currentSceneName = SceneManager.GetActiveScene().name;

        int completed = 0;
        for (int i = 0; i < tasks.Count; i++)
        {
            if (tasks[i].isCompleted)
            {
                completed++;
                PlayerPrefs.SetInt($"{currentSceneName}_Task_{tasks[i].id}", 1);
            }
        }

        float pct = tasks.Count > 0 ? (float)completed / tasks.Count : 0f;
        PlayerPrefs.SetFloat($"{currentSceneName}_ProgressPct", pct);
        PlayerPrefs.SetInt($"{currentSceneName}_Started", 1);
        PlayerPrefs.Save();
    }

    private void LoadSavedProgress()
    {
        if (string.IsNullOrEmpty(currentSceneName))
            currentSceneName = SceneManager.GetActiveScene().name;

        for (int i = 0; i < tasks.Count; i++)
        {
            if (PlayerPrefs.GetInt($"{currentSceneName}_Task_{tasks[i].id}", 0) == 1)
            {
                tasks[i].isCompleted = true;
            }
        }
    }

    /// <summary>
    /// Save progress and return directly to the Levels Menu
    /// </summary>
    public void QuitToLevelsMenu()
    {
        SaveProgress();
        PlayerPrefs.SetInt("OpenLevelsDirectly", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void PlaySuccessChime()
    {
        if (audioSource == null) return;

        int sampleRate = 44100;
        float duration = 0.35f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float freq = (t < 0.15f) ? 1046.5f : 1318.5f;
            float envelope = Mathf.Clamp01(1f - (t / duration));
            samples[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * envelope * 0.25f;
        }

        AudioClip clip = AudioClip.Create("Chime", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        audioSource.PlayOneShot(clip);
    }

    private void InitializeDefaultTasks()
    {
        if (tasks != null && tasks.Count > 0) return;

        tasks = new List<TaskInfo>();
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "Room2")
        {
            tasks.Add(new TaskInfo { id = "water", title = "Revive Plant", hint = "Find water or elixir to revive the dried magic plant on the table." });
            tasks.Add(new TaskInfo { id = "alchemy", title = "Brew Potion", hint = "Collect ingredients and brew the formula in the alchemy cauldron." });
            tasks.Add(new TaskInfo { id = "key", title = "Find Exit Key", hint = "Take the roof exit key revealed by the puzzle." });
            tasks.Add(new TaskInfo { id = "roof", title = "Unlock Roof", hint = "Climb the wooden stairs and use the key on the trapdoor to escape." });
        }
        else
        {
            // Room 1 logical order (Step-by-step single hint progression)
            tasks.Add(new TaskInfo { id = "dust", title = "Clear Dust", hint = "Swipe across the dusty table to clear the dust and discover the secret book." });
            tasks.Add(new TaskInfo { id = "trash", title = "Clean Trash", hint = "Click on the messy trash pile to clean it up and find the flint." });
            tasks.Add(new TaskInfo { id = "candle", title = "Light Candle", hint = "Select the flint from your inventory, then click the unlit candle to light it." });
            tasks.Add(new TaskInfo { id = "chest", title = "Unlock Chest", hint = "Use the secret code illuminated on the wall to unlock the chest dials." });
            tasks.Add(new TaskInfo { id = "key", title = "Collect Key", hint = "Take the golden key from inside the unlocked chest." });
            tasks.Add(new TaskInfo { id = "door", title = "Unlock Door", hint = "Click the main exit door with the golden key to finish the room!" });
        }
    }

    /// <summary>
    /// Build large, prominent buttons (HINT on Top-Left, QUIT on Top-Right)
    /// </summary>
    private void EnsureUIExists()
    {
        if (progressBarFill != null && progressPercentageText != null) return;

        // Create a dedicated top-level Canvas with highest sorting order so it never gets covered or scaled down
        GameObject canvasObj = new GameObject("ProgressGuideCanvas_HUD");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Always on top of everything!

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        guideCanvas = canvas;

        // ================= 1. HINT BUTTON (Top-Left: Extra Large, Away from Edges) =================
        GameObject hintBtnObj = new GameObject("Btn_HintSideToggle");
        hintBtnObj.transform.SetParent(canvas.transform, false);
        RectTransform hbRect = hintBtnObj.AddComponent<RectTransform>();
        hbRect.anchorMin = new Vector2(0f, 1f);
        hbRect.anchorMax = new Vector2(0f, 1f);
        hbRect.pivot = new Vector2(0f, 1f);
        hbRect.anchoredPosition = new Vector2(50, -45); // Safely away from edges
        hbRect.sizeDelta = new Vector2(210, 72); // Extra large button size

        Image hbImg = hintBtnObj.AddComponent<Image>();
        hbImg.color = new Color(0.12f, 0.42f, 0.88f, 0.98f);
        Button hintBtn = hintBtnObj.AddComponent<Button>();
        hintBtn.onClick.AddListener(ToggleHintBubble);
        hintButtonComponent = hintBtn;

        GameObject hbTextObj = new GameObject("HintBtnText");
        hbTextObj.transform.SetParent(hintBtnObj.transform, false);
        RectTransform hbtRect = hbTextObj.AddComponent<RectTransform>();
        hbtRect.anchorMin = Vector2.zero;
        hbtRect.anchorMax = Vector2.one;
        TextMeshProUGUI hbtText = hbTextObj.AddComponent<TextMeshProUGUI>();
        hbtText.text = "💡 HINT (1:00)";
        hbtText.fontSize = 22;
        hbtText.fontStyle = FontStyles.Bold;
        hbtText.alignment = TextAlignmentOptions.Center;
        hbtText.color = new Color(0.85f, 0.92f, 1f, 0.9f);
        hintButtonText = hbtText;

        // ================= 2. QUIT BUTTON (Top-Right: Extra Large, Away from Edges) =================
        GameObject quitObj = new GameObject("Btn_QuitLevel");
        quitObj.transform.SetParent(canvas.transform, false);
        RectTransform quitRect = quitObj.AddComponent<RectTransform>();
        quitRect.anchorMin = new Vector2(1f, 1f);
        quitRect.anchorMax = new Vector2(1f, 1f);
        quitRect.pivot = new Vector2(1f, 1f);
        quitRect.anchoredPosition = new Vector2(-50, -45); // Safely away from edges
        quitRect.sizeDelta = new Vector2(200, 72); // Extra large button size

        Image quitImg = quitObj.AddComponent<Image>();
        quitImg.color = new Color(0.85f, 0.16f, 0.16f, 0.98f);
        Button quitBtn = quitObj.AddComponent<Button>();
        quitBtn.onClick.AddListener(QuitToLevelsMenu);

        GameObject quitTextObj = new GameObject("QuitText");
        quitTextObj.transform.SetParent(quitObj.transform, false);
        RectTransform qtRect = quitTextObj.AddComponent<RectTransform>();
        qtRect.anchorMin = Vector2.zero;
        qtRect.anchorMax = Vector2.one;
        TextMeshProUGUI qText = quitTextObj.AddComponent<TextMeshProUGUI>();
        qText.text = "QUIT ✕";
        qText.fontSize = 24;
        qText.fontStyle = FontStyles.Bold;
        qText.alignment = TextAlignmentOptions.Center;
        qText.color = Color.white;

        // ================= 3. TOP PROGRESS BAR (Center: Substantially Larger & Lower) =================
        GameObject topBarContainer = new GameObject("TaskProgressBar_HUD");
        topBarContainer.transform.SetParent(canvas.transform, false);
        RectTransform barRect = topBarContainer.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 1f);
        barRect.anchorMax = new Vector2(0.5f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.anchoredPosition = new Vector2(0, -45);
        barRect.sizeDelta = new Vector2(500, 50); // Much wider & thicker bar

        Image bgImg = topBarContainer.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);

        GameObject trackObj = new GameObject("ProgressTrack");
        trackObj.transform.SetParent(topBarContainer.transform, false);
        RectTransform trackRect = trackObj.AddComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0.03f, 0.18f);
        trackRect.anchorMax = new Vector2(0.80f, 0.82f);
        trackRect.offsetMin = Vector2.zero;
        trackRect.offsetMax = Vector2.zero;
        Image trackImg = trackObj.AddComponent<Image>();
        trackImg.color = new Color(0.14f, 0.18f, 0.24f, 0.95f);

        GameObject fillObj = new GameObject("ProgressFill");
        fillObj.transform.SetParent(trackObj.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0.0f, 0.90f, 0.65f, 1f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 0f;
        progressBarFill = fillImage;

        GameObject percentObj = new GameObject("PercentText");
        percentObj.transform.SetParent(topBarContainer.transform, false);
        RectTransform percentRect = percentObj.AddComponent<RectTransform>();
        percentRect.anchorMin = new Vector2(0.81f, 0.10f);
        percentRect.anchorMax = new Vector2(0.98f, 0.90f);
        percentRect.offsetMin = Vector2.zero;
        percentRect.offsetMax = Vector2.zero;
        TextMeshProUGUI pText = percentObj.AddComponent<TextMeshProUGUI>();
        pText.fontSize = 20;
        pText.fontStyle = FontStyles.Bold;
        pText.alignment = TextAlignmentOptions.Center;
        pText.color = Color.white;
        pText.text = "0%";
        progressPercentageText = pText;

        // ================= 4. HINT BUBBLE POPUP (Under the Hint Button) =================
        GameObject bubbleObj = new GameObject("Hint_SideBubblePanel");
        bubbleObj.transform.SetParent(canvas.transform, false);
        RectTransform bRect = bubbleObj.AddComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0f, 1f);
        bRect.anchorMax = new Vector2(0f, 1f);
        bRect.pivot = new Vector2(0f, 1f);
        bRect.anchoredPosition = new Vector2(50, -125);
        bRect.sizeDelta = new Vector2(460, 175);

        Image bubbleBg = bubbleObj.AddComponent<Image>();
        bubbleBg.color = new Color(0.08f, 0.11f, 0.16f, 0.98f);

        GameObject hTitleObj = new GameObject("HintTitle");
        hTitleObj.transform.SetParent(bubbleObj.transform, false);
        RectTransform htRect = hTitleObj.AddComponent<RectTransform>();
        htRect.anchorMin = new Vector2(0.05f, 0.72f);
        htRect.anchorMax = new Vector2(0.85f, 0.95f);
        htRect.offsetMin = Vector2.zero;
        htRect.offsetMax = Vector2.zero;
        TextMeshProUGUI htText = hTitleObj.AddComponent<TextMeshProUGUI>();
        htText.text = "💡 CURRENT OBJECTIVE";
        htText.fontSize = 14;
        htText.fontStyle = FontStyles.Bold;
        htText.color = new Color(1f, 0.82f, 0.36f);
        htText.alignment = TextAlignmentOptions.Left;

        GameObject closeBubbleObj = new GameObject("Btn_CloseHint");
        closeBubbleObj.transform.SetParent(bubbleObj.transform, false);
        RectTransform cbRect = closeBubbleObj.AddComponent<RectTransform>();
        cbRect.anchorMin = new Vector2(0.88f, 0.70f);
        cbRect.anchorMax = new Vector2(0.98f, 0.95f);
        cbRect.offsetMin = Vector2.zero;
        cbRect.offsetMax = Vector2.zero;
        Button cbBtn = closeBubbleObj.AddComponent<Button>();
        cbBtn.onClick.AddListener(CloseHintBubble);

        GameObject cbTextObj = new GameObject("XText");
        cbTextObj.transform.SetParent(closeBubbleObj.transform, false);
        RectTransform cbtRect = cbTextObj.AddComponent<RectTransform>();
        cbtRect.anchorMin = Vector2.zero;
        cbtRect.anchorMax = Vector2.one;
        TextMeshProUGUI cbText = cbTextObj.AddComponent<TextMeshProUGUI>();
        cbText.text = "✕";
        cbText.fontSize = 16;
        cbText.fontStyle = FontStyles.Bold;
        cbText.alignment = TextAlignmentOptions.Center;
        cbText.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        GameObject hBodyObj = new GameObject("HintBodyText");
        hBodyObj.transform.SetParent(bubbleObj.transform, false);
        RectTransform hbBodyRect = hBodyObj.AddComponent<RectTransform>();
        hbBodyRect.anchorMin = new Vector2(0.06f, 0.08f);
        hbBodyRect.anchorMax = new Vector2(0.94f, 0.68f);
        hbBodyRect.offsetMin = Vector2.zero;
        hbBodyRect.offsetMax = Vector2.zero;
        TextMeshProUGUI bodyText = hBodyObj.AddComponent<TextMeshProUGUI>();
        bodyText.fontSize = 15;
        bodyText.lineSpacing = 5;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.color = new Color(0.92f, 0.94f, 0.98f, 1f);
        hintBubbleText = bodyText;

        hintBubblePanel = bubbleObj;
        hintBubblePanel.SetActive(false);
    }
}

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
    public float firstHintDelaySeconds = 90f;        // 1.5 minutes (90 seconds)
    public float subsequentHintDelaySeconds = 90f;   // 1.5 minutes (90 seconds)

    private float targetProgress = 0f;
    private float currentProgress = 0f;
    private AudioSource audioSource;
    private string currentSceneName;
    private TaskInfo currentActiveTask = null;

    private float idleTimer = 0f;
    private bool isHintUnlocked = false;
    private Button hintButtonComponent;
    private TextMeshProUGUI hintButtonText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadCallback()
    {
        SceneManager.sceneLoaded -= OnAnySceneLoaded;
        SceneManager.sceneLoaded += OnAnySceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitAfterSceneLoad()
    {
        OnAnySceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu" || scene.name == "Introduction") return;

        LevelProgressGuide existing = Object.FindAnyObjectByType<LevelProgressGuide>();
        if (existing == null)
        {
            GameObject autoGuide = new GameObject("LevelProgressGuide");
            existing = autoGuide.AddComponent<LevelProgressGuide>();
        }
        else
        {
            existing.EnsureUIExists();
            existing.UpdateUIImmediate();
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

        // إنشاء وتثبيت واجهة الهنت والجنية فوراً من بداية تشغيل اللعبة وقبل تفاعل اللاعب
        EnsureUIExists();
        UpdateUIImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        idleTimer = 0f;
        isHintUnlocked = false;

        EnsureUIExists();
        UpdateUIImmediate();
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

        // Always ensure hint button components and fairy companion are valid and present
        if (hintButtonComponent == null || hintButtonText == null || MagicalFairyCompanion.Instance == null)
        {
            EnsureUIExists();
        }

        if (!bagRepositioned)
        {
            RepositionBagButton();
        }

        // إبادة أي لوحة أو فقاعة أليرت قديمة تظهر على الشاشة فوراً
        GameObject strayBubble = GameObject.Find("HintBubblePanel");
        if (strayBubble != null)
        {
            Destroy(strayBubble);
        }

        // ================= 1.5-Minute (90s) Hint Countdown =================
        // العداد يبدأ من دقيقة ونصف (90 ثانية) سواء تحرك اللاعب أو لم يتحرك
        float reqDelay = firstHintDelaySeconds > 0f ? firstHintDelaySeconds : 90f;

        if (!isHintUnlocked && currentActiveTask != null)
        {
            idleTimer += Time.deltaTime;

            int remaining = Mathf.Max(0, Mathf.CeilToInt(reqDelay - idleTimer));
            int minutes = remaining / 60;
            int seconds = remaining % 60;

            if (hintButtonText != null)
            {
                hintButtonText.text = $"<color=#FFD166><b>HINT</b></color> <size=16><color=#CBD5E1>({minutes}:{seconds:D2})</color></size>";
            }

            if (hintButtonComponent != null)
            {
                hintButtonComponent.interactable = true;
            }

            // عند انتهاء الدقيقة ونصف يصبح الهنت جاهزاً (READY)
            if (idleTimer >= reqDelay)
            {
                isHintUnlocked = true;
                if (hintButtonText != null)
                {
                    hintButtonText.text = "<color=#FFD166><b>HINT</b></color> <size=16><color=#00FFA3>READY</color></size>";
                }
            }
        }
        else
        {
            // عند انتهاء الوقت أو توفر التلميح: يظهر READY دائماً
            if (hintButtonComponent != null) hintButtonComponent.interactable = true;
            if (hintButtonText != null)
            {
                hintButtonText.text = "<color=#FFD166><b>HINT</b></color> <size=16><color=#00FFA3>READY</color></size>";
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
                found = true;
                if (!tasks[i].isCompleted)
                {
                    tasks[i].isCompleted = true;
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

        // عند أخذ الغرض أو إنجاز المهمة: يبدأ العداد التنازلي للخطوة التالية فوراً (دقيقة ونصف) سواء تحرك اللاعب أو لم يتحرك
        idleTimer = 0f;
        isHintUnlocked = false;

        SaveProgress();
        CalculateAndUpdateProgress();
        UpdateHintBubbleContent();

        // تحديث إضاءة الغرفة تدريجياً كلما أنجز اللاعب مهمة لينور البيت خطوة بخطوة
        if (RoomDustAtmosphere.Instance != null)
        {
            RoomDustAtmosphere.Instance.ApplyProgressiveRoomLighting();
        }
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
            string arabicInstruction = GetArabicTaskInstruction(currentActiveTask.id);
            hintBubbleText.text = $"<color=#FFD166><b>✨ {currentActiveTask.title}</b></color>\n" +
                                  $"<size=15><color=#F1F5F9>{currentActiveTask.hint}</color></size>\n" +
                                  $"<size=14><color=#38BDF8>{arabicInstruction}</color></size>";
        }
        else
        {
            hintBubbleText.text = "<color=#00FFA3><b>✨ All Tasks Completed!</b></color>\n<size=15><color=#F1F5F9>Use the key to unlock the exit door and finish the level!</color></size>";
        }
    }

    private string GetArabicTaskInstruction(string taskId)
    {
        if (string.IsNullOrEmpty(taskId)) return "";
        string id = taskId.ToLower();

        if (id.Contains("dust")) return "امسح الغبار عن الطاولة لاكتشاف الكتاب السري";
        if (id.Contains("trash")) return "نظف كومة القمامة للحصول على القداحة";
        if (id.Contains("candle")) return "حدد القداحة من الحقيبة واشعل الشمعة لكشف الكود";
        if (id.Contains("chest")) return "أدخل الكود السري الظاهر على الحائط لفتح الصندوق";
        if (id.Contains("key")) return "التقط المفتاح الذهبي من داخل الصندوق المفتوح";
        if (id.Contains("door")) return "حدد المفتاح وافتح باب الخروج لإنهاء المرحلة";
        if (id.Contains("water")) return "اسكب الإكسير لإنعاش النبتة السحرية";
        if (id.Contains("alchemy")) return "امزج العناصر داخل المرجل لتركيب الوصفة";
        if (id.Contains("roof")) return "اصعد الدرج الخشبي وافتح فتحة السطح";
        if (id.Contains("painting") || id.Contains("memory")) return "افحص لوحة الذكريات المعلقة لكشف التوقيت السري (8:15)";
        if (id.Contains("clock_hand") || id.Contains("hand")) return "ابحث عن عقرب الساعة وركبه على الساعة الجدارية";
        if (id.Contains("clock") || id.Contains("align")) return "اضبط عقارب الساعة الجدارية على التوقيت 8:15 لفتح الخزانة";
        if (id.Contains("cupboard") || id.Contains("cabinet") || id.Contains("secret_box")) return "افتح الصندوق السري وخذ عقرب الساعة";

        return "اتبع إشارات الجنية لإكمال هذه الخطوة";
    }

    public void ToggleHintBubble()
    {
        if (!isHintUnlocked)
        {
            // الوقت لم ينتهِ بعد - لا تفتح أي أليرت أو فقاعة كتابية
            return;
        }

        // عند انتهاء الوقت والضغط على الهنت أو الجنية: تطير الجنية وتدور حول الغرض المطلوب فوراً بدون أي أليرت
        ShowHintBubble();
    }

    public void ShowHintBubble()
    {
        // تم إلغاء فقاعة التنبيه الكتابي نهائياً بناءً على طلب المستخدم
        if (hintBubblePanel != null)
        {
            hintBubblePanel.SetActive(false);
        }
        DispatchFairyToCurrentObjective();
    }

    private void DispatchFairyToCurrentObjective()
    {
        if (MagicalFairyCompanion.Instance == null || currentActiveTask == null) return;

        Vector3 targetPos = Vector3.zero;
        bool foundTarget = false;

        string id = currentActiveTask.id.ToLower();
        if (SceneManager.GetActiveScene().name == "Room2")
        {
            id = GetRoom2HintTaskId();
        }

        Debug.Log($"[Fairy Hint] Current task: {currentActiveTask.id} | Selected objective: {id}");

        if (id.Contains("dust"))
        {
            GameObject obj = GameObject.Find("Secretbook") ?? GameObject.Find("Dust_Layer") ?? GameObject.Find("table_alchemist");
            if (obj != null) { targetPos = obj.transform.position + Vector3.up * 0.25f; foundTarget = true; }
        }
        else if (id.Contains("trash"))
        {
            GameObject obj = GameObject.Find("old_trash");
            if (obj != null) { targetPos = obj.transform.position + Vector3.up * 0.35f; foundTarget = true; }
        }
        else if (id.Contains("candle"))
        {
            CandlePuzzle cp = Object.FindAnyObjectByType<CandlePuzzle>();
            if (cp != null)
            {
                targetPos = cp.transform.position + Vector3.up * 0.3f;
                foundTarget = true;
            }
            else
            {
                GameObject obj = GameObject.Find("CandlePuzzle") ?? GameObject.Find("Candle01b") ?? GameObject.Find("Candle01b_on");
                if (obj != null) { targetPos = obj.transform.position + Vector3.up * 0.3f; foundTarget = true; }
            }
        }
        else if (id.Contains("chest"))
        {
            ChestDialLock chestLock = Object.FindAnyObjectByType<ChestDialLock>();
            if (chestLock != null)
            {
                targetPos = chestLock.transform.position + Vector3.up * 0.45f;
                foundTarget = true;
            }
            else
            {
                GameObject obj = GameObject.Find("Trunk01") ?? GameObject.Find("Chest") ?? GameObject.Find("Chest_Body") ?? GameObject.Find("DialLock_Panel");
                if (obj != null) { targetPos = obj.transform.position + Vector3.up * 0.45f; foundTarget = true; }
            }
        }
        else if (id == "flask" || id == "herb")
        {
            string[] names = id == "flask"
                ? new[] { "Water BOTTLE secret" }
                : new[] { "herb_bowls" };

            if (id == "herb")
            {
                GameObject herbTable = GameObject.Find("Table01");
                if (herbTable != null && herbTable.activeInHierarchy)
                {
                    targetPos = herbTable.transform.position + Vector3.up * 1.5f;
                    foundTarget = true;
                }
            }

            foreach (string name in names)
            {
                if (foundTarget) break;
                GameObject objective = GameObject.Find(name);
                if (objective != null && objective.activeInHierarchy)
                {
                    CollectibleItem collectible = objective.GetComponent<CollectibleItem>() ??
                                                  objective.GetComponentInParent<CollectibleItem>() ??
                                                  objective.GetComponentInChildren<CollectibleItem>();
                    targetPos = objective.transform.position;
                    foundTarget = true;
                    break;
                }
            }

            if (!foundTarget && id == "herb")
            {
                CollectibleItem[] collectibles = Object.FindObjectsByType<CollectibleItem>();
                foreach (CollectibleItem collectible in collectibles)
                {
                    if (collectible != null && collectible.gameObject.activeInHierarchy &&
                        collectible.ItemID.Equals("Herb", System.StringComparison.OrdinalIgnoreCase))
                    {
                        targetPos = collectible.transform.position;
                        foundTarget = true;
                        break;
                    }
                }
            }
        }
        else if (id.Contains("alchemy") || id.Contains("cauldron") || id.Contains("brew"))
        {
            GameObject obj = GameObject.Find("secret bowl ") ?? GameObject.Find("secret bowl") ?? GameObject.Find("AlchemyCauldron");
            if (obj != null && obj.activeInHierarchy)
            {
                targetPos = obj.transform.position;
                foundTarget = true;
            }
        }
        else if (id.Contains("plant") || id.Contains("water"))
        {
            GameObject obj = GameObject.Find("Workbench plant") ?? GameObject.Find("plant") ?? GameObject.Find("Plant");
            if (obj != null && obj.activeInHierarchy)
            {
                targetPos = obj.transform.position + Vector3.up * 1.5f;
                foundTarget = true;
            }
        }
        else if (id.Contains("painting") || id.Contains("memory") || id.Contains("art"))
        {
            Room3MemoryPainting painting = Object.FindAnyObjectByType<Room3MemoryPainting>();
            if (painting != null)
            {
                targetPos = painting.PaintingTargetPosition + Vector3.up * 0.1f;
                foundTarget = true;
            }
            else
            {
                GameObject obj = GameObject.Find("Secret Art") ?? GameObject.Find("SecretArt") ??
                                  GameObject.Find("MemoryPainting") ?? GameObject.Find("Painting");
                if (obj != null)
                {
                    targetPos = obj.transform.position + Vector3.up * 0.3f;
                    foundTarget = true;
                }
            }
        }
        else if (id.Contains("cupboard") || id.Contains("secret_box"))
        {
            Room3ClockHandPickup clockPickup = Object.FindAnyObjectByType<Room3ClockHandPickup>();
            if (clockPickup != null)
            {
                targetPos = (clockPickup.IsCabinetOpened && !clockPickup.IsHandCollected
                    ? clockPickup.HourHandTargetPosition
                    : clockPickup.CabinetTargetPosition) + Vector3.down * 0.2f;
                foundTarget = true;
            }
        }
        else if (id.Contains("clock_hand") || id.Contains("clock"))
        {
            GrandfatherClockPuzzle clockPuz = Object.FindAnyObjectByType<GrandfatherClockPuzzle>();
            if (clockPuz != null)
            {
                targetPos = clockPuz.transform.position + Vector3.up * 1.5f;
                foundTarget = true;
            }
            else
            {
                GameObject obj = GameObject.Find("grandfather_clock") ?? GameObject.Find("GrandfatherClock") ?? GameObject.Find("Clock") ?? GameObject.Find("ClockPuzzle");
                if (obj != null)
                {
                    targetPos = obj.transform.position + Vector3.up * 1.5f;
                    foundTarget = true;
                }
            }
        }
        else if (id.Contains("key"))
        {
            if (Room3KeyPickup.Instance != null)
            {
                targetPos = Room3KeyPickup.Instance.KeyTargetPosition;
                foundTarget = true;
            }
            else
            {
                GameObject obj = GameObject.Find("Room3_GoldenKey") ?? GameObject.Find("key") ?? GameObject.Find("ExitKey") ?? GameObject.Find("ChestKey");
                if (obj != null && obj.activeInHierarchy) { targetPos = obj.transform.position; foundTarget = true; }
                else
                {
                    QuickPlantRevive plant = Object.FindAnyObjectByType<QuickPlantRevive>();
                    if (plant != null) { targetPos = plant.transform.position + Vector3.up * 0.35f; foundTarget = true; }
                }
            }
        }
        else if (id.Contains("door") || id.Contains("roof"))
        {
            GameObject stairsObject = GameObject.Find("Stairs");
            if (stairsObject != null && stairsObject.activeInHierarchy && SceneManager.GetActiveScene().name == "Room2")
            {
                targetPos = stairsObject.transform.position + Vector3.up * 1.0f;
                foundTarget = true;
            }
            else
            {
                GameObject obj = GameObject.Find("Door (1)") ?? GameObject.Find("Door") ?? GameObject.Find("ExitDoor") ?? GameObject.Find("StairsTop") ?? GameObject.Find("Trapdoor");
                if (obj != null) { targetPos = obj.transform.position + Vector3.up * 1.0f; foundTarget = true; }
            }
        }

        if (foundTarget)
        {
            Debug.Log($"[Fairy Hint] Target found for '{id}' at world position {targetPos}");
            MagicalFairyCompanion.Instance.FlyToWorldTarget(targetPos);
        }
        else
        {
            Debug.LogWarning($"[Fairy Hint] No target found for '{id}'. Fairy will stay at its idle position.");
        }
    }

    private string GetRoom2HintTaskId()
    {
        if (!IsTaskDone("flask"))
            return "flask";

        if (!IsTaskDone("herb"))
            return "herb";

        if (!IsTaskDone("alchemy") &&
            (InventoryManager.Instance == null || !InventoryManager.Instance.HasItem("AcidPotion")))
            return "alchemy";

        if (!IsTaskDone("water"))
            return "water";

        return "roof";
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
            tasks.Add(new TaskInfo { id = "flask", title = "Take Water Flask", hint = "Find and take the water flask." });
            tasks.Add(new TaskInfo { id = "herb", title = "Take Herb", hint = "Find and take the herb." });
            tasks.Add(new TaskInfo { id = "alchemy", title = "Brew Potion", hint = "Collect the water flask and herbs, then brew the formula in the cauldron." });
            tasks.Add(new TaskInfo { id = "water", title = "Revive Plant", hint = "Select the acid potion from your bag and revive the magic plant on the table." });
            tasks.Add(new TaskInfo { id = "key", title = "Find Exit Key", hint = "Take the roof exit key revealed by the revived plant." });
            tasks.Add(new TaskInfo { id = "roof", title = "Unlock Roof", hint = "Climb the wooden stairs and use the key on the trapdoor to escape." });
        }
        else if (sceneName == "Room3")
        {
            tasks.Add(new TaskInfo { id = "painting", title = "Inspect Secret Art", hint = "Inspect the secret art to reveal the hidden time clue (8:15)." });
            tasks.Add(new TaskInfo { id = "cupboard", title = "Open the Secret Box", hint = "Open the secret box and take the hour hand for the clock." });
            tasks.Add(new TaskInfo { id = "clock_hand", title = "Place the Hour Hand", hint = "Install the hour hand on the grandfather clock." });
            tasks.Add(new TaskInfo { id = "key", title = "Collect Golden Key", hint = "Take the golden key revealed by the grandfather clock." });
            tasks.Add(new TaskInfo { id = "door", title = "Unlock Exit Door", hint = "Select the key from your bag and click the exit door to complete the room!" });
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
        if (guideCanvas != null && progressBarFill != null && progressPercentageText != null && hintButtonComponent != null && hintButtonText != null && MagicalFairyCompanion.Instance != null)
        {
            return;
        }

        // Search if a ProgressGuideCanvas_HUD already exists in this scene
        GameObject existingCanvas = GameObject.Find("ProgressGuideCanvas_HUD");
        if (existingCanvas != null && (guideCanvas == null || hintButtonComponent == null || hintButtonText == null || MagicalFairyCompanion.Instance == null))
        {
            Destroy(existingCanvas);
        }

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

        // ================= 0. BACK BUTTON (Top-Left: Sleek Glassmorphism Design) =================
        GameObject backBtnObj = new GameObject("Btn_BackToLevels");
        backBtnObj.transform.SetParent(canvas.transform, false);
        RectTransform bRect = backBtnObj.AddComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0f, 1f);
        bRect.anchorMax = new Vector2(0f, 1f);
        bRect.pivot = new Vector2(0f, 1f);
        bRect.anchoredPosition = new Vector2(50, -45); // Safe margin from notch & left edge
        bRect.sizeDelta = new Vector2(140, 56);

        Image bImg = backBtnObj.AddComponent<Image>();
        bImg.color = new Color(0.10f, 0.14f, 0.22f, 0.92f); // Sleek modern dark navy

        Outline bOutline = backBtnObj.AddComponent<Outline>();
        bOutline.effectColor = new Color(0.35f, 0.55f, 0.95f, 0.45f); // Glowing border
        bOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Button backBtn = backBtnObj.AddComponent<Button>();
        ReturnToMainMenuButton retComp = backBtnObj.AddComponent<ReturnToMainMenuButton>();
        retComp.menuSceneName = "MainMenu";
        retComp.openLevelsDirectly = true;

        GameObject bTextObj = new GameObject("BackBtnText");
        bTextObj.transform.SetParent(backBtnObj.transform, false);
        RectTransform btRect = bTextObj.AddComponent<RectTransform>();
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.offsetMin = Vector2.zero;
        btRect.offsetMax = Vector2.zero;
        TextMeshProUGUI btText = bTextObj.AddComponent<TextMeshProUGUI>();
        btText.text = "<b><color=#FFD166>«</color> BACK</b>";
        btText.fontSize = 20;
        btText.alignment = TextAlignmentOptions.Center;
        btText.color = Color.white;

        // ================= 1. HINT BUTTON (Top-Right: Sleek Glassmorphism Design) =================
        GameObject hintBtnObj = new GameObject("Btn_HintSideToggle");
        hintBtnObj.transform.SetParent(canvas.transform, false);
        RectTransform hbRect = hintBtnObj.AddComponent<RectTransform>();
        hbRect.anchorMin = new Vector2(1f, 1f);
        hbRect.anchorMax = new Vector2(1f, 1f);
        hbRect.pivot = new Vector2(1f, 1f);
        hbRect.anchoredPosition = new Vector2(-50, -45); // Safe margin from notch & right edge
        hbRect.sizeDelta = new Vector2(210, 60);

        Image hbImg = hintBtnObj.AddComponent<Image>();
        hbImg.color = new Color(0.10f, 0.14f, 0.22f, 0.92f); // Sleek modern dark navy

        Outline hbOutline = hintBtnObj.AddComponent<Outline>();
        hbOutline.effectColor = new Color(0.35f, 0.55f, 0.95f, 0.45f); // Subtle glowing border
        hbOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Button hintBtn = hintBtnObj.AddComponent<Button>();
        hintBtn.onClick.AddListener(ToggleHintBubble);
        hintButtonComponent = hintBtn;

        GameObject hbTextObj = new GameObject("HintBtnText");
        hbTextObj.transform.SetParent(hintBtnObj.transform, false);
        RectTransform hbtRect = hbTextObj.AddComponent<RectTransform>();
        hbtRect.anchorMin = Vector2.zero;
        hbtRect.anchorMax = Vector2.one;
        hbtRect.offsetMin = Vector2.zero;
        hbtRect.offsetMax = Vector2.zero;
        TextMeshProUGUI hbtText = hbTextObj.AddComponent<TextMeshProUGUI>();
        hbtText.text = "<color=#FFD166><b>HINT</b></color> <size=16><color=#CBD5E1>(1:30)</color></size>";
        hbtText.fontSize = 20;
        hbtText.alignment = TextAlignmentOptions.Center;
        hbtText.color = Color.white;
        hintButtonText = hbtText;

        // ================= 1.5. MAGICAL FAIRY COMPANION (Next to Hint Button) =================
        Sprite fairyIcon = null;
        string p = System.IO.Path.Combine(Application.dataPath, "_Project/Resources/magic_fairy_hint.png");
        if (System.IO.File.Exists(p))
        {
            byte[] raw = System.IO.File.ReadAllBytes(p);
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(raw);
            fairyIcon = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f));
        }
        if (fairyIcon == null)
        {
            fairyIcon = Resources.Load<Sprite>("magic_fairy_hint");
        }
        MagicalFairyCompanion.CreateOnCanvas(canvas, new Vector2(-280, -45), fairyIcon);

        // وضع الحقيبة بجانب زر الرجوع في أعلى اليسار
        RepositionBagButton();

        // ================= 2. TOP PROGRESS BAR (Center) =================
        GameObject topBarContainer = new GameObject("TaskProgressBar_HUD");
        topBarContainer.transform.SetParent(canvas.transform, false);
        RectTransform barRect = topBarContainer.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 1f);
        barRect.anchorMax = new Vector2(0.5f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.anchoredPosition = new Vector2(0, -45);
        barRect.sizeDelta = new Vector2(460, 48);

        Image bgImg = topBarContainer.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);

        Outline barOutline = topBarContainer.AddComponent<Outline>();
        barOutline.effectColor = new Color(0.25f, 0.35f, 0.50f, 0.35f);
        barOutline.effectDistance = new Vector2(1f, -1f);

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
        if (SceneManager.GetActiveScene().name == "Room2")
        {
            Texture2D wt = Texture2D.whiteTexture;
            fillImage.sprite = Sprite.Create(wt, new Rect(0, 0, wt.width, wt.height), new Vector2(0.5f, 0.5f));
        }
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
        pText.fontSize = 19;
        pText.fontStyle = FontStyles.Bold;
        pText.alignment = TextAlignmentOptions.Center;
        pText.color = Color.white;
        pText.text = "0%";
        progressPercentageText = pText;

        // تم إلغاء فقاعة التنبيه (HintBubblePanel) نهائياً بناءً على طلب المستخدم
        GameObject oldBubble = GameObject.Find("HintBubblePanel");
        if (oldBubble != null)
        {
            Destroy(oldBubble);
        }
        hintBubblePanel = null;
        hintBubbleText = null;
    }

    private bool bagRepositioned = false;

    private void RepositionBagButton()
    {
        GameObject bagBtn = GameObject.Find("Inventory_Toggle_Button") ?? GameObject.Find("BagButton");
        if (bagBtn != null && guideCanvas != null)
        {
            bagBtn.transform.SetParent(guideCanvas.transform, false);

            RectTransform rt = bagBtn.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                // زر الرجوع يبدأ من x=50 وعرضه 140 (ينتهي عند x=190). زر الحقيبة يستقر بجانبه بوضوح عند x=215
                rt.anchoredPosition = new Vector2(215f, -41f);
                rt.sizeDelta = new Vector2(65f, 65f);
                rt.localScale = Vector3.one;
                bagRepositioned = true;
            }
        }
    }
}

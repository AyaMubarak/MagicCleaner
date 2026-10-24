using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// مدير قائمة المستويات (Levels Menu Manager)
/// - يعرض زر START عند البداية.
/// - يعرض زر CONTINUE إذا بدأ اللاعب المرحلة ولم ينهها بعد.
/// - يعرض بانر Completed الأخضر عند إنهاء المرحلة بالكامل.
/// - يضمن فتح شاشة المستويات مباشرة عند الرجوع من الغرفة عبر QUIT.
/// </summary>
public class LevelsMenuManager : MonoBehaviour
{
    [Header("لوحة المستويات")]
    public GameObject levelsPanel;

    [Header("المستوى الأول (Level 1)")]
    public Button room1Button;
    public TextMeshProUGUI room1Text;
    public GameObject room1CompletedUI;
    public string room1SceneName = "Room1";

    [Header("المستوى الثاني (Level 2)")]
    public Button room2Button;
    public TextMeshProUGUI room2Text;
    public GameObject room2CompletedUI;
    public string room2SceneName = "Room2";

    [Header("المستوى الثالث (Level 3)")]
    public Button room3Button;
    public TextMeshProUGUI room3Text;
    public GameObject room3CompletedUI;
    public string room3SceneName = "Room3";

    [Header("ألوان نصوص الأزرار")]
    public Color startColor = Color.white;
    public Color continueColor = new Color(0.2f, 0.85f, 1f, 1f); // أزرق سماوي مضيء لـ CONTINUE
    public Color lockedColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);

    [Header("أيقونات القفل")]
    public Sprite openLockSprite;
    public Sprite closedLockSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoadedDirectCheck()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu") return;

        if (PlayerPrefs.GetInt("OpenLevelsDirectly", 0) == 1)
        {
            PlayerPrefs.DeleteKey("OpenLevelsDirectly");
            PlayerPrefs.Save();

            LevelsMenuManager[] managers = Object.FindObjectsByType<LevelsMenuManager>(FindObjectsInactive.Include);
            if (managers != null && managers.Length > 0)
            {
                LevelsMenuManager mgr = managers[0];
                if (mgr.levelsPanel != null)
                {
                    mgr.levelsPanel.SetActive(true);
                }
                else
                {
                    mgr.gameObject.SetActive(true);
                }
                mgr.OpenLevelsMenu();
            }

            // تفعيل توهج زر LEVELS وإطفاء PLAY في القائمة الرئيسية
            MainMenuNavigation nav = Object.FindAnyObjectByType<MainMenuNavigation>();
            if (nav != null)
            {
                nav.SelectLevels();
            }
        }
    }

    private void Awake()
    {
        ResolveTextReferences();
        CheckDirectOpen();
    }

    private void Start()
    {
        CheckDirectOpen();
        UpdateLevelsState();
        SetupPlayButtonDirectLaunch();
    }

    private void SetupPlayButtonDirectLaunch()
    {
        // ربط زر PLAY في الواجهة الرئيسية ليبدأ اللعبة فوراً بتشغيل الفيديو الأول ثم الدخول للمرحلة الأولى مباشرة
        GameObject playBtnObj = GameObject.Find("Btn_Play");
        if (playBtnObj != null)
        {
            Button playBtn = playBtnObj.GetComponent<Button>();
            if (playBtn != null)
            {
                playBtn.onClick.RemoveListener(PlayGameDirectly);
                playBtn.onClick.AddListener(PlayGameDirectly);
            }
        }
    }

    public void PlayGameDirectly()
    {
        // تشغيل فيديو البداية ثم الدخول مباشرة للمرحلة الأولى بدون الحاجة لفتح القائمة والضغط على الليفل
        StartCoroutine(LoadSceneFast(room1SceneName, null));
    }

    private void OnEnable()
    {
        ResolveTextReferences();
        UpdateLevelsState();
    }

    private void CheckDirectOpen()
    {
        if (PlayerPrefs.GetInt("OpenLevelsDirectly", 0) == 1)
        {
            PlayerPrefs.DeleteKey("OpenLevelsDirectly");
            PlayerPrefs.Save();
            OpenLevelsMenu();
        }
    }

    private void ResolveTextReferences()
    {
        if (room1Button != null)
        {
            var txt = room1Button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (txt != null) room1Text = txt;
        }

        if (room2Button != null)
        {
            var txt = room2Button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (txt != null) room2Text = txt;
        }

        if (room3Button != null)
        {
            var txt = room3Button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (txt != null) room3Text = txt;
        }
    }

    public void UpdateLevelsState()
    {
        ResolveTextReferences();

        float room1Pct = PlayerPrefs.GetFloat($"{room1SceneName}_ProgressPct", 0f);
        bool isRoom1Done = PlayerPrefs.GetInt("Room1_Done", 0) == 1 || 
                           PlayerPrefs.GetInt("Level1_Done", 0) == 1 || 
                           PlayerPrefs.GetInt("Room1_Won", 0) == 1 ||
                           room1Pct >= 0.99f || Mathf.RoundToInt(room1Pct * 100f) >= 100;

        bool isRoom2Done = PlayerPrefs.GetInt("Room2_Done", 0) == 1 || PlayerPrefs.GetInt("Level2_Done", 0) == 1;
        bool isRoom3Done = PlayerPrefs.GetInt("Room3_Done", 0) == 1 || PlayerPrefs.GetInt("Level3_Done", 0) == 1;

        bool isRoom2Unlocked = isRoom1Done;

        // ================= LEVEL 1 =================
        SetupLevelSlot(
            isCompleted: isRoom1Done,
            isUnlocked: true,
            button: room1Button,
            buttonText: room1Text,
            completedUI: room1CompletedUI,
            sceneName: room1SceneName
        );

        // ================= LEVEL 2 =================
        SetupLevelSlot(
            isCompleted: isRoom2Done,
            isUnlocked: isRoom2Unlocked,
            button: room2Button,
            buttonText: room2Text,
            completedUI: room2CompletedUI,
            sceneName: room2SceneName
        );

        // ================= LEVEL 3 =================
        SetupLevelSlot(
            isCompleted: isRoom3Done,
            isUnlocked: isRoom2Done,
            button: room3Button,
            buttonText: room3Text,
            completedUI: room3CompletedUI,
            sceneName: room3SceneName
        );
    }

    private void SetupLevelSlot(bool isCompleted, bool isUnlocked, Button button, TextMeshProUGUI buttonText, GameObject completedUI, string sceneName)
    {
        float progressPct = PlayerPrefs.GetFloat($"{sceneName}_ProgressPct", 0f);
        bool hasStarted = PlayerPrefs.GetInt($"{sceneName}_Started", 0) == 1;

        // إذا وصلت نسبة الإنجاز 100% تعتبر المرحلة مكتملة فوراً
        if (progressPct >= 0.99f || Mathf.RoundToInt(progressPct * 100f) >= 100)
        {
            isCompleted = true;
        }

        if (button != null)
        {
            button.gameObject.SetActive(true);
        }

        if (isCompleted)
        {
            // المرحلة مكتملة (100%): إظهار بانر COMPLETED الأخضر والإطار الذهبي (وليس CONTINUE)
            if (completedUI != null)
            {
                completedUI.SetActive(true);

                // التأكد من تفعيل الإطار الذهبي الجانبي الخاص بالبطاقة
                Transform goldenBorder = completedUI.transform.Find("Image");
                if (goldenBorder != null) goldenBorder.gameObject.SetActive(true);

                // النص يظهر COMPLETED بشكل احترافي وواضح
                TextMeshProUGUI compText = completedUI.GetComponentInChildren<TextMeshProUGUI>();
                if (compText != null)
                {
                    compText.text = "COMPLETED";
                    compText.fontStyle = FontStyles.Bold;
                    compText.fontSize = 24f;
                    compText.color = Color.white;
                    compText.alignment = TextAlignmentOptions.Center;
                }

                Button replayBtn = completedUI.GetComponent<Button>();
                if (replayBtn == null) replayBtn = completedUI.AddComponent<Button>();
                replayBtn.interactable = true;
                replayBtn.onClick.RemoveAllListeners();
                replayBtn.onClick.AddListener(() =>
                {
                    // إعادة المرحلة من البداية: تصفير تقدم هذا المستوى بالكامل لتبدأ كل ألغازه وعناصره من الصفر
                    ResetSceneData(sceneName);
                    StartCoroutine(LoadSceneFast(sceneName, replayBtn));
                });
            }

            if (button != null)
            {
                // الضغط على البطاقة أيضاً يسمح بالدخول وإعادة اللعبة من البداية
                button.interactable = true;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    ResetSceneData(sceneName);
                    StartCoroutine(LoadSceneFast(sceneName, button));
                });
            }

            if (buttonText != null)
            {
                buttonText.gameObject.SetActive(false);
            }
        }
        else if (isUnlocked)
        {
            // المرحلة مفتوحة ولم تكتمل بعد: إخفاء بانر Completed والإطار الذهبي تماماً
            if (completedUI != null)
            {
                completedUI.SetActive(false);
                Transform goldenBorder = completedUI.transform.Find("Image");
                if (goldenBorder != null) goldenBorder.gameObject.SetActive(false);
            }

            if (button != null)
            {
                button.interactable = true;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => StartCoroutine(LoadSceneFast(sceneName, button)));
            }

            if (buttonText != null)
            {
                buttonText.gameObject.SetActive(true);

                // Make sure text doesn't wrap or get cut off and fits cleanly
                buttonText.textWrappingMode = TextWrappingModes.NoWrap;
                buttonText.overflowMode = TextOverflowModes.Ellipsis;
                buttonText.alignment = TextAlignmentOptions.Center;
                buttonText.fontStyle = FontStyles.Bold;
                buttonText.characterSpacing = 1.5f;

                // وضعية وحجم النص ليملأ مساحة الزر بشكل متناسق وموزون
                // Keep original scene layout for buttonText

                // إذا بدأ اللاعب ولم ينهِ -> CONTINUE (33%) بلون متناسق وواضح، وإلا -> START
                if (hasStarted || progressPct > 0f)
                {
                    int pct = Mathf.RoundToInt(progressPct * 100f);
                    buttonText.text = $"CONTINUE  ({pct}%)";
                    buttonText.color = Color.white;
                    buttonText.fontSize = 20f;
                }
                else
                {
                    buttonText.text = "Start";
                    buttonText.color = startColor;
                    buttonText.fontSize = 22f;
                }
            }

            // ضبط موضع وحجم أيقونة الفتح أو إخفائها إن كانت تشوش على النص
            AdjustButtonIcon(button, true);
        }
        else
        {
            // المرحلة مقفلة LOCKED
            if (completedUI != null) completedUI.SetActive(false);

            if (button != null)
            {
                button.interactable = false;
            }

            if (buttonText != null)
            {
                buttonText.gameObject.SetActive(true);
                buttonText.textWrappingMode = TextWrappingModes.NoWrap;
                buttonText.overflowMode = TextOverflowModes.Ellipsis;
                buttonText.alignment = TextAlignmentOptions.Center;
                buttonText.fontStyle = FontStyles.Bold;
                buttonText.characterSpacing = 2.5f;
                buttonText.text = "Locked";
                buttonText.color = lockedColor;
                buttonText.fontSize = 22f;

                // Keep original scene layout for buttonText
            }

            AdjustButtonIcon(button, false);
        }

        UpdateMiniProgressBar(button, sceneName, progressPct, isCompleted, isUnlocked);
    }

    private static Sprite cachedOpenLockSprite = null;
    private static Sprite cachedClosedLockSprite = null;

    private void AdjustButtonIcon(Button button, bool isUnlocked)
    {
        if (button == null) return;

        // البحث عن أيقونة القفل "new icons" التابعة للزر
        Transform iconT = button.transform.Find("new icons");
        if (iconT != null)
        {
            iconT.gameObject.SetActive(true);
            RectTransform iconRt = iconT.GetComponent<RectTransform>();
            Image iconImg = iconT.GetComponent<Image>();

            // Keep original scene layout for iconRt

            if (iconImg != null)
            {
                if (isUnlocked)
                {
                    // تحميل أيقونة القفل المفتوح (Open Padlock)
                    if (openLockSprite != null)
                    {
                        cachedOpenLockSprite = openLockSprite;
                    }

                    if (cachedOpenLockSprite == null)
                    {
                        cachedOpenLockSprite = Resources.Load<Sprite>("padlock_open") ?? Resources.Load<Sprite>("padlock") ?? 
                                               Resources.Load<Sprite>("padlock_0");
                        if (cachedOpenLockSprite == null)
                        {
                            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                            foreach (var s in allSprites)
                            {
                                if (s != null && (s.name == "padlock_0" || s.name == "padlock"))
                                {
                                    cachedOpenLockSprite = s;
                                    break;
                                }
                            }
                        }
                    }

                    if (cachedOpenLockSprite != null)
                    {
                        iconImg.sprite = cachedOpenLockSprite;
                    }

                    // لون الأيقونة في المرحلة المفتوحة مشرق بلون ذهبي ناصع متناسق
                    iconImg.color = new Color(1f, 0.85f, 0.4f, 1f);
                }
                else
                {
                    // قفل مغلق للمراحل المقفلة LOCKED
                    if (closedLockSprite != null)
                    {
                        cachedClosedLockSprite = closedLockSprite;
                    }

                    if (cachedClosedLockSprite == null)
                    {
                        Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                        foreach (var s in allSprites)
                        {
                            if (s != null && (s.name == "padlock (1)_0" || s.name == "padlock (1)"))
                            {
                                cachedClosedLockSprite = s;
                                break;
                            }
                        }
                    }

                    if (cachedClosedLockSprite != null)
                    {
                        iconImg.sprite = cachedClosedLockSprite;
                    }

                    // لون الأيقونة في المرحلة المقفلة هادئ مع النص
                    iconImg.color = new Color(0.85f, 0.85f, 0.85f, 0.8f);
                }
            }
        }
    }

    private void UpdateMiniProgressBar(Button button, string sceneName, float progressPct, bool isCompleted, bool isUnlocked)
    {
        if (button == null) return;

        // إزالة أي بار كان قد أُنشئ سابقاً بالكامل حتى لا يظهر أي أثر له
        string barName = "MiniProgressHUD_" + sceneName;
        Transform existing = button.transform.Find(barName);
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }
    }

    private IEnumerator LoadSceneFast(string sceneName, Button btn)
    {
        if (btn != null) btn.interactable = false;

        bool isLevel1 = (sceneName == room1SceneName);

        // التحقق إن كانت المرحلة في حالة CONTINUE (تم البدء بها أو هناك تقدم) أو جديدة
        float progressPct = PlayerPrefs.GetFloat($"{sceneName}_ProgressPct", 0f);
        bool hasStarted = PlayerPrefs.GetInt($"{sceneName}_Started", 0) == 1;
        bool isContinueState = hasStarted || progressPct > 0f;

        // 1. إذا كانت المرحلة الأولى: تشغيل فيديو البداية فقط إذا كان الليفل جديداً (ولأول مرة في اللعبة كلها)، وممنوع عرضه بحالة CONTINUE
        bool introAlreadyPlayed = PlayerPrefs.GetInt("IntroVideo_PlayedOnce", 0) == 1;
        if (isLevel1 && !isContinueState && !introAlreadyPlayed && UniversalVideoPlayer.Instance != null &&
            (UniversalVideoPlayer.Instance.backstoryVideoClip != null || !string.IsNullOrEmpty(UniversalVideoPlayer.Instance.backstoryVideoUrl)))
        {
            bool proceed = false;
            bool cancelled = false;

            UniversalVideoPlayer.Instance.TryPlayBackstory(
                onFinishCallback: () => { proceed = true; },
                onBackCallback: () => { cancelled = true; }
            );

            while (!proceed && !cancelled)
            {
                yield return null;
            }

            if (cancelled)
            {
                if (btn != null) btn.interactable = true;
                yield break; // إلغاء الدخول والرجوع لقائمة المستويات
            }
        }
        // 2. إذا كانت مرحلة تالية (Room2, Room3...): الدخول مباشرة لبدء اللعبة
        // (حيث أن فيديو الانتقال تم عرضه بالفعل عند إنهاء المرحلة السابقة وفتح الباب)
        else if (!isLevel1)
        {
            // دخول فوري ومباشر لبدء المرحلة الثانية
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    public void OpenLevelsMenu()
    {
        if (levelsPanel != null)
        {
            levelsPanel.SetActive(true);
            UpdateLevelsState();
        }

        // إضاءة زر LEVELS في القائمة وإطفاء باقي التوهجات
        MainMenuNavigation nav = FindAnyObjectByType<MainMenuNavigation>();
        if (nav != null)
        {
            nav.SelectLevels();
        }
    }

    public void CloseLevelsMenu()
    {
        if (levelsPanel != null)
        {
            levelsPanel.SetActive(false);
        }

        MainMenuNavigation nav = FindAnyObjectByType<MainMenuNavigation>();
        if (nav != null)
        {
            nav.ClearAllGlows();
        }
    }

    private void ResetSceneData(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        // قائمة المهمات والألغاز المربوطة بالمشهد
        string[] taskIds = new string[]
        {
            "trash", "dust", "candle", "chest", "key", "door",
            "flask", "herb", "alchemy", "water", "roof",
            "painting", "memory", "clock_hand", "clock", "cupboard"
        };
        foreach (string t in taskIds)
        {
            PlayerPrefs.DeleteKey($"{sceneName}_Task_{t}");
        }

        // مسح نسبة الإنجاز وحالة البدء لهذا المشهد فقط
        PlayerPrefs.DeleteKey($"{sceneName}_ProgressPct");
        PlayerPrefs.DeleteKey($"{sceneName}_Started");

        // مسح حالة الفوز للغرفة الأولى حتى تعود الغبرة والعتمة وتبدأ اللعبة من الأول والجديد
        if (sceneName == room1SceneName || sceneName == "Room1")
        {
            PlayerPrefs.DeleteKey("Room1_Done");
            PlayerPrefs.DeleteKey("Room1_Won");
            PlayerPrefs.DeleteKey("Room1_DoorOpened");
            PlayerPrefs.DeleteKey("Level1_Done");
            PlayerPrefs.DeleteKey("Room2_Unlocked");
            PlayerPrefs.DeleteKey("Level2_Unlocked");
        }

        if (sceneName == "Room2")
        {
            PlayerPrefs.DeleteKey("Room2_Done");
            PlayerPrefs.DeleteKey("Level2_Done");
        }

        if (sceneName == "Room3")
        {
            PlayerPrefs.DeleteKey("Room3_Done");
            PlayerPrefs.DeleteKey("Level3_Done");
            PlayerPrefs.DeleteKey("Room3_DoorOpened");
            PlayerPrefs.DeleteKey("Room3_ClockSolved");
            PlayerPrefs.DeleteKey("Room3_HandInstalled");
            PlayerPrefs.DeleteKey("Room3_CabinetOpened");
            PlayerPrefs.DeleteKey("Room3_HourHandCollected");
            PlayerPrefs.DeleteKey("HasItem_ClockHand");
            PlayerPrefs.DeleteKey("HasItem_Room3Key");
            PlayerPrefs.DeleteKey("Room3_Task_painting");
            PlayerPrefs.DeleteKey("Room3_Task_cupboard");
            PlayerPrefs.DeleteKey("Room3_Task_clock_hand");
            PlayerPrefs.DeleteKey("Room3_Task_key");
            PlayerPrefs.DeleteKey("Room3_Task_door");
        }

        // مسح مفاتيح مشاهدة الفيديوهات حتى تعود للعمل مع اللعبة الجديدة
        PlayerPrefs.DeleteKey("IntroVideo_PlayedOnce");
        PlayerPrefs.DeleteKey("ItemVideo_Played_Book");
        PlayerPrefs.DeleteKey("ItemVideo_Played_Wall");
        PlayerPrefs.DeleteKey("BackstoryVideoPlayed");
        PlayerPrefs.DeleteKey("ItemVideo_Played_SecretBook");
        PlayerPrefs.DeleteKey("ItemVideo_Played_Candle");
        PlayerPrefs.DeleteKey("ItemVideo_Played_SecretCode");
        PlayerPrefs.DeleteKey("ItemVideo_Played_WallSecretCode");
        PlayerPrefs.DeleteKey("Video_Played_gemini_generated_video_45dcfa05");
        PlayerPrefs.DeleteKey("Video_Played_gemini_generated_video_ff76d4f1");

        // مسح محتويات الحقيبة لبدء اللعبة من جديد
        InventoryManager.ResetInventoryData();

        PlayerPrefs.Save();
        Debug.Log($"<color=cyan>[LevelsMenu]:</color> Scene data for '{sceneName}' was reset for fresh replay.");
    }

    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("<color=cyan>[LevelsMenu]:</color> All saved data wiped clean! Starting fresh.");
        UpdateLevelsState();
    }
}

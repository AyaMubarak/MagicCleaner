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

        // ربط زر PLAY حتى لو كانت LevelsPanel مخفية (Start لا يعمل على كائن غير مفعّل)
        LevelsMenuManager[] allManagers = Object.FindObjectsByType<LevelsMenuManager>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        if (allManagers != null && allManagers.Length > 0)
        {
            allManagers[0].SetupPlayButtonDirectLaunch();
        }

        if (PlayerPrefs.GetInt("OpenLevelsDirectly", 0) == 1)
        {
            PlayerPrefs.DeleteKey("OpenLevelsDirectly");
            PlayerPrefs.Save();

            LevelsMenuManager[] managers = Object.FindObjectsByType<LevelsMenuManager>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
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
        // تحديد المرحلة التي وصل إليها اللاعب لمتابعة اللعب من حيث توقف:
        // 1. إذا كان اللاعب في منتصف أو بداية المرحلة الأولى (أو لأول مرة) -> Room1
        // 2. إذا أنهى المرحلة الأولى ووصل للثانية (سواء في بدايتها أو منتصفها) -> Room2
        // 3. إذا أنهى المرحلة الثانية ووصل للثالثة -> Room3
        // 4. إذا كانت أول مرة تشغيل -> Room1 ويتم عرض فيديو البداية تلقائياً
        float room1Pct = PlayerPrefs.GetFloat($"{room1SceneName}_ProgressPct", 0f);
        bool isRoom1Done = PlayerPrefs.GetInt("Room1_Done", 0) == 1 || 
                           PlayerPrefs.GetInt("Level1_Done", 0) == 1 || 
                           PlayerPrefs.GetInt("Room1_DoorOpened", 0) == 1 || 
                           PlayerPrefs.GetInt("Room1_Won", 0) == 1 ||
                           room1Pct >= 0.99f || Mathf.RoundToInt(room1Pct * 100f) >= 100;

        float room2Pct = PlayerPrefs.GetFloat($"{room2SceneName}_ProgressPct", 0f);
        bool isRoom2Done = PlayerPrefs.GetInt("Room2_Done", 0) == 1 || 
                           PlayerPrefs.GetInt("Level2_Done", 0) == 1 || 
                           PlayerPrefs.GetInt("Room2_DoorOpened", 0) == 1 || 
                           PlayerPrefs.GetInt("Room2_Task_roof", 0) == 1 ||
                           room2Pct >= 0.99f || Mathf.RoundToInt(room2Pct * 100f) >= 100;

        float room3Pct = PlayerPrefs.GetFloat($"{room3SceneName}_ProgressPct", 0f);
        bool isRoom3Done = PlayerPrefs.GetInt("Room3_Done", 0) == 1 || 
                           PlayerPrefs.GetInt("Level3_Done", 0) == 1 || 
                           PlayerPrefs.GetInt("Room3_DoorOpened", 0) == 1 ||
                           room3Pct >= 0.99f || Mathf.RoundToInt(room3Pct * 100f) >= 100;

        string targetScene = room1SceneName;

        if (!isRoom1Done)
        {
            // لم ينهِ المرحلة الأولى بعد (سواء أول مرة أو في منتصفها)
            targetScene = room1SceneName;
        }
        else if (!isRoom2Done)
        {
            // أنهى المرحلة الأولى وهو حالياً في المرحلة الثانية
            targetScene = room2SceneName;
        }
        else if (!isRoom3Done)
        {
            // أنهى الأولى والثانية وهو حالياً في المرحلة الثالثة
            targetScene = room3SceneName;
        }
        else
        {
            // أنهى جميع المراحل، يقوم بتصفير تقدم اللعبة بالكامل لبدء اللعبة من جديد بنظافة
            ResetSceneData(room1SceneName);
            ResetSceneData(room2SceneName);
            ResetSceneData(room3SceneName);
            targetScene = room1SceneName;
        }

        MonoBehaviour runner = this;
        if (!runner.gameObject.activeInHierarchy)
        {
            if (UniversalVideoPlayer.Instance != null && UniversalVideoPlayer.Instance.gameObject.activeInHierarchy)
            {
                runner = UniversalVideoPlayer.Instance;
            }
            else
            {
                MainMenuNavigation nav = Object.FindAnyObjectByType<MainMenuNavigation>();
                if (nav != null && nav.gameObject.activeInHierarchy)
                {
                    runner = nav;
                }
            }
        }

        runner.StartCoroutine(LoadSceneFast(targetScene, null));
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
            var status = room1Button.transform.Find("LevelStatus");
            var txt = status != null ? status.GetComponent<TextMeshProUGUI>() : room1Button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (txt != null) room1Text = txt;
        }

        if (room2Button != null)
        {
            var status = room2Button.transform.Find("LevelStatus");
            var txt = status != null ? status.GetComponent<TextMeshProUGUI>() : room2Button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (txt != null) room2Text = txt;
        }

        if (room3Button != null)
        {
            var status = room3Button.transform.Find("LevelStatus");
            var txt = status != null ? status.GetComponent<TextMeshProUGUI>() : room3Button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (txt != null) room3Text = txt;
        }
    }

    public void UpdateLevelsState()
    {
        ResolveTextReferences();

        float room1Pct = PlayerPrefs.GetFloat($"{room1SceneName}_ProgressPct", 0f);
        bool isRoom1Done = PlayerPrefs.GetInt("Room1_Done", 0) == 1 || 
                           PlayerPrefs.GetInt("Level1_Done", 0) == 1 || 
                           PlayerPrefs.GetInt("Room1_DoorOpened", 0) == 1 || 
                           PlayerPrefs.GetInt("Room1_Won", 0) == 1 ||
                           room1Pct >= 0.99f || Mathf.RoundToInt(room1Pct * 100f) >= 100;

        bool isRoom2Done = PlayerPrefs.GetInt("Room2_Done", 0) == 1 
                        || PlayerPrefs.GetInt("Level2_Done", 0) == 1
                        || PlayerPrefs.GetInt("Room2_Task_roof", 0) == 1;

        bool isRoom3Done = PlayerPrefs.GetInt("Room3_Done", 0) == 1 || PlayerPrefs.GetInt("Level3_Done", 0) == 1 || PlayerPrefs.GetInt("Room3_DoorOpened", 0) == 1;

        bool isRoom2Unlocked = isRoom1Done || PlayerPrefs.GetInt("Room2_Unlocked", 0) == 1;
        bool isRoom3Unlocked = isRoom2Done || PlayerPrefs.GetInt("Room3_Unlocked", 0) == 1;

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
            isUnlocked: isRoom3Unlocked,
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

                // النص يظهر Completed بشكل واضح وعلى سطر واحد دون انقسام
                TextMeshProUGUI compText = completedUI.GetComponentInChildren<TextMeshProUGUI>();
                if (compText != null)
                {
                    compText.text = "Completed";
                    compText.fontStyle = FontStyles.Bold;
                    compText.textWrappingMode = TextWrappingModes.NoWrap;
                    compText.overflowMode = TextOverflowModes.Ellipsis;
                    compText.fontSize = 20f;
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
                // إخفاء أيقونة القفل/الأدوات عند اكتمال المرحلة حتى لا تتداخل مع بانر Completed
                Transform iconT = button.transform.Find("new icons");
                if (iconT != null) iconT.gameObject.SetActive(false);

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

                // إذا بدأ اللاعب ولم ينهِ -> CONTINUE (33%) بحجم خط أصغر لتبقى الجملة كاملة على سطر واحد
                if (hasStarted || progressPct > 0f)
                {
                    int pct = Mathf.RoundToInt(progressPct * 100f);
                    buttonText.text = $"CONTINUE ({pct}%)";
                    buttonText.color = Color.white;
                    buttonText.fontSize = 17f; // حجم أصغر ومدروس لتظهر الجملة والنسبة متكاملة دون انقسام
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

    private static Sprite cachedToolsSprite = null;
    private static Sprite cachedLockedPadlockSprite = null;

    private void AdjustButtonIcon(Button button, bool isUnlocked)
    {
        if (button == null) return;

        // البحث عن أيقونة "new icons" التابعة للزر
        Transform iconT = button.transform.Find("new icons");
        if (iconT != null)
        {
            iconT.gameObject.SetActive(true);
            Image iconImg = iconT.GetComponent<Image>();

            if (iconImg != null)
            {
                if (isUnlocked)
                {
                    // عندما تكون المرحلة Start / Unlocked: نستخدم دائماً أيقونة tools الأصلية الخاصة بك من الكانفاس
                    if (cachedToolsSprite == null)
                    {
                        cachedToolsSprite = openLockSprite;
                    }

                    if (cachedToolsSprite == null)
                    {
                        if (room1Button != null)
                        {
                            Transform r1Icon = room1Button.transform.Find("new icons");
                            if (r1Icon != null)
                            {
                                Image r1Img = r1Icon.GetComponent<Image>();
                                if (r1Img != null && r1Img.sprite != null)
                                {
                                    cachedToolsSprite = r1Img.sprite;
                                }
                            }
                        }

                        if (cachedToolsSprite == null)
                        {
                            cachedToolsSprite = Resources.Load<Sprite>("tools");
                        }

                        if (cachedToolsSprite == null)
                        {
                            Texture2D tex = Resources.Load<Texture2D>("tools");
                            if (tex != null)
                            {
                                cachedToolsSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                            }
                        }

                        if (cachedToolsSprite == null)
                        {
                            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                            foreach (var s in allSprites)
                            {
                                if (s != null && (s.name.StartsWith("tools", System.StringComparison.OrdinalIgnoreCase)))
                                {
                                    cachedToolsSprite = s;
                                    break;
                                }
                            }
                        }
                    }

                    if (cachedToolsSprite != null)
                    {
                        iconImg.sprite = cachedToolsSprite;
                    }

                    // الحفاظ على لون وشكل الأيقونة الأصلي
                    iconImg.color = Color.white;
                }
                else
                {
                    // عندما تكون المرحلة مقفلة Locked: استخدام أيقونة padlock (1) الأصلية الخاصة بك
                    if (cachedLockedPadlockSprite == null)
                    {
                        cachedLockedPadlockSprite = closedLockSprite
                                                 ?? Resources.Load<Sprite>("padlock (1)")
                                                 ?? Resources.Load<Sprite>("padlock_closed");
                    }

                    if (cachedLockedPadlockSprite == null)
                    {
                        Texture2D tex = Resources.Load<Texture2D>("padlock (1)") ?? Resources.Load<Texture2D>("padlock_closed");
                        if (tex != null)
                        {
                            cachedLockedPadlockSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        }
                    }

                    if (cachedLockedPadlockSprite == null)
                    {
                        Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                        foreach (var s in allSprites)
                        {
                            if (s != null && (s.name.StartsWith("padlock (1)", System.StringComparison.OrdinalIgnoreCase)))
                            {
                                cachedLockedPadlockSprite = s;
                                break;
                            }
                        }
                    }

                    if (cachedLockedPadlockSprite != null)
                    {
                        iconImg.sprite = cachedLockedPadlockSprite;
                    }

                    iconImg.color = Color.white;
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

    public void ResetSceneData(string sceneName)
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

        // مسح نسبة الإنجاز وحالة البدء وأرقام الصندوق لهذا المشهد فقط
        PlayerPrefs.DeleteKey($"{sceneName}_ProgressPct");
        PlayerPrefs.DeleteKey($"{sceneName}_Started");
        PlayerPrefs.DeleteKey($"{sceneName}_Chest_EnteredCode");

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
            PlayerPrefs.DeleteKey("Room2_DoorOpened");
            PlayerPrefs.DeleteKey("Room2_Task_roof");
            PlayerPrefs.DeleteKey("Room3_Unlocked");
            PlayerPrefs.DeleteKey("Level3_Unlocked");
            PlayerPrefs.DeleteKey("HasItem_Herb");
            PlayerPrefs.DeleteKey("HasItem_WaterFlask");
            PlayerPrefs.DeleteKey("HasItem_AcidPotion");
            PlayerPrefs.DeleteKey("HasItem_ExitKey");
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
            PlayerPrefs.DeleteKey("HasItem_GoldenKey");
            PlayerPrefs.DeleteKey("HasItem_MemoryPainting");
            PlayerPrefs.DeleteKey("Room3_Task_painting");
            PlayerPrefs.DeleteKey("Room3_Task_cupboard");
            PlayerPrefs.DeleteKey("Room3_Task_clock_hand");
            PlayerPrefs.DeleteKey("Room3_Task_key");
            PlayerPrefs.DeleteKey("Room3_Task_door");
            PlayerPrefs.DeleteKey("Room3_ProgressPct");
            PlayerPrefs.DeleteKey("Room3_Started");
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

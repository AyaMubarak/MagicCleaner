using UnityEngine;

public class MainMenuNavigation : MonoBehaviour
{
    [Header("توهجات الأزرار الثلاثة")]
    public GameObject glowPlay;
    public GameObject glowLevels;
    public GameObject glowSettings;

    // دالة تُستدعى عند الضغط على PLAY
    public void SelectPlay()
    {
        SetGlowState(glowPlay);
        LaunchCurrentGameLevel();
    }

    private void Start()
    {
        // ربط زر Btn_Play أيضاً تلقائياً لضمان استجابته 100%
        GameObject playBtnObj = GameObject.Find("Btn_Play");
        if (playBtnObj != null)
        {
            UnityEngine.UI.Button playBtn = playBtnObj.GetComponent<UnityEngine.UI.Button>();
            if (playBtn != null)
            {
                playBtn.onClick.RemoveListener(SelectPlay);
                playBtn.onClick.AddListener(SelectPlay);
            }
        }
    }

    public void LaunchCurrentGameLevel()
    {
        string room1SceneName = "Room1";
        string room2SceneName = "Room2";
        string room3SceneName = "Room3";

        // التحقق من حالة كل مرحلة بدقة متناهية:
        // 1. أول مرة (أو لم ينهِ Room1) -> Room1
        // 2. إذا خرج من اللعبة وهو في Room1 (has started ولم يكتمل) -> يكمل Room1
        // 3. إذا كان Room1 مكتمل (Done / 100%) و Room2 لم تكتمل بعد -> ينتقل لـ Room2 ويكمل Room2
        // 4. إذا كانت Room2 مكتملة و Room3 لم تكتمل بعد -> ينتقل لـ Room3 ويكمل Room3
        // 5. إذا كانت جميع الغرف مكتملة -> Room1
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
            targetScene = room1SceneName;
        }
        else if (!isRoom2Done)
        {
            targetScene = room2SceneName;
        }
        else if (!isRoom3Done)
        {
            targetScene = room3SceneName;
        }
        else
        {
            // عند اكتمال جميع المراحل والضغط على PLAY:
            // إعادة تصفير مراحل اللعبة بالكامل لبدء اللعبة من أول وجديد بدون أي مشاكل
            ResetAllGameProgress();
            targetScene = room1SceneName;
        }

        Debug.Log($"[MainMenuNavigation] Launching level: {targetScene} (R1 Done: {isRoom1Done}, R2 Done: {isRoom2Done}, R3 Done: {isRoom3Done})");

        StartCoroutine(LoadTargetScene(targetScene));
    }

    private void ResetAllGameProgress()
    {
        LevelsMenuManager levelsMgr = Object.FindAnyObjectByType<LevelsMenuManager>(FindObjectsInactive.Include);
        if (levelsMgr != null)
        {
            levelsMgr.ResetSceneData("Room1");
            levelsMgr.ResetSceneData("Room2");
            levelsMgr.ResetSceneData("Room3");
        }
        else
        {
            // تفريغ يدوي احترافي شامل لجميع مهمات وتقدم المراحل الثلاث
            string[] scenes = new string[] { "Room1", "Room2", "Room3" };
            string[] tasks = new string[]
            {
                "trash", "dust", "candle", "chest", "key", "door",
                "flask", "herb", "alchemy", "water", "roof",
                "painting", "memory", "clock_hand", "clock", "cupboard"
            };

            foreach (var s in scenes)
            {
                PlayerPrefs.DeleteKey($"{s}_ProgressPct");
                PlayerPrefs.DeleteKey($"{s}_Started");
                PlayerPrefs.DeleteKey($"{s}_Chest_EnteredCode");
                PlayerPrefs.DeleteKey($"{s}_Done");
                PlayerPrefs.DeleteKey($"{s}_DoorOpened");
                foreach (var t in tasks)
                {
                    PlayerPrefs.DeleteKey($"{s}_Task_{t}");
                }
            }

            PlayerPrefs.DeleteKey("Room1_Won");
            PlayerPrefs.DeleteKey("Level1_Done");
            PlayerPrefs.DeleteKey("Room2_Unlocked");
            PlayerPrefs.DeleteKey("Level2_Unlocked");
            PlayerPrefs.DeleteKey("Level2_Done");
            PlayerPrefs.DeleteKey("Room2_Task_roof");
            PlayerPrefs.DeleteKey("Room3_Unlocked");
            PlayerPrefs.DeleteKey("Level3_Unlocked");
            PlayerPrefs.DeleteKey("Level3_Done");

            PlayerPrefs.DeleteKey("HasItem_Herb");
            PlayerPrefs.DeleteKey("HasItem_WaterFlask");
            PlayerPrefs.DeleteKey("HasItem_AcidPotion");
            PlayerPrefs.DeleteKey("HasItem_ExitKey");
            PlayerPrefs.DeleteKey("HasItem_ClockHand");
            PlayerPrefs.DeleteKey("HasItem_Room3Key");
            PlayerPrefs.DeleteKey("HasItem_GoldenKey");
            PlayerPrefs.DeleteKey("HasItem_MemoryPainting");

            PlayerPrefs.DeleteKey("IntroVideo_PlayedOnce");
            PlayerPrefs.DeleteKey("BackstoryVideoPlayed");
            InventoryManager.ResetInventoryData();
            PlayerPrefs.Save();
        }
    }

    private System.Collections.IEnumerator LoadTargetScene(string sceneName)
    {
        // في حال كانت المرحلة الأولى ولأول مرة في اللعبة، نشغل فيديو البداية إذا وجد
        float progressPct = PlayerPrefs.GetFloat($"{sceneName}_ProgressPct", 0f);
        bool hasStarted = PlayerPrefs.GetInt($"{sceneName}_Started", 0) == 1;
        bool isContinueState = hasStarted || progressPct > 0f;
        bool introAlreadyPlayed = PlayerPrefs.GetInt("IntroVideo_PlayedOnce", 0) == 1;

        if (sceneName == "Room1" && !isContinueState && !introAlreadyPlayed && UniversalVideoPlayer.Instance != null &&
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
                yield break;
            }
        }

        UnityEngine.AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    // دالة تُستدعى عند الضغط على LEVELS
    public void SelectLevels()
    {
        SetGlowState(glowLevels);
    }

    // دالة تُستدعى عند الضغط على SETTINGS
    public void SelectSettings()
    {
        SetGlowState(glowSettings);
    }

    // دالة لإطفاء التوهج عن كل الأزرار (تُستدعى عند الرجوع أو إغلاق الإعدادات)
    public void ClearAllGlows()
    {
        if (glowPlay != null) glowPlay.SetActive(false);
        if (glowLevels != null) glowLevels.SetActive(false);
        if (glowSettings != null) glowSettings.SetActive(false);
    }

    // دالة مساعدة لتفعيل الزر المختار فقط وإطفاء الباقي
    private void SetGlowState(GameObject activeGlow)
    {
        ClearAllGlows();
        if (activeGlow != null)
        {
            activeGlow.SetActive(true);
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// مشغل الفيديو العام للعبة (Universal Video Popup & Cutscene Player)
/// يتحكم بالفيديوهات الأربعة المطلوبة بدقة:
/// 1. فيديو البداية (Intro / Backstory): gemini_generated_video_164539a7.mp4
/// 2. فيديو الكتاب السري (Book): gemini_generated_video_45dcfa05.mp4
/// 3. فيديو الجدار والشمعة (Wall & Candle): gemini_generated_video_ff76d4f1.mp4
/// 4. فيديو نهاية اللعبة والمرحلة (End of Game): gemini_generated_video_055f4c89.mp4
/// </summary>
[RequireComponent(typeof(VideoPlayer), typeof(AudioSource))]
public class UniversalVideoPlayer : MonoBehaviour
{
    public static UniversalVideoPlayer Instance { get; private set; }

    [System.Serializable]
    public class ItemVideoMapping
    {
        public string itemId;
        public VideoClip videoClip;
        public string videoUrl;
    }

    [Header("عناصر الحقيبة والفيديوهات المقترنة بها")]
    public List<ItemVideoMapping> itemVideos = new List<ItemVideoMapping>();

    [Header("فيديوهات اللعبة الأساسية")]
    public VideoClip backstoryVideoClip;
    public string backstoryVideoUrl;
    public VideoClip nextLevelVideoClip;
    public string nextLevelVideoUrl;

    // المكونات الداخلية
    private Canvas videoCanvas;
    private RawImage videoDisplayImage;
    private VideoPlayer videoPlayer;
    private AudioSource audioSource;
    private RenderTexture renderTexture;
    private GameObject videoContainer;
    
    private Button skipButton;
    private Button backButton;

    private Action onVideoFinishedCallback;
    private Action onVideoBackCancelledCallback;

    private static bool hasPlayedIntroThisSession = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null && FindAnyObjectByType<UniversalVideoPlayer>() == null)
        {
            GameObject videoObj = new GameObject("UniversalVideoPlayer");
            videoObj.AddComponent<UniversalVideoPlayer>();
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
        DontDestroyOnLoad(gameObject);
        SetupVideoUI();
        LoadDefaultUrls();
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoadedCheck;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoadedCheck;
    }

    private void Start()
    {
        StartCoroutine(DeferredPlayBackstory());
    }

    private void OnSceneLoadedCheck(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        StartCoroutine(DeferredPlayBackstory());
    }

    private IEnumerator DeferredPlayBackstory()
    {
        yield return new WaitForSeconds(0.25f);
        if (PlayerPrefs.GetInt("IntroVideo_PlayedOnce", 0) == 1) yield break;

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "Room1" || sceneName == "Level_01_ForgottenRoom")
        {
            bool dustDone = PlayerPrefs.GetInt($"{sceneName}_Task_dust", 0) == 1;
            bool trashDone = PlayerPrefs.GetInt($"{sceneName}_Task_trash", 0) == 1;
            bool candleDone = PlayerPrefs.GetInt($"{sceneName}_Task_candle", 0) == 1;
            float progressPct = PlayerPrefs.GetFloat($"{sceneName}_ProgressPct", 0f);

            // تشغيل فيديو البداية فقط إذا كانت الغرفة جديدة تماماً عند 0% ولأول مرة حصراً
            if (!dustDone && !trashDone && !candleDone && progressPct <= 0.01f && !hasPlayedIntroThisSession)
            {
                hasPlayedIntroThisSession = true;
                PlayIntroVideo();
            }
        }
    }

    public static string GetStreamingVideoPath(string fileName)
    {
        string direct = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
        return direct.Replace("\\", "/");
    }

    private void LoadDefaultUrls()
    {
        backstoryVideoUrl = GetStreamingVideoPath("gemini_generated_video_164539a7.mp4");
        nextLevelVideoUrl = GetStreamingVideoPath("gemini_generated_video_055f4c89.mp4");

        itemVideos = new List<ItemVideoMapping>
        {
            new ItemVideoMapping { itemId = "SecretBook", videoUrl = GetStreamingVideoPath("gemini_generated_video_45dcfa05.mp4") },
            new ItemVideoMapping { itemId = "Book", videoUrl = GetStreamingVideoPath("gemini_generated_video_45dcfa05.mp4") },
            new ItemVideoMapping { itemId = "Candle", videoUrl = GetStreamingVideoPath("gemini_generated_video_ff76d4f1.mp4") },
            new ItemVideoMapping { itemId = "SecretCode", videoUrl = GetStreamingVideoPath("gemini_generated_video_ff76d4f1.mp4") },
            new ItemVideoMapping { itemId = "Wall", videoUrl = GetStreamingVideoPath("gemini_generated_video_ff76d4f1.mp4") },
            new ItemVideoMapping { itemId = "WallSecretCode", videoUrl = GetStreamingVideoPath("gemini_generated_video_ff76d4f1.mp4") },
            new ItemVideoMapping { itemId = "MemoryPainting", videoUrl = GetStreamingVideoPath("Untitled design.mp4") },
            new ItemVideoMapping { itemId = "Painting", videoUrl = GetStreamingVideoPath("Untitled design.mp4") },
            new ItemVideoMapping { itemId = "Herb", videoUrl = GetStreamingVideoPath("gemini_generated_video_96c2170b.mp4") },
            new ItemVideoMapping { itemId = "Herbs", videoUrl = GetStreamingVideoPath("gemini_generated_video_96c2170b.mp4") }
        };
    }

    private void SetupVideoUI()
    {
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;

            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            videoPlayer.SetTargetAudioSource(0, audioSource);

            videoPlayer.loopPointReached += OnVideoEndReached;
            videoPlayer.errorReceived += OnVideoError;
        }

        if (videoCanvas == null)
        {
            GameObject canvasObj = new GameObject("VideoPopupCanvas");
            canvasObj.transform.SetParent(transform);
            videoCanvas = canvasObj.AddComponent<Canvas>();
            videoCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            videoCanvas.sortingOrder = 9999;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            videoContainer = new GameObject("VideoContainer");
            videoContainer.transform.SetParent(canvasObj.transform, false);
            RectTransform containerRt = videoContainer.AddComponent<RectTransform>();
            containerRt.anchorMin = Vector2.zero;
            containerRt.anchorMax = Vector2.one;
            containerRt.offsetMin = Vector2.zero;
            containerRt.offsetMax = Vector2.zero;

            Image bgImage = videoContainer.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.98f);

            GameObject rawImageObj = new GameObject("VideoDisplay");
            rawImageObj.transform.SetParent(videoContainer.transform, false);
            RectTransform rawRt = rawImageObj.AddComponent<RectTransform>();
            rawRt.anchorMin = new Vector2(0.04f, 0.04f);
            rawRt.anchorMax = new Vector2(0.96f, 0.96f);
            rawRt.offsetMin = Vector2.zero;
            rawRt.offsetMax = Vector2.zero;

            videoDisplayImage = rawImageObj.AddComponent<RawImage>();
            videoDisplayImage.color = Color.white;

            renderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            renderTexture.Create();
            videoPlayer.targetTexture = renderTexture;
            videoDisplayImage.texture = renderTexture;

            skipButton = CreateTopButton(videoContainer.transform, "SkipButton", "Skip >", new Vector2(1f, 1f), new Vector2(-60, -45), new Vector2(1f, 1f));
            skipButton.onClick.AddListener(SkipVideo);

            backButton = CreateTopButton(videoContainer.transform, "BackButton", "< Back", new Vector2(0f, 1f), new Vector2(60, -45), new Vector2(0f, 1f));
            backButton.onClick.AddListener(BackVideo);

            videoContainer.SetActive(false);
        }
    }

    private Button CreateTopButton(Transform parent, string name, string text, Vector2 anchor, Vector2 pos, Vector2 pivot)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(150, 48);

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.08f, 0.10f, 0.18f, 0.92f);

        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.70f, 0.35f, 0.75f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 0.95f, 0.8f, 1f);
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        btn.colors = cb;

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TMPro.TextMeshProUGUI tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 20;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.characterSpacing = 2f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.92f, 0.75f, 1f);
        tmp.raycastTarget = false;

        return btn;
    }

    // ================== الفيديوهات الأربعة المطلوبة ==================

    /// <summary>
    /// 1. فيديو البداية (gemini_generated_video_164539a7) - يشتغل مرة واحدة فقط في اللعبة كلها
    /// </summary>
    public void PlayIntroVideo(Action onComplete = null, Action onBack = null)
    {
        // التحقق من أنه لم يُعرض مسبقاً في اللعبة
        if (PlayerPrefs.GetInt("IntroVideo_PlayedOnce", 0) == 1)
        {
            onComplete?.Invoke();
            return;
        }

        PlayerPrefs.SetInt("IntroVideo_PlayedOnce", 1);
        PlayerPrefs.Save();

        string path = GetStreamingVideoPath("gemini_generated_video_164539a7.mp4");
        Debug.Log("<color=cyan>[UniversalVideoPlayer]:</color> تشغيل فيديو البداية (لأول مرة فقط): " + path);
        PlayVideoUrl(path, onComplete, onBack);
    }

    /// <summary>
    /// 2. فيديو الكتاب (gemini_generated_video_45dcfa05) - عند تنظيف وأخذ الكتاب
    /// </summary>
    public void PlayBookVideo(Action onComplete = null)
    {
        string path = GetStreamingVideoPath("gemini_generated_video_45dcfa05.mp4");
        Debug.Log("<color=cyan>[UniversalVideoPlayer]:</color> تشغيل فيديو الكتاب: " + path);
        PlayVideoUrl(path, onComplete, null);
    }

    /// <summary>
    /// 3. فيديو الجدار والشمعة (gemini_generated_video_ff76d4f1) - عند إشعال الشمعة وكشف كود الجدار
    /// </summary>
    public void PlayWallVideo(Action onComplete = null)
    {
        string path = GetStreamingVideoPath("gemini_generated_video_ff76d4f1.mp4");
        Debug.Log("<color=cyan>[UniversalVideoPlayer]:</color> تشغيل فيديو الجدار والشمعة: " + path);
        PlayVideoUrl(path, onComplete, null);
    }

    /// <summary>
    /// 4. فيديو نهاية اللعبة والمرحلة (gemini_generated_video_055f4c89) - عند فتح الباب بنجاح
    /// </summary>
    public void PlayEndGameVideo(Action onComplete = null)
    {
        string path = GetStreamingVideoPath("gemini_generated_video_055f4c89.mp4");
        Debug.Log("<color=cyan>[UniversalVideoPlayer]:</color> تشغيل فيديو نهاية المرحلة: " + path);
        PlayVideoUrl(path, onComplete, null);
    }

    /// <summary>
    /// 5. فيديو لوحة الذكريات (Untitled design.mp4) - عند فحص لوحة الذكريات في الغرفة 3
    /// </summary>
    public void PlayMemoryPaintingVideo(Action onComplete = null)
    {
        string path = GetStreamingVideoPath("Untitled design.mp4");
        Debug.Log("<color=cyan>[UniversalVideoPlayer]:</color> تشغيل فيديو لوحة الذكريات: " + path);
        PlayVideoUrl(path, onComplete, null);
    }

    /// <summary>
    /// 6. فيديو الأعشاب (gemini_generated_video_96c2170b.mp4) - عند التقاط الأعشاب في الغرفة 2
    /// </summary>
    public void PlayHerbVideo(Action onComplete = null)
    {
        string path = GetStreamingVideoPath("gemini_generated_video_96c2170b.mp4");
        Debug.Log("<color=cyan>[UniversalVideoPlayer]:</color> تشغيل فيديو الأعشاب: " + path);
        PlayVideoUrl(path, onComplete, null);
    }

    /// <summary>
    /// 7. فيديو نهاية المرحلة الثانية (Untitled design (1).mp4) - عند إكمال المرحلة الثانية والخروج من السطح
    /// </summary>
    public void PlayRoom2EndVideo(Action onComplete = null)
    {
        string path = GetStreamingVideoPath("Untitled design (1).mp4");
        Debug.Log("<color=cyan>[UniversalVideoPlayer]:</color> تشغيل فيديو نهاية المرحلة الثانية: " + path);
        PlayVideoUrl(path, onComplete, null);
    }

    /// <summary>
    /// 8. فيديو نهاية المرحلة الثالثة (gemini_generated_video_9286ebd6.mp4) - عند فتح باب الغرفة 3 وإنهاء المرحلة
    /// </summary>
    public void PlayRoom3EndVideo(Action onComplete = null)
    {
        string path = GetStreamingVideoPath("gemini_generated_video_9286ebd6.mp4");
        Debug.Log("<color=cyan>[UniversalVideoPlayer]:</color> تشغيل فيديو نهاية المرحلة الثالثة: " + path);
        PlayVideoUrl(path, onComplete, null);
    }

    // ================== دوال التوافق القديمة ==================

    public bool TryPlayBackstory(Action onFinishCallback, Action onBackCallback = null)
    {
        if (PlayerPrefs.GetInt("IntroVideo_PlayedOnce", 0) == 1)
        {
            onFinishCallback?.Invoke();
            return false;
        }
        PlayIntroVideo(onFinishCallback, onBackCallback);
        return true;
    }

    public bool TryPlayItemVideo(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        string lower = itemId.ToLower();

        if (lower.Contains("book"))
        {
            if (PlayerPrefs.GetInt("ItemVideo_Played_Book", 0) == 0)
            {
                PlayerPrefs.SetInt("ItemVideo_Played_Book", 1);
                PlayerPrefs.Save();
                PlayBookVideo();
                return true;
            }
        }
        else if (lower.Contains("candle") || lower.Contains("wall") || lower.Contains("code"))
        {
            if (PlayerPrefs.GetInt("ItemVideo_Played_Wall", 0) == 0)
            {
                PlayerPrefs.SetInt("ItemVideo_Played_Wall", 1);
                PlayerPrefs.Save();
                PlayWallVideo();
                return true;
            }
        }
        else if (lower.Contains("painting") || lower.Contains("memory") || lower.Contains("time"))
        {
            PlayMemoryPaintingVideo();
            return true;
        }
        return false;
    }

    public void PlayNextLevelCutscene(Action onCompleteCallback = null, Action onBackCallback = null)
    {
        PlayEndGameVideo(onCompleteCallback);
    }

    public void PlayVideo(VideoClip clip, Action onComplete = null, Action onBack = null)
    {
        if (clip == null)
        {
            onComplete?.Invoke();
            return;
        }

        onVideoFinishedCallback = onComplete;
        onVideoBackCancelledCallback = onBack;
        SetupVideoUI();

        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = clip;
        StartPlayback();
    }

    public void PlayVideoUrl(string url, Action onComplete = null, Action onBack = null)
    {
        if (string.IsNullOrEmpty(url))
        {
            onComplete?.Invoke();
            return;
        }

        onVideoFinishedCallback = onComplete;
        onVideoBackCancelledCallback = onBack;
        SetupVideoUI();

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        StartPlayback();
    }

    private void StartPlayback()
    {
        if (videoContainer != null)
        {
            videoContainer.SetActive(true);
            videoContainer.transform.SetAsLastSibling();
        }

        if (videoCanvas != null)
        {
            videoCanvas.enabled = true;
        }

        if (videoDisplayImage != null)
        {
            videoDisplayImage.color = Color.white;
        }

        if (videoPlayer != null)
        {
            videoPlayer.Stop();

            if (renderTexture != null && !renderTexture.IsCreated())
            {
                renderTexture.Create();
            }
            videoPlayer.targetTexture = renderTexture;
            if (videoDisplayImage != null)
            {
                videoDisplayImage.texture = renderTexture;
            }

            videoPlayer.Play();
            if (audioSource != null)
            {
                audioSource.Play();
            }
            Debug.Log("<color=green>[UniversalVideoPlayer]:</color> بدأ تشغيل الفيديو بنجاح!");
        }
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError($"<color=red>[UniversalVideoPlayer Error]:</color> {message}");
        CompleteVideoAndTriggerNext();
    }

    private void OnVideoEndReached(VideoPlayer source)
    {
        CompleteVideoAndTriggerNext();
    }

    public void SkipVideo()
    {
        CompleteVideoAndTriggerNext();
    }

    public void BackVideo()
    {
        StopInternal();
        Action backCb = onVideoBackCancelledCallback;
        onVideoFinishedCallback = null;
        onVideoBackCancelledCallback = null;
        backCb?.Invoke();
    }

    private void CompleteVideoAndTriggerNext()
    {
        StopInternal();
        Action finishCb = onVideoFinishedCallback;
        onVideoFinishedCallback = null;
        onVideoBackCancelledCallback = null;
        finishCb?.Invoke();
    }

    private void StopInternal()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        if (videoContainer != null)
        {
            videoContainer.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
}

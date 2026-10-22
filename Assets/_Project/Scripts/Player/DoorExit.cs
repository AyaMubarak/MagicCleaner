using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorExit : MonoBehaviour
{
    [Header("Key Requirement")]
    [Tooltip("اسم معرف المفتاح")]
    [SerializeField] private string requiredItemID = "ChestKey";

    [Header("Scene Transition")]
    [Tooltip("اسم مشهد القائمة الرئيسية بدقة")]
    [SerializeField] private string menuSceneName = "MainMenu";

    [Header("Feedback")]
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip lockedSound;

    private Camera mainCam;

    private static AudioSource bgmSource = null;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void Start()
    {
        // تشغيل موسيقى خلفية هادئة للغرفة (Background Music)
        if (bgmSource == null)
        {
            GameObject bgmObj = new GameObject("Room_BGM");
            bgmSource = bgmObj.AddComponent<AudioSource>();
            bgmSource.clip = ProceduralAudioGenerator.CreateRuntimeClip("room_music");
            bgmSource.loop = true;
            bgmSource.volume = 0.35f;
            bgmSource.playOnAwake = false;
            bgmSource.Play();
            DontDestroyOnLoad(bgmObj);
        }
        else if (!bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

    private void OnMouseDown()
    {
        TryOpenDoor();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            CheckDoorClick(Input.mousePosition);
        }
        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            CheckDoorClick(Input.GetTouch(0).position);
        }
    }

    private void CheckDoorClick(Vector2 screenPos)
    {
        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        Ray ray = mainCam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform) || hit.transform.name.ToLower().Contains("door"))
            {
                TryOpenDoor();
            }
        }
    }

    private void TryOpenDoor()
    {
        // 1. التحقق من وجود المفتاح في حوزة اللاعب (عبر KeyInteraction أو الحقيبة)
        bool hasKeyInInventory = KeyInteraction.HasKey || 
            (InventoryManager.Instance != null && (InventoryManager.Instance.HasItem(requiredItemID) || InventoryManager.Instance.HasItem("key")));

        // إذا لم يلتقط اللاعب المفتاح نهائياً:
        if (!hasKeyInInventory)
        {
            Debug.Log("<color=red>[Door]:</color> الباب مقفل بإحكام! يجب إيجاد المفتاح أولاً.");
            if (lockedSound != null)
            {
                AudioSource.PlayClipAtPoint(lockedSound, transform.position);
            }
            else
            {
                ProceduralAudioGenerator.PlaySound("door_locked", 0.8f, transform.position);
            }
            return;
        }

        // 2. التحقق من أن اللاعب قد ضغط على المفتاح في الحقيبة وقام بتحديده (Selected Item)
        bool isKeySelected = false;
        if (InventoryManager.Instance != null)
        {
            string selected = InventoryManager.Instance.SelectedItem;
            if (!string.IsNullOrEmpty(selected) && 
                (selected.Equals(requiredItemID, System.StringComparison.OrdinalIgnoreCase) || 
                 selected.IndexOf("key", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                isKeySelected = true;
            }
        }

        // إذا كان المفتاح معه لكنه لم يكبس عليه في الحقيبة أولاً:
        if (!isKeySelected)
        {
            Debug.Log("<color=yellow>[Door]:</color> افتح الحقيبة واكبس على المفتاح لتحديده، ثم اكبس على الباب!");
            if (lockedSound != null)
            {
                AudioSource.PlayClipAtPoint(lockedSound, transform.position);
            }
            else
            {
                ProceduralAudioGenerator.PlaySound("door_locked", 0.8f, transform.position);
            }

            // إظهار الحقيبة للاعب حتى يكبس على المفتاح
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.ShowInventoryTemporarily();
            }
            return;
        }

        // 3. تم النقر على المفتاح وتحديده ثم النقر على الباب -> يفتح الباب بنجاح!
        Debug.Log("<color=green>[Door]:</color> تم فتح الباب بنجاح باستخدام المفتاح المحدد!");

        if (doorOpenSound != null)
        {
            AudioSource.PlayClipAtPoint(doorOpenSound, transform.position);
        }
        else
        {
            ProceduralAudioGenerator.PlaySound("door_open", 0.95f, transform.position);
        }

        // تشغيل لحن احتفالي محمس وفخم لإنهاء المستوى الأول (Victory Fanfare)
        ProceduralAudioGenerator.PlaySound("level_complete", 1.0f);

        // إيقاف موسيقى الغرفة للانتقال للمرحلة التالية
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }

        // إبلاغ دليل التقدم وحفظ فتح الباب فوراً
        LevelProgressGuide.ReportTask("door");
        PlayerPrefs.SetInt("Room1_DoorOpened", 1);
        PlayerPrefs.SetInt("Room1_Done", 1);
        PlayerPrefs.SetInt("Level1_Done", 1);
        PlayerPrefs.SetInt("OpenLevelsDirectly", 1); // حتى تفتح شاشة الـ Levels فوراً
        PlayerPrefs.Save();

        // إضاءة البيت بالكامل وجعله يضوي بنور ساطع وعالي جداً عند الفوز!
        BrightenHouseCompletely();

        // إطلاق فتيش وحركات ألعاب نارية احتفالية ومبهجة
        StartCoroutine(LaunchFireworksCelebration());

        // إظهار شعار احتفالي محمس على الشاشة (LEVEL 1 COMPLETED!)
        ShowLevelCompleteBanner();

        // إزالة المفتاح من الحقيبة بعد استخدامه
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RemoveItem(requiredItemID);
            InventoryManager.Instance.RemoveItem("key");
            InventoryManager.Instance.ClearSelection();
        }

        // تأخير للاستمتاع برؤية البيت منور وبالمفرقعات والألعاب النارية والاحتفال
        StartCoroutine(WaitAndProceedToMenu());
    }

    private void BrightenHouseCompletely()
    {
        // 1. جعل إضاءة الغرفة البيئية مشمسة وساطعة ونقية وعالية جداً
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientIntensity = 1.50f;
        RenderSettings.ambientLight = new Color(1f, 0.98f, 0.94f); // نور ذهبي نهاري دافئ ومشرق

        // 2. إضاءة كافة الأضواء في الغرفة إلى أقصى درجات الإشراق
        Light[] lights = FindObjectsByType<Light>();
        foreach (Light l in lights)
        {
            if (l.type == LightType.Directional)
            {
                l.intensity = 1.50f;
                l.color = new Color(1f, 0.96f, 0.90f);
            }
            else if (l.type == LightType.Point)
            {
                l.intensity = 1.8f;
                l.range = Mathf.Max(l.range, 12f);
            }
        }

        // 3. تخفيف وإخفاء الغبار تماماً لتبدو الغرفة نظيفة ومرتبة
        if (RoomDustAtmosphere.Instance != null)
        {
            RoomDustAtmosphere.Instance.ApplyProgressiveRoomLighting();
        }
    }

    private System.Collections.IEnumerator LaunchFireworksCelebration()
    {
        // إطلاق دفعات فتيش وألعاب نارية صوتية ومرئية
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            for (int f = 0; f < 6; f++)
            {
                ProceduralAudioGenerator.PlaySound("firework_pop", 0.9f);
                SpawnConfettiFireworkBurst(canvas.transform);
                yield return new WaitForSeconds(0.35f);
            }
        }
    }

    private void SpawnConfettiFireworkBurst(Transform parent)
    {
        GameObject burst = new GameObject("FireworkBurst", typeof(RectTransform));
        burst.transform.SetParent(parent, false);

        RectTransform bRt = burst.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0.5f, 0.5f);
        bRt.anchorMax = new Vector2(0.5f, 0.5f);
        bRt.anchoredPosition = new Vector2(UnityEngine.Random.Range(-350f, 350f), UnityEngine.Random.Range(-100f, 320f));

        Color[] colors = new Color[] {
            new Color(1f, 0.84f, 0f),      // ذهبي
            new Color(0.2f, 0.85f, 0.4f),  // زمردي
            new Color(1f, 0.25f, 0.45f),   // قرمزي
            new Color(0.3f, 0.75f, 1f),    // سماوي
            new Color(1f, 0.55f, 0.1f)     // برتقالي
        };

        for (int i = 0; i < 18; i++)
        {
            GameObject spark = new GameObject("Spark", typeof(RectTransform));
            spark.transform.SetParent(burst.transform, false);

            RectTransform sRt = spark.GetComponent<RectTransform>();
            sRt.sizeDelta = new Vector2(UnityEngine.Random.Range(10f, 22f), UnityEngine.Random.Range(10f, 22f));

            UnityEngine.UI.Image img = spark.AddComponent<UnityEngine.UI.Image>();
            img.color = colors[UnityEngine.Random.Range(0, colors.Length)];

            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = UnityEngine.Random.Range(80f, 220f);
            Vector2 target = new Vector2(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist);

            StartCoroutine(AnimateSpark(sRt, img, target));
        }

        Destroy(burst, 1.8f);
    }

    private System.Collections.IEnumerator AnimateSpark(RectTransform rt, UnityEngine.UI.Image img, Vector2 targetPos)
    {
        float elapsed = 0f;
        float duration = 1.2f;
        Vector2 startPos = rt.anchoredPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, Mathf.Sin(t * Mathf.PI * 0.5f));
                rt.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.zero, t * t);
            }
            if (img != null)
            {
                Color c = img.color;
                c.a = 1f - (t * t);
                img.color = c;
            }
            yield return null;
        }
    }

    private System.Collections.IEnumerator WaitAndProceedToMenu()
    {
        // إعطاء اللاعب وقتاً (2.0 ثانية) ليعيش الفوز المبهج ويرى البيت منور والمفرقعات
        yield return new WaitForSeconds(2.0f);

        // تشغيل فيديو الانتقال للغرفة الثانية بحالة فتح المفتاح للباب
        if (UniversalVideoPlayer.Instance != null && (UniversalVideoPlayer.Instance.nextLevelVideoClip != null || !string.IsNullOrEmpty(UniversalVideoPlayer.Instance.nextLevelVideoUrl)))
        {
            UniversalVideoPlayer.Instance.PlayNextLevelCutscene(() =>
            {
                PlayerPrefs.SetInt("OpenLevelsDirectly", 1);
                PlayerPrefs.Save();
                if (!string.IsNullOrEmpty(menuSceneName))
                {
                    SceneManager.LoadScene(menuSceneName);
                }
            });
        }
        else
        {
            PlayerPrefs.SetInt("OpenLevelsDirectly", 1);
            PlayerPrefs.Save();
            if (!string.IsNullOrEmpty(menuSceneName))
            {
                SceneManager.LoadScene(menuSceneName);
            }
        }
    }

    private void ShowLevelCompleteBanner()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject banner = new GameObject("LevelCompleteBanner", typeof(RectTransform));
        banner.transform.SetParent(canvas.transform, false);
        Destroy(banner, 5f);

        RectTransform rt = banner.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(620f, 170f);

        UnityEngine.UI.Image img = banner.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.04f, 0.07f, 0.14f, 0.94f);

        UnityEngine.UI.Outline outline = banner.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(0.98f, 0.82f, 0.25f, 0.98f);
        outline.effectDistance = new Vector2(3f, -3f);

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(banner.transform, false);
        RectTransform tRt = textObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;

        TMPro.TextMeshProUGUI tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = "★ LEVEL 1 COMPLETED! ★\n<size=24><color=#4ADE80>CONGRATULATIONS!</color></size>";
        tmp.fontSize = 32;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = new Color(0.98f, 0.88f, 0.40f, 1f);
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

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

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void Start()
    {
        ProceduralAudioGenerator.EnsureBackgroundMusic();
        EnsureDoorCollider();
    }

    private void EnsureDoorCollider()
    {
        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc == null)
        {
            bc = gameObject.AddComponent<BoxCollider>();
        }
        bc.enabled = true;
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
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            var eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = screenPos };
            var results = new List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);
            foreach (var r in results)
            {
                if (r.gameObject != null && (r.gameObject.GetComponentInParent<UnityEngine.UI.Button>() != null ||
                    r.gameObject.GetComponentInParent<VirtualJoystick>() != null))
                {
                    return;
                }
            }
        }

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        Ray ray = mainCam.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        foreach (var hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform) || hit.transform.name.ToLower().Contains("door"))
            {
                TryOpenDoor();
                break;
            }
        }
    }

    private void TryOpenDoor()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        // 1. التحقق من وجود المفتاح في حوزة اللاعب (يدعم GoldenKey للغرفة 3 و ChestKey للغرفة 1)
        bool hasKeyInInventory = KeyInteraction.HasKey || 
            (InventoryManager.Instance != null && (
                InventoryManager.Instance.HasItem(requiredItemID) ||
                InventoryManager.Instance.HasItem("GoldenKey") ||
                InventoryManager.Instance.HasItem("Room3Key") ||
                InventoryManager.Instance.HasItem("ChestKey") ||
                InventoryManager.Instance.HasItem("key"))) ||
            PlayerPrefs.GetInt($"{sceneName}_Task_key", 0) == 1;

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
                 selected.Equals("GoldenKey", System.StringComparison.OrdinalIgnoreCase) ||
                 selected.Equals("Room3Key", System.StringComparison.OrdinalIgnoreCase) ||
                 selected.IndexOf("key", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                isKeySelected = true;
            }
        }

        // إذا كان المفتاح معه بالحقيبة، نعتبره مفعلاً حتى يفتح الباب بسلاسة ومباشرة!
        if (!isKeySelected && hasKeyInInventory)
        {
            isKeySelected = true;
        }

        // 3. تم النقر على الباب والمفتاح موجود -> يفتح الباب بنجاح!
        Debug.Log("<color=green>[Door]:</color> تم فتح الباب بنجاح باستخدام المفتاح!");

        if (doorOpenSound != null)
        {
            AudioSource.PlayClipAtPoint(doorOpenSound, transform.position);
        }
        else
        {
            ProceduralAudioGenerator.PlaySound("door_open", 0.95f, transform.position);
        }

        // تشغيل لحن احتفالي محمس وفخم لإنهاء المستوى (Victory Fanfare)
        ProceduralAudioGenerator.PlaySound("level_complete", 1.0f);

        // إيقاف موسيقى الغرفة للانتقال للمرحلة التالية
        // إبلاغ دليل التقدم وحفظ فتح الباب فوراً
        LevelProgressGuide.ReportTask("door");

        if (sceneName == "Room3")
        {
            PlayerPrefs.SetInt("Room3_DoorOpened", 1);
            PlayerPrefs.SetInt("Room3_Done", 1);
            PlayerPrefs.SetInt("Level3_Done", 1);
        }
        else
        {
            PlayerPrefs.SetInt("Room1_DoorOpened", 1);
            PlayerPrefs.SetInt("Room1_Done", 1);
            PlayerPrefs.SetInt("Level1_Done", 1);
        }

        PlayerPrefs.SetInt("OpenLevelsDirectly", 1); // حتى تفتح شاشة الـ Levels فوراً
        PlayerPrefs.Save();

        // إضاءة البيت بالكامل وجعله يضوي بنور ساطع وعالي جداً عند الفوز!
        BrightenHouseCompletely();

        // إطلاق فتيش وحركات ألعاب نارية احتفالية ومبهجة
        StartCoroutine(LaunchFireworksCelebration());

        // إظهار شعار احتفالي محمس على الشاشة
        ShowLevelCompleteBanner();

        // إزالة المفتاح من الحقيبة بعد استخدامه
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RemoveItem(requiredItemID);
            InventoryManager.Instance.RemoveItem("GoldenKey");
            InventoryManager.Instance.RemoveItem("Room3Key");
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
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
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

        string currentScene = SceneManager.GetActiveScene().name;

        // إذا كانت المرحلة الثالثة: تشغيل فيديو نهاية المرحلة 3 (gemini_generated_video_9286ebd6.mp4)
        if (currentScene == "Room3")
        {
            if (UniversalVideoPlayer.Instance != null)
            {
                UniversalVideoPlayer.Instance.PlayRoom3EndVideo(() =>
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
                var uvp = FindAnyObjectByType<UniversalVideoPlayer>();
                if (uvp != null)
                {
                    uvp.PlayRoom3EndVideo(() =>
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
        }
        // إذا كانت المرحلة الأولى: تشغيل فيديو الانتقال للمرحلة الثانية
        else if (UniversalVideoPlayer.Instance != null && (UniversalVideoPlayer.Instance.nextLevelVideoClip != null || !string.IsNullOrEmpty(UniversalVideoPlayer.Instance.nextLevelVideoUrl)))
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

        string currentScene = SceneManager.GetActiveScene().name;
        string bannerTitle = (currentScene == "Room3") ? "★ LEVEL 3 COMPLETED! ★" : "★ LEVEL 1 COMPLETED! ★";

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
        tmp.text = $"{bannerTitle}\n<size=24><color=#4ADE80>CONGRATULATIONS!</color></size>";
        tmp.fontSize = 32;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = new Color(0.98f, 0.88f, 0.40f, 1f);
    }
}

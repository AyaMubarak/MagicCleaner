using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DustCleaner : MonoBehaviour, IDragHandler
{
    [Header("UI Reference")]
    [SerializeField] private Image dustImage;

    [Header("Settings")]
    [SerializeField] private float cleanSpeed = 0.015f; // سرعة اختفاء الغبار مع كل مسحة
    private float currentAlpha = 1f;
    private bool isCleaned = false;

    [Header("Book Inventory Reward")]
    [SerializeField] private Sprite bookIcon;          // اسحبي صورة الكتاب هنا لتظهر في الحقيبة
    [SerializeField] private GameObject worldBookObject; // اسحبي مجسم الكتاب الموجود في الغرفة هنا

    private void Start()
    {
        if (dustImage == null)
            dustImage = GetComponent<Image>();

        if (dustImage != null)
        {
            currentAlpha = dustImage.color.a;

            // تحسين شكل طبقة الغبار لتظهر بلون كحلي دخاني متناسق مع لون الكتاب
            dustImage.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);
            
            RectTransform dustRt = dustImage.rectTransform;
            if (dustRt != null)
            {
                dustRt.anchorMin = Vector2.zero;
                dustRt.anchorMax = Vector2.one;
                dustRt.offsetMin = new Vector2(16, 18);
                dustRt.offsetMax = new Vector2(-16, -18);
            }

            Outline dustOutline = GetComponent<Outline>();
            if (dustOutline == null) dustOutline = gameObject.AddComponent<Outline>();
            dustOutline.effectColor = new Color(0.06f, 0.08f, 0.14f, 0.9f);
            dustOutline.effectDistance = new Vector2(2f, -2f);

            // تدمير أي نصوص قديمة متبقية كانت تظهر كمربعات
            Transform oldRune = transform.Find("Dust_Secret_Rune");
            if (oldRune != null) Destroy(oldRune.gameObject);

            // تلميح مسح نقي ومريح للعين بدون أي رموز غريبة
            Transform tip = transform.Find("Dust_Swipe_Tip");
            if (tip == null)
            {
                GameObject tipObj = new GameObject("Dust_Swipe_Tip", typeof(RectTransform));
                tipObj.transform.SetParent(transform, false);

                RectTransform tRect = tipObj.GetComponent<RectTransform>();
                tRect.anchorMin = new Vector2(0.1f, 0.12f);
                tRect.anchorMax = new Vector2(0.9f, 0.24f);
                tRect.offsetMin = Vector2.zero;
                tRect.offsetMax = Vector2.zero;

                TMPro.TextMeshProUGUI tmp = tipObj.AddComponent<TMPro.TextMeshProUGUI>();
                tmp.text = "SWIPE TO CLEAN";
                tmp.fontSize = 17;
                tmp.fontStyle = TMPro.FontStyles.Bold;
                tmp.characterSpacing = 3f;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                tmp.color = new Color(0.95f, 0.82f, 0.50f, 0.90f);
                tmp.raycastTarget = false;
            }
            else
            {
                TMPro.TextMeshProUGUI tmp = tip.GetComponent<TMPro.TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "SWIPE TO CLEAN";
                    tmp.fontSize = 17;
                    tmp.fontStyle = TMPro.FontStyles.Bold;
                    tmp.characterSpacing = 3f;
                    tmp.alignment = TMPro.TextAlignmentOptions.Center;
                    tmp.color = new Color(0.95f, 0.82f, 0.50f, 0.90f);
                }
            }
        }

        // التحقق إن كانت المهمة منجزة مسبقاً (Persistence)
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isAlreadyDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("dust"))
                             || PlayerPrefs.GetInt($"{sceneName}_Task_dust", 0) == 1
                             || PlayerPrefs.GetInt("HasItem_SecretBook", 0) == 1
                             || (InventoryManager.Instance != null && (InventoryManager.Instance.HasItem("SecretBook") || InventoryManager.Instance.HasItem("Book")));

        if (isAlreadyDone)
        {
            isCleaned = true;
            gameObject.SetActive(false); // إخفاء الغبار

            // إخفاء مجسم الكتاب الموجود على الطاولة في الغرفة نهائياً
            HideTableBookEverywhere();

            // إبقاء الكتاب متوفراً في الحقيبة بصورته المخصصة
            if (InventoryManager.Instance != null && !InventoryManager.Instance.HasItem("SecretBook") && !InventoryManager.Instance.HasItem("Book"))
            {
                Sprite iconToUse = bookIcon != null ? bookIcon : InventoryManager.GetBookSprite();
                InventoryManager.Instance.AddItem("SecretBook", iconToUse);
            }
        }
    }

    // يتم استدعاؤه تلقائياً أثناء تحريك الإصبع أو الماوس فوق الغبار
    public void OnDrag(PointerEventData eventData)
    {
        if (isCleaned || dustImage == null) return;

        // تقليل الشفافية تدريجياً مع المسح
        currentAlpha -= cleanSpeed;

        Color c = dustImage.color;
        c.a = Mathf.Clamp01(currentAlpha);
        dustImage.color = c;

        // تفتيح صفحة الكتاب وخلفيته تدريجياً مع التنظيف ليعطي إحساساً واضحاً بالنظافة والإشراق
        float cleanProgress = 1f - Mathf.Clamp01(currentAlpha);
        Transform pageT = transform.parent != null ? transform.parent.Find("Grimoire_InnerPage") : null;
        if (pageT != null)
        {
            Image pageImg = pageT.GetComponent<Image>();
            if (pageImg != null)
            {
                // من كحلي داكن باهت (0.07, 0.09, 0.15) إلى كحلي سماوي فاتح ومشرق ونظيف (0.16, 0.26, 0.44)
                pageImg.color = Color.Lerp(new Color(0.07f, 0.09f, 0.15f, 0.98f), new Color(0.16f, 0.26f, 0.44f, 0.98f), cleanProgress);
            }
        }

        // إذا تم مسح معظم الغبار
        if (currentAlpha <= 0.15f)
        {
            CompleteCleaning();
        }
    }

    private void CompleteCleaning()
    {
        isCleaned = true;
        gameObject.SetActive(false); // إخفاء طبقة الغبار بالكامل
        Debug.Log("<color=yellow>[نجاح اللغز]:</color> تم تنظيف الغبار بالكامل وكشف الرمز السري!");

        // تثبيت صفحة الكتاب على اللون الكحلي الفاتح النظيف المشرق
        Transform pageT = transform.parent != null ? transform.parent.Find("Grimoire_InnerPage") : null;
        if (pageT != null)
        {
            Image pageImg = pageT.GetComponent<Image>();
            if (pageImg != null)
            {
                pageImg.color = new Color(0.18f, 0.28f, 0.48f, 0.98f);
            }
        }

        // 1. إضافة الكتاب إلى شريط الحقيبة مع صوت جمع بلوري نقي
        ProceduralAudioGenerator.PlaySound("pickup", 0.85f);
        if (InventoryManager.Instance != null)
        {
            Sprite iconToUse = bookIcon != null ? bookIcon : InventoryManager.GetBookSprite();
            InventoryManager.Instance.AddItem("SecretBook", iconToUse);
            Debug.Log("<color=green>[الحقيبة]:</color> تم حفظ الكتاب السري داخل الحقيبة.");
        }

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        PlayerPrefs.SetInt($"{currentScene}_Task_dust", 1);
        PlayerPrefs.SetInt("HasItem_SecretBook", 1);
        PlayerPrefs.Save();

        // 2. إخفاء مجسم الكتاب الحقيقي من على الطاولة في الغرفة نهائياً
        HideTableBookEverywhere();

        // 3. إبلاغ دليل التقدم ونظام الغبار العام بإنجاز مهمة مسح الغبار
        if (RoomDustAtmosphere.Instance != null)
        {
            RoomDustAtmosphere.Instance.ReduceDustAfterCleaning();
        }
        LevelProgressGuide.ReportTask("dust");
    }

    private void HideTableBookEverywhere()
    {
        if (worldBookObject != null)
        {
            worldBookObject.SetActive(false);
        }

        // البحث عن أي كائن BookInteraction أو كائنات الكتاب المرئية على الطاولة وإخفائها
        BookInteraction[] books = Object.FindObjectsByType<BookInteraction>(FindObjectsSortMode.None);
        foreach (var b in books)
        {
            if (b != null)
            {
                b.gameObject.SetActive(false);
            }
        }

        // البحث بالأسماء الشائعة لمجسم الكتاب في المشهد لضمان اختفائه حتى لو كان تحت كائن آخر
        string[] candidateNames = new string[] { "Book", "Book (1)", "Book (2)", "OldBook", "MagicBook", "Tome", "Grimoire" };
        foreach (string cName in candidateNames)
        {
            GameObject go = GameObject.Find(cName);
            if (go != null && go.layer != 5) // نتجنب إخفاء عناصر الـ UI (Layer 5)
            {
                go.SetActive(false);
            }
        }
    }
}
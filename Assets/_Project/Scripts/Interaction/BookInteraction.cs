using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BookInteraction : MonoBehaviour
{
    public static BookInteraction Instance;

    [Header("UI & Clue")]
    [SerializeField] private GameObject bookInspectUI;    // لوحة Book_Background
    [SerializeField] private TextMeshProUGUI codeDisplayText; // اسحبي Secret_Code_Text هنا
    [SerializeField] private string secretCode = "739";       // الرمز السري المطلوب

    [Header("Inventory & World Object")]
    [SerializeField] private Sprite bookInventoryIcon;   // صورة أيقونة الكتاب للحقيبة
    [SerializeField] private GameObject worldBookObject; // مجسم الكتاب في الغرفة

    private bool isOpen = false;
    private bool isCollected = false;
    public static bool HasDiscoveredCode { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // ضبط نص الكود تلقائياً ليكون 739
        if (codeDisplayText != null)
        {
            codeDisplayText.text = secretCode;
        }

        // إذا كان الكتاب مجموعاً مسبقاً أو مهمة الغبار منجزة، يخفي مجسم الكتاب من الغرفة
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isAlreadyCollected = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("dust"))
                                  || PlayerPrefs.GetInt($"{sceneName}_Task_dust", 0) == 1
                                  || PlayerPrefs.GetInt("HasItem_SecretBook", 0) == 1
                                  || (InventoryManager.Instance != null && (InventoryManager.Instance.HasItem("SecretBook") || InventoryManager.Instance.HasItem("Book")));

        if (isAlreadyCollected)
        {
            isCollected = true;
            if (worldBookObject != null)
            {
                worldBookObject.SetActive(false);
            }
            gameObject.SetActive(false);
        }
    }

    private void OnMouseDown()
    {
        // منع الفتح إذا كان مفتوحاً مسبقاً أو تم التقاط الكتاب
        if (isOpen || isCollected) return;

        OpenBookView();
    }

    public void OpenBookView()
    {
        isOpen = true;

        if (codeDisplayText != null)
        {
            codeDisplayText.text = secretCode;
        }

        if (bookInspectUI != null)
        {
            bookInspectUI.SetActive(true);
            bookInspectUI.transform.SetAsLastSibling();

            // تطبيق ديزاين فخم ومتناسق لكتاب أثري سحري بلون كحلي ملكي (Midnight Royal Grimoire)
            RectTransform rt = bookInspectUI.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(430, 520);
            }

            // غلاف الكتاب الخارجي: كحلي داكن ملكي عميق جداً
            Image bg = bookInspectUI.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = new Color(0.06f, 0.09f, 0.16f, 0.98f);
            }

            // إطار ذهبي معتق فخم وواضح
            Outline outline = bookInspectUI.GetComponent<Outline>();
            if (outline == null) outline = bookInspectUI.AddComponent<Outline>();
            outline.effectColor = new Color(0.88f, 0.72f, 0.35f, 0.95f);
            outline.effectDistance = new Vector2(3f, -3f);

            // صفحة المخطوطة الداخلية: كحلي ليلي أنيق (Navy Midnight Inner Page)
            Transform pageT = bookInspectUI.transform.Find("Grimoire_InnerPage");
            GameObject pageObj = (pageT != null) ? pageT.gameObject : new GameObject("Grimoire_InnerPage", typeof(RectTransform));
            pageObj.transform.SetParent(bookInspectUI.transform, false);
            pageObj.transform.SetAsFirstSibling();

            RectTransform pRect = pageObj.GetComponent<RectTransform>();
            pRect.anchorMin = Vector2.zero;
            pRect.anchorMax = Vector2.one;
            pRect.offsetMin = new Vector2(16, 18);
            pRect.offsetMax = new Vector2(-16, -18);

            Image pageImg = pageObj.GetComponent<Image>() ?? pageObj.AddComponent<Image>();
            pageImg.color = new Color(0.09f, 0.13f, 0.22f, 0.96f); // كحلي أنيق وناعم
            pageImg.raycastTarget = false;

            Outline pageOutline = pageObj.GetComponent<Outline>() ?? pageObj.AddComponent<Outline>();
            pageOutline.effectColor = new Color(0.25f, 0.35f, 0.55f, 0.5f); // لمعة زرقاء ناعمة
            pageOutline.effectDistance = new Vector2(1.5f, -1.5f);

            Transform darkBacking = bookInspectUI.transform.Find("InspectBook_Panel");
            if (darkBacking != null) darkBacking.gameObject.SetActive(false);

            // عنوان المخطوطة: ANCIENT TOME بلون ذهبي نقي ومتباعد بأناقة
            Transform existingHeader = bookInspectUI.transform.Find("Book_HeaderTitle");
            GameObject headerObj = (existingHeader != null) ? existingHeader.gameObject : new GameObject("Book_HeaderTitle", typeof(RectTransform));
            headerObj.transform.SetParent(bookInspectUI.transform, false);

            RectTransform hRect = headerObj.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 1f);
            hRect.anchorMax = new Vector2(1f, 1f);
            hRect.pivot = new Vector2(0.5f, 1f);
            hRect.anchoredPosition = new Vector2(0, -28);
            hRect.sizeDelta = new Vector2(380, 42);

            TextMeshProUGUI hTmp = headerObj.GetComponent<TextMeshProUGUI>() ?? headerObj.AddComponent<TextMeshProUGUI>();
            hTmp.text = "ANCIENT TOME";
            hTmp.fontSize = 20;
            hTmp.fontStyle = FontStyles.Bold;
            hTmp.characterSpacing = 4f;
            hTmp.alignment = TextAlignmentOptions.Center;
            hTmp.color = new Color(0.96f, 0.82f, 0.45f, 1f); // ذهبي دافئ ومقروء

            // خط فاصل ذهبي متناسق أسفل العنوان
            Transform dividerT = bookInspectUI.transform.Find("Book_HeaderDivider");
            GameObject dividerObj = (dividerT != null) ? dividerT.gameObject : new GameObject("Book_HeaderDivider", typeof(RectTransform));
            dividerObj.transform.SetParent(bookInspectUI.transform, false);
            RectTransform divRect = dividerObj.GetComponent<RectTransform>();
            divRect.anchorMin = new Vector2(0.15f, 1f);
            divRect.anchorMax = new Vector2(0.85f, 1f);
            divRect.pivot = new Vector2(0.5f, 1f);
            divRect.anchoredPosition = new Vector2(0, -70);
            divRect.sizeDelta = new Vector2(0, 2f);
            Image divImg = dividerObj.GetComponent<Image>() ?? dividerObj.AddComponent<Image>();
            divImg.color = new Color(0.85f, 0.70f, 0.35f, 0.40f);
            divImg.raycastTarget = false;

            // زر إغلاق دائري عصري فخم (Close Button)
            Transform closeBtn = bookInspectUI.transform.Find("Close_Button");
            if (closeBtn != null)
            {
                RectTransform cbRect = closeBtn.GetComponent<RectTransform>();
                if (cbRect != null)
                {
                    cbRect.anchorMin = new Vector2(1f, 1f);
                    cbRect.anchorMax = new Vector2(1f, 1f);
                    cbRect.pivot = new Vector2(0.5f, 0.5f);
                    cbRect.anchoredPosition = new Vector2(-22, -22);
                    cbRect.sizeDelta = new Vector2(34, 34);
                }

                Image cbImg = closeBtn.GetComponent<Image>();
                if (cbImg != null)
                {
                    cbImg.color = new Color(0.12f, 0.16f, 0.28f, 0.95f); // كحلي متناسق مع خلفية الكتاب
                    cbImg.raycastTarget = true;
                }

                Outline cbOutline = closeBtn.GetComponent<Outline>() ?? closeBtn.gameObject.AddComponent<Outline>();
                cbOutline.effectColor = new Color(0.85f, 0.70f, 0.35f, 0.85f); // إطار ذهبي
                cbOutline.effectDistance = new Vector2(1.5f, -1.5f);

                TextMeshProUGUI cbText = closeBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (cbText != null)
                {
                    cbText.text = "X";
                    cbText.fontSize = 18;
                    cbText.fontStyle = FontStyles.Bold;
                    cbText.alignment = TextAlignmentOptions.Center;
                    cbText.color = new Color(0.96f, 0.82f, 0.45f, 1f);
                    cbText.raycastTarget = false;
                }

                closeBtn.SetAsLastSibling();
            }

            // تنسيق وإبراز نص الكود السري ليظهر بشكل سحري متألق عند مسح الغبار
            Transform codeText = bookInspectUI.transform.Find("Secret_Code_Text");
            if (codeText != null)
            {
                RectTransform ctRect = codeText.GetComponent<RectTransform>();
                if (ctRect != null)
                {
                    ctRect.anchoredPosition = new Vector2(0, -30);
                    ctRect.sizeDelta = new Vector2(340, 100);
                }

                TextMeshProUGUI cTmp = codeText.GetComponent<TextMeshProUGUI>();
                if (cTmp != null)
                {
                    cTmp.fontSize = 32;
                    cTmp.fontStyle = FontStyles.Bold;
                    cTmp.characterSpacing = 6;
                    cTmp.alignment = TextAlignmentOptions.Center;
                    cTmp.color = new Color(1f, 0.92f, 0.65f, 1f); // ذهبي فاتح مشع
                }
            }
        }

        Debug.Log("<color=yellow>[الكتاب]:</color> تم فتح واجهة فحص الكتاب.");
    }

    // يتم استدعاء هذه الدالة عند اكتمال تنظيف الغبار
    public void OnDustCleanedComplete()
    {
        HasDiscoveredCode = true;
        Debug.Log("<color=yellow>[الكتاب]:</color> تم كشف الرمز السري بنجاح: " + secretCode);

        // 1. إضافة الكتاب إلى الحقيبة
        if (!isCollected && InventoryManager.Instance != null && bookInventoryIcon != null)
        {
            InventoryManager.Instance.AddItem("SecretBook", bookInventoryIcon);
            isCollected = true;
        }

        // 2. إخفاء مجسم الكتاب الحقيقي من الغرفة
        if (worldBookObject != null)
        {
            worldBookObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // يتم ربط هذه الدالة بزر الإغلاق (Close_Button)
    public void CloseBookView()
    {
        isOpen = false;

        if (bookInspectUI != null)
        {
            bookInspectUI.SetActive(false);
        }

        Debug.Log("<color=yellow>[الكتاب]:</color> تم إغلاق الكتاب والعودة للغرفة.");
    }

    private void Update()
    {
        // إغلاق سريع بالزر الأيمن للفأرة
        if (isOpen && Input.GetMouseButtonDown(1))
        {
            CloseBookView();
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ChestDialLock : MonoBehaviour
{
    [Header("Puzzle Requirements")]
    [Tooltip("تفعيل شرط وجود الكتاب وكود الجدار بالحقيبة")]
    [SerializeField] private bool requireItemsCollected = true;

    [Tooltip("اسحبي مجسم شعلة الشمعة أو كائن ضوء الشمعة هنا (اختياري)")]
    [SerializeField] private GameObject candleFlameOrLight;

    [Header("Lock Configuration")]
    [Tooltip("أرقام الرمز السري بالترتيب")]
    [SerializeField] private List<int> correctCode = new List<int>() { 7, 3, 9, 4, 7, 2, 9 };

    [Header("UI Panels & Animation")]
    [SerializeField] private GameObject lockUIPanel;
    [SerializeField] private Animator chestAnimator;
    [SerializeField] private string openAnimationTrigger = "Open";

    [Header("Reward Key")]
    [Tooltip("اسحبي مجسم المفتاح الذهبي الموجود داخل الصندوق هنا")]
    [SerializeField] private GameObject keyInsideChest;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip unlockSound;

    [Header("Success Events")]
    public UnityEvent onChestUnlocked;

    private List<int> currentEnteredCode = new List<int>();
    private bool isUnlocked = false;
    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;

        // التأكد من وجود كولايدر على مجسم الصندوق ليلتقط النقرة
        if (GetComponent<Collider>() == null && GetComponentInChildren<Collider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(1.5f, 1.5f, 1.5f);
        }
    }

    private void Start()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string savedCodeStr = PlayerPrefs.GetString($"{sceneName}_Chest_EnteredCode", "");

        currentEnteredCode = new List<int>();
        for (int i = 0; i < correctCode.Count; i++)
        {
            int val = 0;
            if (!string.IsNullOrEmpty(savedCodeStr) && i < savedCodeStr.Length)
            {
                int.TryParse(savedCodeStr[i].ToString(), out val);
            }
            currentEnteredCode.Add(val);
        }
        bool chestDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("chest"))
                         || PlayerPrefs.GetInt($"{sceneName}_Task_chest", 0) == 1;

        bool keyDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("key"))
                       || PlayerPrefs.GetInt($"{sceneName}_Task_key", 0) == 1;

        // العثور على المفتاح إذا لم يكن مربوطاً في المفتش (Inspector)
        if (keyInsideChest == null)
        {
            KeyInteraction keyComp = FindAnyObjectByType<KeyInteraction>(FindObjectsInactive.Include);
            if (keyComp != null)
            {
                keyInsideChest = keyComp.gameObject;
            }
        }

        if (chestDone)
        {
            isUnlocked = true;

            if (chestAnimator != null)
            {
                chestAnimator.SetTrigger(openAnimationTrigger);
            }
            else
            {
                // إذا لم يكن هناك أنيميشن للصندوق، نخفي مجسم الصندوق المغلق تماماً ليظهر المفتاح مكانه بدون حجب
                gameObject.SetActive(false);
            }

            if (keyInsideChest != null)
            {
                // إذا لم يتم أخذ المفتاح بعد، يظهر بوضوح في المكان ليأخذه اللاعب
                keyInsideChest.SetActive(!keyDone);
            }
        }
        else
        {
            if (keyInsideChest != null)
            {
                keyInsideChest.SetActive(false);
            }
        }
    }

    private void Update()
    {
        // إذا كانت لوحة الأرقام مفتوحة حالياً، لا نسمح للنقرات في المشهد بإعادة فتحها
        if (lockUIPanel != null && lockUIPanel.activeSelf) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return; // النقر على عناصر الـ UI لا يفتح الصندوق
            }

            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return;

            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    HandleChestClick();
                }
            }
        }
    }

    private void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (lockUIPanel != null && lockUIPanel.activeSelf) return;

        HandleChestClick();
    }

    private void HandleChestClick()
    {
        // إذا كان الصندوق مفتوحاً بالفعل وتم حل اللغز، يمكن أخذ المفتاح
        if (isUnlocked)
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool keyDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("key"))
                           || PlayerPrefs.GetInt($"{sceneName}_Task_key", 0) == 1;

            if (!keyDone)
            {
                KeyInteraction keyComp = FindAnyObjectByType<KeyInteraction>(FindObjectsInactive.Include);
                if (keyComp != null)
                {
                    keyComp.gameObject.SetActive(true);
                    keyComp.PickupKey();
                    return;
                }
            }
            return;
        }

        // فتح لوحة إدخال الأرقام السرية للصندوق مباشرة ليستمتع اللاعب بحل اللغز
        OpenLockPanel();
    }

    public void OpenLockPanel()
    {
        if (isUnlocked) return;

        if (lockUIPanel != null)
        {
            lockUIPanel.SetActive(true);
            StyleAndEnhanceLockUI();

            RectTransform rt = lockUIPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(-120f, -20f);
            }
        }
    }

    /// <summary>
    /// ترقية ديزاين لوحة الصندوق بتصميم مودرن فخم وزر إغلاق دائري أنيق
    /// </summary>
    private void StyleAndEnhanceLockUI()
    {
        if (lockUIPanel == null) return;

        // 1. تجميل خلفية اللوحة الرئيسية وتكبير أبعادها لتكون مريحة ومتناسقة
        RectTransform panelRect = lockUIPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.sizeDelta = new Vector2(660, 270);
        }

        UnityEngine.UI.Image bgImage = lockUIPanel.GetComponent<UnityEngine.UI.Image>();
        if (bgImage != null)
        {
            // كحلي كربوني فخم شبه شفاف متناسق مع اللعبة
            bgImage.color = new Color(0.07f, 0.09f, 0.13f, 0.97f);
        }

        UnityEngine.UI.Outline outline = lockUIPanel.GetComponent<UnityEngine.UI.Outline>();
        if (outline == null) outline = lockUIPanel.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(0.90f, 0.72f, 0.28f, 0.65f); // إطار ذهبي معتق
        outline.effectDistance = new Vector2(2f, -2f);

        // 2. تصميم زر إغلاق دائري عصري فخم (Floating Circular Close Button)
        Transform existingCloseBtn = lockUIPanel.transform.Find("Btn_CloseLockPanel");
        if (existingCloseBtn != null)
        {
            Destroy(existingCloseBtn.gameObject);
        }

        GameObject closeObj = new GameObject("Btn_CloseLockPanel", typeof(RectTransform));
        closeObj.transform.SetParent(lockUIPanel.transform, false);

        RectTransform cRect = closeObj.GetComponent<RectTransform>();
        cRect.anchorMin = new Vector2(1f, 1f);
        cRect.anchorMax = new Vector2(1f, 1f);
        cRect.pivot = new Vector2(0.5f, 0.5f);
        cRect.anchoredPosition = new Vector2(-25, -25);
        cRect.sizeDelta = new Vector2(40, 40);

        UnityEngine.UI.Image cImg = closeObj.AddComponent<UnityEngine.UI.Image>();
        cImg.color = new Color(0.18f, 0.22f, 0.30f, 0.95f); // خلفية زر مريحة للعين
        cImg.raycastTarget = true; // ضروري جداً لكي يستقبل النقرات

        UnityEngine.UI.Outline cOutline = closeObj.AddComponent<UnityEngine.UI.Outline>();
        cOutline.effectColor = new Color(0.85f, 0.35f, 0.35f, 0.85f); // إطار أحمر لطيف
        cOutline.effectDistance = new Vector2(1f, -1f);

        UnityEngine.UI.Button cBtn = closeObj.AddComponent<UnityEngine.UI.Button>();
        cBtn.targetGraphic = cImg;
        cBtn.onClick.RemoveAllListeners();
        cBtn.onClick.AddListener(CloseLockPanel);

        // تخصيص ألوان التفاعل لزر الإغلاق
        UnityEngine.UI.ColorBlock cb = cBtn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 0.8f, 0.8f, 1f);
        cb.pressedColor = new Color(0.9f, 0.2f, 0.2f, 1f);
        cBtn.colors = cb;

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(closeObj.transform, false);

        RectTransform tRect = textObj.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = Vector2.zero;
        tRect.offsetMax = Vector2.zero;

        TMPro.TextMeshProUGUI tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = "X";
        tmp.fontSize = 20;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = new Color(0.95f, 0.45f, 0.45f, 1f);
        tmp.raycastTarget = false; // عدم حجب النقرة عن الزر

        // رفع زر الإغلاق لأعلى طبقة حتى لا يحجبه أي عنصر مجاور
        closeObj.transform.SetAsLastSibling();

        // 3. عنوان اللوحة (بدون أي إيموجي حتى لا يظهر مربع فارغ مثل □)
        Transform existingTitle = lockUIPanel.transform.Find("Lock_TitleText");
        if (existingTitle != null)
        {
            Destroy(existingTitle.gameObject);
        }

        GameObject titleObj = new GameObject("Lock_TitleText", typeof(RectTransform));
        titleObj.transform.SetParent(lockUIPanel.transform, false);

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -18);
        titleRect.sizeDelta = new Vector2(400, 30);

        TMPro.TextMeshProUGUI titleTMP = titleObj.AddComponent<TMPro.TextMeshProUGUI>();
        titleTMP.text = "CHEST DIAL LOCK";
        titleTMP.fontSize = 17;
        titleTMP.fontStyle = TMPro.FontStyles.Bold;
        titleTMP.characterSpacing = 3;
        titleTMP.alignment = TMPro.TextAlignmentOptions.Center;
        titleTMP.color = new Color(0.95f, 0.80f, 0.40f, 1f); // ذهبي كلاسيكي

        // 4. ضبط موضع وتصميم شريط الأرقام
        Transform dialsBar = lockUIPanel.transform.Find("Dials_Bar");
        if (dialsBar != null)
        {
            RectTransform dbRect = dialsBar.GetComponent<RectTransform>();
            if (dbRect != null)
            {
                dbRect.anchoredPosition = new Vector2(0, -15);
            }

            UnityEngine.UI.Image barImg = dialsBar.GetComponent<UnityEngine.UI.Image>();
            if (barImg != null)
            {
                barImg.color = new Color(0.04f, 0.05f, 0.08f, 0.90f);
            }
        }
    }

    public void SetDialValue(int dialIndex, int newValue)
    {
        UpdateDialValue(dialIndex, newValue);
    }

    public void UpdateDialValue(int dialIndex, int newValue)
    {
        if (isUnlocked) return;

        if (dialIndex >= 0 && dialIndex < currentEnteredCode.Count)
        {
            currentEnteredCode[dialIndex] = newValue;

            // حفظ الأرقام التي أدارها اللاعب فوراً
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string codeStr = "";
            for (int i = 0; i < currentEnteredCode.Count; i++) codeStr += currentEnteredCode[i].ToString();
            PlayerPrefs.SetString($"{sceneName}_Chest_EnteredCode", codeStr);
            PlayerPrefs.Save();

            CheckCode();
        }
    }

    public void CheckCode()
    {
        if (isUnlocked) return;

        // التحقق من جمع كلا الكودين حتى لو تمت كتابة الأرقام الصحيحة
        if (requireItemsCollected)
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool bookTaskDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("dust"))
                                || PlayerPrefs.GetInt($"{sceneName}_Task_dust", 0) == 1;

            bool candleTaskDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("candle"))
                                  || PlayerPrefs.GetInt($"{sceneName}_Task_candle", 0) == 1;

            bool hasBook = bookTaskDone || (InventoryManager.Instance != null && (
                           InventoryManager.Instance.HasItem("SecretBook") ||
                           InventoryManager.Instance.HasItem("Book") ||
                           InventoryManager.Instance.HasItem("book") ||
                           InventoryManager.Instance.HasItem("CleanBook")));

            bool hasCode = candleTaskDone || (InventoryManager.Instance != null && (
                           InventoryManager.Instance.HasItem("SecretCode") ||
                           InventoryManager.Instance.HasItem("code") ||
                           InventoryManager.Instance.HasItem("CandleCode") ||
                           InventoryManager.Instance.HasItem("CandlePuzzle")));

            if (!hasBook || !hasCode) return;
        }

        if (currentEnteredCode.Count != correctCode.Count) return;

        for (int i = 0; i < correctCode.Count; i++)
        {
            if (currentEnteredCode[i] != correctCode[i])
            {
                return;
            }
        }

        UnlockChest();
    }

    private void UnlockChest()
    {
        isUnlocked = true;
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        PlayerPrefs.SetInt($"{sceneName}_Task_chest", 1);
        PlayerPrefs.Save();

        Debug.Log("<color=green>[ChestDialLock]:</color> تم فك القفل وفتح الصندوق بنجاح!");

        if (lockUIPanel != null)
        {
            lockUIPanel.SetActive(false);
        }

        if (audioSource != null && unlockSound != null)
        {
            audioSource.PlayOneShot(unlockSound);
        }
        else
        {
            ProceduralAudioGenerator.PlaySound("chest_unlock", 0.9f);
        }

        // تشغيل أنيميشن الفتح
        if (chestAnimator != null)
        {
            chestAnimator.SetTrigger(openAnimationTrigger);
        }

        // إظهار المفتاح داخل الصندوق فوراً للاعب
        if (keyInsideChest == null)
        {
            KeyInteraction keyComp = FindAnyObjectByType<KeyInteraction>(FindObjectsInactive.Include);
            if (keyComp != null)
            {
                keyInsideChest = keyComp.gameObject;
            }
        }

        if (keyInsideChest != null)
        {
            keyInsideChest.SetActive(true);
        }

        if (chestAnimator == null)
        {
            // إخفاء مجسم الصندوق إذا لم يكن له أنيميشن لكي يظهر المفتاح بوضوح
            gameObject.SetActive(false);
        }

        // إبلاغ دليل التقدم بإنجاز مهمة فتح الصندوق
        LevelProgressGuide.ReportTask("chest");

        onChestUnlocked?.Invoke();
    }

    public void CloseLockPanel()
    {
        if (lockUIPanel != null)
        {
            lockUIPanel.SetActive(false);
        }
    }
}